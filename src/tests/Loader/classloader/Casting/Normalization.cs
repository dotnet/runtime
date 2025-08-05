// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Xunit;

public class NormalizationTests
{
    [Fact]
    public static void IntPtrArrayNormalization()
    {
        object x0 = new long[1];
        object x1 = new ulong[1]; 
        
        Assert.False(x0 is IntPtr[]);
        Assert.False(x1 is IntPtr[]);
    }
}
