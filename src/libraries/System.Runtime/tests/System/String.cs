// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

public static unsafe class StringTests
{
    private const string c_SoftHyphen = "\u00AD";

    [Fact]
    public static void TestWithEmptyString()
    {
        Assert.True("".EndsWith(""));
        Assert.False("".EndsWith("Foo"));
    }

    [Fact]
    public static void TestLastIndexOfWithEmptyString()
    {
        string s = "Dill Guv Dill Guv Dill";
        int lastIndex = s.LastIndexOf("", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(s.Length - 1, lastIndex);
    }

    [Fact]
    public static void TestCtor()
    {
        string s = null;

        Assert.Null(s);

        s = "abc";

        Assert.NotNull(s);
    }

    [Fact]
    public static void TestCtorCharPtr()
    {
        char[] c = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' };
        fixed (char* pc = c)
        {
            String s = new String(pc);
            Assert.Equal("abcdefgh", s);
        }

        String e = new String((char*)null);
        Assert.Equal(String.Empty, e);
    }

    [Fact]
    public static void TestCtorCharPtrIntInt()
    {
        String s;
        char[] c = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' };
        fixed (char* pc = c)
        {
            s = new String(pc, 2, 3);
            Assert.Equal("cde", s);

            s = new String(pc, 0, 8);
            Assert.Equal("abcdefgh", s);

            try
            {
                s = new String(pc, -1, 8);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        String e = new String((char*)null, 0, 0);
        Assert.Equal(String.Empty, e);

        try
        {
            s = new String((char*)null, -1, -1);
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => new String((char*)null, 5, 1));
    }

    [Fact]
    public static void TestCtorCharInt()
    {
        String s;

        // Implementation unrolls copy 4 times.

        s = new String('a', 0);
        Assert.Equal(String.Empty, s);

        s = new String('a', 1);
        Assert.Equal("a", s);

        s = new String('a', 2);
        Assert.Equal("aa", s);

        s = new String('a', 3);
        Assert.Equal("aaa", s);

        s = new String('a', 4);
        Assert.Equal("aaaa", s);

        s = new String('a', 5);
        Assert.Equal("aaaaa", s);

        s = new String('a', 6);
        Assert.Equal("aaaaaa", s);

        s = new String('a', 7);
        Assert.Equal("aaaaaaa", s);

        s = new String('a', 8);
        Assert.Equal("aaaaaaaa", s);

        s = new String('a', 9);
        Assert.Equal("aaaaaaaaa", s);

        Assert.Throws<ArgumentOutOfRangeException>(() => s = new String('a', -1));
    }

    [Fact]
    public static void TestCtorCharArray()
    {
        String s;

        char[] c = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        s = new String(c);
        Assert.Equal("abcdefgh", s);

        s = new String((char[])null);
        Assert.Equal(String.Empty, s);
    }

    [Fact]
    public static void TestCtorCharArrayIntInt()
    {
        String s;

        char[] c = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        s = new String(c, 2, 3);
        Assert.Equal("cde", s);

        s = new String(c, 0, 8);
        Assert.Equal("abcdefgh", s);

        s = new String(c, 0, 0);
        Assert.Equal("", s);

        Assert.Throws<ArgumentOutOfRangeException>(() => s = new String(c, 0, 9));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = new String(c, -1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = new String(c, 5, -1));

        Assert.Throws<ArgumentNullException>(() => s = new String((char[])null, 0, 0));

        Assert.Throws<ArgumentNullException>(() => s = new String((char[])null, 3, 9));
    }

    [Fact]
    public static void TestLength()
    {
        string s = "abc";

        int len = s.Length;

        Assert.Equal(3, len);
    }

    [Fact]
    public static void TestConcatObjectOverloads()
    {
        Object one = 1;
        Object two = 2;
        Object nullAsObj = null;
        String s;

        s = String.Concat(nullAsObj);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one);
        Assert.Equal("1", s);

        s = String.Concat(nullAsObj, nullAsObj);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two);
        Assert.Equal("12", s);

        s = String.Concat(nullAsObj, nullAsObj, nullAsObj);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two, one);
        Assert.Equal("121", s);

        s = String.Concat(nullAsObj, nullAsObj, nullAsObj, nullAsObj);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two, one, one);
        Assert.Equal("1211", s);
    }

    [Fact]
    public static void TestConcatStringOverloads()
    {
        String one = "1";
        String two = "2";
        String nullAsString = null;
        String s;

        s = String.Concat(nullAsString);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one);
        Assert.Equal("1", s);

        s = String.Concat(nullAsString, nullAsString);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two);
        Assert.Equal("12", s);

        s = String.Concat(nullAsString, nullAsString, nullAsString);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two, one);
        Assert.Equal("121", s);

        s = String.Concat(nullAsString, nullAsString, nullAsString, nullAsString);
        Assert.Equal(String.Empty, s);

        s = String.Concat(one, two, one, one);
        Assert.Equal("1211", s);
    }

    [Fact]
    public static void TestCopyTo()
    {
        String s = "Hello";
        char[] dst = new char[10];
        s.CopyTo(1, dst, 5, 3);
        Assert.Equal(0, dst[0]);
        Assert.Equal(0, dst[1]);
        Assert.Equal(0, dst[2]);
        Assert.Equal(0, dst[3]);
        Assert.Equal(0, dst[4]);
        Assert.Equal('e', dst[5]);
        Assert.Equal('l', dst[6]);
        Assert.Equal('l', dst[7]);
        Assert.Equal(0, dst[8]);
        Assert.Equal(0, dst[9]);

        Assert.Throws<ArgumentNullException>(() => s.CopyTo(0, null, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => s.CopyTo(-1, dst, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => s.CopyTo(0, dst, -1, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => s.CopyTo(0, dst, 0, 6));
    }

    [Fact]
    public static void TestCompare()
    {
        String.Compare("A", "B");

        Assert.Equal<int>(0, String.Compare("Hello", "Hello", StringComparison.Ordinal));
        Assert.Equal<int>(0, String.Compare("HELLO", "hello", StringComparison.OrdinalIgnoreCase));

        Assert.Equal<int>(0, String.Compare("Hello", 2, "Hello", 2, 3));

        Assert.Equal<int>(0, String.Compare("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal));
        Assert.Equal<int>(0, String.Compare("HELLO", 2, "hello", 2, 3, StringComparison.OrdinalIgnoreCase));

        Assert.Equal<int>(0, String.Compare("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture));
        Assert.Equal<int>(0, String.Compare("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase));

        Assert.Equal<int>(0, String.Compare("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture));
        Assert.Equal<int>(0, String.Compare("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase));

        int i;

        i = String.Compare("HELLO", 2, "Hello", 2, 3, StringComparison.Ordinal);
        Assert.True(i < 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.Ordinal);
        Assert.True(i > 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, i);

        i = String.Compare("Hello", 2, "Goodbye", 2, 3, StringComparison.OrdinalIgnoreCase);
        Assert.True(i < 0);

        i = String.Compare("HELLO", 2, "Hello", 2, 3, StringComparison.CurrentCulture);
        Assert.True(i > 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture);
        Assert.True(i <= 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(0, i);

        i = String.Compare("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.True(i < 0);

        i = String.Compare("HELLO", 2, "Hello", 2, 3, StringComparison.CurrentCulture);
        Assert.True(i > 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture);
        Assert.True(i < 0);

        i = String.Compare("Hello", 2, "HELLO", 2, 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(0, i);

        i = String.Compare("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.True(i < 0);
    }

    [Fact]
    public static void TestCompareOrdinal()
    {
        char[] c = { 'H', 'e', 'l', 'l', 'o' };
        int i;
        i = String.CompareOrdinal(new String(c), new String(c));
        Assert.Equal(0, i);

        i = String.CompareOrdinal("Hello", "Goodbye");
        Assert.True(i > 0);
    }

    [Theory]
    [InlineData(null, -1, null, -1, -1, 0)]
    [InlineData("Hello", -1, null, -1, -1, 1)]
    [InlineData(null, -1, "Hello", -1, -1, -1)]
    [InlineData("Hello", 0, "Hello", 0, 0, 0)]
    [InlineData("Hello", 0, "Hello", 0, 5, 0)]
    [InlineData("Hello", 0, "Hello", 0, 3, 0)]
    [InlineData("Hello", 2, "Hello", 2, 3, 0)]
    [InlineData("Hello", 0, "He" + c_SoftHyphen + "llo", 0, 5, -1)]
    [InlineData("Hello", 0, "-=<Hello>=-", 3, 5, 0)]
    [InlineData("\uD83D\uDD53Hello\uD83D\uDD50", 1, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, 0)] // Surrogate split
    [InlineData("Hello", 0, "Hello123", 0, int.MaxValue, -1)]           // Recalculated length, second string longer
    [InlineData("Hello123", 0, "Hello", 0, int.MaxValue, 1)]            // Recalculated length, first string longer
    [InlineData("---aaaaaaaaaaa", 3, "+++aaaaaaaaaaa", 3, 100, 0)]      // Equal long alignment 2, equal compare
    [InlineData("aaaaaaaaaaaaaa", 3, "aaaxaaaaaaaaaa", 3, 100, -1)]     // Equal long alignment 2, different compare at n=1
    [InlineData("-aaaaaaaaaaaaa", 1, "+aaaaaaaaaaaaa", 1, 100, 0)]      // Equal long alignment 6, equal compare
    [InlineData("aaaaaaaaaaaaaa", 1, "axaaaaaaaaaaaa", 1, 100, -1)]     // Equal long alignment 6, different compare at n=1
    [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaaa", 0, 100, 0)]      // Equal long alignment 4, equal compare
    [InlineData("aaaaaaaaaaaaaa", 0, "xaaaaaaaaaaaaa", 0, 100, -1)]     // Equal long alignment 4, different compare at n=1
    [InlineData("aaaaaaaaaaaaaa", 0, "axaaaaaaaaaaaa", 0, 100, -1)]     // Equal long alignment 4, different compare at n=2
    [InlineData("--aaaaaaaaaaaa", 2, "++aaaaaaaaaaaa", 2, 100, 0)]      // Equal long alignment 0, equal compare
    [InlineData("aaaaaaaaaaaaaa", 2, "aaxaaaaaaaaaaa", 2, 100, -1)]     // Equal long alignment 0, different compare at n=1
    [InlineData("aaaaaaaaaaaaaa", 2, "aaaxaaaaaaaaaa", 2, 100, -1)]     // Equal long alignment 0, different compare at n=2
    [InlineData("aaaaaaaaaaaaaa", 2, "aaaaxaaaaaaaaa", 2, 100, -1)]     // Equal long alignment 0, different compare at n=3
    [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaxaaaaaaaa", 2, 100, -1)]     // Equal long alignment 0, different compare at n=4
    [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaaxaaaaaaa", 2, 100, -1)]     // Equal long alignment 0, different compare at n=5
    [InlineData("aaaaaaaaaaaaaa", 0, "+aaaaaaaaaaaaa", 1, 13, 0)]       // Different int alignment, equal compare
    [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaax", 1, 100, -1)]     // Different int alignment
    [InlineData("aaaaaaaaaaaaaa", 1, "aaaxaaaaaaaaaa", 3, 100, -1)]     // Different long alignment, abs of 4, one of them is 2, different at n=1
    [InlineData("-aaaaaaaaaaaaa", 1, "++++aaaaaaaaaa", 4, 10, 0)]       // Different long alignment, equal compare
    [InlineData("aaaaaaaaaaaaaa", 1, "aaaaaaaaaaaaax", 4, 100, -1)]     // Different long alignment
    public static void TestCompareOrdinalIndexed(string strA, int indexA, string strB, int indexB, int length, int expectedResult)
    {
        int result = String.CompareOrdinal(strA, indexA, strB, indexB, length);
        result = Math.Max(-1, Math.Min(1, result));

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public static void TestCompareOrdinalIndexedInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => String.CompareOrdinal("Hello", -1, "Hello", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => String.CompareOrdinal("Hello", 0, "Hello", -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => String.CompareOrdinal("Hello", 0, "Hello", 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => String.CompareOrdinal("Hello", 6, "Hello", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => String.CompareOrdinal("Hello", 0, "Hello", 6, 1));
    }

    [Fact]
    public static void TestCompareTo()
    {
        int i;
        String s = "Hello";
        i = s.CompareTo("Hello");
        Assert.Equal(0, i);
        i = s.CompareTo("Goodbye");
        Assert.True(i > 0);
    }

    [Fact]
    public static void TestContains()
    {
        String s = "Hello";
        bool b;
        b = s.Contains("ell");
        Assert.True(b);
        b = s.Contains("ELL");
        Assert.False(b);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s.Contains(null);
            });
    }

    [Theory]
    [InlineData(StringComparison.CurrentCulture, "Hello", "llo", true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Hello", true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "", true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "HELLO", false)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Abc", false)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "llo" + c_SoftHyphen, true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "llo", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "Hello", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "LLO", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "Abc", false)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "llo" + c_SoftHyphen, true)]
    [InlineData(StringComparison.Ordinal, "Hello", "o", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "llo", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "Hello", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "Larger Hello", false)]
    [InlineData(StringComparison.Ordinal, "Hello", "", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "LLO", false)]
    [InlineData(StringComparison.Ordinal, "Hello", "Abc", false)]
    [InlineData(StringComparison.Ordinal, "Hello", "llo" + c_SoftHyphen, false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "llo", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Hello", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Larger Hello", false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "LLO", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Abc", false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "llo" + c_SoftHyphen, false)]
    public static void TestEndsWith(StringComparison comparisonType, string text, string value, bool expected)
    {
        if (comparisonType == StringComparison.CurrentCulture)
        {
            Assert.Equal(expected, text.EndsWith(value));
        }
        Assert.Equal(expected, text.EndsWith(value, comparisonType));
    }

    [Fact]
    public static void TestEndsWithInvalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentException>(() => s.EndsWith("o", (StringComparison.CurrentCulture - 1)));
        Assert.Throws<ArgumentException>(() => s.EndsWith("o", (StringComparison.OrdinalIgnoreCase + 1)));
        Assert.Throws<ArgumentNullException>(() => s.EndsWith(null));
        Assert.Throws<ArgumentNullException>(() => s.EndsWith(null, StringComparison.CurrentCultureIgnoreCase));
        Assert.Throws<ArgumentNullException>(() => s.EndsWith(null, StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>(() => s.EndsWith(null, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public static void TestEnumerator()
    {
        IEnumerable ie = (IEnumerable)"abc";
        IEnumerator e = ie.GetEnumerator();
        char c;
        bool b;
        b = e.MoveNext();
        Assert.True(b);
        c = (char)e.Current;
        Assert.Equal('a', c);

        b = e.MoveNext();
        Assert.True(b);
        c = (char)e.Current;
        Assert.Equal('b', c);

        b = e.MoveNext();
        Assert.True(b);
        c = (char)e.Current;
        Assert.Equal('c', c);

        b = e.MoveNext();
        Assert.False(b);
    }

    [Fact]
    public static void TestEquals()
    {
        char[] hello = { 'H', 'e', 'l', 'l', 'o' };
        String sHello = new String(hello);
        Object oHello = new String(hello);

        bool b;
        b = "Hello".Equals(oHello);
        Assert.True(b);
        b = "Hello".Equals((Object)"hello");
        Assert.False(b);
        b = "Hello".Equals((Object)null);
        Assert.False(b);

        b = "Hello".Equals(sHello);
        Assert.True(b);
        b = "Hello".Equals((String)"hello");
        Assert.False(b);
        b = "Hello".Equals((String)null);
        Assert.False(b);

        b = "Hello".Equals(sHello, StringComparison.Ordinal);
        Assert.True(b);
        b = "Hello".Equals((String)"hello", StringComparison.Ordinal);
        Assert.False(b);
        b = "Hello".Equals((String)null, StringComparison.Ordinal);
        Assert.False(b);

        b = "Hello".Equals(sHello, StringComparison.OrdinalIgnoreCase);
        Assert.True(b);
        b = "Hello".Equals((String)"hello", StringComparison.OrdinalIgnoreCase);
        Assert.True(b);
        b = "Hello".Equals((String)null, StringComparison.OrdinalIgnoreCase);
        Assert.False(b);

        b = String.Equals("Hello", sHello);
        Assert.True(b);

        b = String.Equals("Hello", "hello");
        Assert.False(b);

        b = String.Equals(null, null);
        Assert.True(b);

        b = String.Equals("Hello", null);
        Assert.False(b);

        b = String.Equals(null, "Hello");
        Assert.False(b);

        b = String.Equals("Hello", sHello, StringComparison.Ordinal);
        Assert.True(b);

        b = String.Equals("Hello", "hello", StringComparison.Ordinal);
        Assert.False(b);

        b = String.Equals(null, null, StringComparison.Ordinal);
        Assert.True(b);

        b = String.Equals("Hello", null, StringComparison.Ordinal);
        Assert.False(b);

        b = String.Equals(null, "Hello", StringComparison.Ordinal);
        Assert.False(b);

        b = String.Equals("Hello", sHello, StringComparison.OrdinalIgnoreCase);
        Assert.True(b);

        b = String.Equals("Hello", "hello", StringComparison.OrdinalIgnoreCase);
        Assert.True(b);

        b = String.Equals(null, null, StringComparison.OrdinalIgnoreCase);
        Assert.True(b);

        b = String.Equals("Hello", null, StringComparison.OrdinalIgnoreCase);
        Assert.False(b);

        b = String.Equals(null, "Hello", StringComparison.OrdinalIgnoreCase);
        Assert.False(b);
    }

    [Fact]
    public static void TestFormat()
    {
        String s;
        s = String.Format(null, "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", "zero", "one", "two", "three", "four");
        Assert.Equal("0 = zero 1 = one 2 = two 3 = three 4 = four", s);

        TestFormatter testFormatter = new TestFormatter();
        s = String.Format(testFormatter, "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", "zero", "one", "two", "three", "four");
        Assert.Equal("0 = Test: : zero 1 = Test: : one 2 = Test: : two 3 = Test: : three 4 = Test: : four", s);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s = String.Format(testFormatter, null, 0, 1, 2, 3, 4);
            });

        Assert.Throws<FormatException>(
            delegate ()
            {
                s = String.Format(testFormatter, "Missing={5}", 0, 1, 2, 3, 4);
            });
    }

    private class TestFormatter : IFormatProvider, ICustomFormatter
    {
        public Object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            return null;
        }

        public String Format(String format, Object arg, IFormatProvider formatProvider)
        {
            return "Test: " + format + ": " + arg;
        }
    }

    [Fact]
    public static void TestGetHashCode()
    {
        int h1 = "Hello".GetHashCode();
        int h2 = (new String(new char[] { 'H', 'e', 'l', 'l', 'o' })).GetHashCode();
        Assert.Equal(h1, h2);
    }

    [Theory]
    [InlineData("Hello", 'l', 0, 5, 2)]
    [InlineData("Hello", 'x', 0, 5, -1)]
    [InlineData("Hello", 'l', 1, 4, 2)]
    [InlineData("Hello", 'l', 3, 2, 3)]
    [InlineData("Hello", 'l', 4, 1, -1)]
    [InlineData("Hello", 'x', 1, 4, -1)]
    [InlineData("Hello", 'l', 3, 0, -1)]
    [InlineData("Hello", 'l', 0, 2, -1)]
    [InlineData("Hello", 'l', 0, 3, 2)]
    [InlineData("Hello", 'l', 4, 1, -1)]
    [InlineData("Hello", 'x', 1, 4, -1)]
    public static void TestIndexOf_SingleLetter(string source, char target, int startIndex, int count, int expectedResult)
    {
        if (count == source.Length - startIndex)
        {
            if (startIndex == 0)
            {
                Assert.Equal(expectedResult, source.IndexOf(target));
                Assert.Equal(expectedResult, source.IndexOf(target.ToString()));
            }
            Assert.Equal(expectedResult, source.IndexOf(target, startIndex));
            Assert.Equal(expectedResult, source.IndexOf(target.ToString(), startIndex));
        }
        Assert.Equal(expectedResult, source.IndexOf(target, startIndex, count));
        Assert.Equal(expectedResult, source.IndexOf(target.ToString(), startIndex, count));

        Assert.Equal(expectedResult, source.IndexOf(target.ToString(), startIndex, count, StringComparison.CurrentCulture));
        Assert.Equal(expectedResult, source.IndexOf(target.ToString(), startIndex, count, StringComparison.Ordinal));
        Assert.Equal(expectedResult, source.IndexOf(target.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData("AllSubstringsAndComparisons", new object[] { "abcde" })]
    public static void TestIndexOf_AllSubstrings(string source, string substring, int i, StringComparison comparison)
    {
        bool ignoringCase =
            comparison == StringComparison.OrdinalIgnoreCase ||
            comparison == StringComparison.CurrentCultureIgnoreCase;

        // First find the substring.  We should be able to with all comparison types.
        Assert.Equal(i, source.IndexOf(substring, comparison)); // in the whole string
        Assert.Equal(i, source.IndexOf(substring, i, comparison)); // starting at substring
        if (i > 0)
        {
            Assert.Equal(i, source.IndexOf(substring, i - 1, comparison)); // starting just before substring
        }
        Assert.Equal(-1, source.IndexOf(substring, i + 1, comparison)); // starting just after start of substring

        // Shouldn't be able to find the substring if the count is less than substring's length
        Assert.Equal(-1, source.IndexOf(substring, 0, substring.Length - 1, comparison));

        // Now double the source.  Make sure we find the first copy of the substring.
        int halfLen = source.Length;
        source += source;
        Assert.Equal(i, source.IndexOf(substring, comparison));

        // Now change the case of a letter.
        source = source.ToUpperInvariant();
        Assert.Equal(
            ignoringCase ? i : -1,
            source.IndexOf(substring, comparison));
    }

    [Fact]
    public static void TestIndexOf_TurkishI()
    {
        string source = "Turkish I \u0131s TROUBL\u0130NG!";
        WithCulture(new CultureInfo("tr-TR"), () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.IndexOf(target));
            Assert.Equal(19, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(4, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(19, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(19, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(10, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(10, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.IndexOf(target));
            Assert.Equal(19, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(19, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
        WithCulture(new CultureInfo("en-US"), () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.IndexOf(target));
            Assert.Equal(19, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(19, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
    }

    [Fact]
    public static void TestLastIndexOf_TurkishI()
    {
        string source = "Turkish I \u0131s TROUBL\u0130NG!";
        WithCulture(new CultureInfo("tr-TR"), () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.LastIndexOf(target));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.Ordinal));
            Assert.Equal(19, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.Ordinal));
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.LastIndexOf(target));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
        WithCulture(new CultureInfo("en-US"), () =>
        {
            string target = "\u0130";
            Assert.Equal(19, source.LastIndexOf(target));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(19, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));

            target = "\u0131";
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(10, source.LastIndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
    }

    [Fact]
    public static void TestIndexOf_HungarianDoubleCompression()
    {
        string source = "dzsdzs";
        string target = "ddzs";
        WithCulture(new CultureInfo("hu-HU"), () =>
        {
            // TODO: [ActiveIssue(3972)]

            //Assert.Equal(0, source.IndexOf(target));
            //Assert.Equal(0, source.IndexOf(target, StringComparison.CurrentCulture));

            Assert.Equal(0, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(-1, source.IndexOf(target));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
    }

    [Fact]
    public static void TestIndexOf_EquivalentDiacritics()
    {
        string source = "Exhibit a\u0300\u00C0";
        string target = "\u00C0";
        WithCulture(new CultureInfo("en-US"), () =>
        {
            Assert.Equal(10, source.IndexOf(target));
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(10, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(10, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(10, source.IndexOf(target));
            Assert.Equal(10, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
        
        target = "\u0300";
        WithCulture(new CultureInfo("en-US"), () =>
        {
            // TODO: [ActiveIssue(3973)]

            //Assert.Equal(9, source.IndexOf(target));
            //Assert.Equal(9, source.IndexOf(target, StringComparison.CurrentCulture));
            //Assert.Equal(9, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(9, source.IndexOf(target, StringComparison.Ordinal));
            //Assert.Equal(9, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            //Assert.Equal(9, source.IndexOf(target));
            //Assert.Equal(9, source.IndexOf(target, StringComparison.CurrentCulture));
            //Assert.Equal(9, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
    }

    [Fact]
    public static void TestIndexOf_CyrillicE()
    {
        string source = "Foo\u0400Bar";
        string target = "\u0400";
        WithCulture(new CultureInfo("en-US"), () =>
        {
            Assert.Equal(3, source.IndexOf(target));
            Assert.Equal(3, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(3, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(3, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(3, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(3, source.IndexOf(target));
            Assert.Equal(3, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(3, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });

        target = "bar";
        WithCulture(new CultureInfo("en-US"), () =>
        {
            Assert.Equal(-1, source.IndexOf(target));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(4, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(4, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(-1, source.IndexOf(target));
            Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(4, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
        });
    }

    [Fact]
    public static void TestIndexOfAny()
    {
        int i;

        i = "Hello".IndexOfAny(new char[] { 'd', 'e', 'f' }, 0, 3);
        Assert.Equal(1, i);

        i = "Hello".IndexOfAny(new char[] { 'a', 'b', 'c' }, 0, 3);
        Assert.Equal(-1, i);
    }

    [Fact]
    public static void TestInsert()
    {
        String s;

        s = "Hello".Insert(0, "!$%");
        Assert.Equal("!$%Hello", s);

        s = "Hello".Insert(1, "!$%");
        Assert.Equal("H!$%ello", s);

        s = "Hello".Insert(3, "!$%");
        Assert.Equal("Hel!$%lo", s);

        s = "Hello".Insert(5, "!$%");
        Assert.Equal("Hello!$%", s);

        s = "Hello".Insert(3, "");
        Assert.Equal("Hello", s);

        Assert.Throws<ArgumentNullException>(() => s = "Hello".Insert(Int32.MaxValue, null));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Insert(6, "!"));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Insert(-1, "!"));

        return;
    }

    [Fact]
    public static void TestIsNullOr()
    {
        bool b;

        b = String.IsNullOrEmpty(null);
        Assert.True(b);

        b = String.IsNullOrEmpty("");
        Assert.True(b);

        b = String.IsNullOrEmpty("Foo");
        Assert.False(b);

        b = String.IsNullOrWhiteSpace(null);
        Assert.True(b);

        b = String.IsNullOrWhiteSpace("");
        Assert.True(b);

        b = String.IsNullOrWhiteSpace(" \t");
        Assert.True(b);

        b = String.IsNullOrWhiteSpace(" \tX");
        Assert.False(b);
    }

    [Fact]
    public static void TestJoin()
    {
        String s;

        // String Array
        s = String.Join("$$", new String[] { }, 0, 0);
        Assert.Equal("", s);

        s = String.Join("$$", new String[] { null }, 0, 1);
        Assert.Equal("", s);

        s = String.Join("$$", new String[] { null, "Bar", null }, 0, 3);
        Assert.Equal("$$Bar$$", s);

        s = String.Join(null, new String[] { "Foo", "Bar", "Baz" }, 0, 3);
        Assert.Equal("FooBarBaz", s);

        s = String.Join("$$", new String[] { "Foo", "Bar", "Baz" }, 0, 3);
        Assert.Equal("Foo$$Bar$$Baz", s);

        s = String.Join("$$", new String[] { "Foo", "Bar", "Baz" }, 3, 0);
        Assert.Equal("", s);

        s = String.Join("$$", new String[] { "Foo", "Bar", "Baz" }, 1, 1);
        Assert.Equal("Bar", s);

        s = String.Join("$$", new String[] { "Red", "Green", "Blue" });
        Assert.Equal("Red$$Green$$Blue", s);

        Assert.Throws<ArgumentNullException>(() => s = String.Join("$$", (String[])null));
        Assert.Throws<ArgumentNullException>(() => s = String.Join("$$", (String[])null, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => s = String.Join("$$", new String[] { "Foo" }, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => s = String.Join("$$", new String[] { "Foo" }, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s = String.Join("$$", new String[] { "Foo" }, 0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => s = String.Join("$$", new String[] { "Foo" }, 2, 1));

        // Object Array
        s = String.Join("@@", new object[] { });
        Assert.Equal("", s);

        s = String.Join("@@", new object[] { "Red" });
        Assert.Equal("Red", s);

        s = String.Join("@@", new object[] { "Red", "Green", "Blue" });
        Assert.Equal("Red@@Green@@Blue", s);

        s = String.Join(null, new object[] { "Red", "Green", "Blue" });
        Assert.Equal("RedGreenBlue", s);

        s = String.Join("@@", new object[] { null, "Green", "Blue" }); // Feature of object[] overload to exit if [0] is null
        Assert.Equal("", s);

        s = String.Join("@@", new object[] { "Red", null, "Blue" });
        Assert.Equal("Red@@@@Blue", s);

        Assert.Throws<ArgumentNullException>(() => s = String.Join("@@", (Object[])null));

        // IEnumerable<String> with IList optimization
        s = String.Join("|", new List<string>() { });
        Assert.Equal("", s);

        s = String.Join("|", new List<string>() { null });
        Assert.Equal("", s);

        s = String.Join("|", new List<string>() { "Red" });
        Assert.Equal("Red", s);

        s = String.Join(null, new List<string>() { "Red", "Green", "Blue" });
        Assert.Equal("RedGreenBlue", s);

        s = String.Join("|", new List<string>() { "Red", "Green", "Blue" });
        Assert.Equal("Red|Green|Blue", s);

        s = String.Join("|", new List<string>() { null, "Green", null });
        Assert.Equal("|Green|", s);

        // IEnumerable<String> *without* IList optimization
        Queue<string> values = new Queue<string>();
        s = String.Join("|", values);
        Assert.Equal("", s);

        values.Enqueue(null);
        s = String.Join("|", values);
        Assert.Equal("", s);

        values.Clear();
        values.Enqueue("Red");
        s = String.Join("|", values);
        Assert.Equal("Red", s);

        values.Clear();
        values.Enqueue("Red");
        values.Enqueue("Green");
        values.Enqueue("Blue");
        s = String.Join(null, values);
        Assert.Equal("RedGreenBlue", s);
        s = String.Join("|", values);
        Assert.Equal("Red|Green|Blue", s);

        values.Clear();
        values.Enqueue(null);
        values.Enqueue("Green");
        values.Enqueue(null);
        s = String.Join("|", new List<string>() { null, "Green", null });
        Assert.Equal("|Green|", s);

        Assert.Throws<ArgumentNullException>(() => s = String.Join("|", (IEnumerable<String>)null));

        // IEnumerable<Object>
        s = String.Join("--", new List<Object>() { });
        Assert.Equal("", s);

        s = String.Join("--", new List<Object>() { null });
        Assert.Equal("", s);

        s = String.Join("--", new List<Object>() { "Red" });
        Assert.Equal("Red", s);

        s = String.Join(null, new List<Object>() { "Red", "Green", "Blue" });
        Assert.Equal("RedGreenBlue", s);

        s = String.Join("--", new List<Object>() { "Red", "Green", "Blue" });
        Assert.Equal("Red--Green--Blue", s);

        s = String.Join("--", new List<Object>() { null, "Green", null });
        Assert.Equal("--Green--", s);

        Assert.Throws<ArgumentNullException>(() => s = String.Join("--", (IEnumerable<Object>)null));
    }

    [Theory]
    [InlineData("Hello", 'l', 4, 5, 3)]
    [InlineData("Hello", 'x', 4, 5, -1)]
    [InlineData("Hello", 'l', 3, 4, 3)]
    [InlineData("Hello", 'l', 1, 2, -1)]
    [InlineData("Hello", 'l', 0, 1, -1)]
    [InlineData("Hello", 'x', 3, 4, -1)]
    [InlineData("Hello", 'l', 3, 4, 3)]
    [InlineData("Hello", 'l', 1, 2, -1)]
    [InlineData("Hello", 'l', 1, 0, -1)]
    [InlineData("Hello", 'l', 4, 2, 3)]
    [InlineData("Hello", 'l', 4, 3, 3)]
    [InlineData("Hello", 'l', 0, 1, -1)]
    [InlineData("Hello", 'x', 3, 4, -1)]
    public static void TestLastIndexOf_SingleLetter(string source, char target, int startIndex, int count, int expectedResult)
    {
        if (count == source.Length)
        {
            if (startIndex == source.Length - 1)
            {
                Assert.Equal(expectedResult, source.LastIndexOf(target));
                Assert.Equal(expectedResult, source.LastIndexOf(target.ToString()));
            }
            Assert.Equal(expectedResult, source.LastIndexOf(target, startIndex));
            Assert.Equal(expectedResult, source.LastIndexOf(target.ToString(), startIndex));
        }
        Assert.Equal(expectedResult, source.LastIndexOf(target, startIndex, count));
        Assert.Equal(expectedResult, source.LastIndexOf(target.ToString(), startIndex, count));

        Assert.Equal(expectedResult, source.LastIndexOf(target.ToString(), startIndex, count, StringComparison.CurrentCulture));
        Assert.Equal(expectedResult, source.LastIndexOf(target.ToString(), startIndex, count, StringComparison.Ordinal));
        Assert.Equal(expectedResult, source.LastIndexOf(target.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData("AllSubstringsAndComparisons", new object[] { "abcde" })]
    public static void TestLastIndexOf_AllSubstrings(string source, string substring, int i, StringComparison comparison)
    {
        bool ignoringCase =
            comparison == StringComparison.OrdinalIgnoreCase ||
            comparison == StringComparison.CurrentCultureIgnoreCase;

        // First find the substring.  We should be able to with all comparison types.
        Assert.Equal(i, source.LastIndexOf(substring, comparison)); // in the whole string
        Assert.Equal(i, source.LastIndexOf(substring, i + substring.Length - 1, comparison)); // starting at end of substring
        Assert.Equal(i, source.LastIndexOf(substring, i + substring.Length, comparison)); // starting just beyond end of substring
        if (i + substring.Length < source.Length)
        {
            Assert.Equal(i, source.LastIndexOf(substring, i + substring.Length + 1, comparison)); // starting a bit more beyond end of substring
        }
        if (i + substring.Length > 1)
        {
            Assert.Equal(-1, source.LastIndexOf(substring, i + substring.Length - 2, comparison)); // starting before end of substring
        }

        // Shouldn't be able to find the substring if the count is less than substring's length
        Assert.Equal(-1, source.LastIndexOf(substring, source.Length - 1, substring.Length - 1, comparison));

        // Now double the source.  Make sure we find the second copy of the substring.
        int halfLen = source.Length;
        source += source;
        Assert.Equal(halfLen + i, source.LastIndexOf(substring, comparison));

        // Now change the case of a letter.
        source = source.ToUpperInvariant();
        Assert.Equal(
            ignoringCase ? halfLen + i : -1,
            source.LastIndexOf(substring, comparison));
    }

    [Fact]
    public static void TestLastIndexOfAny()
    {
        int i;

        i = "Hello".LastIndexOfAny(new char[] { 'd', 'e', 'f' }, 2, 3);
        Assert.Equal(1, i);

        i = "Hello".LastIndexOfAny(new char[] { 'a', 'b', 'c' }, 2, 3);
        Assert.Equal(-1, i);
    }

    [Fact]
    public static void TestOperators()
    {
        bool b;

        String s1 = new String(new char[] { 'a' });
        String s1a = new String(new char[] { 'a' });
        String s2 = new String(new char[] { 'b' });

        b = (s1 == s1a);
        Assert.True(b);

        b = (s1 == s2);
        Assert.False(b);

        b = (s1 != s1a);
        Assert.False(b);

        b = (s1 != s2);
        Assert.True(b);
    }

    [Fact]
    public static void TestPad()
    {
        String s;

        s = "Hello".PadLeft(5);
        Assert.Equal("Hello", s);

        s = "Hello".PadLeft(7);
        Assert.Equal("  Hello", s);

        s = "Hello".PadLeft(7, '.');
        Assert.Equal("..Hello", s);

        s = "".PadLeft(0, 'X');
        Assert.Equal("", s);

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "".PadLeft(-1, '.'));

        s = "Hello".PadRight(5);
        Assert.Equal("Hello", s);

        s = "Hello".PadRight(7);
        Assert.Equal("Hello  ", s);

        s = "Hello".PadRight(7, '.');
        Assert.Equal("Hello..", s);

        s = "".PadRight(0, 'X');
        Assert.Equal("", s);

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "".PadRight(-1, '.'));

        return;
    }

    [Fact]
    public static void TestRemove()
    {
        String s;

        s = "Hello".Remove(2);
        Assert.Equal("He", s);

        s = "Hello".Remove(1, 2);
        Assert.Equal("Hlo", s);

        s = "Hello".Remove(0, 5);
        Assert.Equal("", s);

        s = "Hello".Remove(5, 0);
        Assert.Equal("Hello", s);

        s = "".Remove(0, 0);
        Assert.Equal("", s);

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Remove(0, Int32.MaxValue));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Remove(Int32.MaxValue, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Remove(0, 6));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "Hello".Remove(5, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => s = "".Remove(Int32.MaxValue, Int32.MaxValue));
    }

    [Fact]
    public static void TestReplaceChar()
    {
        String s = "Hello";
        String s1 = s.Replace('l', '!');
        Assert.Equal(5, s1.Length);
        Assert.Equal("He!!o", s1);
    }

    [Fact]
    public static void TestReplaceString()
    {
        String s = "Hello";
        String s1 = s.Replace("ll", "!!!!");
        Assert.Equal(7, s1.Length);
        Assert.Equal("He!!!!o", s1);

        s = "11111";
        s1 = s.Replace("1", "23");
        Assert.Equal("2323232323", s1);

        s = "111111";
        s1 = s.Replace("111", "23");
        Assert.Equal("2323", s1);

        s = "1111111";
        s1 = s.Replace("111", "23");
        Assert.Equal("23231", s1);

        s = "11111111";
        s1 = s.Replace("111", "23");
        Assert.Equal("232311", s1);

        s = "111111111";
        s1 = s.Replace("111", "23");
        Assert.Equal("232323", s1);

        s = "A1B1C1D1E1F";
        s1 = s.Replace("1", "23");
        Assert.Equal("A23B23C23D23E23F", s1);

        s = "Aa1Bbb1Cccc1Ddddd1Eeeeee1Fffffff";
        s1 = s.Replace("1", "23");
        Assert.Equal("Aa23Bbb23Cccc23Ddddd23Eeeeee23Fffffff", s1);

        // (Perf test: If nothing is replaced, don't waste an allocation on a new string.)
        s = "XYZ";
        s1 = s.Replace("1", "2");
        Assert.True(Object.ReferenceEquals(s, s1));

        // (Perf test: If nothing is replaced, don't waste an allocation on a new string.)
        s = "";
        s1 = s.Replace("1", "2");
        Assert.True(Object.ReferenceEquals(s, s1));

        // Make sure it can handle the maximum possible # of matches.
        s = "11111111111111111111111";
        s1 = s.Replace("1", "11");
        Assert.Equal("1111111111111111111111111111111111111111111111", s1);

        // Make sure it can handle the maximum possible # of matches.
        s = "11111111111111111111111";
        s1 = s.Replace("1", "");
        Assert.Equal("", s1);

        s = "abcdefghijkl";
        s1 = s.Replace("cdef", "12345");
        Assert.Equal("ab12345ghijkl", s1);

        // Cannot pass null for "oldValue"
        s = "Hello";
        Assert.Throws<ArgumentNullException>(() => s.Replace(null, "l"));

        // Cannot pass empty string for "oldValue"
        s = "Hello";
        Assert.Throws<ArgumentException>(() => s.Replace("", "l"));

        // null is a "valid" input for newValue (equivalent to "")
        s = "Hello";
        s1 = s.Replace("l", null);
        Assert.Equal("Heo", s1);

        return;
    }

    [Theory]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Hel",  true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Hello", true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "", true)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "HELLO", false)]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Abc", false)]
    [InlineData(StringComparison.CurrentCulture, "Hello", c_SoftHyphen + "Hel", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "Hel", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "Hello", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "HEL", true)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", "Abc", false)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase, "Hello", c_SoftHyphen + "Hel", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "H", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "Hel", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "Hello", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "Hello Larger", false)]
    [InlineData(StringComparison.Ordinal, "Hello", "", true)]
    [InlineData(StringComparison.Ordinal, "Hello", "HEL", false)]
    [InlineData(StringComparison.Ordinal, "Hello", "Abc", false)]
    [InlineData(StringComparison.Ordinal, "Hello", c_SoftHyphen + "Hel", false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Hel", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Hello", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Hello Larger", false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "HEL", true)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", "Abc", false)]
    [InlineData(StringComparison.OrdinalIgnoreCase, "Hello", c_SoftHyphen + "Hel", false)]
    public static void TestStartsWith(StringComparison comparisonType, string text, string value, bool expected)
    {
        if (comparisonType == StringComparison.CurrentCulture)
        {
            Assert.Equal(expected, text.StartsWith(value));
        }
        Assert.Equal(expected, text.StartsWith(value, comparisonType));
    }

    [Fact]
    public static void TestStartsWithInvalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentException>(() => s.StartsWith("H", (StringComparison.CurrentCulture - 1)));
        Assert.Throws<ArgumentException>(() => s.StartsWith("H", (StringComparison.OrdinalIgnoreCase + 1)));
        Assert.Throws<ArgumentNullException>(() => s.StartsWith(null));
        Assert.Throws<ArgumentNullException>(() => s.StartsWith(null, StringComparison.CurrentCultureIgnoreCase));
        Assert.Throws<ArgumentNullException>(() => s.StartsWith(null, StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>(() => s.StartsWith(null, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public static void TestSubstring()
    {
        String s;

        s = "Hello".Substring(2);
        Assert.Equal("llo", s);

        s = "Hello".Substring(0);
        Assert.Equal("Hello", s);

        s = "Hello".Substring(5);
        Assert.Equal("", s);

        s = "Hello".Substring(2, 3);
        Assert.Equal("llo", s);

        s = "Hello".Substring(0, 3);
        Assert.Equal("Hel", s);

        s = "Hello".Substring(0, 5);
        Assert.Equal("Hello", s);

        s = "Hello".Substring(5, 0);
        Assert.Equal("", s);
    }

    [Fact]
    public static void TestToCharArray()
    {
        char[] c;

        c = "Hello".ToCharArray();
        Assert.Equal(5, c.Length);
        Assert.Equal('H', c[0]);
        Assert.Equal('e', c[1]);
        Assert.Equal('l', c[2]);
        Assert.Equal('l', c[3]);
        Assert.Equal('o', c[4]);

        c = "".ToCharArray();
        Assert.Equal(0, c.Length);

        c = "Hello".ToCharArray(2, 3);
        Assert.Equal(3, c.Length);
        Assert.Equal('l', c[0]);
        Assert.Equal('l', c[1]);
        Assert.Equal('o', c[2]);

        c = "Hello".ToCharArray(0, 5);
        Assert.Equal('H', c[0]);
        Assert.Equal('e', c[1]);
        Assert.Equal('l', c[2]);
        Assert.Equal('l', c[3]);
        Assert.Equal('o', c[4]);

        c = "Hello".ToCharArray(5, 0);
        Assert.Equal(0, c.Length);
    }

    [Fact]
    public static void TestToLowerToUpper_Basic()
    {
        //@todo: Add tests for ToLower/Upper(CultureInfo) when we have better support for
        // getting more than just the invariant culture. In any case, ToLower() and ToUpper()
        // call ToLower/Upper(CultureInfo) internally.
        String s;

        s = "HELLO".ToLower();
        Assert.Equal("hello", s);

        s = "HELLO".ToLowerInvariant();
        Assert.Equal("hello", s);

        s = "hello".ToUpper();
        Assert.Equal("HELLO", s);

        s = "hello".ToUpperInvariant();
        Assert.Equal("HELLO", s);

        return;
    }

    [Fact]
    public static void TestToLowerToUpper_TurkishI()
    {
        WithCulture(new CultureInfo("tr-TR"), () =>
        {
            Assert.True("H\u0049 World".ToLower().Equals("h\u0131 world", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToLower().Equals("h\u0069 world", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToLower().Equals("h\u0131 world", StringComparison.Ordinal));

            Assert.True("H\u0069 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
        });
        WithCulture(new CultureInfo("en-US"), () =>
        {
            Assert.True("H\u0049 World".ToLower().Equals("h\u0069 world", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToLower().Equals("h\u0069 world", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToLower().Equals("h\u0131 world", StringComparison.Ordinal));

            Assert.True("H\u0069 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.True("H\u0049 World".ToLower().Equals("h\u0069 world", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToLower().Equals("h\u0130 world", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToLower().Equals("h\u0131 world", StringComparison.Ordinal));

            Assert.True("H\u0069 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
            Assert.True("H\u0131 World".ToUpper().Equals("H\u0131 WORLD", StringComparison.Ordinal));
        });
    }

    [Fact]
    public static void TestToLowerToUpperInvariant_ASCII()
    {
        char[] asciiChars = new char[128];
        char[] asciiCharsUpper = new char[128];
        char[] asciiCharsLower = new char[128];

        for (int i = 0; i < asciiChars.Length; i++)  
        {
            char c = (char)i;
            asciiChars[i] = c;  

            // Purposefully avoiding char.ToUpper/ToLower here so as not  
            // to use the same thing we're testing.  
            asciiCharsLower[i] = (c >= 'A' && c <= 'Z') ? (char)(c - 'A' + 'a') : c;
            asciiCharsUpper[i] = (c >= 'a' && c <= 'z') ? (char)(c - 'a' + 'A') : c;
        }

        string ascii = new string(asciiChars);
        string asciiLower = new string(asciiCharsLower);
        string asciiUpper = new string(asciiCharsUpper);

        Assert.Equal(asciiLower, ascii.ToLowerInvariant());
        Assert.Equal(asciiUpper, ascii.ToUpperInvariant());
    }

    [Fact]
    public static void TestTrim()
    {
        String s;

        s = "  Foo  ".Trim();
        Assert.Equal("Foo", s);

        s = ". Foo .".Trim('.');
        Assert.Equal(" Foo ", s);

        s = "  Foo  ".TrimStart();
        Assert.Equal("Foo  ", s);

        s = ". Foo .".TrimStart('.');
        Assert.Equal(" Foo .", s);

        s = "  Foo  ".TrimEnd();
        Assert.Equal("  Foo", s);

        s = ". Foo .".TrimEnd('.');
        Assert.Equal(". Foo ", s);
    }

    [Fact]
    public static void TestCompareWithLongString()
    {
        int Local_282_0 = String.Compare("{Policy_PS_Nothing}", 0, "<NamedPermissionSets><PermissionSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022 Name=\u0022FullTrust\u0022 Description=\u0022{Policy_PS_FullTrust}\u0022/><PermissionSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022Everything\u0022 Description=\u0022{Policy_PS_Everything}\u0022><Permission class=\u0022System.Security.Permissions.IsolatedStorageFilePermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.EnvironmentPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.FileIOPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.FileDialogPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.ReflectionPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Flags=\u0022Assertion, UnmanagedCode, Execution, ControlThread, ControlEvidence, ControlPolicy, ControlAppDomain, SerializationFormatter, ControlDomainPolicy, ControlPrincipal, RemotingConfiguration, Infrastructure, BindingRedirects\u0022/><Permission class=\u0022System.Security.Permissions.UIPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Net.SocketPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Net.WebPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Net.DnsPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Security.Permissions.KeyContainerPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.RegistryPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Drawing.Printing.PrintingPermission, System.Drawing, Version={VERSION}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Diagnostics.EventLogPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Security.Permissions.StorePermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022 version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Diagnostics.PerformanceCounterPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Data.OleDb.OleDbPermission, System.Data, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022 version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Data.SqlClient.SqlClientPermission, System.Data, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022 version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Security.Permissions.DataProtectionPermission, System.Security, Version={VERSION}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\u0022 version=\u00221\u0022 Unrestricted=\u0022true\u0022/></PermissionSet><PermissionSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022Nothing\u0022 Description=\u0022{Policy_PS_Nothing}\u0022/><PermissionSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022Execution\u0022 Description=\u0022{Policy_PS_Execution}\u0022><Permission class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Flags=\u0022Execution\u0022/></PermissionSet><PermissionSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022SkipVerification\u0022 Description=\u0022{Policy_PS_SkipVerification}\u0022><Permission class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Flags=\u0022SkipVerification\u0022/></PermissionSet></NamedPermissionSets>", 4380, 19, StringComparison.Ordinal);
        Assert.True(Local_282_0 < 0);
    }

    public static IEnumerable<object[]> AllSubstringsAndComparisons(string source)
    {
        var comparisons = new StringComparison[] {
            StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase,
            StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase
        };

        foreach (StringComparison comparison in comparisons)
            for (int i = 0; i <= source.Length; i++)
                for (int subLen = source.Length - i; subLen > 0; subLen--)
                    yield return new object[] { source, source.Substring(i, subLen), i, comparison };
    }

    private static void WithCulture(CultureInfo culture, Action test)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            test();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}

