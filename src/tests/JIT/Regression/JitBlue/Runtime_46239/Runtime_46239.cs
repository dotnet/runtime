// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test was reproducing an issue on Arm32 when we required an 8-byte alignment
// for a struct which size was rounded to 4-byte.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_46239
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct S1 // Marshal.SizeOf 12 bytes, EE getClassSize 12 (here and below for arm32).
    {
        public ulong tmp1;
        public Object q;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct S2 // Marshal.SizeOf 12 bytes, EE getClassSize 12
    {
        public ulong tmp1;
        public int tmp2;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct S3 // Marshal.SizeOf 16 bytes, EE getClassSize 12
    {
        [FieldOffset(0)]
        public ulong tmp1;
        [FieldOffset(8)]
        public Object tmp2;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct S4 // Marshal.SizeOf 16 bytes, EE getClassSize 16
    {
        [FieldOffset(0)]
        public ulong tmp1;
        [FieldOffset(8)]
        public int tmp2;
    }

    internal struct S5 // Marshal.SizeOf 16 bytes, EE getClassSize 16
    {
        public ulong tmp1;
        public Object tmp2;
    }

    internal struct S6 // Marshal.SizeOf 16 bytes, EE getClassSize 16
    {
        public ulong tmp1;
        public int tmp2;
    }

    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test1<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int num, T a, T b)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test2<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int num, T a, T b, T c)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test3<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, byte b1, T a, T b, T c)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test4<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, T a, T b, T c)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test5<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int num, T a, T b, int i)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test6<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, T a, T b, int i)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test5<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, byte b1, T a, T b, byte b2)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test6<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, T a, T b, byte b1)
        {
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int test<T>() where T : struct
        {

            // Marshal.SizeOf throws "System.ArgumentException: Type 'Runtime_46239.S1' cannot 
            // be marshaled as an unmanaged structure; no meaningful size or offset can be computed."
            // on non-Windows platforms.
            //
            // int size = Marshal.SizeOf(typeof(T));
            // Console.WriteLine("size of " + typeof(T).Name + " is: " + size);


            if (test1<T>(1, 2, 3, 4, 5, 6, 7, 8, 1, new T(), new T()) != 100)
            {
                Console.WriteLine("test1() failed.");
                return 101;
            }
            if (test2<T>(1, 2, 3, 4, 5, 6, 7, 8, 1, new T(), new T(), new T()) != 100)
            {
                Console.WriteLine("test2() failed.");
                return 101;
            }
            if (test3<T>(1, 2, 3, 4, 5, 6, 7, 8, 1, new T(), new T(), new T()) != 100)
            {
                Console.WriteLine("test3() failed.");
                return 101;
            }
            if (test4<T>(1, 2, 3, 4, 5, 6, 7, 8, new T(), new T(), new T()) != 100)
            {
                Console.WriteLine("test4() failed.");
                return 101;
            }
            if (test5<T>(1, 2, 3, 4, 5, 6, 7, 8, 1, new T(), new T(), 2) != 100)
            {
                Console.WriteLine("test5() failed.");
                return 101;
            }
            if (test6<T>(1, 2, 3, 4, 5, 6, 7, 8, new T(), new T(), 1) != 100)
            {
                Console.WriteLine("test6() failed.");
                return 101;
            }
            return 100;

        }

        [Fact]
        public static int TestEntryPoint()
        {

            if (test<S1>() != 100)
            {
                Console.WriteLine("test<S1>() failed.");
                return 101;
            }

            if (test<S2>() != 100)
            {
                Console.WriteLine("test<S2>() failed.");
                return 101;
            }

            if (test<S3>() != 100)
            {
                Console.WriteLine("test<S3>() failed.");
                return 101;
            }

            if (test<S4>() != 100)
            {
                Console.WriteLine("test<S4>() failed.");
                return 101;
            }

            if (test<S5>() != 100)
            {
                Console.WriteLine("test<S5>() failed.");
                return 101;
            }

            if (test<S6>() != 100)
            {
                Console.WriteLine("test<S6>() failed.");
                return 101;
            }

            return 100;
        }
    }
}
