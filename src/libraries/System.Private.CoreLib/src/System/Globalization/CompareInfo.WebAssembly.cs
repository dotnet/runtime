// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private unsafe int JsCompareString(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);


            if (CompareOptionsNotSupported(options))
                throw new PlatformNotSupportedException(GetPNSE(options));

            string cultureName = m_name;

            if (CompareOptionsNotSupportedForCulture(options, cultureName))
                throw new PlatformNotSupportedException(GetPNSEForCulture(options, cultureName));

            string exceptionMessage;
            int cmpResult = Interop.JsGlobalization.CompareString(out exceptionMessage, cultureName, span1.ToString(), span2.ToString(), options);

            if (!string.IsNullOrEmpty(exceptionMessage))
                throw new Exception(exceptionMessage);

            return cmpResult;
        }

        private static bool CompareOptionsNotSupported(CompareOptions options) =>
            (options & CompareOptions.IgnoreWidth) == CompareOptions.IgnoreWidth ||
            ((options & CompareOptions.IgnoreNonSpace) == CompareOptions.IgnoreNonSpace && (options & CompareOptions.IgnoreKanaType) != CompareOptions.IgnoreKanaType);


        private static string GetPNSE(CompareOptions options) =>
            $"CompareOptions = {options} are not supported when HybridGlobalization=true. Disable it to load larger ICU bundle, then use this option.";


        private static bool CompareOptionsNotSupportedForCulture(CompareOptions options, string cultureName) =>
            (options == CompareOptions.IgnoreKanaType &&
            (string.IsNullOrEmpty(cultureName) || cultureName.Split('-')[0] != "ja")) ||
            (options == CompareOptions.None &&
            (cultureName.Split('-')[0] == "ja"));


        private static string GetPNSEForCulture(CompareOptions options, string cultureName) =>
            $"CompareOptions = {options} are not supported for culture = {cultureName} when HybridGlobalization=true. Disable it to load larger ICU bundle, then use this option.";
    }
}
