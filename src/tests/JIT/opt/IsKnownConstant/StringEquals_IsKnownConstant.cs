// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        AssertEquals(true, TestEquals1(""));
        AssertEquals(false, TestEquals1(null));
        AssertEquals(false, TestEquals1("1"));

        AssertEquals(true, TestEquals2(""));
        AssertEquals(false, TestEquals2(null));
        AssertEquals(false, TestEquals2("1"));

        AssertEquals(true, TestEquals3(""));
        AssertEquals(false, TestEquals3(null));
        AssertEquals(false, TestEquals3("1"));

        AssertEquals(true, TestEquals4(""));
        AssertEquals(false, TestEquals4(null));
        AssertEquals(false, TestEquals4("1"));

        AssertEquals(true, TestEquals5(""));
        AssertEquals(false, TestEquals5(null));
        AssertEquals(false, TestEquals5("1"));

        AssertEquals(false, TestEquals6(""));
        AssertEquals(true, TestEquals6(null));
        AssertEquals(false, TestEquals6("1"));

        AssertEquals(false, TestEquals7());
        AssertEquals(false, TestEquals8());

        AssertEquals(true, TestEquals5(""));
        AssertEquals(false, TestEquals5(null));
        AssertEquals(false, TestEquals5("1"));

        AssertEquals(true, TestStartWith("c"));
        AssertEquals(false, TestStartWith("C"));
        AssertThrowsNRE(() => TestStartWith(null));
        
        return 100;
    }

    private static void AssertEquals(bool expected, bool actual, [CallerLineNumber]int l = 0)
    {
        if (expected != actual)
            throw new InvalidOperationException();
    }

    private static void AssertThrowsNRE(Action a)
    {
        try
        {
            a();
        }
        catch (NullReferenceException)
        {
            return;
        }
        throw new InvalidOperationException();
    }

    private static string NullStr() => null;
    private static string EmptyStr() => "";
    private static string NonEmptyStr() => "1";


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals1(string str) => str == "";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals2(string str) => str == string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals3(string str) => "" == str;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals4(string str) => string.Empty == str;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals5(string str) => string.Empty == str;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals6(string str) => string.Equals(NullStr(), str);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals7() => string.Equals(NullStr(), EmptyStr());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestEquals8() => string.Equals(NullStr(), NonEmptyStr());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestStartWith(string str) => str.StartsWith('c');
}
