// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using TestLibrary;

using static LPTStrTestNative;

class LPTStrTest
{
    private static readonly string InitialString = "Hello World";

    public static int Main()
    {
        try
        {
            CommonStringTests.RunTests();
            RunStringBuilderTests();
            RunByValTStrTests();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return 101;
        }
        return 100;
    }

    private static void RunStringBuilderTests()
    {
        int length = 10;
        StringBuilder nullTerminatorBuilder = new StringBuilder(length);
        Assert.IsTrue(Verify_NullTerminators_PastEnd(nullTerminatorBuilder, length));
        Assert.IsTrue(Verify_NullTerminators_PastEnd_Out(nullTerminatorBuilder, length));
    }

    private static void RunByValTStrTests()
    {
        Assert.IsTrue(MatchFuncNameAnsi(new ByValStringInStructAnsi { str = nameof(MatchFuncNameAnsi)}));

        var ansiStr = new ByValStringInStructAnsi
        {
            str = InitialString
        };

        ReverseByValStringAnsi(ref ansiStr);

        Assert.AreEqual(Helpers.Reverse(InitialString), ansiStr.str);

        Assert.IsTrue(MatchFuncNameUni(new ByValStringInStructUnicode { str = nameof(MatchFuncNameUni)}));

        var uniStr = new ByValStringInStructUnicode
        {
            str = InitialString
        };

        ReverseByValStringUni(ref uniStr);

        Assert.AreEqual(Helpers.Reverse(InitialString), uniStr.str);
    }
}
