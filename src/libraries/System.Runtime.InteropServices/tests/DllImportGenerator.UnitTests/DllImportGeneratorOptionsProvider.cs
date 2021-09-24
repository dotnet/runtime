using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Interop;

namespace DllImportGenerator.UnitTests
{
    /// <summary>
    /// An implementation of <see cref="AnalyzerConfigOptionsProvider"/> that provides configuration in code
    /// of the options supported by the DllImportGenerator source generator. Used for testing various configurations.
    /// </summary>
    internal class DllImportGeneratorOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public DllImportGeneratorOptionsProvider(bool useMarshalType, bool generateForwarders)
        {
            GlobalOptions = new GlobalGeneratorOptions(useMarshalType, generateForwarders);
        }

        public override AnalyzerConfigOptions GlobalOptions  { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return EmptyOptions.Instance;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return EmptyOptions.Instance;
        }

        private class GlobalGeneratorOptions : AnalyzerConfigOptions
        {
            private readonly bool _useMarshalType = false;
            private readonly bool _generateForwarders = false;
            public GlobalGeneratorOptions(bool useMarshalType, bool generateForwarders)
            {
                _useMarshalType = useMarshalType;
                _generateForwarders = generateForwarders;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                switch (key)
                {
                    case OptionsHelper.UseMarshalTypeOption:
                        value = _useMarshalType.ToString();
                        return true;
                    
                    case OptionsHelper.GenerateForwardersOption:
                        value = _generateForwarders.ToString();
                        return true;
                    
                    default:
                        value = null;
                        return false;
                }
            }
        }

        private class EmptyOptions : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }

            public static AnalyzerConfigOptions Instance = new EmptyOptions();
        }
    }
}