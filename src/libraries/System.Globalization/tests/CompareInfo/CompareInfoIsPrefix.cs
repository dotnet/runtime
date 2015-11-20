// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIsPrefix
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
        public void Test7() { Test(CultureInfo.InvariantCulture, "foo", "", true, CompareOptions.None); }

        [Fact]
        public void Test8() { Test(CultureInfo.InvariantCulture, "", "", true, CompareOptions.None); }

        [Fact]
        public void Test9() { Test(CultureInfo.InvariantCulture, new string('a', 5555), "aaaaaaaaaaaaaaa", true, CompareOptions.None); }

        [Fact]
        public void Test10() { Test(CultureInfo.InvariantCulture, new string('a', 5555), new string('a', 5000), true, CompareOptions.None); }

        [Fact]
        public void Test11() { Test(CultureInfo.InvariantCulture, new string('a', 5555), new string('a', 5000) + "b", false, CompareOptions.None); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void Test12() { Test(s_hungarian, "dzsdzsfoobar", "ddzsf", true, CompareOptions.None); }

        [Fact]
        public void Test13() { Test(s_hungarian, "dzsdzsfoobar", "ddzsf", false, CompareOptions.Ordinal); }

        [Fact]
        public void Test14() { Test(CultureInfo.InvariantCulture, "dzsdzsfoobar", "ddzsf", false, CompareOptions.None); }

        [Fact]
        public void Test15() { Test(CultureInfo.InvariantCulture, "dzsdzsfoobar", "ddzsf", false, CompareOptions.Ordinal); }

        [Fact]
        public void Test16() { Test(s_turkish, "interesting", "I", false, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test17() { Test(CultureInfo.InvariantCulture, "interesting", "I", true, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test18() { Test(s_turkish, "interesting", "\u0130", true, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test19() { Test(CultureInfo.InvariantCulture, "interesting", "\u0130", false, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test20() { Test(s_turkish, "interesting", "I", false, CompareOptions.None); }

        [Fact]
        public void Test21() { Test(CultureInfo.InvariantCulture, "interesting", "I", false, CompareOptions.None); }

        [Fact]
        public void Test22() { Test(s_turkish, "interesting", "\u0130", false, CompareOptions.None); }

        [Fact]
        public void Test23() { Test(CultureInfo.InvariantCulture, "interesting", "\u0130", false, CompareOptions.None); }

        [Fact]
        public void Test24() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "A\u0300", true, CompareOptions.None); }

        [Fact]
        public void Test25() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "A\u0300", false, CompareOptions.Ordinal); }

        [Fact]
        public void Test26() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "a\u0300", true, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test27() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "a\u0300", false, CompareOptions.OrdinalIgnoreCase); }

        [Fact]
        public void Test28() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "a\u0300", false, CompareOptions.None); }

        [Fact]
        public void Test29() { Test(CultureInfo.InvariantCulture, "\u00C0nimal", "a\u0300", false, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void Test30()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "FooBar", "Foo" + unassignedUnicode + "Bar", true, CompareOptions.None);
        }

        [Fact]
        public void Test31() { Test(CultureInfo.InvariantCulture, "FooBar", "Foo\u0400Bar", false, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void Test32()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "FooBar", "Foo" + unassignedUnicode + "Bar", true, CompareOptions.IgnoreNonSpace);
        }

        [Fact]
        public void Test33()
        {
            Test(CultureInfo.InvariantCulture, "FooBA\u0300R", "FooB\u00C0R", true, CompareOptions.IgnoreNonSpace);
        }

        [Fact]
        public void Test34() { Test(CultureInfo.InvariantCulture, "Test's can be interesting", "Tests", true, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test35() { Test(CultureInfo.InvariantCulture, "Test's can be interesting", "Tests", false, CompareOptions.None); }

        public void Test(CultureInfo culture, string str1, string str2, bool expected, CompareOptions options)
        {
            CompareInfo ci = culture.CompareInfo;
            bool i = ci.IsPrefix(str1, str2, options);
            Assert.Equal(expected, i);
        }

        public void TestExc<T>(CultureInfo culture, string str1, string str2, CompareOptions options)
            where T : Exception
        {
            CompareInfo ci = culture.CompareInfo;
            Assert.Throws<T>(() =>
            {
                bool i = ci.IsPrefix(str1, str2, options);
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
