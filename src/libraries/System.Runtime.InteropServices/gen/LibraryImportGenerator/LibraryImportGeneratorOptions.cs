// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop
{
    internal sealed record LibraryImportGeneratorOptions(bool GenerateForwarders, bool UseMarshalType)
    {
        public LibraryImportGeneratorOptions(AnalyzerConfigOptions options)
            : this(options.GenerateForwarders(), options.UseMarshalType())
        {
        }
    }

    public static class OptionsHelper
    {
        public const string UseMarshalTypeOption = "build_property.LibraryImportGenerator_UseMarshalType";
        public const string GenerateForwardersOption = "build_property.LibraryImportGenerator_GenerateForwarders";
        private static bool GetBoolOption(this AnalyzerConfigOptions options, string key)
        {
            return options.TryGetValue(key, out string? value)
                && bool.TryParse(value, out bool result)
                && result;
        }

        internal static bool UseMarshalType(this AnalyzerConfigOptions options) => options.GetBoolOption(UseMarshalTypeOption);

        internal static bool GenerateForwarders(this AnalyzerConfigOptions options) => options.GetBoolOption(GenerateForwardersOption);
    }
}
