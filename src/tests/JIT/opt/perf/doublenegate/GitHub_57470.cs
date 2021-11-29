// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This file is auto-generated.
// Seed: -1
//
using System;
using System.Runtime.CompilerServices;
public class TestClass8505
{
    static int s_int32_6 = -5;
    static sbyte s_sbyte_8 = 2;
    static int s_loopInvariant = 3;
    public sbyte LeafMethod8()
    {
        unchecked
        {
            return s_sbyte_8 <<= s_int32_6 >>= s_int32_6 ^ (-2 - (s_int32_6 &= -5)) / (-1 * s_int32_6 * (2 ^ -2)) + 77;
        }
    }
    public void Method0()
    {
        unchecked
        {
            try
            {
            }
            finally
            {
                {
                    int __loopvar1 = s_loopInvariant, __loopSecondaryVar1_0 = 15 - 4;
                    do
                    {
                    }
                    while (15 % 4 > LeafMethod8() / 15 + 4);
                }
                {
                }
            }
            return;
        }
    }
    public static int Main(string[] args)
    {
        TestClass8505 objTestClass8505 = new TestClass8505();
        objTestClass8505.Method0();
        return 100;
    }
}
