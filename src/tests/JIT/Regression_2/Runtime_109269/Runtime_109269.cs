// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_108969
{
    [Fact]
    public static int TestEntryPoint() => (int)Foo(100.0).D;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Foo(double d)
    {
        S s = default;
        s.D = d;
        return s;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct S
    {
        public byte B;
        public double D;
    }
}