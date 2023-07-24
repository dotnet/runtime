// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_42723
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Test(new S { X = 17, Y = 83 });
    }

    // On ARM32 we were asserting for a 8-byte aligned 12-byte struct as a parameter
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(S s) => (int)(s.X + s.Y);

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct S
    {
        public double X;
        public float Y;
    }
}
