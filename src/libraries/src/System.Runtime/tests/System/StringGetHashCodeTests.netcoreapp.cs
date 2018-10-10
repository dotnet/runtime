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

            yield return new object[] { "Игры и развлечения без границ в формате 4K." }; // ru-RU
            yield return new object[] { "Poder portátil." }; // es-ES
            yield return new object[] { "想像を超えた、パフォーマンスを。" }; // ja-JP
            yield return new object[] { "Élégant et performant." }; // fr-FR
        }
    }
}
