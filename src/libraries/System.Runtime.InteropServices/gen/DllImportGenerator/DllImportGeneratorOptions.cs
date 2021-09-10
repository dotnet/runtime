using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop
{
    record DllImportGeneratorOptions(bool GenerateForwarders, bool UseMarshalType, bool UseInternalUnsafeType)
    {
        public DllImportGeneratorOptions(AnalyzerConfigOptions options)
            : this(options.GenerateForwarders(), options.UseMarshalType(), options.UseInternalUnsafeType())
        {
        }
    }

    public static class OptionsHelper
    {
        public const string UseMarshalTypeOption = "build_property.DllImportGenerator_UseMarshalType";
        public const string GenerateForwardersOption = "build_property.DllImportGenerator_GenerateForwarders";
        public const string UseInternalUnsafeTypeOption = "build_property.DllImportGenerator_UseInternalUnsafeType";

        private static bool GetBoolOption(this AnalyzerConfigOptions options, string key)
        {
            return options.TryGetValue(key, out string? value)
                && bool.TryParse(value, out bool result)
                && result;
        }

        internal static bool UseMarshalType(this AnalyzerConfigOptions options) => options.GetBoolOption(UseMarshalTypeOption);

        internal static bool GenerateForwarders(this AnalyzerConfigOptions options) => options.GetBoolOption(GenerateForwardersOption);

        internal static bool UseInternalUnsafeType(this AnalyzerConfigOptions options) => options.GetBoolOption(UseInternalUnsafeTypeOption);
    }
}
