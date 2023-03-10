// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test originally showed incorrect VN for different fields with the same offset.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Opt_Error
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public class FourByteClass
    {
        [FieldOffset(0)]
        public int val;
        [FieldOffset(0)]
        public uint uval;
        [FieldOffset(0)]
        public float fval;
        [FieldOffset(0)]
        public byte b0;
        [FieldOffset(1)]
        public byte b1;
        [FieldOffset(2)]
        public byte b2;
        [FieldOffset(3)]
        public byte b3;

        public FourByteClass(int val)
        {
            val = val;
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct FourByteStruct
    {
        [FieldOffset(0)]
        public int val;
        [FieldOffset(0)]
        public uint uval;
        [FieldOffset(0)]
        public float fval;
        [FieldOffset(0)]
        public byte b0;
        [FieldOffset(1)]
        public byte b1;
        [FieldOffset(2)]
        public byte b2;
        [FieldOffset(3)]
        public byte b3;

        public FourByteStruct(int val)
        {
            this.val = 0;
            uval = 0;
            fval = 0;
            b0 = 0;
            b1 = 0;
            b2 = 0;
            b3 = 0;
            this.val = val;
        }
    }

    public class Program
    {
        static void TestClass(int initVal)
        {
            FourByteClass fb = new FourByteClass(initVal);
            fb.fval = 0;
            fb.b0 = 1;
            fb.uval = 2;

            int cseb0_1 = fb.b0 * 5 + 3;
            uint cse_uval_1 = fb.uval * 2 - 5 + fb.uval;
            int cse_val_1 = fb.val * 7 - 4 + fb.val * 7;

            Console.WriteLine("First result: " + cseb0_1 + ", " + cse_uval_1 + ", " + cse_val_1 + ";");
            Debug.Assert(cseb0_1 == 13);
            Debug.Assert(cse_uval_1 == 1);
            Debug.Assert(cse_val_1 == 24);
            fb.val = 4;
            int cseb0_2 = fb.b0 * 5 + 3;
            uint cse_uval_2 = fb.uval * 2 - 5 + fb.uval;
            int cse_val_2 = fb.val * 7 - 4 + fb.val * 7;

            Console.WriteLine("Second result: " + cseb0_2 + ", " + cse_uval_2 + ", " + cse_val_2 + ";");
            Debug.Assert(cseb0_2 == 23);
            Debug.Assert(cse_uval_2 == 7);
            Debug.Assert(cse_val_2 == 52);
        }

        static void TestStruct(int initVal)
        {
            FourByteStruct fb = new FourByteStruct(initVal);
            fb.fval = 0;
            fb.b0 = 1;
            fb.uval = 2;

            int cseb0_1 = fb.b0 * 5 + 3;
            uint cse_uval_1 = fb.uval * 2 - 5 + fb.uval;
            int cse_val_1 = fb.val * 7 - 4 + fb.val * 7;

            Console.WriteLine("First result: " + cseb0_1 + ", " + cse_uval_1 + ", " + cse_val_1 + ";");
            Debug.Assert(cseb0_1 == 13);
            Debug.Assert(cse_uval_1 == 1);
            Debug.Assert(cse_val_1 == 24);
            fb.val = 4;
            int cseb0_2 = fb.b0 * 5 + 3;
            uint cse_uval_2 = fb.uval * 2 - 5 + fb.uval;
            int cse_val_2 = fb.val * 7 - 4 + fb.val * 7;

            Console.WriteLine("Second result: " + cseb0_2 + ", " + cse_uval_2 + ", " + cse_val_2 + ";");
            Debug.Assert(cseb0_2 == 23);
            Debug.Assert(cse_uval_2 == 7);
            Debug.Assert(cse_val_2 == 52);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TestClass(2);
            TestStruct(2);
            return 100;
        }
    }
}
