// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIndexOf
    {
        private static CultureInfo s_hungarian = new CultureInfo("hu-HU");
        private static CultureInfo s_turkish = new CultureInfo("tr-TR");

        [Fact]
        public void Test1() { TestExc<ArgumentNullException>(CultureInfo.InvariantCulture, null, "Test", CompareOptions.None); }

        [Fact]
        public void Test2() { TestExc<ArgumentNullException>(CultureInfo.InvariantCulture, "Test", null, CompareOptions.None); }

        [Fact]
        public void Test3() { TestExc<ArgumentNullException>(CultureInfo.InvariantCulture, null, null, CompareOptions.None); }

        [Fact]
        public void Test4() { TestExc<ArgumentException>(CultureInfo.InvariantCulture, "Test's", "Tests", CompareOptions.StringSort); }

        [Fact]
        public void Test5() { TestExc<ArgumentException>(CultureInfo.InvariantCulture, "Test's", "Tests", (CompareOptions)(-1)); }

        [Fact]
        public void Test6() { TestExc<ArgumentException>(CultureInfo.InvariantCulture, "Test's", "Tests", (CompareOptions)0x11111111); }

        [Fact]
        public void Test7() { Test(CultureInfo.InvariantCulture, "foo", "", 0, CompareOptions.None); }

        [Fact]
        public void Test8() { Test(CultureInfo.InvariantCulture, "", "", 0, CompareOptions.None); }

        [Fact]
        public void Test9() { Test(CultureInfo.InvariantCulture, new string('b', 100) + new string('a', 5555), "aaaaaaaaaaaaaaa", 100, CompareOptions.None); }

        [Fact]
        public void Test10() { Test(CultureInfo.InvariantCulture, new string('b', 101) + new string('a', 5555), new string('a', 5000), 101, CompareOptions.None); }

        [Fact]
        public void Test11() { Test(CultureInfo.InvariantCulture, new string('a', 5555), new string('a', 5000) + "b", -1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void Test12() { Test(s_hungarian, "foobardzsdzs", "rddzs", 5, CompareOptions.None); }

        [Fact]
        public void Test13() { Test(s_hungarian, "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal); }

        [Fact]
        public void Test14() { Test(CultureInfo.InvariantCulture, "foobardzsdzs", "rddzs", -1, CompareOptions.None); }

        [Fact]
        public void Test15() { Test(CultureInfo.InvariantCulture, "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal); }

        [Fact]
        public void Test16() { Test(s_turkish, "Hi", "I", -1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test17() { Test(CultureInfo.InvariantCulture, "Hi", "I", 1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test18() { Test(s_turkish, "Hi", "\u0130", 1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test19() { Test(CultureInfo.InvariantCulture, "Hi", "\u0130", -1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test20() { Test(s_turkish, "Hi", "I", -1, CompareOptions.None); }

        [Fact]
        public void Test21() { Test(CultureInfo.InvariantCulture, "Hi", "I", -1, CompareOptions.None); }

        [Fact]
        public void Test22() { Test(s_turkish, "Hi", "\u0130", -1, CompareOptions.None); }

        [Fact]
        public void Test23() { Test(CultureInfo.InvariantCulture, "Hi", "\u0130", -1, CompareOptions.None); }

        [Fact]
        public void Test24() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "A\u0300", 8, CompareOptions.None); }

        [Fact]
        public void Test25() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "A\u0300", -1, CompareOptions.Ordinal); }

        [Fact]
        public void Test26() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "a\u0300", 8, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test27() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "a\u0300", -1, CompareOptions.OrdinalIgnoreCase); }

        [Fact]
        public void Test28() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "a\u0300", -1, CompareOptions.None); }

        [Fact]
        public void Test29() { Test(CultureInfo.InvariantCulture, "Exhibit \u00C0", "a\u0300", -1, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void Test30()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "FooBar", "Foo" + unassignedUnicode + "Bar", 0, CompareOptions.None);
        }

        [Fact]
        public void Test31() { Test(CultureInfo.InvariantCulture, "FooBar", "Foo\u0400Bar", -1, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void Test32()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "~FooBar", "Foo" + unassignedUnicode + "Bar", 1, CompareOptions.IgnoreNonSpace);
        }

        [Fact]
        public void Test33()
        {
            Test(CultureInfo.InvariantCulture, "TestFooBA\u0300R", "FooB\u00C0R", 4, CompareOptions.IgnoreNonSpace);
        }

        //   Now IgnoreSymbols will not consider the ' ' as a Symbol
        [Fact]
        public void Test34() { Test(CultureInfo.InvariantCulture, "More Test's", "Tests", 5, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test35() { Test(CultureInfo.InvariantCulture, "More Test's", "Tests", -1, CompareOptions.None); }

        [Fact]
        public void Test36() { Test(CultureInfo.InvariantCulture, "cbabababdbaba", "ab", 2, CompareOptions.None); }

        public void Test(CultureInfo culture, string str1, string str2, int expected, CompareOptions options)
        {
            CompareInfo ci = culture.CompareInfo;
            int i = ci.IndexOf(str1, str2, options);
            Assert.Equal(expected, i);
        }

        public void TestExc<T>(CultureInfo culture, string str1, string str2, CompareOptions options)
            where T : Exception
        {
            CompareInfo ci = culture.CompareInfo;
            Assert.Throws<T>(() =>
           {
               int i = ci.IndexOf(str1, str2, options);
           });
        }
        
        private char GetNextUnassignedUnicode()
        {
            for (char ch = '\uFFFF'; ch > '\u0000'; ch++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherNotAssigned)
                {
                    return ch;
                }
            }
            return Char.MinValue; // there are no unassigned unicode characters from \u0000 - \uFFFF
        }
    }
}
