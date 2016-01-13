// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

public static unsafe class StringTests
{
    private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows); 
    private const string c_SoftHyphen = "\u00AD";

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

        Assert.Throws<ArgumentOutOfRangeException>("ptr", () => new String((char*)null, 5, 1));
    }

    [Theory]
    [InlineData('a', 0, "")]
    [InlineData('a', 1, "a")]
    [InlineData('a', 2, "aa")]
    [InlineData('a', 3, "aaa")]
    [InlineData('a', 4, "aaaa")]
    [InlineData('a', 5, "aaaaa")]
    [InlineData('a', 6, "aaaaaa")]
    [InlineData('a', 7, "aaaaaaa")]
    [InlineData('a', 8, "aaaaaaaa")]
    [InlineData('a', 9, "aaaaaaaaa")]
    public static void TestCtorCharInt(char c, int count, string expected)
    {
        String s = new String(c, count);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestCtorCharIntInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("count", () => new String('a', -1));
    }

    [Theory]
    [InlineData(new char[] { }, 0, 0, "")]
    [InlineData((char[])null, 0, 0, "")]

    [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 0, "")]
    [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 3, "abc")]
    [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 2, 3, "cde")]
    [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 8, "abcdefgh")]
    [InlineData(new char[] { 'a', 'b', 'c', 'd', '\0', 'e', 'f', 'g', 'h' }, 0, 9, "abcd\0efgh")]
    [InlineData(new char[] { 'П', 'Р', 'И', 'В', 'Е', 'Т' }, 0, 6, "ПРИВЕТ")]
    public static void TestCtorCharArray(char[] c, int startIndex, int length, string expected)
    {
        if (c == null)
        {
            Assert.Equal(expected, new String(c));
            return;
        }
        if (startIndex + length == c.Length && length != 0)
        {
            Assert.Equal(expected, new String(c));
        }
        Assert.Equal(expected, new String(c, startIndex, length));
    }

    [Fact]
    public static void TestCtorCharArrayIntIntInvalid()
    {
        char[] c = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => new String(c, 0, 9)); //Length > array length
        Assert.Throws<ArgumentOutOfRangeException>("length", () => new String(c, 5, -1)); //Length < 0
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => new String(c, -1, 1)); //Start Index < 0
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => new String(c, 6, 5)); //Walks off array

        Assert.Throws<ArgumentNullException>("value", () => new String((char[])null, 0, 0));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("hello", 5)]
    public static void TestLength(string text, int expected)
    {
        Assert.Equal(expected, text.Length);
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

    [Theory]
    [InlineData("Hello", 1, 5, 3, new char[] { '\0', '\0', '\0', '\0', '\0', 'e', 'l', 'l', '\0', '\0' })]
    [InlineData("Hello", 2, 0, 3, new char[] { 'l', 'l', 'o', '\0', '\0', '\0', '\0', '\0', '\0', '\0' })]
    [InlineData("Hello", 0, 7, 3, new char[] { '\0', '\0', '\0', '\0', '\0', '\0', '\0', 'H', 'e', 'l' })]
    [InlineData("Hello", 5, 10, 0, new char[] { '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' })]
    [InlineData("H" + c_SoftHyphen + "ello", 0, 0, 3, new char[] { 'H', '\u00AD', 'e' })]
    public static void TestCopyTo(string s, int sourceIndex, int destinationIndex, int count, char[] expected)
    {
        char[] dst = new char[expected.Length];
        s.CopyTo(sourceIndex, dst, destinationIndex, count);
        Assert.Equal(expected, dst);
    }

    [Fact]
    public static void TestCopyToInvalid()
    {
        String s = "Hello";
        char[] dst = new char[10];

        Assert.Throws<ArgumentNullException>("destination", () => s.CopyTo(0, null, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.CopyTo(-1, dst, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(-1, dst, -1, 0)); //Source index < 0
        Assert.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, -1, 0)); //Destination index < 0
        Assert.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(0, dst, 0, 6));
        Assert.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, 11, 0));
        Assert.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, 9, 2));

        dst = new char[2];

        Assert.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, 0, 4));
    }

    [Theory]
    [InlineData("Hello", "Hello", StringComparison.CurrentCulture, 0)]
    [InlineData("HELLO", "hello", StringComparison.CurrentCultureIgnoreCase, 0)]
    [InlineData("Hello", "Hello", StringComparison.Ordinal, 0)]
    [InlineData("HELLO", "hello", StringComparison.OrdinalIgnoreCase, 0)]

    [InlineData("A", "B", StringComparison.CurrentCulture, -1)]
    public static void TestCompare(string strA, string strB, StringComparison comparisonType, int expected)
    {
        if (comparisonType == StringComparison.CurrentCulture)
        {
            Assert.Equal(expected, NormalizeCompare(String.Compare(strA, strB)));
        }
        else if (comparisonType == StringComparison.Ordinal)
        {
            Assert.Equal(expected, NormalizeCompare(String.CompareOrdinal(strA, strB)));
        }
        Assert.Equal(expected, NormalizeCompare(String.Compare(strA, strB, comparisonType)));
    }

    [Theory]
    [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture, 0)]
    [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
    [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0)]
    [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]

    [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCulture, 1)]
    [InlineData("hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture, -1)]
    [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.Ordinal, -1)]

    [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCulture, -1)]
    [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase, -1)]
    [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.OrdinalIgnoreCase, -1)]
    [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.Ordinal, -1)]
    public static void TestCompareIndexed(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType, int expected)
    {
        if (comparisonType == StringComparison.CurrentCulture)
        {
            Assert.Equal(expected, NormalizeCompare(String.Compare(strA, indexA, strB, indexB, length)));
        }
        else if (comparisonType == StringComparison.Ordinal)
        {
            Assert.Equal(expected, NormalizeCompare(String.CompareOrdinal(strA, indexA, strB, indexB, length)));
        }
        Assert.Equal(expected, NormalizeCompare(String.Compare(strA, indexA, strB, indexB, length, comparisonType)));
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

        result = String.Compare(strA, indexA, strB, indexB, length, StringComparison.Ordinal);
        result = Math.Max(-1, Math.Min(1, result));
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(-1, 0, 1, "indexA")] //IndexA < 0
    [InlineData(0, -1, 1, "indexB")] //IndexB < 0
    [InlineData(0, 0, -1, "count")] //Length < 0

    [InlineData(Int32.MaxValue, 0, 0, "indexA")] //IndexA > string length
    [InlineData(0, Int32.MaxValue, 0, "indexB")] //IndexB > string length
    public static void TestCompareOrdinalIndexedInvalid(int indexA, int indexB, int length, string paramName)
    {
        Assert.Throws<ArgumentOutOfRangeException>(paramName, () => String.CompareOrdinal("Hello", indexA, "Hello", indexB, length));
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

    [Theory]
    [InlineData("Hello", "ello", true)]
    [InlineData("Hello", "ELL", false)]
    [InlineData("Hello", "Larger Hello", false)]
    [InlineData("Hello", "Goodbye", false)]

    [InlineData("", "", true)]
    [InlineData("", "hello", false)]
    [InlineData("Hello", "", true)]
    public static void TestContains(string text, string value, bool expected)
    {
        Assert.Equal(expected, text.Contains(value));
    }

    [Fact]
    public static void TestContainsInvalid()
    {
        Assert.Throws<ArgumentNullException>("value", () => "foo".Contains(null));
    }

    [Theory]
    [InlineData(StringComparison.CurrentCulture, "", "", true)]
    [InlineData(StringComparison.CurrentCulture, "", "Foo", false)]
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

    [ActiveIssue("https://github.com/dotnet/coreclr/issues/1716", PlatformID.AnyUnix)]
    [Theory]
    [InlineData(StringComparison.CurrentCulture)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase)]
    [InlineData(StringComparison.Ordinal)]
    [InlineData(StringComparison.OrdinalIgnoreCase)]
    public static void TestEndsWith_NullInStrings(StringComparison comparison)
    {
        Assert.True("\0test".EndsWith("test", comparison));
        Assert.True("te\0st".EndsWith("e\0st", comparison));
        Assert.False("te\0st".EndsWith("test", comparison));
        Assert.False("test\0".EndsWith("test", comparison));
        Assert.False("test".EndsWith("\0st", comparison));
    }

    [Fact]
    public static void TestEndsWithInvalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentException>("comparisonType", () => s.EndsWith("o", (StringComparison.CurrentCulture - 1)));
        Assert.Throws<ArgumentException>("comparisonType", () => s.EndsWith("o", (StringComparison.OrdinalIgnoreCase + 1)));

        Assert.Throws<ArgumentNullException>("value", () => s.EndsWith(null));
        Assert.Throws<ArgumentNullException>("value", () => s.EndsWith(null, StringComparison.CurrentCultureIgnoreCase));
        Assert.Throws<ArgumentNullException>("value", () => s.EndsWith(null, StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>("value", () => s.EndsWith(null, StringComparison.OrdinalIgnoreCase));
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
    }

    [Fact]
    public static void TestFormatInvalid()
    {
        TestFormatter testFormatter = new TestFormatter();

        Assert.Throws<ArgumentNullException>("format", () => String.Format(testFormatter, null, 0, 1, 2, 3, 4));

        Assert.Throws<FormatException>(() => String.Format(testFormatter, "Missing={5}", 0, 1, 2, 3, 4));
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

    [Theory]
    [InlineData("Hello", new char[] { 'H', 'e', 'l', 'l', 'o' }, true)]
    [InlineData("", new char[] { }, true)]
    [InlineData("Hello", new char[] { }, false)] // Technically could be equal but if so its an inadequate implementation
    public static void TestGetHashCode(string strA, char[] arrayB, bool expectedResult)
    {
        string strB = new String(arrayB);
        Assert.Equal(expectedResult, strA.GetHashCode() == strB.GetHashCode());
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
    [InlineData("Hello", 'o', 5, 0, -1)]
    [InlineData("H" + c_SoftHyphen + "ello", 'e', 0, 3, 2)]
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

    [ActiveIssue("https://github.com/dotnet/coreclr/issues/1716", PlatformID.AnyUnix)]
    [InlineData("He\0lo", "He\0lo", 0)]
    [InlineData("He\0lo", "He\0", 0)]
    [InlineData("He\0lo", "\0", 2)]
    [InlineData("He\0lo", "\0lo", 2)]
    [InlineData("He\0lo", "lo", 3)]
    [InlineData("Hello", "lo\0", -1)]
    [InlineData("Hello", "\0lo", -1)]
    [InlineData("Hello", "l\0o", -1)]
    public static void TestIndexOf_NullInStrings(string source, char target, int expectedResult)
    {
        Assert.Equal(expectedResult, source.IndexOf(target));
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
    public static void TestIndexOf_Invalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf('e', -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf('e', -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf('e', 6));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf('e', 6, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf('e', 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf('e', 0, 6));

        Assert.Throws<ArgumentNullException>("value", () => s.IndexOf(null));
        Assert.Throws<ArgumentNullException>("value", () => s.IndexOf(null, -1));
        Assert.Throws<ArgumentNullException>("value", () => s.IndexOf(null, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentNullException>("value", () => s.IndexOf(null, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentNullException>("value", () => s.IndexOf(null, -1, 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf(null, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", -1, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", 6));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf(null, 6, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.IndexOf("e", 6, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf(null, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf("e", 1, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf(null, 0, 6));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOf("e", 0, 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", StringComparison.OrdinalIgnoreCase + 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", 0, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", 0, StringComparison.OrdinalIgnoreCase + 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", 0, 1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.IndexOf("e", 0, 1, StringComparison.OrdinalIgnoreCase + 1));
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
            /* 
             There are differences between Windows and ICU regarding contractions.
             Windows has equal contraction collation weights, including case (target="Ddzs" same behavior as "ddzs").
             ICU has different contraction collation weights, depending on locale collation rules.
             If CurrentCultureIgnoreCase is specified, ICU will use 'secondary' collation rules
              which ignore the contraction collation weights (defined as 'tertiary' rules)
            */
            Assert.Equal(s_isWindows ? 0 : -1, source.IndexOf(target));
            Assert.Equal(s_isWindows ? 0 : -1, source.IndexOf(target, StringComparison.CurrentCulture));

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

        target = "a\u0300"; // this diacritic combines with preceding character
        WithCulture(new CultureInfo("en-US"), () =>
        {
            Assert.Equal(8, source.IndexOf(target));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(8, source.IndexOf(target, StringComparison.Ordinal));
            Assert.Equal(8, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));
        });
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(8, source.IndexOf(target));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCulture));
            Assert.Equal(8, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
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

    [Theory]
    [InlineData("Hello", new char[] { 'd', 'o', 'l' }, 0, 5, 2)]
    [InlineData("Hello", new char[] { 'd', 'e', 'H' }, 0, 0, -1)]
    [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 1, 3, 1)]
    [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
    [InlineData("Hello", new char[] { }, 2, 3, -1)]
    [InlineData("H" + c_SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 0, 2, 1)]
    [InlineData("", new char[] { 'd', 'e', 'f' }, 0, 0, -1)]
    public static void TestIndexOfAny(string text, char[] c, int startIndex, int count, int expected)
    {
        if (count == text.Length - startIndex)
        {
            if (startIndex == 0)
            {
                Assert.Equal(expected, text.IndexOfAny(c));
            }
            Assert.Equal(expected, text.IndexOfAny(c, startIndex));
        }
        Assert.Equal(expected, text.IndexOfAny(c, startIndex, count));
    }

    [Fact]
    public static void TestIndexOfAny_Invalid()
    {
        string s = "Hello";
        char[] find = new char[] { 'a', 'b', 'c' };

        Assert.Throws<ArgumentNullException>(() => s.IndexOfAny(null));
        Assert.Throws<ArgumentNullException>(() => s.IndexOfAny(null, -1));
        Assert.Throws<ArgumentNullException>(() => s.IndexOfAny(null, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.IndexOfAny(find, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.IndexOfAny(find, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.IndexOfAny(find, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.IndexOfAny(find, 6, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOfAny(find, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.IndexOfAny(find, 3, 3));
    }

    [Theory]
    [InlineData("Hello", 0, "!$%", "!$%Hello")]
    [InlineData("Hello", 1, "!$%", "H!$%ello")]
    [InlineData("Hello", 2, "!$%", "He!$%llo")]
    [InlineData("Hello", 3, "!$%", "Hel!$%lo")]
    [InlineData("Hello", 4, "!$%", "Hell!$%o")]
    [InlineData("Hello", 5, "!$%", "Hello!$%")]
    [InlineData("Hello", 3, "", "Hello")]
    public static void TestInsert(string text, int startIndex, string value, string expected)
    {
        String s = text.Insert(startIndex, value);
        Assert.Equal(expected, s);
    }

    [InlineData]
    public static void TestInsertInvalid()
    {
        Assert.Throws<ArgumentNullException>("value", () => "Hello".Insert(Int32.MaxValue, null));

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".Insert(-1, "!")); //Start index < 0
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".Insert(6, "!")); //Start index > string.length
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("foo", false)]
    public static void TestIsNullOrEmpty(string value, bool expected)
    {
        bool b = String.IsNullOrEmpty(value);
        Assert.Equal(expected, b);
    }

    public static IEnumerable<object[]> StringsWithWhitespace()
    {
        for (int i = 0; i < char.MaxValue; i++)
        {
            if (char.IsWhiteSpace((char)i))
            {
                yield return new object[] { new string((char)i, 3), true };
                yield return new object[] { new string((char)i, 3) + "x", false };
            }
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [MemberData("StringsWithWhitespace")]
    public static void TestIsNullOrWhitespace(string value, bool expected)
    {
        bool b = String.IsNullOrWhiteSpace(value);
        Assert.Equal(expected, b);
    }

    [Theory]
    [InlineData("$$", new string[] { }, 0, 0, "")]
    [InlineData("$$", new string[] { null }, 0, 1, "")]
    [InlineData("$$", new string[] { null, "Bar", null }, 0, 3, "$$Bar$$")]
    [InlineData(null, new string[] { "Foo", "Bar", "Baz" }, 0, 3, "FooBarBaz")]
    [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 0, 3, "Foo$$Bar$$Baz")]
    [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 3, 0, "")]
    [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 1, 1, "Bar")]
    public static void TestJoinStringArray(string seperator, string[] values, int startIndex, int count, string expected)
    {
        if (startIndex + count == values.Length && count != 0)
        {
            Assert.Equal(expected, String.Join(seperator, values));

            List<string> iEnumerableStringOptimized = new List<string>(values);
            Assert.Equal(expected, String.Join(seperator, iEnumerableStringOptimized));

            Queue<string> iEnumerableStringNotOptimized = new Queue<string>(values);
            Assert.Equal(expected, String.Join(seperator, iEnumerableStringNotOptimized));

            List<object> iEnumerableObject = new List<object>(values);
            Assert.Equal(expected, String.Join(seperator, iEnumerableObject));
        }
        Assert.Equal(expected, String.Join(seperator, values, startIndex, count));
    }

    [Fact]
    public static void TestJoinStringArrayInvalid()
    {
        Assert.Throws<ArgumentNullException>("value", () => String.Join("$$", (string[])null));
        Assert.Throws<ArgumentNullException>("value", () => String.Join("$$", (string[])null, 0, 0));

        Assert.Throws<ArgumentNullException>("values", () => String.Join("|", (IEnumerable<String>)null));
        Assert.Throws<ArgumentNullException>("values", () => String.Join("--", (IEnumerable<Object>)null));

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => String.Join("$$", new string[] { "Foo" }, -1, 0)); //Start index < 0
        Assert.Throws<ArgumentOutOfRangeException>("count", () => String.Join("$$", new string[] { "Foo" }, 0, -1)); //Count < 0

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => String.Join("$$", new string[] { "Foo" }, 2, 1)); //Start index > seperators.Length
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => String.Join("$$", new string[] { "Foo" }, 0, 2)); //Start index + count > seperators.Length
    }

    [Theory]
    [InlineData("$$", new object[] { }, "")]
    [InlineData("$$", new object[] { "Foo" }, "Foo")]
    [InlineData("$$", new object[] { "Foo", "Bar", "Baz" }, "Foo$$Bar$$Baz")]
    [InlineData(null, new object[] { "Foo", "Bar", "Baz" }, "FooBarBaz")]
    [InlineData("$$", new object[] { null, "Bar", "Baz" }, "")] //Feature: overload exits if [0] is null
    [InlineData("$$", new object[] { "Foo", null, "Baz" }, "Foo$$$$Baz")]
    public static void TestJoinObjectArray(string seperator, object[] values, string expected)
    {
        Assert.Equal(expected, String.Join(seperator, values));
    }

    [Fact]
    public static void TestJoinObjectArrayInvalid()
    {
        Assert.Throws<ArgumentNullException>("values", () => String.Join("$$", (object[])null));
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
    [InlineData("H" + c_SoftHyphen + "ello", 'H', 2, 3, 0)]
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

    [InlineData("Hello", "o", 5, 0, -1)]
    [InlineData("Hello", "o", 5, 6, -1)]
    public static void TestLastIndexOf_OutsideIndex(string source, string target, int startIndex, int count, int expectedResult)
    {
        Assert.Equal(expectedResult, source.LastIndexOf(target, startIndex));
        Assert.Equal(expectedResult, source.LastIndexOf(target, startIndex, count));
        Assert.Equal(expectedResult, source.LastIndexOf(target, startIndex, count, StringComparison.CurrentCulture));
    }

    [ActiveIssue("https://github.com/dotnet/coreclr/issues/1716", PlatformID.AnyUnix)]
    [InlineData("He\0lo", "He\0lo", 0)]
    [InlineData("He\0lo", "He\0", 0)]
    [InlineData("He\0lo", "\0", 2)]
    [InlineData("He\0lo", "\0lo", 2)]
    [InlineData("He\0lo", "lo", 3)]
    [InlineData("Hello", "lo\0", -1)]
    [InlineData("Hello", "\0lo", -1)]
    [InlineData("Hello", "l\0o", -1)]
    public static void TestLastIndexOf_NullInStrings(string source, char target, int expectedResult)
    {
        Assert.Equal(expectedResult, source.LastIndexOf(target));
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
    public static void TestLastIndexOf_Invalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('e', -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('e', -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('e', 5));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('e', 5, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf('e', 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf('e', 0, 6));

        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null));
        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, -1));
        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, 0, 6));
        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, -1, 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", -1, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", 6));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("e", 6, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf(null, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("e", 1, -1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("e", 0, 6, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("e", 5, 7, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", StringComparison.OrdinalIgnoreCase + 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", 1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", 1, StringComparison.OrdinalIgnoreCase + 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", 1, 1, StringComparison.CurrentCulture - 1));
        Assert.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("e", 1, 1, StringComparison.OrdinalIgnoreCase + 1));
    }

    [Theory]
    [InlineData("foo", 2)]
    [InlineData("hello", 4)]
    [InlineData("", 0)]
    public static void TestLastIndexOfEmptyString(string s, int expected)
    {
        Assert.Equal(expected, s.LastIndexOf("", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 5, 3)]
    [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 0, -1)]
    [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 2, 3, 1)]
    [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
    [InlineData("Hello", new char[] { }, 2, 3, -1)]
    [InlineData("H" + c_SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 2, 3, 1)]
    [InlineData("", new char[] { 'd', 'e', 'f' }, -1, -1, -1)]
    public static void TestLastIndexOfAny(string text, char[] c, int startIndex, int count, int expected)
    {
        if (count == startIndex + 1)
        {
            if (startIndex == text.Length - 1)
            {
                Assert.Equal(expected, text.LastIndexOfAny(c));
            }
            Assert.Equal(expected, text.LastIndexOfAny(c, startIndex));
        }
        Assert.Equal(expected, text.LastIndexOfAny(c, startIndex, count));
    }

    [Fact]
    public static void TestLastIndexOfAny_Invalid()
    {
        string s = "Hello";
        char[] find = new char[] { 'a', 'b', 'c' };

        Assert.Throws<ArgumentNullException>(() => s.LastIndexOfAny(null));
        Assert.Throws<ArgumentNullException>(() => s.LastIndexOfAny(null, -1));
        Assert.Throws<ArgumentNullException>(() => s.LastIndexOfAny(null, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOfAny(find, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOfAny(find, -1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOfAny(find, 5));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOfAny(find, 5, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOfAny(find, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOfAny(find, 1, 3));
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

    [Theory]
    [InlineData("Hello", 5, ' ', "Hello")]
    [InlineData("Hello", 7, ' ', "  Hello")]
    [InlineData("Hello", 7, '.', "..Hello")]
    [InlineData("", 0, '.', "")]
    public static void TestPadLeft(string text, int totalWidth, char paddingChar, string expected)
    {
        String s;
        if (paddingChar == ' ')
        {
            s = text.PadLeft(totalWidth);
            Assert.Equal(expected, s);
        }
        s = text.PadLeft(totalWidth, paddingChar);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestPadLeftInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("totalWidth", () => "".PadLeft(-1, '.')); //Total width < 0
    }

    [Theory]
    [InlineData("Hello", 5, ' ', "Hello")]
    [InlineData("Hello", 7, ' ', "Hello  ")]
    [InlineData("Hello", 7, '.', "Hello..")]
    [InlineData("", 0, '.', "")]
    public static void TestPadRight(string text, int totalWidth, char paddingChar, string expected)
    {
        String s;
        if (paddingChar == ' ')
        {
            s = text.PadRight(totalWidth);
            Assert.Equal(expected, s);
        }
        s = text.PadRight(totalWidth, paddingChar);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestPadRightInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("totalWidth", () => "".PadRight(-1, '.')); //Total width < 0
    }

    [Theory]
    [InlineData("Hello", 2, null, "He")]
    [InlineData("Hello", 1, 2, "Hlo")]
    [InlineData("Hello", 0, 5, "")]
    [InlineData("Hello", 5, 0, "Hello")]
    [InlineData("Hello", 0, 0, "Hello")]
    [InlineData("", 0, 0, "")]
    public static void TestRemove(string text, int startIndex, int? length, string expected)
    {
        String s;
        if (length == null)
        {
            length = text.Length - startIndex;
            s = text.Remove(startIndex);
            Assert.Equal(expected, s);
        }
        s = text.Remove(startIndex, length.Value);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestRemoveInvalid()
    {
        string s = "Hello";

        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => s.Remove(-1, 0)); //Start index < 0
        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(0, -1)); //Count < 0

        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length + 1, 0)); //Start index + count >= string.length

        Assert.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length, 1)); //Start index + count >= string.length
    }

    [Theory]
    [InlineData("Hello", 'l', '!', "He!!o")]
    [InlineData("Hello", 'a', 'b', "Hello")]
    public static void TestReplaceChar(string text, char oldChar, char newChar, string expected)
    {
        String s = text.Replace(oldChar, newChar);
        Assert.Equal(expected.Length, s.Length);
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData("", "1", "2", "")]
    [InlineData("Hello", "ll", "!!!!", "He!!!!o")]
    [InlineData("Hello", "l", "", "Heo")]
    [InlineData("Hello", "l", null, "Heo")]
    [InlineData("11111", "1", "23", "2323232323")]
    [InlineData("111111", "111", "23", "2323")]
    [InlineData("1111111", "111", "23", "23231")]
    [InlineData("11111111", "111", "23", "232311")]
    [InlineData("111111111", "111", "23", "232323")]
    [InlineData("A1B1C1D1E1F", "1", "23", "A23B23C23D23E23F")]
    [InlineData("Aa1Bbb1Cccc1Ddddd1Eeeeee1Fffffff", "1", "23", "Aa23Bbb23Cccc23Ddddd23Eeeeee23Fffffff")]
    [InlineData("11111111111111111111111", "1", "11", "1111111111111111111111111111111111111111111111")] //Checks if we handle the max # of matches
    [InlineData("11111111111111111111111", "1", "", "")] //Checks if we handle the max # of matches
    [InlineData("abcdefghijkl", "cdef", "12345", "ab12345ghijkl")]
    public static void TestReplaceString(string text, string oldValue, string newValue, string expected)
    {
        String s = text.Replace(oldValue, newValue);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestReplaceStringInvalid()
    {
        Assert.Throws<ArgumentNullException>("oldValue", () => "Hello".Replace(null, ""));

        Assert.Throws<ArgumentException>("oldValue", () => "Hello".Replace("", "l"));
    }

    [Theory]
    [InlineData("XYZ", "1", "2")]
    [InlineData("", "1", "2")]
    public static void TestReplaceStringPerf(string text, string oldValue, string newValue)
    {
        // Perf test: If nothing is replaced, don't waste an allocation on a new string.
        String s = text.Replace(oldValue, newValue);
        Assert.Same(text, s);
    }

    [Theory]
    [InlineData(StringComparison.CurrentCulture, "Hello", "Hel", true)]
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

        Assert.Throws<ArgumentNullException>("value", () => s.StartsWith(null));
        Assert.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.CurrentCultureIgnoreCase));
        Assert.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.OrdinalIgnoreCase));

        Assert.Throws<ArgumentException>("comparisonType", () => s.StartsWith("H", (StringComparison.CurrentCulture - 1))); //Invalid comparison type
        Assert.Throws<ArgumentException>("comparisonType", () => s.StartsWith("H", (StringComparison.OrdinalIgnoreCase + 1))); //Invalid comparison type
    }

    [ActiveIssue("https://github.com/dotnet/coreclr/issues/1716", PlatformID.AnyUnix)]
    [Theory]
    [InlineData(StringComparison.CurrentCulture)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase)]
    [InlineData(StringComparison.Ordinal)]
    [InlineData(StringComparison.OrdinalIgnoreCase)]
    public static void TestStartsWith_NullInStrings(StringComparison comparison)
    {
        Assert.False("\0test".StartsWith("test", comparison));
        Assert.False("te\0st".StartsWith("test", comparison));
        Assert.True("te\0st".StartsWith("te\0s", comparison));
        Assert.True("test\0".StartsWith("test", comparison));
        Assert.False("test".StartsWith("te\0", comparison));
    }

    [Theory]
    [InlineData("Hello", 0, 5, "Hello")]
    [InlineData("Hello", 0, 3, "Hel")]
    [InlineData("Hello", 2, 3, "llo")]
    [InlineData("Hello", 5, 0, "")]
    public static void TestSubstring(string text, int startIndex, int length, string expected)
    {
        if (startIndex + length == text.Length)
        {
            Assert.Equal(expected, text.Substring(startIndex));
        }

        Assert.Equal(expected, text.Substring(startIndex, length));
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
    public static void TestToCharArray_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".ToCharArray(-1, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".ToCharArray(6, -1));
        Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".ToCharArray(0, 6));
        Assert.Throws<ArgumentOutOfRangeException>("length", () => "Hello".ToCharArray(0, -1));
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

    [Theory]
    [InlineData("  Hello  ", new char[] { ' ' }, "Hello")]
    [InlineData(".  Hello  ..", new char[] { '.' }, "  Hello  ")]
    [InlineData(".  Hello  ..", new char[] { '.', ' ' }, "Hello")]
    [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "Hello")]

    [InlineData("  Hello  ", null, "Hello")]
    [InlineData("  Hello  ", new char[] { }, "Hello")]
    public static void TestTrim(string text, char[] trimChars, string expected)
    {
        if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
        {
            Assert.Equal(expected, text.Trim());
        }
        Assert.Equal(expected, text.Trim(trimChars));
    }

    [Theory]
    [InlineData("  Hello  ", new char[] { ' ' }, "Hello  ")]
    [InlineData(".  Hello  ..", new char[] { '.' }, "  Hello  ..")]
    [InlineData(".  Hello  ..", new char[] { '.', ' ' }, "Hello  ..")]
    [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "Hello123abc")]

    [InlineData("  Hello  ", null, "Hello  ")]
    [InlineData("  Hello  ", new char[] { }, "Hello  ")]
    public static void TestTrimStart(string text, char[] trimChars, string expected)
    {
        if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
        {
            Assert.Equal(expected, text.TrimStart());
        }
        Assert.Equal(expected, text.TrimStart(trimChars));
    }

    [Theory]
    [InlineData("  Hello  ", new char[] { ' ' }, "  Hello")]
    [InlineData(".  Hello  ..", new char[] { '.' }, ".  Hello  ")]
    [InlineData(".  Hello  ..", new char[] { '.', ' ' }, ".  Hello")]
    [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "123abcHello")]

    [InlineData("  Hello  ", null, "  Hello")]
    [InlineData("  Hello  ", new char[] { }, "  Hello")]
    public static void TestTrimEnd(string text, char[] trimChars, string expected)
    {
        if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
        {
            Assert.Equal(expected, text.TrimEnd());
        }
        Assert.Equal(expected, text.TrimEnd(trimChars));
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
        {
            for (int i = 0; i <= source.Length; i++)
            {
                for (int subLen = source.Length - i; subLen > 0; subLen--)
                {
                    yield return new object[] { source, source.Substring(i, subLen), i, comparison };
                }
            }
        }
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

    private static int NormalizeCompare(int i)
    {
        return
            i == 0 ? 0 :
            i > 0 ? 1 :
            -1;
    }
}
