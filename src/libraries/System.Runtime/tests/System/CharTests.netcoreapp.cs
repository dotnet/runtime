// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Unicode.Tests;
using Xunit;
using Xunit.Sdk;

namespace System.Tests
{
    public partial class CharTests
    {
        [OuterLoop]
        [Fact]
        public static void GetUnicodeCategory_Char_AllInputs()
        {
            // This tests calls char.GetUnicodeCategory for every possible input, ensuring that
            // the runtime agrees with the data in the core Unicode files.

            for (uint i = 0; i <= char.MaxValue; i++)
            {
                UnicodeCategory expected;

                // The code points in the switch block below must be special-cased
                // because they switched categories between versions of the Unicode
                // specification. For compatibility reasons Char keeps its own copy
                // of the categories for the first 256 code points, as it's locked
                // to an earlier version of the standard. For an example of a code
                // point that switched categories, see the discussion on U+00AD
                // SOFT HYPHEN at https://www.unicode.org/versions/Unicode4.0.0/.

                switch (i)
                {
                    case '\u00a7':
                    case '\u00b6':
                        expected = UnicodeCategory.OtherSymbol;
                        break;

                    case '\u00aa':
                    case '\u00ba':
                        expected = UnicodeCategory.LowercaseLetter;
                        break;

                    case '\u00ad':
                        expected = UnicodeCategory.DashPunctuation;
                        break;

                    default:
                        expected = UnicodeData.GetUnicodeCategory(i);
                        break;
                }

                if (expected != char.GetUnicodeCategory((char)i))
                {
                    // We'll build up the exception message ourselves so the dev knows what code point failed.
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: char.GetUnicodeCategory((char)i),
                        userMessage: FormattableString.Invariant($@"char.GetUnicodeCategory('\u{i:X4}') returned wrong value."));
                }
            }
        }

        [OuterLoop]
        [Fact]
        public static void IsLetter_Char_AllInputs()
        {
            // This tests calls char.IsLetter for every possible input, ensuring that
            // the runtime agrees with the data in the core Unicode files.

            for (uint i = 0; i <= char.MaxValue; i++)
            {
                if (UnicodeData.IsLetter((char)i) != char.IsLetter((char)i))
                {
                    // We'll build up the exception message ourselves so the dev knows what code point failed.
                    throw new AssertActualExpectedException(
                        expected: UnicodeData.IsLetter((char)i),
                        actual: char.IsLetter((char)i),
                        userMessage: FormattableString.Invariant($@"char.IsLetter('\u{i:X4}') returned wrong value."));
                }
            }
        }

        [OuterLoop]
        [Fact]
        public static void IsLower_Char_AllInputs()
        {
            // This tests calls char.IsLower for every possible input, ensuring that
            // the runtime agrees with the data in the core Unicode files.

            for (uint i = 0; i <= char.MaxValue; i++)
            {
                bool expected;

                switch (i)
                {
                    case '\u00AA': // FEMININE ORDINAL INDICATOR
                    case '\u00BA': // MASCULINE ORDINAL INDICATOR

                        // In Unicode 6.1 the code points U+00AA and U+00BA were reassigned
                        // from category Ll to category Lo. However, for compatibility reasons,
                        // Char uses the older version of the Unicode standard for code points
                        // in the range U+0000..U+00FF. So we'll special-case these here.
                        // More info: https://www.unicode.org/review/pri181/

                        expected = true;
                        break;

                    default:
                        expected = UnicodeData.GetUnicodeCategory((char)i) == UnicodeCategory.LowercaseLetter;
                        break;
                }

                if (expected != char.IsLower((char)i))
                {
                    // We'll build up the exception message ourselves so the dev knows what code point failed.
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: char.IsLower((char)i),
                        userMessage: FormattableString.Invariant($@"char.IsLower('\u{i:X4}') returned wrong value."));
                }
            }
        }

        [OuterLoop]
        [Fact]
        public static void IsUpper_Char_AllInputs()
        {
            // This tests calls char.IsUpper for every possible input, ensuring that
            // the runtime agrees with the data in the core Unicode files.

            for (uint i = 0; i <= char.MaxValue; i++)
            {
                bool expected = UnicodeData.GetUnicodeCategory((char)i) == UnicodeCategory.UppercaseLetter;

                if (expected != char.IsUpper((char)i))
                {
                    // We'll build up the exception message ourselves so the dev knows what code point failed.
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: char.IsUpper((char)i),
                        userMessage: FormattableString.Invariant($@"char.IsUpper('\u{i:X4}') returned wrong value."));
                }
            }
        }

        [OuterLoop]
        [Fact]
        public static void IsWhiteSpace_Char_AllInputs()
        {
            // This tests calls char.IsWhiteSpace for every possible input, ensuring that
            // the runtime agrees with the data in the core Unicode files.

            for (uint i = 0; i <= char.MaxValue; i++)
            {
                if (UnicodeData.IsWhiteSpace(i) != char.IsWhiteSpace((char)i))
                {
                    // We'll build up the exception message ourselves so the dev knows what code point failed.
                    throw new AssertActualExpectedException(
                        expected: UnicodeData.IsWhiteSpace(i),
                        actual: char.IsWhiteSpace((char)i),
                        userMessage: FormattableString.Invariant($@"char.IsWhiteSpace('\u{i:X4}') returned wrong value."));
                }
            }
        }
    }
}
