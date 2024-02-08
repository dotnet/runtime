// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoTestsBase
    {
        // On Windows's NLS, hiragana characters sort after katakana.
        // On ICU, it is the opposite
        protected static int s_expectedHiraganaToKatakanaCompare = PlatformDetection.IsNlsGlobalization ? 1 : -1;

        // On Windows's NLS, all halfwidth characters sort before fullwidth characters.
        // On ICU, half and fullwidth characters that aren't in the "Halfwidth and fullwidth forms" block U+FF00-U+FFEF
        // sort before the corresponding characters that are in the block U+FF00-U+FFEF
        protected static int s_expectedHalfToFullFormsComparison = PlatformDetection.IsNlsGlobalization ? -1 : 1;

        protected static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        protected static CompareInfo s_currentCompare = CultureInfo.CurrentCulture.CompareInfo;
        protected static CompareInfo s_germanCompare = new CultureInfo("de-DE").CompareInfo;
        protected static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        protected static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        protected static CompareInfo s_japaneseCompare = new CultureInfo("ja-JP").CompareInfo;
        protected static CompareInfo s_slovakCompare = new CultureInfo("sk-SK").CompareInfo;
        protected static CompareInfo s_frenchCompare = new CultureInfo("fr-FR").CompareInfo;
        protected static CompareOptions supportedIgnoreNonSpaceOption =
            PlatformDetection.IsHybridGlobalizationOnBrowser ?
            CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType :
            CompareOptions.IgnoreNonSpace;

        protected static CompareOptions supportedIgnoreCaseIgnoreNonSpaceOptions =
            PlatformDetection.IsHybridGlobalizationOnBrowser ?
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType :
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        // There is a regression in Windows 190xx version with the Kana comparison. Avoid running this test there.
        protected static bool IsNotWindowsKanaRegressedVersion() => !PlatformDetection.IsWindows10Version1903OrGreater ||
                                                              PlatformDetection.IsIcuGlobalization ||
                                                              s_invariantCompare.Compare("\u3060", "\uFF80\uFF9E", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase) == 0;

        protected static bool IsNotWindowsKanaRegressedVersionAndNotHybridGlobalizationOnWasm() => !PlatformDetection.IsHybridGlobalizationOnBrowser && IsNotWindowsKanaRegressedVersion();

        public class CustomComparer : StringComparer
        {
            private readonly CompareInfo _compareInfo;
            private readonly CompareOptions _compareOptions;

            public CustomComparer(CompareInfo cmpInfo, CompareOptions cmpOptions)
            {
                _compareInfo = cmpInfo;
                _compareOptions = cmpOptions;
            }

            public override int Compare(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions);

            public override bool Equals(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions) == 0;

            public override int GetHashCode(string obj) =>
                _compareInfo.GetHashCode(obj, _compareOptions);
        }
    }
}
