// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<Guid>> GuidParserTestData
        {
            get
            {
                foreach (ParserTestData<Guid> testData in GuidFormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                Guid guid = new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3");
                foreach (SupportedFormat format in GuidFormats)
                {
                    string goodText = guid.ToString(format.Symbol.ToString(), CultureInfo.InvariantCulture);

                    // Corrupt a single character
                    for (int i = 0; i < goodText.Length; i++)
                    {
                        char[] bad = (char[])(goodText.ToCharArray().Clone());
                        if (char.IsDigit(goodText[i]))
                        {
                            bad[i] = (char)('0' - 1);
                            yield return new ParserTestData<Guid>(new string(bad), default, format.Symbol, expectedSuccess: false);
                            bad[i] = (char)('9' + 1);
                            yield return new ParserTestData<Guid>(new string(bad), default, format.Symbol, expectedSuccess: false);
                        }
                        else if (char.IsLetter(goodText[i]))
                        {
                            bad[i] = (char)('a' - 1);
                            yield return new ParserTestData<Guid>(new string(bad), default, format.Symbol, expectedSuccess: false);
                            bad[i] = (char)('f' + 1);
                            yield return new ParserTestData<Guid>(new string(bad), default, format.Symbol, expectedSuccess: false);
                        }
                        else
                        {
                            bad[i] = '!';
                            yield return new ParserTestData<Guid>(new string(bad), default, format.Symbol, expectedSuccess: false);
                        }
                    }

                    // Stop short
                    for (int truncationPoint = 0; truncationPoint < goodText.Length; truncationPoint++)
                    {
                        string truncatedText = goodText.Substring(0, truncationPoint);
                        yield return new ParserTestData<Guid>(truncatedText, default, format.Symbol, expectedSuccess: false);
                        yield return new ParserTestData<Guid>(truncatedText.PadRight(goodText.Length, '$'), default, format.Symbol, expectedSuccess: false);
                    }
                }
            }
        }
    }
}
