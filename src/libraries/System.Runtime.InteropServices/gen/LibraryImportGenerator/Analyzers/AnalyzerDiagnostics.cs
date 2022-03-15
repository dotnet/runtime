// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            public const string MarshallerTypeMustSpecifyManagedType = Prefix + "001";
            public const string CustomTypeMarshallerAttributeMustBeValid = Prefix + "002";
            public const string NativeTypeMustHaveCustomTypeMarshallerAttribute = Prefix + "003";
            public const string NativeTypeMustBeBlittable = Prefix + "004";
            public const string GetPinnableReferenceReturnTypeBlittable = Prefix + "005";
            public const string NativeTypeMustBePointerSized = Prefix + "006";
            public const string CustomMarshallerTypeMustHaveRequiredShape = Prefix + "007";
            public const string CustomMarshallerTypeMustSupportDirection = Prefix + "008";
            public const string ProvidedMethodsNotSpecifiedInShape = Prefix + "009";
            public const string GetPinnableReferenceShouldSupportAllocatingMarshallingFallback = Prefix + "010";
            public const string CallerAllocMarshallingShouldSupportAllocatingMarshallingFallback = Prefix + "011";
            public const string CallerAllocConstructorMustHaveStackBufferSize = Prefix + "012";
            public const string TwoStageMarshallingNativeTypesMustMatch = Prefix + "013";
            public const string LinearCollectionElementTypesMustMatch = Prefix + "014";
            public const string RefValuePropertyUnsupported = Prefix + "015";
            public const string NativeGenericTypeMustBeClosedOrMatchArity = Prefix + "016";
            public const string MarshallerGetPinnableReferenceRequiresTwoStageMarshalling = Prefix + "017";

            // Migration from DllImport to LibraryImport
            public const string ConvertToLibraryImport = Prefix + "018";
        }

        internal static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
        }
    }
}
