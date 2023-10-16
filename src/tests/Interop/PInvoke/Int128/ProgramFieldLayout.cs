// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public unsafe partial class Int128NativeFieldLayout
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing Int128");
            Int128Native.TestInt128FieldLayout();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 0;
        }
        return 100;
    }
}
