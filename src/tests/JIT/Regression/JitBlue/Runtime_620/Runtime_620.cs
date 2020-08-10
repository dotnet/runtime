// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace NormalizeTest
{
    class Program
    {
        static int testResult = 100;
        static bool s_print = false;

        ////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ByteTest1(sbyte a, int b)
        {
            sbyte c = (sbyte)(a * 2);
            byte t = *((byte*)&c);
            if (s_print)
            {
                Console.WriteLine(t);
            }
            int d = ((int) t) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe sbyte ByteTest2(sbyte a, int b)
        {
            sbyte c = (sbyte)(b * 2);
            *((byte*)&c) = (byte)(a * b);
            return c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ByteTest3(byte a, int b)
        {
            byte c = (byte)(a * 2);
            int d = *((sbyte*)&c) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte ByteTest4(byte a, int b)
        {
            byte c = (byte)(b * 2);
            *((sbyte*)&c) = (sbyte)(a * b);
            return c;
        }

        struct S1 {
            public long  l64;
            public sbyte s8;
            public byte  u8;
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ByteTestField1(sbyte a, int b)
        {
            S1 s;
            s.l64 = 0;
            s.s8 = (sbyte)(a * 2);
            int d = *((byte*)&s.s8) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe sbyte ByteTestField2(sbyte a, int b)
        {
            S1 s;
            s.s8 = (sbyte)(b * 2);
            *((byte*)&s.s8) = (byte)(a * b);
            return s.s8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ByteTestField3(byte a, int b)
        {
            S1 s;
            s.u8 = (byte)(a * 2);
            int d = *((sbyte*)&s.u8) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte ByteTestField4(byte a, int b)
        {
            S1 s;
            s.u8 = (byte)(b * 2);
            *((sbyte*)&s.u8) = (sbyte)(a * b);
            return s.u8;
        }

        ////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ShortTest1(short a, int b)
        {
            short c = (short)(a * 2);
            ushort t = *((ushort*)&c);
            if (s_print)
            {
                Console.WriteLine(t);
            }
            int d = ((int) t) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe short ShortTest2(short a, int b)
        {
            short c = (short)(b * 2);
            *((ushort*)&c) = (ushort)(a * b);
            return c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ShortTest3(ushort a, int b)
        {
            ushort c = (ushort)(a * 2);
            int d = *((short*)&c) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ushort ShortTest4(ushort a, int b)
        {
            ushort c = (ushort)(b * 2);
            *((short*)&c) = (short)(a * b);
            return c;
        }

        struct S2 {
            public long   l64;
            public short  s16;
            public ushort u16;
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ShortTestField1(short a, int b)
        {
            S2 s;
            s.l64 = 0;
            s.s16 = (short)(a * 2);
            int d = *((ushort*)&s.s16) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe short ShortTestField2(short a, int b)
        {
            S2 s;
            s.s16 = (short)(b * 2);
            *((ushort*)&s.s16) = (ushort)(a * b);
            return s.s16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int ShortTestField3(ushort a, int b)
        {
            S2 s;
            s.u16 = (ushort)(a * 2);
            int d = *((short*)&s.u16) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ushort ShortTestField4(ushort a, int b)
        {
            S2 s;
            s.u16 = (ushort)(b * 2);
            *((short*)&s.u16) = (short)(a * b);
            return s.u16;
        }

        ////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe long IntTest1(int a, long b)
        {
            int c = (int)(a * 2);
            uint t = *((uint*)&c);
            if (s_print)
            {
                Console.WriteLine(t);
            }
            long d = ((long) t) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int IntTest2(int a, long b)
        {
            int c = (int)(b * 2);
            *((uint*)&c) = (uint)(a * b);
            return c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe long IntTest3(uint a, long b)
        {
            uint c = (uint)(a * 2);
            long d = *((int*)&c) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint IntTest4(uint a, long b)
        {
            uint c = (uint)(b * 2);
            *((int*)&c) = (int)(a * b);
            return c;
        }

        struct S3 {
            public long l64;
            public int  s32;
            public uint u32;
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe long IntTestField1(int a, long b)
        {
            S3 s;
            s.l64 = 0;
            s.s32 = (int)(a * 2);
            long d = *((uint*)&s.s32) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int IntTestField2(int a, long b)
        {
            S3 s;
            s.s32 = (int)(b * 2);
            *((uint*)&s.s32) = (uint)(a * b);
            return s.s32;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe long IntTestField3(uint a, long b)
        {
            S3 s;
            s.u32 = (uint)(a * 2);
            long d = *((int*)&s.u32) / b;
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint IntTestField4(uint a, long b)
        {
            S3 s;
            s.u32 = (uint)(b * 2);
            *((int*)&s.u32) = (int)(a * b);
            return s.u32;
        }

        ////////////////////////////////////////////////////////////

        static void CheckInt(String id, int result, int expected)
        {
            if (result != expected)
            {
                Console.WriteLine("CheckInt - FAILED: {0} -- result {1}, expected {2}", id, result, expected);
                testResult = -1;
            }
        }

        static void CheckLong(String id, long result, long expected)
        {
            if (result != expected)
            {
                Console.WriteLine("CheckLong - FAILED: {0} -- result {1}, expected {2}", id, result, expected);
                testResult = -1;
            }
        }

        static int Main()
        {
            {
                int    result1a = ByteTest1(-1,1);
                CheckInt("ByteTest1a", result1a, 0xFE);
                
                int    result1b = ByteTest1(-1,-1);
                CheckInt("ByteTest1b", result1b, -0xFE);
                
                sbyte   result2a = ByteTest2(-1,1);
                CheckInt("ByteTest2a", (int) result2a, -1);
                
                sbyte  result2b = ByteTest2(-1,-1);
                CheckInt("ByteTest2b", (int) result2b, 1);
                
                int    result3 = ByteTest3(0x7F,-1);
                CheckInt("ByteTest3", result3, 2);
                
                byte   result4 = ByteTest4(0x7F,-1);
                CheckInt("ByteTest4", (int) result4, 0x81);
                
                int    resultF1a = ByteTestField1(-1,1);
                CheckInt("ByteTestField1a", resultF1a, 0xFE);
                
                int    resultF1b = ByteTestField1(-1,-1);
                CheckInt("ByteTestField1b", resultF1b, -0xFE);
                
                sbyte  resultF2a = ByteTestField2(-1,1);
                CheckInt("ByteTestField2a", (int) resultF2a, -1);
                
                sbyte  resultF2b = ByteTestField2(-1,-1);
                CheckInt("ByteTestField2b", (int) resultF2b, 1);
                
                int    resultF3 = ByteTestField3(0x7F,-1);
                CheckInt("ByteTestField3", resultF3, 2);
                
                byte   resultF4 = ByteTestField4(0x7F,-1);
                CheckInt("ByteTestField4", (int) resultF4, 0x81);
            }
            ////////////////////////////////////////////////////////////
            {
                int    result1a = ShortTest1(-1,1);
                CheckInt("ShortTest1a", result1a, 0xFFFE);
                
                int    result1b = ShortTest1(-1,-1);
                CheckInt("ShortTest1b", result1b, -0xFFFE);
                
                short  result2a = ShortTest2(-1,1);
                CheckInt("ShortTest2a", (int) result2a, -1);
                
                short  result2b = ShortTest2(-1,-1);
                CheckInt("ShortTest2b", (int) result2b, 1);
                
                int    result3 = ShortTest3(0x7FFF,-1);
                CheckInt("ShortTest3", result3, 2);
                
                ushort result4 = ShortTest4(0x7FFF,-1);
                CheckInt("ShortTest4", (int) result4, 0x8001);
                
                int    resultF1a = ShortTestField1(-1,1);
                CheckInt("ShortTestField1a", resultF1a, 0xFFFE);
                
                int    resultF1b = ShortTestField1(-1,-1);
                CheckInt("ShortTestField1b", resultF1b, -0xFFFE);
                
                short  resultF2a = ShortTestField2(-1,1);
                CheckInt("ShortTestField2a", (int) resultF2a, -1);
                
                short  resultF2b = ShortTestField2(-1,-1);
                CheckInt("ShortTestField2b", (int) resultF2b, 1);
                
                int    resultF3 = ShortTestField3(0x7FFF,-1);
                CheckInt("ShortTestField3", resultF3, 2);
                
                ushort resultF4 = ShortTestField4(0x7FFF,-1);
                CheckInt("ShortTestField4", (int) resultF4, 0x8001);
            }
            ////////////////////////////////////////////////////////////
            {
                long    result1a = IntTest1(-1,1);
                CheckLong("IntTest1a", result1a, 0xFFFFFFFE);
                
                long    result1b = IntTest1(-1,-1);
                CheckLong("IntTest1b", result1b, -0xFFFFFFFE);
                
                int     result2a = IntTest2(-1,1);
                CheckLong("IntTest2a", (long) result2a, -1);
                
                int     result2b = IntTest2(-1,-1);
                CheckLong("IntTest2b", (long) result2b, 1);
                
                long    result3 = IntTest3(0x7FFFFFFF,-1);
                CheckLong("IntTest3", result3, 2);
                
                uint result4 = IntTest4(0x7FFFFFFF,-1);
                CheckLong("IntTest4", (long) result4, 0x80000001);
                
                long    resultF1a = IntTestField1(-1,1);
                CheckLong("IntTestField1a", resultF1a, 0xFFFFFFFE);
                
                long    resultF1b = IntTestField1(-1,-1);
                CheckLong("IntTestField1b", resultF1b, -0xFFFFFFFE);
                
                int     resultF2a = IntTestField2(-1,1);
                CheckLong("IntTestField2a", (long) resultF2a, -1);
                
                int     resultF2b = IntTestField2(-1,-1);
                CheckLong("IntTestField2b", (long) resultF2b, 1);
                
                long    resultF3 = IntTestField3(0x7FFFFFFF,-1);
                CheckLong("IntTestField3", resultF3, 2);
                
                uint    resultF4 = IntTestField4(0x7FFFFFFF,-1);
                CheckLong("IntTestField4", (long) resultF4, 0x80000001);
            }
            ////////////////////////////////////////////////////////////
            
            if (testResult == 100)
            {
                Console.WriteLine("Test Passed");
            }
            return testResult;
        }
    }
}
