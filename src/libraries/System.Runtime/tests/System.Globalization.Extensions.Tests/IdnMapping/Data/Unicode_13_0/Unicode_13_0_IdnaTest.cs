// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Class to read data obtained from http://www.unicode.org/Public/idna.  For more information read the information
    /// contained in Data\Unicode_13_0\IdnaTest_13.txt
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
    public class Unicode_13_0_IdnaTest : Unicode_IdnaTest
    {
        public Unicode_13_0_IdnaTest(string line, int lineNumber)
        {
            var split = line.Split(';');

            Type = PlatformDetection.IsNlsGlobalization ? IdnType.Transitional : IdnType.Nontransitional;

            Source = EscapedToLiteralString(split[0], lineNumber);

            UnicodeResult = new ConformanceIdnaUnicodeTestResult(EscapedToLiteralString(split[1], lineNumber), Source, EscapedToLiteralString(split[2], lineNumber), string.Empty);
            ASCIIResult = new ConformanceIdnaTestResult(EscapedToLiteralString(split[3], lineNumber), UnicodeResult.Value, EscapedToLiteralString(split[4], lineNumber), UnicodeResult.StatusValue);

            // NLS uses transitional IDN processing.
            if (Type == IdnType.Transitional)
            {
                ASCIIResult = new ConformanceIdnaTestResult(EscapedToLiteralString(split[5], lineNumber), ASCIIResult.Value, EscapedToLiteralString(split[6], lineNumber), ASCIIResult.StatusValue);
            }
            
            LineNumber = lineNumber;
        }
    }
}
