// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<bool>> BooleanParserTestData
        {
            get
            {
                foreach (ParserTestData<bool> testData in BooleanFormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                string[] booleanNegativeInput =
                {
                    string.Empty,
                    "0",
                    "1",
                    " TRUE",
                    " FALSE",
                    "TRU",
                    "FALS",
                    "TRU$",
                    "FALS$",
                };

                foreach (char formatSymbol in new char[] { 'G', 'l' })
                {
                    foreach (string unparsableText in booleanNegativeInput)
                    {
                        yield return new ParserTestData<bool>(unparsableText, default, formatSymbol, expectedSuccess: false);
                    }
                }
            }
        }
    }
}
