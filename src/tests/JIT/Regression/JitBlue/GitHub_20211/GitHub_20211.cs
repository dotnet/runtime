// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// GitHub 20211: bug with lowering SIMDIntrinsicGetItem on ARM64
// when INS_mov (move w/o sign-extension) was used to copy signed value
// from Vn.b[i] (or Vn.h[i]) to general register Wd (or Xd) instead of
// INS_smov (move with sign-extension).

namespace GitHub_20211
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe sbyte SquareRootAt0(Vector<sbyte> arg)
        {
            return (sbyte)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte SquareRootAt0(Vector<byte> arg)
        {
            return (byte)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe short SquareRootAt0(Vector<short> arg)
        {
            return (short)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ushort SquareRootAt0(Vector<ushort> arg)
        {
            return (ushort)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int SquareRootAt0(Vector<int> arg)
        {
            return (int)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint SquareRootAt0(Vector<uint> arg)
        {
            return (uint)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe long SquareRootAt0(Vector<long> arg)
        {
            return (long)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ulong SquareRootAt0(Vector<ulong> arg)
        {
            return (ulong)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe float SquareRootAt0(Vector<float> arg)
        {
            return (float)Math.Sqrt(arg[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe double SquareRootAt0(Vector<double> arg)
        {
            return (double)Math.Sqrt(arg[0]);
        }

        enum Result { Pass, Fail }

        struct TestRunner
        {
            void TestSquareRootAt0(sbyte arg0)
            {
                Vector<sbyte> arg = new Vector<sbyte>(arg0);
                sbyte actual = SquareRootAt0(arg);
                sbyte expected = (sbyte)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: sbyte (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(byte arg0)
            {
                Vector<byte> arg = new Vector<byte>(arg0);
                byte actual = SquareRootAt0(arg);
                byte expected = (byte)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: byte (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(short arg0)
            {
                Vector<short> arg = new Vector<short>(arg0);
                short actual = SquareRootAt0(arg);
                short expected = (short)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: short (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(ushort arg0)
            {
                Vector<ushort> arg = new Vector<ushort>(arg0);
                ushort actual = SquareRootAt0(arg);
                ushort expected = (ushort)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: ushort (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(int arg0)
            {
                Vector<int> arg = new Vector<int>(arg0);
                int actual = SquareRootAt0(arg);
                int expected = (int)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: int (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(uint arg0)
            {
                Vector<uint> arg = new Vector<uint>(arg0);
                uint actual = SquareRootAt0(arg);
                uint expected = (uint)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: uint (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(long arg0)
            {
                Vector<long> arg = new Vector<long>(arg0);
                long actual = SquareRootAt0(arg);
                long expected = (long)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: long (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(ulong arg0)
            {
                Vector<ulong> arg = new Vector<ulong>(arg0);
                ulong actual = SquareRootAt0(arg);
                ulong expected = (ulong)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: ulong (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(float arg0)
            {
                Vector<float> arg = new Vector<float>(arg0);
                float actual = SquareRootAt0(arg);
                float expected = (float)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: float (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            void TestSquareRootAt0(double arg0)
            {
                Vector<double> arg = new Vector<double>(arg0);
                double actual = SquareRootAt0(arg);
                double expected = (double)Math.Sqrt(arg0);

                if (actual != expected)
                {
                    Console.WriteLine($"Fail: double (actual={actual}, expected={expected})");
                    result = Result.Fail;
                }
            }

            Result result;

            public Result Run()
            {
                result = Result.Pass;

                TestSquareRootAt0((sbyte)-1);
                TestSquareRootAt0((short)-1);
                TestSquareRootAt0((int)-1);
                TestSquareRootAt0((long)-1);

                TestSquareRootAt0((sbyte)1);
                TestSquareRootAt0((byte)1);
                TestSquareRootAt0((short)1);
                TestSquareRootAt0((ushort)1);
                TestSquareRootAt0((int)1);
                TestSquareRootAt0((uint)1);
                TestSquareRootAt0((long)1);
                TestSquareRootAt0((ulong)1);

                TestSquareRootAt0((float)4);
                TestSquareRootAt0((double)4);

                return result;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (new TestRunner().Run() == Result.Pass)
            {
                Console.WriteLine("Pass");
                return 100;
            }
            else
            {
                return 0;
            }
        }
    }
}
