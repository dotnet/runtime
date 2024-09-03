// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

#pragma warning disable CS0612, CS0618

public class Test
{

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/65698", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/179", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static int TestEntryPoint()
    {
        try
        {
            string expected = "abcdefgh";
            string actual;
            string newValue = "zyxwvut\0";

            actual = expected;
            Assert.True(VBByRefStrNative.Marshal_Ansi(expected, ref actual, newValue));
            Assert.Equal(newValue, actual);

            actual = expected;
            Assert.True(VBByRefStrNative.Marshal_Unicode(expected, ref actual, newValue));
            Assert.Equal(newValue, actual);

            StringBuilder builder = new StringBuilder();

            Assert.Throws<MarshalDirectiveException>(() => VBByRefStrNative.Marshal_StringBuilder(ref builder));
            Assert.Throws<MarshalDirectiveException>(() => VBByRefStrNative.Marshal_ByVal(string.Empty));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
        return 100;
    }
}
