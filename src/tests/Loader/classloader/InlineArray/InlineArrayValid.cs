// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

[InlineArray(42)]
struct FourtyTwoBytes
{
    byte b;
}

unsafe class Validate
{
    [Fact]
    public static void Sizeof()
    {
        Console.WriteLine($"{nameof(Sizeof)}...");
        Assert.Equal(42, sizeof(FourtyTwoBytes));
    } 
}
