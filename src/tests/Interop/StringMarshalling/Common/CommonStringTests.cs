// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

using static StringMarshalingTestNative;

class CommonStringTests
{
    private static readonly string InitialString = "Hello World";

    public static void RunTests(bool runStringBuilderTests = true, bool runStructTests = true)
    {
        RunStringTests();
        if (runStringBuilderTests)
        {
            RunStringBuilderTests();
        }
        if (runStructTests)
        {
            RunStructTests();
        }
    }

    private static void RunStringTests()
    {
        Assert.True(MatchFunctionName(nameof(MatchFunctionName)));
        {
            string funcNameLocal = nameof(MatchFunctionNameByRef);
            Assert.True(MatchFunctionNameByRef(ref funcNameLocal));
        }

        {
            string reversed = InitialString;
            ReverseInplaceByref(ref reversed);
            Assert.Equal(Helpers.Reverse(InitialString), reversed);
        }

        {
            Reverse(InitialString, out string reversed);
            Assert.Equal(Helpers.Reverse(InitialString), reversed);
        }

        Assert.Equal(Helpers.Reverse(InitialString), ReverseAndReturn(InitialString));

        Assert.True(VerifyReversed(InitialString, (orig, rev) => rev == Helpers.Reverse(orig)));

        Assert.True(ReverseInCallback(InitialString, (string str, out string rev) => rev = Helpers.Reverse(InitialString)));

        Assert.True(ReverseInCallbackReturned(InitialString, str => Helpers.Reverse(str)));
    }

    private static void RunStringBuilderTests()
    {
        var builder = new StringBuilder(InitialString);
        ReverseInplace(builder);
        Assert.Equal(Helpers.Reverse(InitialString), builder.ToString());

        builder = new StringBuilder(InitialString);
        ReverseInplaceByref(ref builder);
        Assert.Equal(Helpers.Reverse(InitialString), builder.ToString());

        builder = new StringBuilder(InitialString);
        Assert.True(ReverseInplaceInCallback(builder, b =>
        {
            string reversed = Helpers.Reverse(b.ToString());
            b.Clear();
            b.Append(reversed);
        }));
    }

    private static void RunStructTests()
    {
        Assert.True(MatchFunctionNameInStruct(new StringInStruct { str = nameof(MatchFunctionNameInStruct)}));

        var str = new StringInStruct
        {
            str = InitialString
        };

        ReverseInplaceByrefInStruct(ref str);

        Assert.Equal(Helpers.Reverse(InitialString), str.str);
    }
}
