// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Class to read data obtained from http://www.unicode.org/Public/idna.  For more information read the information
    /// contained in Data\9.0\IdnaTest.txt
    ///
    /// The structure of the data set is a semicolon delimited list with the following columns:
    ///
    /// Column 1: type - T for transitional, N for nontransitional, B for both
    /// Column 2: source - the source string to be tested
    /// Column 3: toUnicode - the result of applying toUnicode to the source, using the specified type. A blank value means the same as the source value.
    /// Column 4: toASCII - the result of applying toASCII to the source, using nontransitional. A blank value means the same as the toUnicode value.
    /// Column 5: NV8 - present if the toUnicode value would not be a valid domain name under IDNA2008. Not a normative field.
    /// </summary>
    public class Unicode_9_0_IdnaTest : Unicode_IdnaTest
    {
        public Unicode_9_0_IdnaTest(string line, int lineNumber)
        {
            string[] split = line.Split(';');

            Type = ConvertStringToType(split[0].Trim());
            Source = EscapedToLiteralString(split[1], lineNumber);
            bool validDomainName = (split.Length != 5 || split[4].Trim() != "NV8"); 
            
            // Server 2019 uses ICU 61.0 whose IDNA does not support the following cases. Ignore these entries there.
            if (PlatformDetection.IsWindowsServer2019 && Source.EndsWith("\ud802\udf8b\u3002\udb40\udd0a", StringComparison.Ordinal))
            {
                Source = "";
            }

            UnicodeResult = new ConformanceIdnaUnicodeTestResult(EscapedToLiteralString(split[2], lineNumber), Source, validDomainName);
            ASCIIResult = new ConformanceIdnaTestResult(EscapedToLiteralString(split[3], lineNumber), UnicodeResult.Value);
            LineNumber = lineNumber;
        }

        private static IdnType ConvertStringToType(string idnType)
        {
            switch (idnType)
            {
                case "T":
                    return IdnType.Transitional;
                case "N":
                    return IdnType.Nontransitional;
                case "B":
                    return IdnType.Both;
                default:
                    throw new ArgumentOutOfRangeException(nameof(idnType), "Unknown idnType");
            }
        }
    }
}
