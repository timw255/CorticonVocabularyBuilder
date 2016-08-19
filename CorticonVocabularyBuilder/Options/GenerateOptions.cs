using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorticonVocabularyBuilder.Options
{
    [Verb("generate", HelpText = "Generate vocabulary.")]
    internal class GenerateOptions
    {
        [Option('f', "file", Required = true, HelpText = "File to be processed.")]
        public string InputFile { get; set; }

        [Option('o', "output", HelpText = "Where to save the vocabulary file.")]
        public string OutputPath { get; set; }

        [Option('d', "dependency", HelpText = "Where to search for missing dependencies.")]
        public string DependencyPath { get; set; }

        [Option('n', "namespaces", HelpText = "Custom list of namespaces to use.")]
        public IEnumerable<string> Namespaces { get; set; }
    }
}
