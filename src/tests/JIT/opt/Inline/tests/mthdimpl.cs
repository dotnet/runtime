// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;
public class MthdImpl
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int f(int a)
    {
        return a + 3;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int retval = f(97);
        return retval;
    }
}
