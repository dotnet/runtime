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
            public const string Prefix = "SYSLIB";

            // Migration from DllImport to LibraryImport
            public const string ConvertToLibraryImport = Prefix + "1054";

            // CustomTypeMarshaller
            public const string InvalidCustomTypeMarshallerAttributeUsage = Prefix + "1055";
            public const string InvalidNativeType = Prefix + "1056";
            public const string CustomMarshallerTypeMustHaveRequiredShape = Prefix + "1057";
            public const string ProvidedMethodsNotSpecifiedInFeatures = Prefix + "1058";
            public const string MissingAllocatingMarshallingFallback = Prefix + "1059";
            public const string CallerAllocConstructorMustHaveBufferSize = Prefix + "1060";
            public const string InvalidSignaturesInMarshallerShape = Prefix + "1061";
        }

        internal static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
        }
    }
}
