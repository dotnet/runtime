// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoParseCombiningCharacters
    {
        // PosTest1: The mothod should return the indexes of each base character
        [Fact]
        public void PosTest()
        {
            VerificationHelper("\u4f00\u302a\ud800\udc00\u4f01", new int[] { 0, 2, 4 });
            VerificationHelper("abcdefgh", new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            VerificationHelper("zj\uDBFF\uDFFFlk", new int[] { 0, 1, 2, 4, 5 });
            VerificationHelper("!@#$%^&", new int[] { 0, 1, 2, 3, 4, 5, 6 });
            VerificationHelper("!\u20D1bo\uFE22\u20D1\u20EB|", new int[] { 0, 2, 3, 7 });
            VerificationHelper("1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", new int[] { 0, 1, 3, 7 });
            VerificationHelper("   ", new int[] { 0, 1, 2 });
        }

        // PosTest2: The argument string is an empty string
        [Fact]
        public void TestEmptyString()
        {
            int[] result = StringInfo.ParseCombiningCharacters(string.Empty);
            Assert.NotNull(result);
            Assert.Equal(0, result.Length);
        }

        // NegTest1: The argument string is a null reference
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int[] result = StringInfo.ParseCombiningCharacters(str);
            });
        }

        private void VerificationHelper(string str, int[] expected)
        {
            int[] result = StringInfo.ParseCombiningCharacters(str);
            Assert.True(compare<int>(result, expected));
        }

        private bool compare<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (!a[i].Equals(b[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
