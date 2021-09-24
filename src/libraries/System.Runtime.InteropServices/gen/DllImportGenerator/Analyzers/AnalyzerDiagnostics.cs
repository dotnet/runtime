using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.Analyzers
{
    internal static class AnalyzerDiagnostics
    {
        /// <summary>
        /// Analyzer rule IDs
        /// </summary>
        public static class Ids
        {
            public const string Prefix = "DLLIMPORTGENANALYZER";

            // ManualTypeMarshalling
            public const string BlittableTypeMustBeBlittable = Prefix + "001";
            public const string CannotHaveMultipleMarshallingAttributes = Prefix + "002";
            public const string NativeTypeMustBeNonNull = Prefix + "003";
            public const string NativeTypeMustBeBlittable = Prefix + "004";
            public const string GetPinnableReferenceReturnTypeBlittable = Prefix + "005";
            public const string NativeTypeMustBePointerSized = Prefix + "006";
            public const string NativeTypeMustHaveRequiredShape = Prefix + "007";
            public const string ValuePropertyMustHaveSetter = Prefix + "008";
            public const string ValuePropertyMustHaveGetter = Prefix + "009";
            public const string GetPinnableReferenceShouldSupportAllocatingMarshallingFallback = Prefix + "010";
            public const string StackallocMarshallingShouldSupportAllocatingMarshallingFallback = Prefix + "011";
            public const string StackallocConstructorMustHaveStackBufferSizeConstant = Prefix + "012";
            public const string RefValuePropertyUnsupported = Prefix + "014";
            public const string NativeGenericTypeMustBeClosedOrMatchArity = Prefix + "016";

            // GeneratedDllImport
            public const string GeneratedDllImportMissingRequiredModifiers = Prefix + "013";
            public const string GeneratedDllImportContaiingTypeMissingRequiredModifiers = Prefix + "017";

            // Migration from DllImport to GeneratedDllImport
            public const string ConvertToGeneratedDllImport = Prefix + "015";
        }

        internal static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
        }
    }
}
