// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

#pragma warning disable CS0612, CS0618

class Test
{

    public static int Main(string[] args)
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
