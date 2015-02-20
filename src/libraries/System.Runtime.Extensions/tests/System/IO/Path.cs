// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class PathTests
{
    [Fact]
    public static void TestChangeExtension()
    {
        String s;

        s = Path.ChangeExtension("foo.txt", "exe");
        Assert.Equal(s, "foo.exe");
        return;
    }

    [Fact]
    public static void TestCombine()
    {
        String s = Path.Combine(@"c:\", "foo", "bar", "baz.exe");
        Assert.Equal(s, @"c:\foo\bar\baz.exe");

        s = Path.Combine();
        Assert.Equal(s, String.Empty);
    }

    [Fact]
    public static void TestGetDirectoryName()
    {
        String s;

        s = Path.GetDirectoryName(@"foo\baz");
        Assert.Equal(s, @"foo");

        s = Path.GetDirectoryName(@"c:\");
        Assert.Equal(s, null);
    }

    [Fact]
    public static void TestShortFileName()
    {
        //@todo: Not implemented yet.
        //String s = Path.GetDirectoryName(@"c:\x~x\yy\zz.txt");
        //Console.WriteLine(s);
    }

    [Fact]
    public static void TestGetExtension()
    {
        String s;
        bool b;

        s = Path.GetExtension("foo.exe");
        Assert.Equal(s, ".exe");

        b = Path.HasExtension("foo.exe");
        Assert.True(b);

        s = Path.GetExtension("foo");
        Assert.Equal(s, "");

        b = Path.HasExtension("foo");
        Assert.False(b);
    }

    [Fact]
    public static void TestGetFileName()
    {
        String s;
        s = Path.GetFileName(@"c:\bar\baz\foo.exe");
        Assert.Equal(s, "foo.exe");

        s = Path.GetFileName(@"c:\bar\baz\");
        Assert.Equal(s, "");
    }

    [Fact]
    public static void TestGetFileNameWithoutExtension()
    {
        String s;
        s = Path.GetFileNameWithoutExtension(@"c:\bar\baz\foo.exe");
        Assert.Equal(s, "foo");

        s = Path.GetFileNameWithoutExtension(@"c:\bar\baz\");
        Assert.Equal(s, "");
    }

    [Fact]
    public static void TestGetPathRoot()
    {
        String s;
        bool b;
        s = Path.GetPathRoot(@"c:\foo\bar\bar\");
        Assert.Equal(s, @"c:\");
        b = Path.IsPathRooted(@"c:\foo\bar\bar\");
        Assert.True(b);
        s = Path.GetPathRoot(@"foo.exe");
        Assert.Equal(s, @"");
        b = Path.IsPathRooted(@"foo.exe");
        Assert.False(b);
    }

    [Fact]
    public static void TestGetRandomFileName()
    {
        for (int i = 0; i < 100; i++)
        {
            String s = Path.GetRandomFileName();
            Assert.Equal(s.Length, 8 + 1 + 3);
            Assert.Equal(s[8], '.');
        }
    }

    [Fact]
    public static void TestShortNamesExpansion()
    {
        // we are not sure if 'c:\Program Files' exist so the result should be 
        // either 'PROGRA~1' or 'Program Files'
        string s = Path.GetDirectoryName(@"C:\PROGRA~1\");
        Assert.True(s.Equals(@"c:\PROGRA~1", StringComparison.OrdinalIgnoreCase) || s.Equals(@"c:\Program Files", StringComparison.OrdinalIgnoreCase));
    }
}

