// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class StringGetHashCodeTests
    {
        /// <summary>
        /// Ensure that hash codes are randomized by getting the hash in two processes
        /// and confirming it is different (modulo possible values of int).
        /// If the legacy hash codes are being returned, it will not be different.
        /// </summary>
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(GetHashCode_TestData))]
        public void GetHashCodeWithStringComparer_UseSameStringInTwoProcesses_ReturnsDifferentHashCodes(int getHashCodeIndex)
        {
            Func<string, string, int> method = (parentHash, i) => int.Parse(parentHash) != s_GetHashCodes[int.Parse(i)]() ? RemoteExecutor.SuccessExitCode : -1;
            int parentHashCode = s_GetHashCodes[getHashCodeIndex]();
            int exitCode, retry = 0;
            do
            {
                // very small chance the child and parent hashcode are the same. To further reduce chance of collision we try up to 3 times
                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, parentHashCode.ToString(), getHashCodeIndex.ToString(), new RemoteInvokeOptions { CheckExitCode = false }))
                {
                    exitCode = handle.ExitCode;
                    retry++;
                }
            } while (exitCode != RemoteExecutor.SuccessExitCode && retry < 3);
            Assert.Equal(RemoteExecutor.SuccessExitCode, exitCode);
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            for (int i = 0; i < s_GetHashCodes.Length; i++)
            {
                yield return new object[] { i };
            }
        }

        private static readonly Func<int>[] s_GetHashCodes = {
            () => { return StringComparer.CurrentCulture.GetHashCode("abc"); },
            () => { return StringComparer.CurrentCultureIgnoreCase.GetHashCode("abc"); },
            () => { return StringComparer.InvariantCulture.GetHashCode("abc"); },
            () => { return StringComparer.InvariantCultureIgnoreCase.GetHashCode("abc"); },
            () => { return StringComparer.Ordinal.GetHashCode("abc"); },
            () => { return StringComparer.OrdinalIgnoreCase.GetHashCode("abc"); },
            () => { return "abc".GetHashCode(); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreCase); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreKanaType); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreNonSpace); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreSymbols); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreWidth); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.None); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.Ordinal); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.OrdinalIgnoreCase); }
        };

        [Theory]
        [MemberData(nameof(GetHashCodeOrdinalIgnoreCase_TestData))]
        public void GetHashCode_OrdinalIgnoreCase_ReturnsSameHashCodeAsUpperCaseOrdinal(string input)
        {
            // As an implementation detail, the OrdinalIgnoreCase hash code calculation is simply the hash code
            // of the upper-invariant version of the input string.

            Assert.Equal(input.ToUpperInvariant().GetHashCode(), input.GetHashCode(StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<object[]> GetHashCodeOrdinalIgnoreCase_TestData()
        {
            // 0 through 8 char lowercase & uppercase ASCII strings
            // tests the various branches within the hash code calculation routines

            for (int i = 0; i <= 8; i++)
            {
                yield return new object[] { "abcdefgh".Substring(0, i) };
                yield return new object[] { "ABCDEFGH".Substring(0, i) };
            }

            // 16 char mixed case mostly-ASCII string plus one non-ASCII character inserted at various locations
            // tests fallback logic for OrdinalIgnoreCase hash

            for (int i = 0; i <= 16; i++)
            {
                yield return new object[] { "AaBbCcDdEeFfGgHh".Insert(i, "\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */) };
                yield return new object[] { "AaBbCcDdEeFfGgHh".Insert(i, "\u044D" /* CYRILLIC SMALL LETTER E */) };
                // https://github.com/dotnet/runtime/issues/95503
                if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform && PlatformDetection.IsNotHybridGlobalizationOnBrowser)
                    yield return new object[] { "AaBbCcDdEeFfGgHh".Insert(i, "\u0131" /* LATIN SMALL LETTER DOTLESS I */) };
            }

            // Various texts copied from Microsoft's non-U.S. home pages, for further localization tests

            yield return new object[] { "\u0418\u0433\u0440\u044B \u0438 \u0440\u0430\u0437\u0432\u043B\u0435\u0447\u0435\u043D\u0438\u044F \u0431\u0435\u0437 \u0433\u0440\u0430\u043D\u0438\u0446 \u0432 \u0444\u043E\u0440\u043C\u0430\u0442\u0435 4K." }; // ru-RU
            yield return new object[] { "Poder port\u00E1til." }; // es-ES
            yield return new object[] { "\u60F3\u50CF\u3092\u8D85\u3048\u305F\u3001\u30D1\u30D5\u30A9\u30FC\u30DE\u30F3\u30B9\u3092\u3002" }; // ja-JP
            yield return new object[] { "\u00C9l\u00E9gant et performant." }; // fr-FR
        }
    }
}
