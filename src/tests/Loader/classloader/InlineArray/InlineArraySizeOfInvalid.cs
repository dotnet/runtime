// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

unsafe class Validate
{
    [InlineArray(1)]
    [StructLayout(LayoutKind.Explicit)]
    private struct Explicit
    {
        [FieldOffset(0)]
        public Guid Guid;
    }

    [Fact]
    public static void Explicit_SizeOf_Fails()
    {
        Console.WriteLine($"{nameof(Explicit_SizeOf_Fails)}...");
        Assert.Throws<TypeLoadException>(() =>
        {
            return sizeof(Explicit);
        });
    }
}
