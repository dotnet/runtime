// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetBytes
    {
        public static IEnumerable<object[]> GetBytes_Basic_TestData()
        {
            yield return new object[] { "\t\n\rXYZabc123", 0, 12, new byte[] { 9, 10, 13, 88, 89, 90, 97, 98, 99, 49, 50, 51 } };

            yield return new object[] { "\u03a0\u03a3", 0, 2, new byte[] { 43, 65, 54, 65, 68, 111, 119, 45 } };

            yield return new object[] { string.Empty, 0, 0, new byte[0] };

            string chars2 = "UTF7 Encoding Example";
            yield return new object[] { chars2, 1, 2, new byte[] { 84, 70 } };
        }

        [Theory]
        [MemberData(nameof(GetBytes_Basic_TestData))]
        public void GetBytes(string source, int index, int count, byte[] expected)
        {
            GetBytes(true, source, index, count, expected);
            GetBytes(false, source, index, count, expected);
        }

        public static IEnumerable<object[]> GetBytes_Advanced_TestData()
        {
            string optionalChars1 = "!\"#$%&*;<=>@[]^_`{|}";
            byte[] optionalFalseBytes = new byte[] 
            {
                43, 65, 67, 69, 65, 73, 103, 65,
                106, 65, 67, 81, 65, 74, 81, 65,
                109, 65, 67, 111, 65, 79, 119, 65,
                56, 65, 68, 48, 65, 80, 103, 66, 65,
                65, 70, 115, 65, 88, 81, 66, 101, 65,
                70, 56, 65, 89, 65, 66, 55, 65,
                72, 119, 65, 102, 81, 45
            };
            byte[] optionalTrueBytes = new byte[]
            {
                33, 34, 35, 36, 37, 38, 42, 59, 60, 61, 62,
                64, 91, 93, 94, 95, 96, 123, 124, 125
            };

            yield return new object[] { false, optionalChars1, 0, optionalChars1.Length, optionalFalseBytes };
            yield return new object[] { true, optionalChars1, 0, optionalChars1.Length, optionalTrueBytes };
            
            yield return new object[] { false, "\u0023\u0025\u03a0\u03a3", 1, 2, new byte[] { 43, 65, 67, 85, 68, 111, 65, 45 } };
            yield return new object[] { true, "\u0023\u0025\u03a0\u03a3", 1, 2, new byte[] { 37, 43, 65, 54, 65, 45 } };
        }

        [Theory]
        [MemberData(nameof(GetBytes_Advanced_TestData))]
        public void GetBytes(bool allowOptionals, string source, int index, int count, byte[] expected)
        {
            EncodingHelpers.Encode(new UTF7Encoding(allowOptionals), source, index, count, expected);
        }
    }
}
