// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.JavaScript
{
    internal sealed record JSGeneratorOptions(bool EnableJSImport, bool EnableJSExport)
    {
        public JSGeneratorOptions(AnalyzerConfigOptions options)
            : this(options.EnableJSImport(), options.EnableJSExport())
        {
        }
    }

    public static class OptionsHelper
    {
        public const string EnableJSImportOption = "build_property.JSImportGenerator_EnableJSImport";
        public const string EnableJSExportOption = "build_property.JSImportGenerator_EnableJSExport";
        private static bool GetBoolOption(this AnalyzerConfigOptions options, string key)
        {
            return options.TryGetValue(key, out string? value)
                && bool.TryParse(value, out bool result)
                && result;
        }

        internal static bool EnableJSImport(this AnalyzerConfigOptions options) => options.GetBoolOption(EnableJSImportOption);
        internal static bool EnableJSExport(this AnalyzerConfigOptions options) => options.GetBoolOption(EnableJSExportOption);
    }
}
