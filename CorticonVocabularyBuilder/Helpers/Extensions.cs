﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorticonVocabularyBuilder
{
    public static class Extensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            if (type.BaseType == null) return type.GetInterfaces();

            return Enumerable.Repeat(type.BaseType, 1)
                             .Concat(type.GetInterfaces())
                             .Concat(type.GetInterfaces().SelectMany<Type, Type>(GetBaseTypes))
                             .Concat(type.BaseType.GetBaseTypes());
        }
    }
}
