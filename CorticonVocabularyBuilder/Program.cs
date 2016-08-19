using com.corticon.eclipse.studio.rule.rulesheet.table.core;
using com.corticon.eclipse.studio.vocabulary.core;
using CommandLine;
using CorticonRules;
using CorticonVocabularyBuilder.Options;
using org.eclipse.emf.common;
using org.eclipse.emf.common.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CorticonVocabularyBuilder
{
    class Program
    {
        static string dependencyPath;
        static IVocabularyModelAPI vocabularyModelAPI;
        static IEnumerable<Type> types;
        static Assembly assembly;

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
        }

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<GenerateOptions>(args);
            var texts = result.MapResult(
                  (GenerateOptions opts) => Generate(opts),
                  errors => 1);
        }

        private static int Generate(GenerateOptions options)
        {
            dependencyPath = string.IsNullOrWhiteSpace(options.DependencyPath) ? Path.GetDirectoryName(options.InputFile) + Path.DirectorySeparatorChar : options.DependencyPath;
            assembly = Assembly.LoadFrom(options.InputFile);

            types = GetVocabularyEntities(options.Namespaces);

            var corticonHome = ConfigurationManager.AppSettings["CORTICON_HOME"];
            var corticonWorkDir = ConfigurationManager.AppSettings["CORTICON_WORK_DIR"];
            var corticonConfiguration = new CorticonConfiguration();
            corticonConfiguration.readConfiguration(corticonHome, corticonWorkDir);

            using (var progress = new ProgressBar())
            {
                int loopCount = types.Count() + types.Where(t => !t.IsEnum).Count();
                int loopsCompleted = 0;

                vocabularyModelAPI = VocabularyModelAPIFactory.getInstance();

                var fullOutputPath = options.OutputPath + Path.DirectorySeparatorChar + assembly.GetName().Name + ".ecore";

                var vocabulary = vocabularyModelAPI.createVocabulary(URI.createFileURI(fullOutputPath));

                foreach (var type in types.Where(t => t.IsEnum))
                {
                    CreateCustomDataType(type);

                    progress.Report(loopsCompleted++ / (double)loopCount);
                }

                foreach (var type in types.Where(t => !t.IsEnum))
                {
                    CreateEntity(type);

                    progress.Report(loopsCompleted++ / (double)loopCount);
                }

                foreach (var type in types.Where(t => !t.IsEnum))
                {
                    CreateAssociations(type);

                    progress.Report(loopsCompleted++ / (double)loopCount);
                }

                vocabularyModelAPI.saveResource(vocabularyModelAPI.getPrimaryResource());
                vocabularyModelAPI.dispose();
            }

            return 0;
        }

        #region Create Custom Data Types

        static void CreateCustomDataType(Type type)
        {
            var dataType = vocabularyModelAPI.addCustomDataType();

            vocabularyModelAPI.setCustomDataTypeName(dataType, type.Name);
            vocabularyModelAPI.setCustomDataTypeBaseDataType(dataType, "Integer");
            vocabularyModelAPI.setCustomDataTypeEnumeration(dataType, "true");

            foreach (var name in Enum.GetNames(type))
            {
                var enumElement = vocabularyModelAPI.addEnumerationElement(dataType);

                vocabularyModelAPI.setEnumerationLabel(enumElement, name);
                vocabularyModelAPI.setEnumerationValue(enumElement, ((int)Enum.Parse(type, name)).ToString());
            }
        }

        #endregion

        #region Create Entities

        static void CreateEntity(Type type)
        {
            var entity = vocabularyModelAPI.addEntity();

            vocabularyModelAPI.setEntityName(entity, type.Name);
            vocabularyModelAPI.setEntityDatastorePersistent(entity, "false");
            vocabularyModelAPI.setEntityDatastoreCaching(entity, "false");

            var properties = GetIncludedProperties(type);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.PropertyType.IsPrimitive
                    || propertyInfo.PropertyType == typeof(Guid)
                    || propertyInfo.PropertyType == typeof(string)
                    || propertyInfo.PropertyType == typeof(DateTime)
                    || propertyInfo.PropertyType == typeof(Decimal)
                    || (propertyInfo.PropertyType.IsEnum && types.Contains(propertyInfo.PropertyType)))
                {
                    var attribute = vocabularyModelAPI.addAttribute(entity);

                    var typeName = GetDataTypeName(propertyInfo);

                    vocabularyModelAPI.setAttributeDataType(attribute, typeName);
                    vocabularyModelAPI.setAttributeName(attribute, propertyInfo.Name);

                    if (Attribute.IsDefined(propertyInfo, typeof(NotMappedAttribute)))
                    {
                        vocabularyModelAPI.setAttributeMode(attribute, "ExtendedTransient");
                    }
                }
            }

            if (type.BaseType != typeof(Object) && type.BaseType != null)
            {
                vocabularyModelAPI.setEntitySupertypes(entity, type.BaseType.Name);
            }
        }

        #endregion

        #region Create Associations

        static void CreateAssociations(Type type)
        {
            var entity = vocabularyModelAPI.findEntity(type.Name);

            var properties = GetIncludedProperties(type);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (types.Contains(propertyInfo.PropertyType) && !propertyInfo.PropertyType.IsEnum)
                {
                    var association = vocabularyModelAPI.findAssociation(type.Name, propertyInfo.Name);

                    if (association == null)
                    {
                        var associationParams = vocabularyModelAPI.getAssociationParametersNew(entity);

                        var myTargetEntity = vocabularyModelAPI.findEntity(GetElementType(propertyInfo.PropertyType).Name);

                        associationParams.myRoleName = propertyInfo.Name;
                        associationParams.myTargetEntity = myTargetEntity;

                        if (propertyInfo.PropertyType.GetInterface(typeof(IEnumerable).Name) != null
                        || propertyInfo.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null)
                        {
                            associationParams.myMany = true;
                        }
                        else
                        {
                            associationParams.myMany = false;
                        }

                        var remotePropertyInfo = GetOppositeProperty(type, propertyInfo);

                        if (remotePropertyInfo != null)
                        {
                            associationParams.myNavigable = true;

                            var oppositeTargetEntity = vocabularyModelAPI.findEntity(GetElementType(remotePropertyInfo.PropertyType).Name);

                            associationParams.oppositeRoleName = remotePropertyInfo.Name;
                            associationParams.oppositeTargetEntity = oppositeTargetEntity;

                            if (remotePropertyInfo.PropertyType.GetInterface(typeof(IEnumerable).Name) != null
                            || remotePropertyInfo.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null)
                            {
                                associationParams.oppositeMany = true;
                            }
                            else
                            {
                                associationParams.oppositeMany = false;
                            }

                            associationParams.oppositeNavigable = true;

                            if (Attribute.IsDefined(remotePropertyInfo, typeof(RequiredAttribute)))
                            {
                                associationParams.oppositeMandatory = true;
                            }
                        }
                        else
                        {
                            associationParams.oppositeNavigable = false;
                        }

                        if (Attribute.IsDefined(propertyInfo, typeof(RequiredAttribute)))
                        {
                            associationParams.myMandatory = true;
                        }

                        if (Attribute.IsDefined(propertyInfo, typeof(KeyAttribute)))
                        {
                            vocabularyModelAPI.setEntityIdentity(entity, propertyInfo.Name);
                        }

                        vocabularyModelAPI.addAssociation(associationParams);
                    }
                }
            }
        }

        #endregion

        #region Property Methods

        static IEnumerable<PropertyInfo> GetIncludedProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        }

        static PropertyInfo GetOppositeProperty(Type type, PropertyInfo property)
        {
            string typeName = property.PropertyType.FullName;

            var properties = GetIncludedProperties(assembly.GetType(typeName));

            return properties.Where(prop => GetElementType(prop.PropertyType).FullName == type.FullName).FirstOrDefault();
        }

        static string GetDataTypeName(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsEnum)
            {
                return propertyInfo.PropertyType.Name;
            }

            switch (propertyInfo.PropertyType.Name)
            {
                case "Int16":
                case "Int32":
                case "Int64":
                    {
                        return "Integer";
                    }
                case "DateTime":
                    {
                        return "Date";
                    }
                case "Float":
                case "Double":
                    {
                        return "Decimal";
                    }
                case "Char":
                case "Guid":
                    {
                        return "String";
                    }
                default:
                    {
                        return propertyInfo.PropertyType.Name;
                    }
            }
        }

        internal static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }

        #endregion

        #region Type Methods

        static IEnumerable<Type> GetVocabularyEntities(IEnumerable<string> namespaces)
        {
            IEnumerable<Type> types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            foreach (var t in types.Where(t => t != null))
            {
                if (!t.IsDefined(typeof(CompilerGeneratedAttribute), false) && t.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0 && (t.IsPublic || t.IsNestedPublic) && !t.IsGenericType && (t.IsClass || t.IsEnum))
                {
                    if (
                        (!t.GetBaseTypes().Contains(typeof(IEnumerable)) && !t.GetBaseTypes().Contains(typeof(IEnumerable<>)))
                        || ((t.GetBaseTypes().Contains(typeof(IEnumerable)) || t.GetBaseTypes().Contains(typeof(IEnumerable<>))) && t.IsEnum)
                        )
                    {
                        if (namespaces.Count() == 0 || (namespaces.Count() > 0 && namespaces.Contains(t.Namespace)))
                        {
                            yield return t;
                        }
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];
            var assembly = Assembly.LoadFile(dependencyPath + name + ".dll");
            return assembly;
        }

        #endregion
    }
}
