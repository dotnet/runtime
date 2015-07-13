// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoCompare2
    {
        public static string[] InterestingStrings = new string[] { null, "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57" };

        // PosTest1: Compare interesting strings ordinally
        [Fact]
        public void CompareInterestingStrings()
        {
            foreach (string s in InterestingStrings)
            {
                foreach (string r in InterestingStrings)
                {
                    TestStrings(s, r);
                }
            }
        }

        // PosTest2: Compare many characters
        [Fact]
        public void CompareManyCharacters()
        {
            for (int i = 0; i < 40; i++)
            {
                char c = TestLibrary.Generator.GetChar(-55);
                Assert.Equal(0, CultureInfo.CurrentCulture.CompareInfo.Compare(new string(new char[] { c }), new string(new char[] { c }), CompareOptions.Ordinal));
                for (int j = 0; j < (int)c; j++)
                {
                    int compareResult = CultureInfo.CurrentCulture.CompareInfo.Compare(new string(new char[] { c }), new string(new char[] { (char)j }), CompareOptions.Ordinal);
                    if (compareResult != 0) compareResult = compareResult / Math.Abs(compareResult);
                    Assert.Equal(1, compareResult);
                }
            }
        }

        // PosTest3: Compare many strings
        [Fact]
        public void CompareManyStrings()
        {
            for (int i = 0; i < 1000; i++)
            {
                string str1 = TestLibrary.Generator.GetString(-55, false, 5, 20);
                string str2 = TestLibrary.Generator.GetString(-55, false, 5, 20);
                Assert.Equal(0, CultureInfo.CurrentCulture.CompareInfo.Compare(str1, str1, CompareOptions.Ordinal));
                Assert.Equal(0, CultureInfo.CurrentCulture.CompareInfo.Compare(str2, str2, CompareOptions.Ordinal));
                TestStrings(str1, str2);
            }
        }

        // PosTest4: Test Hungarian Culture
        [Fact]
        public void TestHungarianCulture()
        {
            CultureInfo oldCi = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("hu-HU");
            TestStrings("dzsdzs", "ddzs");
            CultureInfo.DefaultThreadCurrentCulture = oldCi;
            TestStrings("\u00C0nimal", "A\u0300nimal");
        }

        private void TestStrings(string str1, string str2)
        {
            int expectValue = PredictValue(str1, str2);
            int actualValue = CultureInfo.CurrentCulture.CompareInfo.Compare(str1, str2, CompareOptions.Ordinal);
            if (expectValue != 0) expectValue = expectValue / Math.Abs(expectValue);
            if (actualValue != 0) actualValue = actualValue / Math.Abs(actualValue);

            Assert.Equal(expectValue, actualValue);
        }

        private int PredictValue(string str1, string str2)
        {
            if (str1 == null)
            {
                if (str2 == null) return 0;
                else return -1;
            }
            if (str2 == null) return 1;

            for (int i = 0; i < str1.Length; i++)
            {
                if (i >= str2.Length) return 1;
                if ((int)str1[i] > (int)str2[i]) return 1;
                if ((int)str1[i] < (int)str2[i]) return -1;
            }

            if (str2.Length > str1.Length) return -1;

            return 0;
        }
    }
}
