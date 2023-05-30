// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    public class C0
    {
        public C0(ulong f0, sbyte f1, byte f2, ulong f3, ulong f4, int f5, short f6)
        {
        }
    }

    // This is trying to verify that we eliminate 'mov' instructions correctly.
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ulong Consume(ulong x) { return x; }

        public static ushort s_1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ulong M0()
        {
            ulong var0 = ~(ulong)(uint)(-15356 * (2637600427U % (byte)((2147483647 ^ (4167361218894137384UL * (ushort)(-(ushort)(-(ushort)(128 * (5114990800133743712L % (byte)((byte)(-(byte)(-(byte)(-(byte)~(byte)(-2 % (short)((short)~(short)(-(short)(-90400400 - (-(0 | ~~(int)(32766 & (uint)(-~(uint)(-(uint)(629572031969723397L ^ (sbyte)(1UL * (byte)(8945663325738713761L / ((-(long)(127 / (sbyte)((sbyte)(-23817 % (ushort)((ushort)~M1(new C0(8144643251292930980UL, -118, 183, 18446744073709551614UL, 1827525571111008345UL, 1751590714, -32653)) | 1)) | 1))) | 1))))))))))) | 1))))) | 1))))))) | 1)));
            return Program.Consume(var0);
        }

        public static ushort M1(C0 argThis)
        {
            return s_1;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var result = Test.Program.M0();
        if (result != 18446744069414922151)
        {
            return 0;
        }
        return 100;
    }
}
