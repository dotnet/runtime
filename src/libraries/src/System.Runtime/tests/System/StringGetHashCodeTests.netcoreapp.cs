// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public partial class StringGetHashCodeTests
    {
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
