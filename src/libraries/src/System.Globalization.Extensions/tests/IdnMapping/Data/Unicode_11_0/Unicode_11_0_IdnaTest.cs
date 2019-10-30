// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Class to read data obtained from http://www.unicode.org/Public/idna.  For more information read the information
    /// contained in Data\Unicode_11_0\IdnaTestV2.txt
    ///
    /// The structure of the data set is a semicolon delimited list with the following columns:
    ///
    /// Column 1: source -          The source string to be tested
    /// Column 2: toUnicode -       The result of applying toUnicode to the source,
    ///                             with Transitional_Processing=false.
    ///                             A blank value means the same as the source value.
    /// Column 3: toUnicodeStatus - A set of status codes, each corresponding to a particular test.
    ///                             A blank value means [] (no errors).
    /// Column 4: toAsciiN -        The result of applying toASCII to the source,
    ///                             with Transitional_Processing=false.
    ///                             A blank value means the same as the toUnicode value.
    /// Column 5: toAsciiNStatus -  A set of status codes, each corresponding to a particular test.
    ///                             A blank value means the same as the toUnicodeStatus value.
    ///                             An explicit [] means no errors.
    /// Column 6: toAsciiT -        The result of applying toASCII to the source,
    ///                             with Transitional_Processing=true.
    ///                             A blank value means the same as the toAsciiN value.
    /// Column 7: toAsciiTStatus -  A set of status codes, each corresponding to a particular test.
    ///                             A blank value means the same as the toAsciiNStatus value.
    ///                             An explicit [] means no errors.
    ///
    /// If the value of toUnicode or toAsciiN is the same as source, the column will be blank.
    /// </summary>
    public class Unicode_11_0_IdnaTest : IConformanceIdnaTest
    {
        public IdnType Type => IdnType.Nontransitional;
        public string Source { get; set; }
        public ConformanceIdnaUnicodeTestResult UnicodeResult { get; set; }
        public ConformanceIdnaTestResult ASCIIResult { get; set; }
        public int LineNumber { get; set; }

        public Unicode_11_0_IdnaTest(string line, int lineNumber)
        {
            var split = line.Split(';');

            Source = EscapedToLiteralString(split[0], lineNumber);
            UnicodeResult = new ConformanceIdnaUnicodeTestResult(EscapedToLiteralString(split[1], lineNumber), Source);
            ASCIIResult = new ConformanceIdnaTestResult(EscapedToLiteralString(split[3], lineNumber), UnicodeResult.Value);
            LineNumber = lineNumber;
        }

        /// <summary>
        /// This will convert strings with escaped sequences to literal characters.  The input string is
        /// expected to have escaped sequences in the form of '\uXXXX'.
        ///
        /// Example: "a\u0020b" will be converted to 'a b'.
        /// </summary>
        private static string EscapedToLiteralString(string escaped, int lineNumber)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < escaped.Length; i++)
            {
                if (i + 1 < escaped.Length && escaped[i] == '\\' && escaped[i + 1] == 'u')
                {
                    // Verify that the escaped sequence is not malformed
                    Assert.True(i + 5 < escaped.Length, "There was a problem converting to literal string on Line " + lineNumber);

                    var codepoint = Convert.ToInt32(escaped.Substring(i + 2, 4), 16);
                    sb.Append((char)codepoint);
                    i += 5;
                }
                else
                {
                    sb.Append(escaped[i]);
                }
            }

            return sb.ToString().Trim();
        }
    }
}
