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

            // Migration from DllImport to LibraryImport
            public const string ConvertToLibraryImport = Prefix + "001";

            // ManualTypeMarshalling
            public const string MarshallerTypeMustSpecifyManagedType = Prefix + "002";
            public const string CustomTypeMarshallerAttributeMustBeValid = Prefix + "003";
            public const string InvalidNativeType = Prefix + "004";
            public const string GetPinnableReferenceReturnTypeBlittable = Prefix + "005";
            public const string CustomMarshallerTypeMustHaveRequiredShape = Prefix + "006";
            public const string CustomMarshallerTypeMustSupportDirection = Prefix + "007";
            public const string ProvidedMethodsNotSpecifiedInShape = Prefix + "008";
            public const string MissingAllocatingMarshallingFallback = Prefix + "009";
            public const string CallerAllocConstructorMustHaveBufferSize = Prefix + "010";
            public const string InvalidSignaturesInMarshallerShape = Prefix + "011";
            public const string MarshallerGetPinnableReferenceRequiresTwoStageMarshalling = Prefix + "012";
        }

        internal static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
        }
    }
}
