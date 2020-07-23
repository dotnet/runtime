// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace System.Text.Json.Tests
{
    internal class JsonGuidTestData
    {
        private static readonly string s_guidStr = "08314FA2-B1FE-4792-BCD1-6E62338AC7F3";

        public static IEnumerable<object[]> ValidGuidTests()
        {
            yield return new object[] { s_guidStr, s_guidStr };
            yield return new object[] { s_guidStr.ToLower(), s_guidStr };
            yield return new object[] { "08314Fa2-B1Fe-4792-BCD1-6e62338aC7F3", s_guidStr }; // mixed case

            string newGuidStr = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            yield return new object[] { newGuidStr, newGuidStr };
        }

        public static IEnumerable<object[]> ValidHexGuidTests()
        {
            // Uppercase hex
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033", s_guidStr };
            // Lowercase hex
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0066\\u0061\\u0032\\u002d\\u0062\\u0031\\u0066\\u0065\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0062\\u0063\\u0064\\u0031\\u002d\\u0036\\u0065\\u0036\\u0032\\u0033\\u0033\\u0038\\u0061\\u0063\\u0037\\u0066\\u0033", s_guidStr };
            // A mix of upper and lower case hex
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0061\\u0032\\u002d\\u0062\\u0031\\u0066\\u0065\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0062\\u0063\\u0064\\u0031\\u002d\\u0036\\u0065\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0066\\u0033", s_guidStr };
            // Combine hex and plain string
            yield return new object[] { "\\u00308314Fa2-B1Fe-4792-\\u0062CD1-6e62338aC7F\\u0033", s_guidStr };
        }

        public static IEnumerable<object[]> InvalidGuidTests()
        {
            // Invalid formats
            Guid testGuid = new Guid(s_guidStr);
            yield return new object[] { testGuid.ToString("B", CultureInfo.InvariantCulture) };
            yield return new object[] { testGuid.ToString("P", CultureInfo.InvariantCulture) };
            yield return new object[] { testGuid.ToString("N", CultureInfo.InvariantCulture) };

            yield return new object[] { new string('$', 1) };
            yield return new object[] { new string(' ', 1) };
            yield return new object[] { new string('$', s_guidStr.Length) };
            yield return new object[] { new string(' ', s_guidStr.Length) };

            for (int truncationPoint = 1; truncationPoint < s_guidStr.Length - 1; truncationPoint++)
            {
                string truncatedText = s_guidStr.Substring(0, truncationPoint);

                // Stop short
                yield return new object[] { truncatedText };

                // Append junk
                yield return new object[] { truncatedText.PadRight(s_guidStr.Length, '$') };
                yield return new object[] { truncatedText.PadRight(s_guidStr.Length, ' ') };
                yield return new object[] { truncatedText.PadRight(truncatedText.Length + 1, '$') };
                yield return new object[] { truncatedText.PadRight(truncatedText.Length + 1, ' ') };
                // Prepend junk
                yield return new object[] { truncatedText.PadLeft(s_guidStr.Length, '$') };
                yield return new object[] { truncatedText.PadLeft(s_guidStr.Length, ' ') };
                yield return new object[] { truncatedText.PadLeft(truncatedText.Length + 1, '$') };
                yield return new object[] { truncatedText.PadLeft(truncatedText.Length + 1, ' ') };
            }

            foreach (object[] guid in ValidGuidTests())
            {
                string guidStr = (string)guid[0];

                for (int i = 0; i < guidStr.Length; i++)
                {
                    // Corrupt one character

                    char[] bad = (char[])(guidStr.ToCharArray().Clone());

                    if (char.IsDigit(guidStr[i]))
                    {
                        bad[i] = (char)('0' - 1);
                        yield return new object[] { new string(bad) };
                        bad[i] = (char)('9' + 1);
                        yield return new object[] { new string(bad) };
                    }
                    else if (char.IsLetter(guidStr[i]))
                    {
                        bad[i] = (char)('a' - 1);
                        yield return new object[] { new string(bad) };
                        bad[i] = (char)('f' + 1);
                        yield return new object[] { new string(bad) };
                    }
                    else
                    {
                        bad[i] = '!';
                        yield return new object[] { new string(bad) };
                    }
                }

                // Too long
                yield return new object[] { $"{guidStr} " };
                yield return new object[] { $"{guidStr}$" };
                yield return new object[] { $"{guidStr}1" };
                yield return new object[] { $"{guidStr}A" };
                yield return new object[] { $"{guidStr}a" };
                yield return new object[] { $" {guidStr}" };
                yield return new object[] { $"${guidStr}" };
                yield return new object[] { $"1{guidStr}" };
                yield return new object[] { $"A{guidStr}" };
                yield return new object[] { $"a{guidStr}" };
            }

            // Invalid hex strings

            // Invalid format: "8314FA2B1FE4792BCD16E62338AC7F3"
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u0042\\u0031\\u0046\\u0045\\u0034\\u0037\\u0039\\u0032\\u0042\\u0043\\u0044\\u0031\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033" };
            // Contains invalid character: "?8314FA2-B1FE-4792-BCD1-6E62338AC7F3"
            yield return new object[] { "\\u003F\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033" };
            // Too short: "8314FA2-B1FE-4792-BCD1-6E62338AC7F"
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046" };
            // Too long: "8314FA2-B1FE-4792-BCD1-6E62338AC7F33"
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033\\u0033" };
            // Trailing space: "8314FA2-B1FE-4792-BCD1-6E62338AC7F3 "
            yield return new object[] { "\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033\\u0020" };
            // Leading space: " 8314FA2-B1FE-4792-BCD1-6E62338AC7F3"
            yield return new object[] { "\\u0020\\u0030\\u0038\\u0033\\u0031\\u0034\\u0046\\u0041\\u0032\\u002d\\u0042\\u0031\\u0046\\u0045\\u002d\\u0034\\u0037\\u0039\\u0032\\u002d\\u0042\\u0043\\u0044\\u0031\\u002d\\u0036\\u0045\\u0036\\u0032\\u0033\\u0033\\u0038\\u0041\\u0043\\u0037\\u0046\\u0033" };
        }
    }
}
