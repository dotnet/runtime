// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    // This is trying to verify that we properly zero-extend on all platforms.
    public class Program
    {
        public static long s_15;
        public static sbyte s_17;
        public static ushort s_21 = 36659;

        [Fact]
        public static int Test()
        {
            s_15 = ~1;
            return M40(0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(ushort x) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int M40(ushort arg0)
        {
            for (int var0 = 0; var0 < 2; var0++)
            {
                arg0 = 65535;
                arg0 &= (ushort)(s_15++ >> s_17);
                arg0 %= s_21;
            }

            Consume(arg0);

            if (arg0 != 28876)
            {
                return 0;
            }
            return 100;
        }
    }

    public class Program2
    {
        public static long s_15;
        public static sbyte s_17;
        public static ushort s_21 = 36659;

        [Fact]
        public static int Test()
        {
            s_15 = ~1;
            return M40();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(ushort x) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int M40()
        {
            S s = default;
            for (int var0 = 0; var0 < 2; var0++)
            {
                s.U = 65535;
                s.U &= (ushort)(s_15++ >> s_17);
                s.U %= s_21;
            }

            Consume(s.U);

            if (s.U != 28876)
            {
                return 0;
            }
            return 100;
        }

        public struct S { public ushort U; }
    }
}
