// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIndexOf2
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        public static string[] InterestingStrings = new string[] { "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57" };

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

        // PosTest2: Compare many strings
        [Fact]
        public void CompareManyStrings()
        {
            for (int i = 0; i < 1000; i++)
            {
                string str1 = _generator.GetString(-55, false, 5, 20);
                string str2 = _generator.GetString(-55, false, 5, 20);
                Assert.Equal(0, CultureInfo.CurrentCulture.CompareInfo.IndexOf(str1, str1, CompareOptions.Ordinal));
                Assert.Equal(0, CultureInfo.CurrentCulture.CompareInfo.IndexOf(str2, str2, CompareOptions.Ordinal));
                TestStrings(str1, str2);
                TestStrings(str1 + str2, str2);
            }
        }

        // PosTest3: Specific regression cases
        [Fact]
        public void RegressionTests()
        {
            CultureInfo oldCi = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("hu-HU");
            TestStrings("Foodzsdzsbar", "ddzs");
            CultureInfo.DefaultThreadCurrentCulture = oldCi;
            TestStrings("\u00C0nimal", "A\u0300");
        }

        private void TestStrings(string str1, string str2)
        {
            int expectValue = PredictValue(str1, str2);
            int actualValue = CultureInfo.CurrentCulture.CompareInfo.IndexOf(str1, str2, CompareOptions.Ordinal);
            Assert.Equal(expectValue, actualValue);
        }

        private int PredictValue(string str1, string str2)
        {
            if (str1 == null)
            {
                if (str2 == null) return 0;
                else return -1;
            }
            if (str2 == null) return -1;

            if (str2.Length > str1.Length) return -1;

            for (int i = 0; i <= str1.Length - str2.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < str2.Length; j++)
                {
                    if ((int)str1[i + j] != (int)str2[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
