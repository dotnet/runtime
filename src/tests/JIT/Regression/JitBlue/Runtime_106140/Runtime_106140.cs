// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_106140
{
    public struct S1
    {
        public int F0;
        public Vector256<short> F1;
        public S1(bool f3) : this()
        {
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        S1[][,] vr0 = new S1[][,]
        {
            new S1[, ]
            {
                {
                    new S1(false)
                }
            }
        };
    }
}