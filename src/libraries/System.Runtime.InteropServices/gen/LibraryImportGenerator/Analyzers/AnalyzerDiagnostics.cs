// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

            // CustomTypeMarshaller
            public const string InvalidCustomTypeMarshallerAttributeUsage = Prefix + "002";
            public const string InvalidNativeType = Prefix + "003";
            public const string CustomMarshallerTypeMustHaveRequiredShape = Prefix + "004";
            public const string ProvidedMethodsNotSpecifiedInFeatures = Prefix + "005";
            public const string MissingAllocatingMarshallingFallback = Prefix + "006";
            public const string CallerAllocConstructorMustHaveBufferSize = Prefix + "007";
            public const string InvalidSignaturesInMarshallerShape = Prefix + "008";
        }

        internal static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
        }
    }
}
