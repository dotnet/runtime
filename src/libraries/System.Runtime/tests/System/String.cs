// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class StringTests
{
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
    [ActiveIssue(846, PlatformID.AnyUnix)]
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

        i = String.CompareOrdinal(new String(c), 2, new String(c), 2, 3);
        Assert.Equal(0, i);

        i = String.CompareOrdinal("Hello", 2, "Goodbye", 2, 3);
        Assert.True(i < 0);
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

    [Fact]
    public static void TestEndsWith()
    {
        String s = "Hello";
        bool b;
        b = s.EndsWith("ello");
        Assert.True(b);
        b = s.EndsWith("Hello");
        Assert.True(b);
        b = s.EndsWith("");
        Assert.True(b);
        b = s.EndsWith("ELLO");
        Assert.False(b);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s.EndsWith(null);
            });

        b = s.EndsWith("ello", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.EndsWith("Hello", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.EndsWith("", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.EndsWith("ELLO", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.EndsWith("Goodbye", StringComparison.CurrentCultureIgnoreCase);
        Assert.False(b);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s.EndsWith(null, StringComparison.CurrentCultureIgnoreCase);
            });
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

    [Fact]
    public static void TestIndexOf()
    {
        Assert.Equal(2, "Hello".IndexOf('l'));
        Assert.Equal(-1, "Hello".IndexOf('x'));

        Assert.Equal(2, "Hello".IndexOf('l', 1));
        Assert.Equal(3, "Hello".IndexOf('l', 3));
        Assert.Equal(-1, "Hello".IndexOf('l', 4));
        Assert.Equal(-1, "Hello".IndexOf('x', 1));

        Assert.Equal(2, "Hello".IndexOf('l', 1, 4));
        Assert.Equal(3, "Hello".IndexOf('l', 3, 2));
        Assert.Equal(-1, "Hello".IndexOf('l', 3, 0));
        Assert.Equal(-1, "Hello".IndexOf('l', 0, 2));
        Assert.Equal(2, "Hello".IndexOf('l', 0, 3));
        Assert.Equal(-1, "Hello".IndexOf('l', 4, 1));
        Assert.Equal(-1, "Hello".IndexOf('x', 1, 4));

        Assert.Equal(2, "Hello".IndexOf("llo"));
        Assert.Equal(-1, "Hello".IndexOf("LLO"));
        Assert.Equal(2, "Hello".IndexOf("LLO", StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("NoWay", StringComparison.CurrentCultureIgnoreCase));

        Assert.Equal(2, "Hello".IndexOf("l", 1));
        Assert.Equal(3, "Hello".IndexOf("l", 3));
        Assert.Equal(-1, "Hello".IndexOf("l", 4));
        Assert.Equal(-1, "Hello".IndexOf("x", 1));

        Assert.Equal(2, "Hello".IndexOf("l", 1, 4));
        Assert.Equal(3, "Hello".IndexOf("l", 3, 2));
        Assert.Equal(-1, "Hello".IndexOf("l", 3, 0));
        Assert.Equal(-1, "Hello".IndexOf("l", 0, 2));
        Assert.Equal(2, "Hello".IndexOf("l", 0, 3));
        Assert.Equal(-1, "Hello".IndexOf("l", 4, 1));
        Assert.Equal(-1, "Hello".IndexOf("x", 1, 4));

        Assert.Equal(2, "Hello".IndexOf("l", 1, StringComparison.CurrentCulture));
        Assert.Equal(3, "Hello".IndexOf("l", 3, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("l", 4, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("L", 1, StringComparison.CurrentCulture));

        Assert.Equal(2, "Hello".IndexOf("l", 1, 4, StringComparison.CurrentCulture));
        Assert.Equal(3, "Hello".IndexOf("l", 3, 2, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("l", 3, 0, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("l", 0, 2, StringComparison.CurrentCulture));
        Assert.Equal(2, "Hello".IndexOf("l", 0, 3, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("l", 4, 1, StringComparison.CurrentCulture));
        Assert.Equal(-1, "Hello".IndexOf("L", 1, 4, StringComparison.CurrentCulture));

        Assert.Equal(2, "Hello".IndexOf("l", 1, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(3, "Hello".IndexOf("l", 3, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("l", 4, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(2, "Hello".IndexOf("L", 1, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("X", 1, StringComparison.CurrentCultureIgnoreCase));

        Assert.Equal(2, "Hello".IndexOf("l", 1, 4, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(3, "Hello".IndexOf("l", 3, 2, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("l", 3, 0, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("l", 0, 2, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(2, "Hello".IndexOf("l", 0, 3, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("l", 4, 1, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(-1, "Hello".IndexOf("X", 1, 4, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(1, "Hello".IndexOf("", 1, 4, StringComparison.CurrentCultureIgnoreCase));
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

    [Fact]
    public static void TestLastIndexOf()
    {
        int i;
        i = "Hello".LastIndexOf('l');
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf('x');
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf('l', 3);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf('l', 1);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf('l', 0);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf('x', 3);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf('l', 3, 4);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf('l', 1, 2);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf('l', 1, 0);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf('l', 4, 2);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf('l', 4, 3);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf('l', 0, 1);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf('x', 3, 4);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("llo");
        Assert.Equal(2, i);

        i = "Hello".LastIndexOf("LLO");
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("LLO", StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(2, i);

        i = "Hello".LastIndexOf("NoWay", StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 0);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 0);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("x", 3);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3, 4);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 1, 2);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 1, 0);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 4, 2);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 4, 3);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 0, 1);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("x", 3, 4);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3, StringComparison.CurrentCulture);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 1, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 0, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("L", 3, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3, 4, StringComparison.CurrentCulture);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 1, 2, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 1, 0, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 4, 2, StringComparison.CurrentCulture);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 4, 3, StringComparison.CurrentCulture);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 0, 1, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("L", 3, 4, StringComparison.CurrentCulture);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 1, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 0, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("L", 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("X", 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);

        i = "Hello".LastIndexOf("l", 3, 4, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 1, 2, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 1, 0, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("l", 4, 2, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 4, 3, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
        i = "Hello".LastIndexOf("l", 0, 1, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("X", 3, 4, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(-1, i);
        i = "Hello".LastIndexOf("", 3, 4, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(3, i);
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

    [Fact]
    public static void TestStartsWith()
    {
        String s = "Hello";
        bool b;
        b = s.StartsWith("Hel");
        Assert.True(b);
        b = s.StartsWith("Hello");
        Assert.True(b);
        b = s.StartsWith("");
        Assert.True(b);
        b = s.StartsWith("HELLO");
        Assert.False(b);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s.StartsWith(null);
            });

        b = s.StartsWith("Hel", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.StartsWith("Hello", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.StartsWith("", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.StartsWith("HEL", StringComparison.CurrentCultureIgnoreCase);
        Assert.True(b);
        b = s.StartsWith("Goodbye", StringComparison.CurrentCultureIgnoreCase);
        Assert.False(b);

        Assert.Throws<ArgumentNullException>(
            delegate ()
            {
                s.StartsWith(null, StringComparison.CurrentCultureIgnoreCase);
            });
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
    public static void TestToLowerToUpper()
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

        String Param_4_2312 = "/PUGETSOUNDTRAFFICMAP;COMPONENT/TRAFFICMAPWINDOW.XAML";
        bool Local_4_1 = Param_4_2312.StartsWith("/PUGETSOUNDTRAFFICMAP;COMPONENT/APP.XAML", StringComparison.Ordinal);
        Assert.False(Local_4_1);
    }
}

