// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    // Verify that containment on NEG is correctly handled for ARM64.
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(int x) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(bool x) { }
        //---------------------------------
        public static uint s_2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test1()
        {
            int vr0 = default(int);
            if (56058 < (uint)(-s_2))
            {
                Consume(vr0);
                return 0;
            }
            return 100;
        }
        //---------------------------------
        public class C0
        {
            public bool F8;
        }

        public static C0 s_11;
        public static byte s_35;
        public static sbyte s_44;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test2()
        {
            try
            {
                s_11.F8 |= s_35 < (-(1 << s_44));
                return 0;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
        //---------------------------------
        public static uint s_4;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test3()
        {
            return M17(0);
        }

        public static int M17(long arg0)
        {
            short var0 = default(short);
            if ((ulong)((-s_4) & arg0) >= 1)
            {
                Consume(var0);
                return 0;
            }
            return 100;
        }
        //---------------------------------
        public static long s_7;
        public static int[] s_12 = new int[] { 0 };

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test4()
        {
            s_12[0] = -2147483648;
            var vr9 = (int)s_7 < (-s_12[0]);
            Consume(vr9);
            return vr9 ? 0 : 100;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (Program.Test1() != 100)
            return 0;

        if (Program.Test2() != 100)
            return 0;

        if (Program.Test3() != 100)
            return 0;

        if (Program.Test4() != 100)
            return 0;

        return 100;
    }
}
