// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Globalization;
using Xunit;

public static class StringBuilderTests
{
    [Fact]
    public static void TestToString()
    {
        StringBuilder sb = new StringBuilder("Finally");
        String s = sb.ToString(2, 3);
        Assert.Equal(s, "nal");
    }

    [Fact]
    public static void TestReplace()
    {
        StringBuilder sb;
        String s;

        sb = new StringBuilder("aaaabbbbccccdddd");
        sb.Replace('a', '!', 2, 3);
        s = sb.ToString();
        Assert.Equal(s, "aa!!bbbbccccdddd");

        sb = new StringBuilder("aaaabbbbccccdddd");
        sb.Replace("a", "$!", 2, 3);
        s = sb.ToString();
        Assert.Equal(s, "aa$!$!bbbbccccdddd");
    }

    [Fact]
    public static void TestRemove()
    {
        StringBuilder sb = new StringBuilder("Almost");
        sb.Remove(1, 3);
        String s = sb.ToString();
        Assert.Equal(s, "Ast");
    }

    [Fact]
    public static void TestInsert()
    {
        //@todo: Not testing all the Insert() overloads that just call ToString() on the input and forward to Insert(int, String).
        StringBuilder sb = new StringBuilder("Hello");
        sb.Insert(2, "!!");
        String s = sb.ToString();
        Assert.Equal(s, "He!!llo");
    }

    [Fact]
    public static void TestEquals()
    {
        StringBuilder sb1 = new StringBuilder("Hello");
        StringBuilder sb2 = new StringBuilder("HelloX");

        bool b;
        b = sb1.Equals(sb1);
        Assert.True(b);
        b = sb1.Equals(sb2);
        Assert.False(b);
    }

    [Fact]
    public static void TestCopyTo()
    {
        StringBuilder sb;
        sb = new StringBuilder("Hello");
        char[] ca = new char[10];
        sb.CopyTo(1, ca, 5, 4);
        Assert.Equal(ca[0], 0);
        Assert.Equal(ca[1], 0);
        Assert.Equal(ca[2], 0);
        Assert.Equal(ca[3], 0);
        Assert.Equal(ca[4], 0);
        Assert.Equal(ca[5], 'e');
        Assert.Equal(ca[6], 'l');
        Assert.Equal(ca[7], 'l');
        Assert.Equal(ca[8], 'o');
        Assert.Equal(ca[9], 0);
    }

    [Fact]
    public static void TestClear()
    {
        StringBuilder sb;
        String s;

        sb = new StringBuilder("Hello");
        sb.Clear();
        s = sb.ToString();
        Assert.Equal(s, "");
    }

    [Fact]
    public static void TestLength()
    {
        StringBuilder sb;
        String s;
        int len;

        sb = new StringBuilder("Hello");
        len = sb.Length;
        Assert.Equal(len, 5);
        sb.Length = 2;
        len = sb.Length;
        Assert.Equal(len, 2);
        s = sb.ToString();
        Assert.Equal(s, "He");
        sb.Length = 10;
        len = sb.Length;
        Assert.Equal(len, 10);
        s = sb.ToString();
        Assert.Equal(s, "He" + new String((char)0, 8));
    }

    [Fact]
    public static void TestAppendFormat()
    {
        StringBuilder sb;
        String s;

        sb = new StringBuilder();
        sb.AppendFormat("Foo {0} Bar {1}", "Red", "Green");
        s = sb.ToString();
        Assert.Equal(s, "Foo Red Bar Green");
    }

    [Fact]
    public static void TestAppend()
    {
        //@todo: Skipped all the Append overloads that just call ToString() on the argument and delegate to Append(String)

        StringBuilder sb;
        String s;

        sb = new StringBuilder();
        s = "";
        for (int i = 0; i < 500; i++)
        {
            char c = (char)(0x41 + (i % 10));
            sb.Append(c);
            s += c;
            Assert.Equal(sb.ToString(), s);
        }

        sb = new StringBuilder();
        s = "";
        for (int i = 0; i < 500; i++)
        {
            char c = (char)(0x41 + (i % 10));
            int repeat = i % 8;
            sb.Append(c, repeat);
            s += new String(c, repeat);
            Assert.Equal(sb.ToString(), s);
        }

        sb = new StringBuilder();
        s = "";
        for (int i = 0; i < 500; i++)
        {
            char c = (char)(0x41 + (i % 10));
            int repeat = i % 8;
            char[] ca = new char[repeat];
            for (int j = 0; j < ca.Length; j++)
                ca[j] = c;
            sb.Append(ca);
            s += new String(ca);
            Assert.Equal(sb.ToString(), s);
        }

        sb = new StringBuilder();
        s = "";
        for (int i = 0; i < 500; i++)
        {
            sb.Append("ab");
            s += "ab";
            Assert.Equal(sb.ToString(), s);
        }

        sb = new StringBuilder();
        s = "Hello";
        sb.Append(s, 2, 3);
        Assert.Equal(sb.ToString(), "llo");
    }

    [Fact]
    public static void TestChars()
    {
        StringBuilder sb = new StringBuilder("Hello");
        char c = sb[1];
        Assert.Equal(c, 'e');
        sb[1] = 'X';
        String s = sb.ToString();
        Assert.Equal(s, "HXllo");
    }

    [Fact]
    public static void TestCtors()
    {
        StringBuilder sb;
        String s;
        int c;
        int l;
        int m;

        sb = new StringBuilder();
        s = sb.ToString();
        Assert.Equal(s, "");
        l = sb.Length;
        Assert.Equal(l, 0);

        sb = new StringBuilder(42);
        c = sb.Capacity;
        Assert.True(c >= 42);
        l = sb.Length;
        Assert.Equal(l, 0);

        // The second int parameter is MaxCapacity but in CLR4.0 and later, StringBuilder isn't required to honor it.
        sb = new StringBuilder(42, 50);
        c = sb.Capacity;
        Assert.True(c >= 42);
        l = sb.Length;
        Assert.Equal(l, 0);
        m = sb.MaxCapacity;
        Assert.Equal(m, 50);

        sb = new StringBuilder("Hello");
        s = sb.ToString();
        Assert.Equal(s, "Hello");
        l = sb.Length;
        Assert.Equal(l, 5);

        sb = new StringBuilder("Hello", 42);
        s = sb.ToString();
        Assert.Equal(s, "Hello");
        c = sb.Capacity;
        Assert.True(c >= 42);
        l = sb.Length;
        Assert.Equal(l, 5);

        sb = new StringBuilder("Hello", 2, 3, 42);
        s = sb.ToString();
        Assert.Equal(s, "llo");
        c = sb.Capacity;
        Assert.True(c >= 42);
        l = sb.Length;
        Assert.Equal(l, 3);
    }

    [Fact]
    public unsafe static void TestAppendUsingNativePointers()
    {
        StringBuilder sb = new StringBuilder();
        string s = "abc ";
        fixed (char* pS = s)
        {
            sb.Append(pS, s.Length);
            sb.Append(pS, s.Length);
        }
        Assert.True("abc abc ".Equals(sb.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public unsafe static void TestAppendUsingNativePointerExceptions()
    {
        StringBuilder sb = new StringBuilder();
        string s = "abc ";
        fixed (char* pS = s)
        {
            Assert.Throws<NullReferenceException>(() => sb.Append(null, s.Length));

            char* p = pS; // cannot use pS directly inside an anonymous method 
            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(p, -1));
        }
    }
}
