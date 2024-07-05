// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is auto-generated from template file Helpers.tt
// In order to make changes to this file, please update Helpers.tt
// and run the following command from Developer Command Prompt for Visual Studio
//   "%DevEnvDir%\TextTransform.exe" .\Helpers.tt

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    static class Helpers
    {
        public static Vector<T> InitVector<T>(Func<int, T> f)
        {
            var count = Vector<T>.Count;
            var arr = new T[count];
            for (var i = 0; i < count; i++)
            {
                arr[i] = f(i);
            }
            return new Vector<T>(arr);
        }

        public static byte[] CreateMaskForFirstActiveElement(byte[] mask, byte[] srcMask)
        {
            var count = srcMask.Length;
            var result = new byte[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static short[] CreateMaskForFirstActiveElement(short[] mask, short[] srcMask)
        {
            var count = srcMask.Length;
            var result = new short[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static int[] CreateMaskForFirstActiveElement(int[] mask, int[] srcMask)
        {
            var count = srcMask.Length;
            var result = new int[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static long[] CreateMaskForFirstActiveElement(long[] mask, long[] srcMask)
        {
            var count = srcMask.Length;
            var result = new long[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static sbyte[] CreateMaskForFirstActiveElement(sbyte[] mask, sbyte[] srcMask)
        {
            var count = srcMask.Length;
            var result = new sbyte[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static ushort[] CreateMaskForFirstActiveElement(ushort[] mask, ushort[] srcMask)
        {
            var count = srcMask.Length;
            var result = new ushort[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static uint[] CreateMaskForFirstActiveElement(uint[] mask, uint[] srcMask)
        {
            var count = srcMask.Length;
            var result = new uint[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static ulong[] CreateMaskForFirstActiveElement(ulong[] mask, ulong[] srcMask)
        {
            var count = srcMask.Length;
            var result = new ulong[count];
            Array.Copy(srcMask, 0, result, 0, count);
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static int LastActiveElement(byte[] v)
        {
            for (var i = v.Length - 1; i >= 0; i--)
            {
                if (v[i] != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastActiveElement(ushort[] v)
        {
            for (var i = v.Length - 1; i >= 0; i--)
            {
                if (v[i] != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastActiveElement(uint[] v)
        {
            for (var i = v.Length - 1; i >= 0; i--)
            {
                if (v[i] != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastActiveElement(ulong[] v)
        {
            for (var i = v.Length - 1; i >= 0; i--)
            {
                if (v[i] != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static byte[] CreateMaskForNextActiveElement(byte[] mask, byte[] srcMask)
        {
            var count = srcMask.Length;
            var result = new byte[count];
            for (var i = LastActiveElement(srcMask) + 1; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static ushort[] CreateMaskForNextActiveElement(ushort[] mask, ushort[] srcMask)
        {
            var count = srcMask.Length;
            var result = new ushort[count];
            for (var i = LastActiveElement(srcMask) + 1; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static uint[] CreateMaskForNextActiveElement(uint[] mask, uint[] srcMask)
        {
            var count = srcMask.Length;
            var result = new uint[count];
            for (var i = LastActiveElement(srcMask) + 1; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static ulong[] CreateMaskForNextActiveElement(ulong[] mask, ulong[] srcMask)
        {
            var count = srcMask.Length;
            var result = new ulong[count];
            for (var i = LastActiveElement(srcMask) + 1; i < count; i++)
            {
                if (mask[i] != 0)
                {
                    result[i] = 1;
                    return result;
                }
            }
            return result;
        }

        public static sbyte CountLeadingSignBits(sbyte op1)
        {
            return (sbyte)(CountLeadingZeroBits((sbyte)((ulong)op1 ^ ((ulong)op1 >> 1))) - 1);
        }

        public static short CountLeadingSignBits(short op1)
        {
            return (short)(CountLeadingZeroBits((short)((ulong)op1 ^ ((ulong)op1 >> 1))) - 1);
        }

        public static int CountLeadingSignBits(int op1)
        {
            return (int)(CountLeadingZeroBits((int)((ulong)op1 ^ ((ulong)op1 >> 1))) - 1);
        }

        public static long CountLeadingSignBits(long op1)
        {
            return (long)(CountLeadingZeroBits((long)((ulong)op1 ^ ((ulong)op1 >> 1))) - 1);
        }

        public static sbyte CountLeadingZeroBits(sbyte op1)
        {
            return (sbyte)(8 * sizeof(sbyte) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(sbyte op1)
        {
            for (int i = 8 * sizeof(sbyte) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static byte CountLeadingZeroBits(byte op1)
        {
            return (byte)(8 * sizeof(byte) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(byte op1)
        {
            for (int i = 8 * sizeof(byte) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static short CountLeadingZeroBits(short op1)
        {
            return (short)(8 * sizeof(short) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(short op1)
        {
            for (int i = 8 * sizeof(short) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static ushort CountLeadingZeroBits(ushort op1)
        {
            return (ushort)(8 * sizeof(ushort) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(ushort op1)
        {
            for (int i = 8 * sizeof(ushort) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int CountLeadingZeroBits(int op1)
        {
            return (int)(8 * sizeof(int) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(int op1)
        {
            for (int i = 8 * sizeof(int) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static uint CountLeadingZeroBits(uint op1)
        {
            return (uint)(8 * sizeof(uint) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(uint op1)
        {
            for (int i = 8 * sizeof(uint) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static long CountLeadingZeroBits(long op1)
        {
            return (long)(8 * sizeof(long) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(long op1)
        {
            for (int i = 8 * sizeof(long) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static ulong CountLeadingZeroBits(ulong op1)
        {
            return (ulong)(8 * sizeof(ulong) - (HighestSetBit(op1) + 1));
        }

        private static int HighestSetBit(ulong op1)
        {
            for (int i = 8 * sizeof(ulong) - 1; i >= 0; i--)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static sbyte BitCount(sbyte op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(sbyte); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (sbyte)result;
        }

        public static byte BitCount(byte op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(byte); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (byte)result;
        }

        public static short BitCount(short op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(short); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (short)result;
        }

        public static ushort BitCount(ushort op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(ushort); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (ushort)result;
        }

        public static int BitCount(int op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(int); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (int)result;
        }

        public static uint BitCount(uint op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(uint); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (uint)result;
        }

        public static long BitCount(long op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(long); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (long)result;
        }

        public static ulong BitCount(ulong op1)
        {
            int result = 0;

            for (int i = 0; i < 8 * sizeof(ulong); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result + 1;
                }
            }

            return (ulong)result;
        }

        public static int BitCount(float op1) => BitCount(BitConverter.SingleToInt32Bits(op1));

        public static long BitCount(double op1) => BitCount(BitConverter.DoubleToInt64Bits(op1));

        public static byte ReverseElementBits(byte op1)
        {
            byte val = (byte)op1;
            byte result = 0;
            const int bitsize = sizeof(byte) * 8;
            const byte cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (byte)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (byte)result;
        }

        public static short ReverseElementBits(short op1)
        {
            short val = (short)op1;
            short result = 0;
            const int bitsize = sizeof(short) * 8;
            const short cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (short)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (short)result;
        }

        public static int ReverseElementBits(int op1)
        {
            uint val = (uint)op1;
            uint result = 0;
            const int bitsize = sizeof(uint) * 8;
            const uint cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (uint)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (int)result;
        }

        public static long ReverseElementBits(long op1)
        {
            ulong val = (ulong)op1;
            ulong result = 0;
            const int bitsize = sizeof(ulong) * 8;
            const ulong cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (ulong)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (long)result;
        }

        public static sbyte ReverseElementBits(sbyte op1)
        {
            byte val = (byte)op1;
            byte result = 0;
            const int bitsize = sizeof(byte) * 8;
            const byte cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (byte)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (sbyte)result;
        }

        public static ushort ReverseElementBits(ushort op1)
        {
            ushort val = (ushort)op1;
            ushort result = 0;
            const int bitsize = sizeof(ushort) * 8;
            const ushort cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (ushort)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (ushort)result;
        }

        public static uint ReverseElementBits(uint op1)
        {
            uint val = (uint)op1;
            uint result = 0;
            const int bitsize = sizeof(uint) * 8;
            const uint cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (uint)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (uint)result;
        }

        public static ulong ReverseElementBits(ulong op1)
        {
            ulong val = (ulong)op1;
            ulong result = 0;
            const int bitsize = sizeof(ulong) * 8;
            const ulong cst_one = 1;

            for (int i = 0; i < bitsize; i++)
            {
                if ((val & (cst_one << i)) != 0)
                {
                    result |= (ulong)(cst_one << (bitsize  - 1 - i));
                }
            }

            return (ulong)result;
        }

        public static sbyte And(sbyte op1, sbyte op2) => (sbyte)(op1 & op2);

        public static sbyte BitwiseClear(sbyte op1, sbyte op2) => (sbyte)(op1 & ~op2);

        public static sbyte BitwiseSelect(sbyte op1, sbyte op2, sbyte op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(sbyte); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (sbyte)result;
        }

        public static sbyte Not(sbyte op1) => (sbyte)(~op1);

        public static sbyte Or(sbyte op1, sbyte op2) => (sbyte)(op1 | op2);

        public static sbyte OrNot(sbyte op1, sbyte op2) => (sbyte)(op1 | ~op2);

        public static sbyte Xor(sbyte op1, sbyte op2) => (sbyte)(op1 ^ op2);

        public static byte And(byte op1, byte op2) => (byte)(op1 & op2);

        public static byte BitwiseClear(byte op1, byte op2) => (byte)(op1 & ~op2);

        public static byte BitwiseSelect(byte op1, byte op2, byte op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(byte); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (byte)result;
        }

        public static byte Not(byte op1) => (byte)(~op1);

        public static byte Or(byte op1, byte op2) => (byte)(op1 | op2);

        public static byte OrNot(byte op1, byte op2) => (byte)(op1 | ~op2);

        public static byte Xor(byte op1, byte op2) => (byte)(op1 ^ op2);

        public static short And(short op1, short op2) => (short)(op1 & op2);

        public static short BitwiseClear(short op1, short op2) => (short)(op1 & ~op2);

        public static short BitwiseSelect(short op1, short op2, short op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(short); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (short)result;
        }

        public static short Not(short op1) => (short)(~op1);

        public static short Or(short op1, short op2) => (short)(op1 | op2);

        public static short OrNot(short op1, short op2) => (short)(op1 | ~op2);

        public static short Xor(short op1, short op2) => (short)(op1 ^ op2);

        public static ushort And(ushort op1, ushort op2) => (ushort)(op1 & op2);

        public static ushort BitwiseClear(ushort op1, ushort op2) => (ushort)(op1 & ~op2);

        public static ushort BitwiseSelect(ushort op1, ushort op2, ushort op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(ushort); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (ushort)result;
        }

        public static ushort Not(ushort op1) => (ushort)(~op1);

        public static ushort Or(ushort op1, ushort op2) => (ushort)(op1 | op2);

        public static ushort OrNot(ushort op1, ushort op2) => (ushort)(op1 | ~op2);

        public static ushort Xor(ushort op1, ushort op2) => (ushort)(op1 ^ op2);

        public static int And(int op1, int op2) => (int)(op1 & op2);

        public static int BitwiseClear(int op1, int op2) => (int)(op1 & ~op2);

        public static int BitwiseSelect(int op1, int op2, int op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(int); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (int)result;
        }

        public static int Not(int op1) => (int)(~op1);

        public static int Or(int op1, int op2) => (int)(op1 | op2);

        public static int OrNot(int op1, int op2) => (int)(op1 | ~op2);

        public static int Xor(int op1, int op2) => (int)(op1 ^ op2);

        public static uint And(uint op1, uint op2) => (uint)(op1 & op2);

        public static uint BitwiseClear(uint op1, uint op2) => (uint)(op1 & ~op2);

        public static uint BitwiseSelect(uint op1, uint op2, uint op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(uint); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (uint)result;
        }

        public static uint Not(uint op1) => (uint)(~op1);

        public static uint Or(uint op1, uint op2) => (uint)(op1 | op2);

        public static uint OrNot(uint op1, uint op2) => (uint)(op1 | ~op2);

        public static uint Xor(uint op1, uint op2) => (uint)(op1 ^ op2);

        public static long And(long op1, long op2) => (long)(op1 & op2);

        public static long BitwiseClear(long op1, long op2) => (long)(op1 & ~op2);

        public static long BitwiseSelect(long op1, long op2, long op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(long); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (long)result;
        }

        public static long Not(long op1) => (long)(~op1);

        public static long Or(long op1, long op2) => (long)(op1 | op2);

        public static long OrNot(long op1, long op2) => (long)(op1 | ~op2);

        public static long Xor(long op1, long op2) => (long)(op1 ^ op2);

        public static ulong And(ulong op1, ulong op2) => (ulong)(op1 & op2);

        public static ulong BitwiseClear(ulong op1, ulong op2) => (ulong)(op1 & ~op2);

        public static ulong BitwiseSelect(ulong op1, ulong op2, ulong op3)
        {
            ulong result = 0;

            for (int i = 0; i < 8 * sizeof(ulong); i++)
            {
                if (((ulong)op1 & (1UL << i)) != 0)
                {
                    result = result | ((ulong)op2 & (1UL << i));
                }
                else
                {
                    result = result | ((ulong)op3 & (1UL << i));
                }
            }

            return (ulong)result;
        }

        public static ulong Not(ulong op1) => (ulong)(~op1);

        public static ulong Or(ulong op1, ulong op2) => (ulong)(op1 | op2);

        public static ulong OrNot(ulong op1, ulong op2) => (ulong)(op1 | ~op2);

        public static ulong Xor(ulong op1, ulong op2) => (ulong)(op1 ^ op2);

        public static float Not(float op1) => BitConverter.Int32BitsToSingle(~BitConverter.SingleToInt32Bits(op1));

        public static double Not(double op1) => BitConverter.Int64BitsToDouble(~BitConverter.DoubleToInt64Bits(op1));

        public static float And(float op1, float op2) => BitConverter.Int32BitsToSingle(And(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double And(double op1, double op2) => BitConverter.Int64BitsToDouble(And(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float BitwiseClear(float op1, float op2) => BitConverter.Int32BitsToSingle(BitwiseClear(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double BitwiseClear(double op1, double op2) => BitConverter.Int64BitsToDouble(BitwiseClear(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float Or(float op1, float op2) => BitConverter.Int32BitsToSingle(Or(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double Or(double op1, double op2) => BitConverter.Int64BitsToDouble(Or(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float OrNot(float op1, float op2) => BitConverter.Int32BitsToSingle(OrNot(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double OrNot(double op1, double op2) => BitConverter.Int64BitsToDouble(OrNot(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float Xor(float op1, float op2) => BitConverter.Int32BitsToSingle(Xor(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double Xor(double op1, double op2) => BitConverter.Int64BitsToDouble(Xor(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float BitwiseSelect(float op1, float op2, float op3) => BitConverter.Int32BitsToSingle(BitwiseSelect(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2), BitConverter.SingleToInt32Bits(op3)));
        public static double BitwiseSelect(double op1, double op2, double op3) => BitConverter.Int64BitsToDouble(BitwiseSelect(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2), BitConverter.DoubleToInt64Bits(op3)));

        public static sbyte CompareEqual(sbyte left, sbyte right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static sbyte CompareGreaterThan(sbyte left, sbyte right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static sbyte CompareGreaterThanOrEqual(sbyte left, sbyte right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static sbyte CompareLessThan(sbyte left, sbyte right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static sbyte CompareLessThanOrEqual(sbyte left, sbyte right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static sbyte CompareTest(sbyte left, sbyte right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (sbyte)result;
        }

        public static byte CompareEqual(byte left, byte right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static byte CompareGreaterThan(byte left, byte right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static byte CompareGreaterThanOrEqual(byte left, byte right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static byte CompareLessThan(byte left, byte right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static byte CompareLessThanOrEqual(byte left, byte right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static byte CompareTest(byte left, byte right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (byte)result;
        }

        public static short CompareEqual(short left, short right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (short)result;
        }

        public static short CompareGreaterThan(short left, short right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (short)result;
        }

        public static short CompareGreaterThanOrEqual(short left, short right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (short)result;
        }

        public static short CompareLessThan(short left, short right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (short)result;
        }

        public static short CompareLessThanOrEqual(short left, short right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (short)result;
        }

        public static short CompareTest(short left, short right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (short)result;
        }

        public static ushort CompareEqual(ushort left, ushort right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static ushort CompareGreaterThan(ushort left, ushort right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static ushort CompareGreaterThanOrEqual(ushort left, ushort right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static ushort CompareLessThan(ushort left, ushort right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static ushort CompareLessThanOrEqual(ushort left, ushort right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static ushort CompareTest(ushort left, ushort right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (ushort)result;
        }

        public static int CompareEqual(int left, int right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (int)result;
        }

        public static int CompareGreaterThan(int left, int right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (int)result;
        }

        public static int CompareGreaterThanOrEqual(int left, int right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (int)result;
        }

        public static int CompareLessThan(int left, int right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (int)result;
        }

        public static int CompareLessThanOrEqual(int left, int right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (int)result;
        }

        public static int CompareTest(int left, int right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (int)result;
        }

        public static uint CompareEqual(uint left, uint right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static uint CompareGreaterThan(uint left, uint right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static uint CompareGreaterThanOrEqual(uint left, uint right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static uint CompareLessThan(uint left, uint right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static uint CompareLessThanOrEqual(uint left, uint right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static uint CompareTest(uint left, uint right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (uint)result;
        }

        public static long CompareEqual(long left, long right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (long)result;
        }

        public static long CompareGreaterThan(long left, long right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (long)result;
        }

        public static long CompareGreaterThanOrEqual(long left, long right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (long)result;
        }

        public static long CompareLessThan(long left, long right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (long)result;
        }

        public static long CompareLessThanOrEqual(long left, long right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (long)result;
        }

        public static long CompareTest(long left, long right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (long)result;
        }

        public static ulong CompareEqual(ulong left, ulong right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static ulong CompareGreaterThan(ulong left, ulong right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static ulong CompareGreaterThanOrEqual(ulong left, ulong right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static ulong CompareLessThan(ulong left, ulong right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static ulong CompareLessThanOrEqual(ulong left, ulong right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static ulong CompareTest(ulong left, ulong right)
        {
            long result = 0;

            if ((left & right) != 0)
            {
                result = -1;
            }

            return (ulong)result;
        }

        public static double AbsoluteCompareGreaterThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareGreaterThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareLessThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareLessThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }


        public static double SveAbsoluteCompareGreaterThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareGreaterThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareLessThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareLessThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveCompareEqual(double left, double right) => BitConverter.Int64BitsToDouble((left == right) ? 1 : 0);
        public static float SveCompareEqual(float left, float right) => BitConverter.Int32BitsToSingle((left == right) ? 1 : 0);
        public static sbyte SveCompareEqual(sbyte left, sbyte right) => (sbyte)((left == right) ? 1 : 0);
        public static byte SveCompareEqual(byte left, byte right) => (byte)((left == right) ? 1 : 0);
        public static short SveCompareEqual(short left, short right) => (short)((left == right) ? 1 : 0);
        public static ushort SveCompareEqual(ushort left, ushort right) => (ushort)((left == right) ? 1 : 0);
        public static int SveCompareEqual(int left, int right) => (int)((left == right) ? 1 : 0);
        public static uint SveCompareEqual(uint left, uint right) => (uint)((left == right) ? 1 : 0);
        public static long SveCompareEqual(long left, long right) => (long)((left == right) ? 1 : 0);
        public static ulong SveCompareEqual(ulong left, ulong right) => (ulong)((left == right) ? 1 : 0);

        public static double SveCompareNotEqual(double left, double right) => BitConverter.Int64BitsToDouble((left != right) ? 1 : 0);
        public static float SveCompareNotEqual(float left, float right) => BitConverter.Int32BitsToSingle((left != right) ? 1 : 0);
        public static sbyte SveCompareNotEqual(sbyte left, sbyte right) => (sbyte)((left != right) ? 1 : 0);
        public static byte SveCompareNotEqual(byte left, byte right) => (byte)((left != right) ? 1 : 0);
        public static short SveCompareNotEqual(short left, short right) => (short)((left != right) ? 1 : 0);
        public static ushort SveCompareNotEqual(ushort left, ushort right) => (ushort)((left != right) ? 1 : 0);
        public static int SveCompareNotEqual(int left, int right) => (int)((left != right) ? 1 : 0);
        public static uint SveCompareNotEqual(uint left, uint right) => (uint)((left != right) ? 1 : 0);
        public static long SveCompareNotEqual(long left, long right) => (long)((left != right) ? 1 : 0);
        public static ulong SveCompareNotEqual(ulong left, ulong right) => (ulong)((left != right) ? 1 : 0);
        
        public static double SveCompareGreaterThan(double left, double right) => BitConverter.Int64BitsToDouble((left > right) ? 1 : 0);
        public static float SveCompareGreaterThan(float left, float right) => BitConverter.Int32BitsToSingle((left > right) ? 1 : 0);
        public static sbyte SveCompareGreaterThan(sbyte left, sbyte right) => (sbyte)((left > right) ? 1 : 0);
        public static byte SveCompareGreaterThan(byte left, byte right) => (byte)((left > right) ? 1 : 0);
        public static short SveCompareGreaterThan(short left, short right) => (short)((left > right) ? 1 : 0);
        public static ushort SveCompareGreaterThan(ushort left, ushort right) => (ushort)((left > right) ? 1 : 0);
        public static int SveCompareGreaterThan(int left, int right) => (int)((left > right) ? 1 : 0);
        public static uint SveCompareGreaterThan(uint left, uint right) => (uint)((left > right) ? 1 : 0);
        public static long SveCompareGreaterThan(long left, long right) => (long)((left > right) ? 1 : 0);
        public static ulong SveCompareGreaterThan(ulong left, ulong right) => (ulong)((left > right) ? 1 : 0);
        
        public static double SveCompareGreaterThanOrEqual(double left, double right) => BitConverter.Int64BitsToDouble((left >= right) ? 1 : 0);
        public static float SveCompareGreaterThanOrEqual(float left, float right) => BitConverter.Int32BitsToSingle((left >= right) ? 1 : 0);
        public static sbyte SveCompareGreaterThanOrEqual(sbyte left, sbyte right) => (sbyte)((left >= right) ? 1 : 0);
        public static byte SveCompareGreaterThanOrEqual(byte left, byte right) => (byte)((left >= right) ? 1 : 0);
        public static short SveCompareGreaterThanOrEqual(short left, short right) => (short)((left >= right) ? 1 : 0);
        public static ushort SveCompareGreaterThanOrEqual(ushort left, ushort right) => (ushort)((left >= right) ? 1 : 0);
        public static int SveCompareGreaterThanOrEqual(int left, int right) => (int)((left >= right) ? 1 : 0);
        public static uint SveCompareGreaterThanOrEqual(uint left, uint right) => (uint)((left >= right) ? 1 : 0);
        public static long SveCompareGreaterThanOrEqual(long left, long right) => (long)((left >= right) ? 1 : 0);
        public static ulong SveCompareGreaterThanOrEqual(ulong left, ulong right) => (ulong)((left >= right) ? 1 : 0);

        public static double SveCompareLessThan(double left, double right) => BitConverter.Int64BitsToDouble((left < right) ? 1 : 0);
        public static float SveCompareLessThan(float left, float right) => BitConverter.Int32BitsToSingle((left < right) ? 1 : 0);
        public static sbyte SveCompareLessThan(sbyte left, sbyte right) => (sbyte)((left < right) ? 1 : 0);
        public static byte SveCompareLessThan(byte left, byte right) => (byte)((left < right) ? 1 : 0);
        public static short SveCompareLessThan(short left, short right) => (short)((left < right) ? 1 : 0);
        public static ushort SveCompareLessThan(ushort left, ushort right) => (ushort)((left < right) ? 1 : 0);
        public static int SveCompareLessThan(int left, int right) => (int)((left < right) ? 1 : 0);
        public static uint SveCompareLessThan(uint left, uint right) => (uint)((left < right) ? 1 : 0);
        public static long SveCompareLessThan(long left, long right) => (long)((left < right) ? 1 : 0);
        public static ulong SveCompareLessThan(ulong left, ulong right) => (ulong)((left < right) ? 1 : 0);      

        public static double SveCompareLessThanOrEqual(double left, double right) => BitConverter.Int64BitsToDouble((left <= right) ? 1 : 0);
        public static float SveCompareLessThanOrEqual(float left, float right) => BitConverter.Int32BitsToSingle((left <= right) ? 1 : 0);
        public static sbyte SveCompareLessThanOrEqual(sbyte left, sbyte right) => (sbyte)((left <= right) ? 1 : 0);
        public static byte SveCompareLessThanOrEqual(byte left, byte right) => (byte)((left <= right) ? 1 : 0);
        public static short SveCompareLessThanOrEqual(short left, short right) => (short)((left <= right) ? 1 : 0);
        public static ushort SveCompareLessThanOrEqual(ushort left, ushort right) => (ushort)((left <= right) ? 1 : 0);
        public static int SveCompareLessThanOrEqual(int left, int right) => (int)((left <= right) ? 1 : 0);
        public static uint SveCompareLessThanOrEqual(uint left, uint right) => (uint)((left <= right) ? 1 : 0);
        public static long SveCompareLessThanOrEqual(long left, long right) => (long)((left <= right) ? 1 : 0);
        public static ulong SveCompareLessThanOrEqual(ulong left, ulong right) => (ulong)((left <= right) ? 1 : 0);

        public static double CompareEqual(double left, double right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareEqual(float left, float right)
        {
            int result = 0;

            if (left == right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareGreaterThan(double left, double right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareGreaterThan(float left, float right)
        {
            int result = 0;

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareLessThan(double left, double right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareLessThan(float left, float right)
        {
            int result = 0;

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareTest(double left, double right)
        {
            long result = 0;

            if ((BitConverter.DoubleToInt64Bits(left) & BitConverter.DoubleToInt64Bits(right)) != 0)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareTest(float left, float right)
        {
            int result = 0;

            if ((BitConverter.SingleToInt32Bits(left) & BitConverter.SingleToInt32Bits(right)) != 0)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static byte Abs(sbyte value) => value < 0 ? (byte)-value : (byte)value;

        public static ushort Abs(short value) => value < 0 ? (ushort)-value : (ushort)value;

        public static uint Abs(int value) => value < 0 ? (uint)-value : (uint)value;

        public static ulong Abs(long value) => value < 0 ? (ulong)-value : (ulong)value;

        public static float Abs(float value) => Math.Abs(value);

        public static double Abs(double value) => Math.Abs(value);

        public static float Divide(float op1, float op2) => op1 / op2;

        public static double Divide(double op1, double op2) => op1 / op2;

        public static float Scale(float op1, int op2) => op1 * MathF.Pow((float)2.0, op2);

        public static double Scale(double op1, long op2) =>  op1 * Math.Pow(2.0, op2);

        public static float Sqrt(float value) => MathF.Sqrt(value);

        public static double Sqrt(double value) => Math.Sqrt(value);

        public static long AbsoluteDifference(long op1, long op2) => op1 < op2 ? (long)(op2 - op1) : (long)(op1 - op2);

        public static byte AbsoluteDifference(sbyte op1, sbyte op2) => op1 < op2 ? (byte)(op2 - op1) : (byte)(op1 - op2);

        public static sbyte AbsoluteDifferenceAdd(sbyte op1, sbyte op2, sbyte op3) => (sbyte)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifference(short op1, short op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static short AbsoluteDifferenceAdd(short op1, short op2, short op3) => (short)(op1 + AbsoluteDifference(op2, op3));

        public static uint AbsoluteDifference(int op1, int op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static int AbsoluteDifferenceAdd(int op1, int op2, int op3) => (int)(op1 + AbsoluteDifference(op2, op3));

        public static byte AbsoluteDifference(byte op1, byte op2) => op1 < op2 ? (byte)(op2 - op1) : (byte)(op1 - op2);

        public static byte AbsoluteDifferenceAdd(byte op1, byte op2, byte op3) => (byte)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifference(ushort op1, ushort op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ulong AbsoluteDifference(ulong op1, ulong op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ushort AbsoluteDifferenceAdd(ushort op1, ushort op2, ushort op3) => (ushort)(op1 + AbsoluteDifference(op2, op3));

        public static uint AbsoluteDifference(uint op1, uint op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceAdd(uint op1, uint op2, uint op3) => (uint)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifferenceWidening(sbyte op1, sbyte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AbsoluteDifferenceWideningAndAdd(short op1, sbyte op2, sbyte op3) => (short)(op1 + (short)AbsoluteDifferenceWidening(op2, op3));

        public static short AbsoluteDifferenceWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short AddAcrossWidening(sbyte[] op1) => Reduce(AddWidening, op1);

        public static long AddAcrossWideningLong(sbyte[] op1) => Reduce(AddWidening, op1);

        public static short AddPairwiseWidening(sbyte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static short AddPairwiseWideningAndAdd(short[] op1, sbyte[] op2, int i) => (short)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static sbyte HighNarrowing(short op1, bool round)
        {
            ushort roundConst = 0;
            if (round)
            {
                roundConst = (ushort)1 << (8 * sizeof(sbyte) - 1);
            }
            return (sbyte)(((ushort)op1 + roundConst) >> (8 * sizeof(sbyte)));
        }

        public static sbyte AddHighNarrowing(short op1, short op2) => HighNarrowing((short)(op1 + op2), round: false);

        public static sbyte AddHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static sbyte AddRoundedHighNarrowing(short op1, short op2) => HighNarrowing((short)(op1 + op2), round: true);

        public static short AddRoundedHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddWidening(sbyte op1, sbyte op2) => (short)((short)op1 + (short)op2);

        public static short AddWidening(short op1, sbyte op2) => (short)(op1 + op2);

        public static long AddWidening(long op1, sbyte op2) => (long)(op1 + (long)op2);

        public static short AddWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AddWideningUpper(short[] op1, sbyte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static sbyte BooleanNot(sbyte value) => (sbyte)(value == 0 ? 1 : 0);

        public static byte BooleanNot(byte value) => (byte)(value == 0 ? 1 : 0);

        public static short BooleanNot(short value) => (short)(value == 0 ? 1 : 0);

        public static ushort BooleanNot(ushort value) => (ushort)(value == 0 ? 1 : 0);

        public static int BooleanNot(int value) => (int)(value == 0 ? 1 : 0);

        public static uint BooleanNot(uint value) => (uint)(value == 0 ? 1 : 0);

        public static long BooleanNot(long value) => (long)(value == 0 ? 1 : 0);

        public static ulong BooleanNot(ulong value) => (ulong)(value == 0 ? 1 : 0);

        public static sbyte ExtractNarrowing(short op1) => (sbyte)op1;

        public static sbyte ExtractNarrowingUpper(sbyte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static sbyte FusedAddHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2) >> 1);

        public static sbyte FusedAddRoundedHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2 + 1) >> 1);

        public static sbyte FusedSubtractHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 - (short)op2) >> 1);

        public static short MultiplyByScalarWideningUpper(sbyte[] op1, sbyte op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static short MultiplyByScalarWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyByScalarWideningUpperAndSubtract(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyWidening(sbyte op1, sbyte op2) => (short)((short)op1 * (short)op2);

        public static short MultiplyWideningAndAdd(short op1, sbyte op2, sbyte op3) => (short)(op1 + MultiplyWidening(op2, op3));

        public static short MultiplyWideningAndSubtract(short op1, sbyte op2, sbyte op3) => (short)(op1 - MultiplyWidening(op2, op3));

        public static short MultiplyWideningUpper(sbyte[] op1, sbyte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short MultiplyWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short MultiplyWideningUpperAndSubtract(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static sbyte SubtractHighNarrowing(short op1, short op2) => HighNarrowing((short)(op1 - op2), round: false);

        public static short SubtractHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static sbyte SubtractRoundedHighNarrowing(short op1, short op2) => HighNarrowing((short)(op1 - op2), round: true);

        public static short SubtractRoundedHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static short SubtractWidening(sbyte op1, sbyte op2) => (short)((short)op1 - (short)op2);

        public static short SubtractWidening(short op1, sbyte op2) => (short)(op1 - op2);

        public static short SubtractWideningUpper(sbyte[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short SubtractWideningUpper(short[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static short ZeroExtendWidening(sbyte op1) => (short)(ushort)op1;

        public static short ZeroExtendWideningUpper(sbyte[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static short Reduce(Func<short, sbyte, short> reduceOp, sbyte[] op1)
        {
            short acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        private static long Reduce(Func<long, sbyte, long> reduceOp, sbyte[] op1)
        {
            long acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static uint AbsoluteDifferenceWidening(short op1, short op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(short[] op1, short[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AbsoluteDifferenceWideningAndAdd(int op1, short op2, short op3) => (int)(op1 + (int)AbsoluteDifferenceWidening(op2, op3));

        public static int AbsoluteDifferenceWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int AddAcrossWidening(short[] op1) => Reduce(AddWidening, op1);

        public static long AddAcrossWideningLong(short[] op1) => Reduce(AddWidening, op1);

        public static int AddPairwiseWidening(short[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static int AddPairwiseWideningAndAdd(int[] op1, short[] op2, int i) => (int)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static short HighNarrowing(int op1, bool round)
        {
            uint roundConst = 0;
            if (round)
            {
                roundConst = (uint)1 << (8 * sizeof(short) - 1);
            }
            return (short)(((uint)op1 + roundConst) >> (8 * sizeof(short)));
        }

        public static short AddHighNarrowing(int op1, int op2) => HighNarrowing((int)(op1 + op2), round: false);

        public static short AddHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddRoundedHighNarrowing(int op1, int op2) => HighNarrowing((int)(op1 + op2), round: true);

        public static int AddRoundedHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddWidening(short op1, short op2) => (int)((int)op1 + (int)op2);

        public static int AddWidening(int op1, short op2) => (int)(op1 + op2);

        public static long AddWidening(long op1, short op2) => (long)(op1 + (long)op2);

        public static int AddWideningUpper(short[] op1, short[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AddWideningUpper(int[] op1, short[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static short ExtractNarrowing(int op1) => (short)op1;

        public static short ExtractNarrowingUpper(short[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static short FusedAddHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2) >> 1);

        public static short FusedAddRoundedHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2 + 1) >> 1);

        public static short FusedSubtractHalving(short op1, short op2) => (short)((uint)((int)op1 - (int)op2) >> 1);

        public static int MultiplyByScalarWideningUpper(short[] op1, short op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static int MultiplyByScalarWideningUpperAndAdd(int[] op1, short[] op2, short op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyByScalarWideningUpperAndSubtract(int[] op1, short[] op2, short op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyWidening(short op1, short op2) => (int)((int)op1 * (int)op2);

        public static int MultiplyWideningAndAdd(int op1, short op2, short op3) => (int)(op1 + MultiplyWidening(op2, op3));

        public static int MultiplyWideningAndSubtract(int op1, short op2, short op3) => (int)(op1 - MultiplyWidening(op2, op3));

        public static int MultiplyWideningUpper(short[] op1, short[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int MultiplyWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int MultiplyWideningUpperAndSubtract(int[] op1, short[] op2, short[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short SubtractHighNarrowing(int op1, int op2) => HighNarrowing((int)(op1 - op2), round: false);

        public static int SubtractHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static short SubtractRoundedHighNarrowing(int op1, int op2) => HighNarrowing((int)(op1 - op2), round: true);

        public static int SubtractRoundedHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractWidening(short op1, short op2) => (int)((int)op1 - (int)op2);

        public static int SubtractWidening(int op1, short op2) => (int)(op1 - op2);

        public static int SubtractWideningUpper(short[] op1, short[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int SubtractWideningUpper(int[] op1, short[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static int ZeroExtendWidening(short op1) => (int)(uint)op1;

        public static int ZeroExtendWideningUpper(short[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static int Reduce(Func<int, short, int> reduceOp, short[] op1)
        {
            int acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        private static long Reduce(Func<long, short, long> reduceOp, short[] op1)
        {
            long acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static ulong AbsoluteDifferenceWidening(int op1, int op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(int[] op1, int[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AbsoluteDifferenceWideningAndAdd(long op1, int op2, int op3) => (long)(op1 + (long)AbsoluteDifferenceWidening(op2, op3));

        public static long AbsoluteDifferenceWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long AddAcrossWidening(int[] op1) => Reduce(AddWidening, op1);

        public static long AddPairwiseWidening(int[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static long AddPairwiseWideningAndAdd(long[] op1, int[] op2, int i) => (long)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static int HighNarrowing(long op1, bool round)
        {
            ulong roundConst = 0;
            if (round)
            {
                roundConst = (ulong)1 << (8 * sizeof(int) - 1);
            }
            return (int)(((ulong)op1 + roundConst) >> (8 * sizeof(int)));
        }

        public static int AddHighNarrowing(long op1, long op2) => HighNarrowing((long)(op1 + op2), round: false);

        public static int AddHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddRoundedHighNarrowing(long op1, long op2) => HighNarrowing((long)(op1 + op2), round: true);

        public static long AddRoundedHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static long AddWidening(int op1, int op2) => (long)((long)op1 + (long)op2);

        public static long AddWidening(long op1, int op2) => (long)(op1 + op2);

        public static long AddWideningUpper(int[] op1, int[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AddWideningUpper(long[] op1, int[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static int ExtractNarrowing(long op1) => (int)op1;

        public static int ExtractNarrowingUpper(int[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static int FusedAddHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2) >> 1);

        public static int FusedAddRoundedHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2 + 1) >> 1);

        public static int FusedSubtractHalving(int op1, int op2) => (int)((ulong)((long)op1 - (long)op2) >> 1);

        public static long MultiplyByScalarWideningUpper(int[] op1, int op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static long MultiplyByScalarWideningUpperAndAdd(long[] op1, int[] op2, int op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyByScalarWideningUpperAndSubtract(long[] op1, int[] op2, int op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyWidening(int op1, int op2) => (long)((long)op1 * (long)op2);

        public static long MultiplyWideningAndAdd(long op1, int op2, int op3) => (long)(op1 + MultiplyWidening(op2, op3));

        public static long MultiplyWideningAndSubtract(long op1, int op2, int op3) => (long)(op1 - MultiplyWidening(op2, op3));

        public static long MultiplyWideningUpper(int[] op1, int[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long MultiplyWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyWideningUpperAndSubtract(long[] op1, int[] op2, int[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static T ShiftLeft<T>(T op1, ulong op2) where T : INumber<T>
        {
            T two = T.One + T.One;
            for (ulong i = 0; (op1 != T.Zero) && (i < op2); i++)
            {
                op1 *= two;
            }

            return op1;
        }

        public static T ShiftRight<T>(T op1, ulong op2) where T : INumber<T>
        {
            T two = T.One + T.One;
            for (ulong i = 0; (op1 != T.Zero) && (i < op2); i++)
            {
                op1 /= two;
            }

            return op1;
        }

        public static T SignExtend<T>(T n, int numBits, bool zeroExtend) where T : struct, IComparable, IConvertible
        {
            // Get the underlying integer value
            dynamic value = Convert.ChangeType(n, typeof(long));

            // Mask to extract the lowest numBits
            long mask = (1L << numBits) - 1;
            long lowestBits = value & mask;

            // Sign extension for signed integers
            long signBitMask = 1L << (numBits - 1);
            if (!zeroExtend && ((lowestBits & signBitMask) != 0))
            {
                // If sign bit is set, it's a negative number
                return (T)Convert.ChangeType(-((~lowestBits & mask) + 1), typeof(T));
            }
            else
            {
                // If sign bit is not set, it's a positive number
                return (T)Convert.ChangeType(lowestBits, typeof(T));
            }
        }

        public static int SubtractHighNarrowing(long op1, long op2) => HighNarrowing((long)(op1 - op2), round: false);

        public static long SubtractHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractRoundedHighNarrowing(long op1, long op2) => HighNarrowing((long)(op1 - op2), round: true);

        public static long SubtractRoundedHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static long SubtractWidening(int op1, int op2) => (long)((long)op1 - (long)op2);

        public static long SubtractWidening(long op1, int op2) => (long)(op1 - op2);

        public static long SubtractWideningUpper(int[] op1, int[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long SubtractWideningUpper(long[] op1, int[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static long ZeroExtendWidening(int op1) => (long)(ulong)op1;

        public static long ZeroExtendWideningUpper(int[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static long Reduce(Func<long, int, long> reduceOp, int[] op1)
        {
            long acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static ushort AbsoluteDifferenceWidening(byte op1, byte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(byte[] op1, byte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AbsoluteDifferenceWideningAndAdd(ushort op1, byte op2, byte op3) => (ushort)(op1 + (ushort)AbsoluteDifferenceWidening(op2, op3));

        public static ushort AbsoluteDifferenceWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort AddAcrossWidening(byte[] op1) => Reduce(AddWidening, op1);

        public static ulong AddAcrossWideningULong(byte[] op1) => Reduce(AddWidening, op1);

        public static ushort AddPairwiseWidening(byte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ushort AddPairwiseWideningAndAdd(ushort[] op1, byte[] op2, int i) => (ushort)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static byte HighNarrowing(ushort op1, bool round)
        {
            ushort roundConst = 0;
            if (round)
            {
                roundConst = (ushort)1 << (8 * sizeof(byte) - 1);
            }
            return (byte)(((ushort)op1 + roundConst) >> (8 * sizeof(byte)));
        }

        public static byte AddHighNarrowing(ushort op1, ushort op2) => HighNarrowing((ushort)(op1 + op2), round: false);

        public static byte AddHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static byte AddRoundedHighNarrowing(ushort op1, ushort op2) => HighNarrowing((ushort)(op1 + op2), round: true);

        public static ushort AddRoundedHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddWidening(byte op1, byte op2) => (ushort)((ushort)op1 + (ushort)op2);

        public static ushort AddWidening(ushort op1, byte op2) => (ushort)(op1 + op2);

        public static ulong AddWidening(ulong op1, byte op2) => (ulong)(op1 + (ulong)op2);

        public static ushort AddWideningUpper(byte[] op1, byte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AddWideningUpper(ushort[] op1, byte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static byte ExtractNarrowing(ushort op1) => (byte)op1;

        public static byte ExtractNarrowingUpper(byte[] op1, ushort[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static byte FusedAddHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2) >> 1);

        public static byte FusedAddRoundedHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2 + 1) >> 1);

        public static byte FusedSubtractHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 - (ushort)op2) >> 1);

        public static ushort MultiplyByScalarWideningUpper(byte[] op1, byte op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static ushort MultiplyByScalarWideningUpperAndAdd(ushort[] op1, byte[] op2, byte op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static ushort MultiplyByScalarWideningUpperAndSubtract(ushort[] op1, byte[] op2, byte op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static ushort MultiplyWidening(byte op1, byte op2) => (ushort)((ushort)op1 * (ushort)op2);

        public static ushort MultiplyWideningAndAdd(ushort op1, byte op2, byte op3) => (ushort)(op1 + MultiplyWidening(op2, op3));

        public static ushort MultiplyWideningAndSubtract(ushort op1, byte op2, byte op3) => (ushort)(op1 - MultiplyWidening(op2, op3));

        public static ushort MultiplyWideningUpper(byte[] op1, byte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort MultiplyWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort MultiplyWideningUpperAndSubtract(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static byte SubtractHighNarrowing(ushort op1, ushort op2) => HighNarrowing((ushort)(op1 - op2), round: false);

        public static ushort SubtractHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static byte SubtractRoundedHighNarrowing(ushort op1, ushort op2) => HighNarrowing((ushort)(op1 - op2), round: true);

        public static ushort SubtractRoundedHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractWidening(byte op1, byte op2) => (ushort)((ushort)op1 - (ushort)op2);

        public static ushort SubtractWidening(ushort op1, byte op2) => (ushort)(op1 - op2);

        public static ushort SubtractWideningUpper(byte[] op1, byte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort SubtractWideningUpper(ushort[] op1, byte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort ZeroExtendWidening(byte op1) => (ushort)(ushort)op1;

        public static ushort ZeroExtendWideningUpper(byte[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static ushort Reduce(Func<ushort, byte, ushort> reduceOp, byte[] op1)
        {
            ushort acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        private static ulong Reduce(Func<ulong, byte, ulong> reduceOp, byte[] op1)
        {
            ulong acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static uint AbsoluteDifferenceWidening(ushort op1, ushort op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(ushort[] op1, ushort[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AbsoluteDifferenceWideningAndAdd(uint op1, ushort op2, ushort op3) => (uint)(op1 + (uint)AbsoluteDifferenceWidening(op2, op3));

        public static uint AbsoluteDifferenceWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint AddAcrossWidening(ushort[] op1) => Reduce(AddWidening, op1);

        public static ulong AddAcrossWideningULong(ushort[] op1) => Reduce(AddWidening, op1);

        public static uint AddPairwiseWidening(ushort[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static uint AddPairwiseWideningAndAdd(uint[] op1, ushort[] op2, int i) => (uint)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static ushort HighNarrowing(uint op1, bool round)
        {
            uint roundConst = 0;
            if (round)
            {
                roundConst = (uint)1 << (8 * sizeof(ushort) - 1);
            }
            return (ushort)(((uint)op1 + roundConst) >> (8 * sizeof(ushort)));
        }

        public static ushort AddHighNarrowing(uint op1, uint op2) => HighNarrowing((uint)(op1 + op2), round: false);

        public static ushort AddHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddRoundedHighNarrowing(uint op1, uint op2) => HighNarrowing((uint)(op1 + op2), round: true);

        public static uint AddRoundedHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddWidening(ushort op1, ushort op2) => (uint)((uint)op1 + (uint)op2);

        public static uint AddWidening(uint op1, ushort op2) => (uint)(op1 + op2);

        public static ulong AddWidening(ulong op1, ushort op2) => (ulong)(op1 + (ulong)op2);

        public static uint AddWideningUpper(ushort[] op1, ushort[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AddWideningUpper(uint[] op1, ushort[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort ExtractNarrowing(uint op1) => (ushort)op1;

        public static ushort ExtractNarrowingUpper(ushort[] op1, uint[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static ushort FusedAddHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2) >> 1);

        public static ushort FusedAddRoundedHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2 + 1) >> 1);

        public static ushort FusedSubtractHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 - (uint)op2) >> 1);

        public static uint MultiplyByScalarWideningUpper(ushort[] op1, ushort op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static uint MultiplyByScalarWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static uint MultiplyByScalarWideningUpperAndSubtract(uint[] op1, ushort[] op2, ushort op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static uint MultiplyWidening(ushort op1, ushort op2) => (uint)((uint)op1 * (uint)op2);

        public static uint MultiplyWideningAndAdd(uint op1, ushort op2, ushort op3) => (uint)(op1 + MultiplyWidening(op2, op3));

        public static uint MultiplyWideningAndSubtract(uint op1, ushort op2, ushort op3) => (uint)(op1 - MultiplyWidening(op2, op3));

        public static uint MultiplyWideningUpper(ushort[] op1, ushort[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint MultiplyWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint MultiplyWideningUpperAndSubtract(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort SubtractHighNarrowing(uint op1, uint op2) => HighNarrowing((uint)(op1 - op2), round: false);

        public static uint SubtractHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractRoundedHighNarrowing(uint op1, uint op2) => HighNarrowing((uint)(op1 - op2), round: true);

        public static uint SubtractRoundedHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractWidening(ushort op1, ushort op2) => (uint)((uint)op1 - (uint)op2);

        public static uint SubtractWidening(uint op1, ushort op2) => (uint)(op1 - op2);

        public static uint SubtractWideningUpper(ushort[] op1, ushort[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint SubtractWideningUpper(uint[] op1, ushort[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint ZeroExtendWidening(ushort op1) => (uint)(uint)op1;

        public static uint ZeroExtendWideningUpper(ushort[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static uint Reduce(Func<uint, ushort, uint> reduceOp, ushort[] op1)
        {
            uint acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        private static ulong Reduce(Func<ulong, ushort, ulong> reduceOp, ushort[] op1)
        {
            ulong acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static ulong AbsoluteDifferenceWidening(uint op1, uint op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(uint[] op1, uint[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AbsoluteDifferenceWideningAndAdd(ulong op1, uint op2, uint op3) => (ulong)(op1 + (ulong)AbsoluteDifferenceWidening(op2, op3));

        public static ulong AbsoluteDifferenceWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong AddAcrossWidening(uint[] op1) => Reduce(AddWidening, op1);

        public static ulong AddPairwiseWidening(uint[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ulong AddPairwiseWideningAndAdd(ulong[] op1, uint[] op2, int i) => (ulong)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static uint HighNarrowing(ulong op1, bool round)
        {
            ulong roundConst = 0;
            if (round)
            {
                roundConst = (ulong)1 << (8 * sizeof(uint) - 1);
            }
            return (uint)(((ulong)op1 + roundConst) >> (8 * sizeof(uint)));
        }

        public static uint AddHighNarrowing(ulong op1, ulong op2) => HighNarrowing((ulong)(op1 + op2), round: false);

        public static uint AddHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddRoundedHighNarrowing(ulong op1, ulong op2) => HighNarrowing((ulong)(op1 + op2), round: true);

        public static ulong AddRoundedHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong AddWidening(uint op1, uint op2) => (ulong)((ulong)op1 + (ulong)op2);

        public static ulong AddWidening(ulong op1, uint op2) => (ulong)(op1 + op2);

        public static ulong AddWideningUpper(uint[] op1, uint[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AddWideningUpper(ulong[] op1, uint[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint ExtractNarrowing(ulong op1) => (uint)op1;

        public static uint ExtractNarrowingUpper(uint[] op1, ulong[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static uint FusedAddHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2) >> 1);

        public static uint FusedAddRoundedHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2 + 1) >> 1);

        public static uint FusedSubtractHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 - (ulong)op2) >> 1);

        public static ulong MultiplyByScalarWideningUpper(uint[] op1, uint op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static ulong MultiplyByScalarWideningUpperAndAdd(ulong[] op1, uint[] op2, uint op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3);

        public static ulong MultiplyByScalarWideningUpperAndSubtract(ulong[] op1, uint[] op2, uint op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static ulong MultiplyWidening(uint op1, uint op2) => (ulong)((ulong)op1 * (ulong)op2);

        public static ulong MultiplyWideningAndAdd(ulong op1, uint op2, uint op3) => (ulong)(op1 + MultiplyWidening(op2, op3));

        public static ulong MultiplyWideningAndSubtract(ulong op1, uint op2, uint op3) => (ulong)(op1 - MultiplyWidening(op2, op3));

        public static ulong MultiplyWideningUpper(uint[] op1, uint[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong MultiplyWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong MultiplyWideningUpperAndSubtract(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint SubtractHighNarrowing(ulong op1, ulong op2) => HighNarrowing((ulong)(op1 - op2), round: false);

        public static ulong SubtractHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractRoundedHighNarrowing(ulong op1, ulong op2) => HighNarrowing((ulong)(op1 - op2), round: true);

        public static ulong SubtractRoundedHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong SubtractWidening(uint op1, uint op2) => (ulong)((ulong)op1 - (ulong)op2);

        public static ulong SubtractWidening(ulong op1, uint op2) => (ulong)(op1 - op2);

        public static ulong SubtractWideningUpper(uint[] op1, uint[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong SubtractWideningUpper(ulong[] op1, uint[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ulong ZeroExtendWidening(uint op1) => (ulong)(ulong)op1;

        public static ulong ZeroExtendWideningUpper(uint[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static ulong Reduce(Func<ulong, uint, ulong> reduceOp, uint[] op1)
        {
            ulong acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static double AddWidening(double op1, float op2) => (double)(op1 + (double)op2);


        private static bool SignedSatQ(short val, out sbyte result)
        {
            bool saturated = false;

            if (val > sbyte.MaxValue)
            {
                result = sbyte.MaxValue;
                saturated = true;
            }
            else if (val < sbyte.MinValue)
            {
                result = sbyte.MinValue;
                saturated = true;
            }
            else
            {
                result = (sbyte)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(short val, out byte result)
        {
            bool saturated = false;

            if (val > byte.MaxValue)
            {
                result = byte.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (byte)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(ushort val, out byte result)
        {
            bool saturated = false;

            if (val > byte.MaxValue)
            {
                result = byte.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (byte)val;
            }

            return saturated;
        }

        private static bool SatQ(short val, out sbyte result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                byte res;
                saturated = UnsignedSatQ((ushort)val, out res);
                result = (sbyte)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(ushort val, out byte result) => UnsignedSatQ(val, out result);

        public static sbyte ExtractNarrowingSaturate(short op1)
        {
            sbyte result;

            SatQ(op1, out result);

            return result;
        }

        public static sbyte ExtractNarrowingSaturateUpper(sbyte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static byte ExtractNarrowingSaturate(ushort op1)
        {
            byte result;

            SatQ(op1, out result);

            return result;
        }

        public static byte ExtractNarrowingSaturateUpper(byte[] op1, ushort[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static byte ExtractNarrowingSaturateUnsigned(short op1)
        {
            byte result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static byte ExtractNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (short val, bool ovf) MultiplyDoublingOvf(sbyte op1, sbyte op2, bool rounding, short op3, bool subOp)
        {
            short product = (short)((short)op1 * (short)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            short accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            short roundConst = 0;

            if (rounding)
            {
                roundConst = (short)1 << (8 * sizeof(sbyte) - 1);
            }

            bool rndOvf;
            short result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static sbyte SaturateHigh(short val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? sbyte.MaxValue : sbyte.MinValue;
            }

            return (sbyte)UnsignedShift(val, (short)(-8 * sizeof(sbyte)));
        }

        public static sbyte MultiplyDoublingSaturateHigh(sbyte op1, sbyte op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (short)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static sbyte MultiplyRoundedDoublingSaturateHigh(sbyte op1, sbyte op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (short)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyDoublingWideningSaturate(sbyte op1, sbyte op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (short)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? sbyte.MaxValue : sbyte.MinValue;
            }

            return product;
        }

        public static sbyte MultiplyRoundedDoublingAndAddSaturateHigh(sbyte op1, sbyte op2, sbyte op3)
        {
            short addend = UnsignedShift((short)op1, (short)(8 * sizeof(sbyte)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static sbyte MultiplyRoundedDoublingAndSubtractSaturateHigh(sbyte op1, sbyte op2, sbyte op3)
        {
            short minuend = UnsignedShift((short)op1, (short)(8 * sizeof(sbyte)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyDoublingWideningAndAddSaturate(short op1, sbyte op2, sbyte op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static short MultiplyDoublingWideningAndSubtractSaturate(short op1, sbyte op2, sbyte op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static short MultiplyDoublingWideningSaturateUpperByScalar(sbyte[] op1, sbyte op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static short MultiplyDoublingWideningUpperByScalarAndAddSaturate(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyDoublingWideningSaturateUpper(sbyte[] op1, sbyte[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short MultiplyDoublingWideningUpperAndAddSaturate(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short MultiplyDoublingWideningUpperAndSubtractSaturate(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short ShiftLeftLogicalWidening(sbyte op1, byte op2) => UnsignedShift((short)op1, (short)op2);

        public static ushort ShiftLeftLogicalWidening(byte op1, byte op2) => UnsignedShift((ushort)op1, (short)op2);

        public static short ShiftLeftLogicalWideningUpper(sbyte[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static ushort ShiftLeftLogicalWideningUpper(byte[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static sbyte ShiftRightArithmeticRoundedNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(SignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static byte ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(short op1, byte op2)
        {
            byte result;

            UnsignedSatQ(SignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static byte ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticRoundedNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(SignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static byte ShiftRightArithmeticNarrowingSaturateUnsigned(short op1, byte op2)
        {
            byte result;

            UnsignedSatQ(SignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static byte ShiftRightArithmeticNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowing(short op1, byte op2) => (sbyte)UnsignedShift(op1, (short)(-op2));

        public static byte ShiftRightLogicalNarrowing(ushort op1, byte op2) => (byte)UnsignedShift(op1, (short)(-op2));

        public static sbyte ShiftRightLogicalRoundedNarrowing(short op1, byte op2) => (sbyte)UnsignedShift(op1, (short)(-op2), rounding: true);

        public static byte ShiftRightLogicalRoundedNarrowing(ushort op1, byte op2) => (byte)UnsignedShift(op1, (short)(-op2), rounding: true);

        public static sbyte ShiftRightLogicalRoundedNarrowingUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalRoundedNarrowingUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalRoundedNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(UnsignedShift(op1, (short)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static byte ShiftRightLogicalRoundedNarrowingSaturate(ushort op1, byte op2)
        {
            byte result;

            SatQ(UnsignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static sbyte ShiftRightLogicalRoundedNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalRoundedNarrowingSaturateUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowingUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalNarrowingUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(UnsignedShift(op1, (short)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static byte ShiftRightLogicalNarrowingSaturate(ushort op1, byte op2)
        {
            byte result;

            SatQ(UnsignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static sbyte ShiftRightLogicalNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalNarrowingSaturateUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static short SignExtendWidening(sbyte op1) => op1;

        public static short SignExtendWideningUpper(sbyte[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        private static bool SignedSatQ(int val, out short result)
        {
            bool saturated = false;

            if (val > short.MaxValue)
            {
                result = short.MaxValue;
                saturated = true;
            }
            else if (val < short.MinValue)
            {
                result = short.MinValue;
                saturated = true;
            }
            else
            {
                result = (short)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(int val, out ushort result)
        {
            bool saturated = false;

            if (val > ushort.MaxValue)
            {
                result = ushort.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (ushort)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(uint val, out ushort result)
        {
            bool saturated = false;

            if (val > ushort.MaxValue)
            {
                result = ushort.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (ushort)val;
            }

            return saturated;
        }

        private static bool SatQ(int val, out short result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                ushort res;
                saturated = UnsignedSatQ((uint)val, out res);
                result = (short)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(uint val, out ushort result) => UnsignedSatQ(val, out result);

        public static short ExtractNarrowingSaturate(int op1)
        {
            short result;

            SatQ(op1, out result);

            return result;
        }

        public static short ExtractNarrowingSaturateUpper(short[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static ushort ExtractNarrowingSaturate(uint op1)
        {
            ushort result;

            SatQ(op1, out result);

            return result;
        }

        public static ushort ExtractNarrowingSaturateUpper(ushort[] op1, uint[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static ushort ExtractNarrowingSaturateUnsigned(int op1)
        {
            ushort result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static ushort ExtractNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (int val, bool ovf) MultiplyDoublingOvf(short op1, short op2, bool rounding, int op3, bool subOp)
        {
            int product = (int)((int)op1 * (int)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            int accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            int roundConst = 0;

            if (rounding)
            {
                roundConst = (int)1 << (8 * sizeof(short) - 1);
            }

            bool rndOvf;
            int result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static short SaturateHigh(int val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? short.MaxValue : short.MinValue;
            }

            return (short)UnsignedShift(val, (int)(-8 * sizeof(short)));
        }

        public static short MultiplyDoublingSaturateHigh(short op1, short op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (int)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyRoundedDoublingSaturateHigh(short op1, short op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (int)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyDoublingWideningSaturate(short op1, short op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (int)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? short.MaxValue : short.MinValue;
            }

            return product;
        }

        public static short MultiplyRoundedDoublingAndAddSaturateHigh(short op1, short op2, short op3)
        {
            int addend = UnsignedShift((int)op1, (int)(8 * sizeof(short)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyRoundedDoublingAndSubtractSaturateHigh(short op1, short op2, short op3)
        {
            int minuend = UnsignedShift((int)op1, (int)(8 * sizeof(short)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyDoublingWideningAndAddSaturate(int op1, short op2, short op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static int MultiplyDoublingWideningAndSubtractSaturate(int op1, short op2, short op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static int MultiplyDoublingWideningSaturateUpperByScalar(short[] op1, short op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static int MultiplyDoublingWideningUpperByScalarAndAddSaturate(int[] op1, short[] op2, short op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(int[] op1, short[] op2, short op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyDoublingWideningSaturateUpper(short[] op1, short[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int MultiplyDoublingWideningUpperAndAddSaturate(int[] op1, short[] op2, short[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int MultiplyDoublingWideningUpperAndSubtractSaturate(int[] op1, short[] op2, short[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int ShiftLeftLogicalWidening(short op1, byte op2) => UnsignedShift((int)op1, (int)op2);

        public static uint ShiftLeftLogicalWidening(ushort op1, byte op2) => UnsignedShift((uint)op1, (int)op2);

        public static int ShiftLeftLogicalWideningUpper(short[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static uint ShiftLeftLogicalWideningUpper(ushort[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static short ShiftRightArithmeticRoundedNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(SignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(int op1, byte op2)
        {
            ushort result;

            UnsignedSatQ(SignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticRoundedNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(SignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticNarrowingSaturateUnsigned(int op1, byte op2)
        {
            ushort result;

            UnsignedSatQ(SignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowing(int op1, byte op2) => (short)UnsignedShift(op1, (int)(-op2));

        public static ushort ShiftRightLogicalNarrowing(uint op1, byte op2) => (ushort)UnsignedShift(op1, (int)(-op2));

        public static short ShiftRightLogicalRoundedNarrowing(int op1, byte op2) => (short)UnsignedShift(op1, (int)(-op2), rounding: true);

        public static ushort ShiftRightLogicalRoundedNarrowing(uint op1, byte op2) => (ushort)UnsignedShift(op1, (int)(-op2), rounding: true);

        public static short ShiftRightLogicalRoundedNarrowingUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalRoundedNarrowingUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalRoundedNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(UnsignedShift(op1, (int)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static ushort ShiftRightLogicalRoundedNarrowingSaturate(uint op1, byte op2)
        {
            ushort result;

            SatQ(UnsignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static short ShiftRightLogicalRoundedNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalRoundedNarrowingSaturateUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowingUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalNarrowingUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(UnsignedShift(op1, (int)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static ushort ShiftRightLogicalNarrowingSaturate(uint op1, byte op2)
        {
            ushort result;

            SatQ(UnsignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static short ShiftRightLogicalNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalNarrowingSaturateUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static int SignExtendWidening(short op1) => op1;

        public static int SignExtendWideningUpper(short[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        private static bool SignedSatQ(long val, out int result)
        {
            bool saturated = false;

            if (val > int.MaxValue)
            {
                result = int.MaxValue;
                saturated = true;
            }
            else if (val < int.MinValue)
            {
                result = int.MinValue;
                saturated = true;
            }
            else
            {
                result = (int)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(long val, out uint result)
        {
            bool saturated = false;

            if (val > uint.MaxValue)
            {
                result = uint.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (uint)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(ulong val, out uint result)
        {
            bool saturated = false;

            if (val > uint.MaxValue)
            {
                result = uint.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (uint)val;
            }

            return saturated;
        }

        private static bool SatQ(long val, out int result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                uint res;
                saturated = UnsignedSatQ((ulong)val, out res);
                result = (int)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(ulong val, out uint result) => UnsignedSatQ(val, out result);

        public static int ExtractNarrowingSaturate(long op1)
        {
            int result;

            SatQ(op1, out result);

            return result;
        }

        public static int ExtractNarrowingSaturateUpper(int[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static uint ExtractNarrowingSaturate(ulong op1)
        {
            uint result;

            SatQ(op1, out result);

            return result;
        }

        public static uint ExtractNarrowingSaturateUpper(uint[] op1, ulong[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static uint ExtractNarrowingSaturateUnsigned(long op1)
        {
            uint result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static uint ExtractNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (long val, bool ovf) MultiplyDoublingOvf(int op1, int op2, bool rounding, long op3, bool subOp)
        {
            long product = (long)((long)op1 * (long)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            long accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            long roundConst = 0;

            if (rounding)
            {
                roundConst = (long)1 << (8 * sizeof(int) - 1);
            }

            bool rndOvf;
            long result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static int SaturateHigh(long val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? int.MaxValue : int.MinValue;
            }

            return (int)UnsignedShift(val, (long)(-8 * sizeof(int)));
        }

        public static int MultiplyDoublingSaturateHigh(int op1, int op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (long)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyRoundedDoublingSaturateHigh(int op1, int op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (long)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static long MultiplyDoublingWideningSaturate(int op1, int op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (long)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? int.MaxValue : int.MinValue;
            }

            return product;
        }

        public static int MultiplyRoundedDoublingAndAddSaturateHigh(int op1, int op2, int op3)
        {
            long addend = UnsignedShift((long)op1, (long)(8 * sizeof(int)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyRoundedDoublingAndSubtractSaturateHigh(int op1, int op2, int op3)
        {
            long minuend = UnsignedShift((long)op1, (long)(8 * sizeof(int)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static long MultiplyDoublingWideningAndAddSaturate(long op1, int op2, int op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static long MultiplyDoublingWideningAndSubtractSaturate(long op1, int op2, int op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static long MultiplyDoublingWideningSaturateUpperByScalar(int[] op1, int op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static long MultiplyDoublingWideningUpperByScalarAndAddSaturate(long[] op1, int[] op2, int op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(long[] op1, int[] op2, int op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyDoublingWideningSaturateUpper(int[] op1, int[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long MultiplyDoublingWideningUpperAndAddSaturate(long[] op1, int[] op2, int[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyDoublingWideningUpperAndSubtractSaturate(long[] op1, int[] op2, int[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long ShiftLeftLogicalWidening(int op1, byte op2) => UnsignedShift((long)op1, (long)op2);

        public static ulong ShiftLeftLogicalWidening(uint op1, byte op2) => UnsignedShift((ulong)op1, (long)op2);

        public static long ShiftLeftLogicalWideningUpper(int[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static ulong ShiftLeftLogicalWideningUpper(uint[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static int ShiftRightArithmeticRoundedNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(SignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static uint ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(long op1, byte op2)
        {
            uint result;

            UnsignedSatQ(SignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static uint ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticRoundedNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(SignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        public static uint ShiftRightArithmeticNarrowingSaturateUnsigned(long op1, byte op2)
        {
            uint result;

            UnsignedSatQ(SignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        public static uint ShiftRightArithmeticNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowing(long op1, byte op2) => (int)UnsignedShift(op1, (long)(-op2));

        public static uint ShiftRightLogicalNarrowing(ulong op1, byte op2) => (uint)UnsignedShift(op1, (long)(-op2));

        public static int ShiftRightLogicalRoundedNarrowing(long op1, byte op2) => (int)UnsignedShift(op1, (long)(-op2), rounding: true);

        public static uint ShiftRightLogicalRoundedNarrowing(ulong op1, byte op2) => (uint)UnsignedShift(op1, (long)(-op2), rounding: true);

        public static int ShiftRightLogicalRoundedNarrowingUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalRoundedNarrowingUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalRoundedNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(UnsignedShift(op1, (long)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static uint ShiftRightLogicalRoundedNarrowingSaturate(ulong op1, byte op2)
        {
            uint result;

            SatQ(UnsignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static int ShiftRightLogicalRoundedNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalRoundedNarrowingSaturateUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowingUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalNarrowingUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(UnsignedShift(op1, (long)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static uint ShiftRightLogicalNarrowingSaturate(ulong op1, byte op2)
        {
            uint result;

            SatQ(UnsignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        public static int ShiftRightLogicalNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalNarrowingSaturateUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static long SignExtendWidening(int op1) => op1;

        public static long SignExtendWideningUpper(int[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        public static sbyte ShiftArithmetic(sbyte op1, sbyte op2) => SignedShift(op1, op2);

        public static sbyte ShiftArithmeticRounded(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true);

        public static sbyte ShiftArithmeticSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, saturating: true);

        public static sbyte ShiftArithmeticRoundedSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static sbyte SignedShift(sbyte op1, sbyte op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            sbyte rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((sbyte)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            sbyte result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (sbyte)ShiftOvf((byte)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = sbyte.MaxValue;
                    }
                }
            }

            return result;
        }

        public static T[] ShiftAndInsert<T>(T[] op1, T op2)
        {
            T nextValue = op2;

            for (int i = 0; i < op1.Length; i++)
            {
                (op1[i], nextValue) = (nextValue, op1[i]);
            }

            return op1;
        }

        public static sbyte ShiftLeftLogical(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)op2);

        public static byte ShiftLeftLogical(byte op1, byte op2) => UnsignedShift(op1, (sbyte)op2);

        public static sbyte ShiftLeftLogicalSaturate(sbyte op1, byte op2) => SignedShift(op1, (sbyte)op2, saturating: true);

        public static byte ShiftLeftLogicalSaturate(byte op1, byte op2) => UnsignedShift(op1, (sbyte)op2, saturating: true);

        public static byte ShiftLeftLogicalSaturateUnsigned(sbyte op1, byte op2) => (byte)UnsignedShift(op1, (sbyte)op2, saturating: true);

        public static sbyte ShiftLogical(sbyte op1, sbyte op2) => UnsignedShift(op1, op2);

        public static byte ShiftLogical(byte op1, sbyte op2) => UnsignedShift(op1, op2);

        public static byte ShiftLogicalRounded(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true);

        public static sbyte ShiftLogicalRounded(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true);

        public static byte ShiftLogicalRoundedSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static sbyte ShiftLogicalRoundedSaturate(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static sbyte ShiftLogicalSaturate(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, saturating: true);

        public static byte ShiftLogicalSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, saturating: true);

        public static sbyte ShiftRightArithmetic(sbyte op1, byte op2) => SignedShift(op1, (sbyte)(-op2));

        public static sbyte ShiftRightArithmeticAdd(sbyte op1, sbyte op2, byte op3) =>  (sbyte)(op1 + ShiftRightArithmetic(op2, op3));

        public static sbyte ShiftRightArithmeticRounded(sbyte op1, byte op2) => SignedShift(op1, (sbyte)(-op2), rounding: true);

        public static sbyte ShiftRightArithmeticRoundedAdd(sbyte op1, sbyte op2, byte op3) =>  (sbyte)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static sbyte ShiftRightLogical(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2));

        public static byte ShiftRightLogical(byte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2));

        public static sbyte ShiftRightLogicalAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightLogical(op2, op3));

        public static byte ShiftRightLogicalAdd(byte op1, byte op2, byte op3) => (byte)(op1 + ShiftRightLogical(op2, op3));

        public static sbyte ShiftRightLogicalRounded(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2), rounding: true);

        public static byte ShiftRightLogicalRounded(byte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2), rounding: true);

        public static sbyte ShiftRightLogicalRoundedAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static byte ShiftRightLogicalRoundedAdd(byte op1, byte op2, byte op3) => (byte)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static byte UnsignedShift(byte op1, sbyte op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            byte rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((byte)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            (byte result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                byte shiftedCarry = ShiftOvf((byte)1, 8 * sizeof(byte) + shift).val;
                result = (byte)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = byte.MaxValue;
                }
            }

            return result;
        }

        private static sbyte UnsignedShift(sbyte op1, sbyte op2, bool rounding = false, bool saturating = false) => (sbyte)UnsignedShift((byte)op1, op2, rounding, saturating);

        private static (sbyte val, bool ovf) AddOvf(sbyte op1, sbyte op2)
        {
            sbyte result = (sbyte)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (sbyte val, bool ovf) AddOvf(sbyte op1, byte op2)
        {
            sbyte result = (sbyte)(op1 + (sbyte)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (byte val, bool ovf) AddOvf(byte op1, sbyte op2)
        {
            byte result = (byte)(op1 + (byte)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (byte val, bool ovf) AddOvf(byte op1, byte op2)
        {
            byte result = (byte)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (sbyte val, bool ovf) SubtractOvf(sbyte op1, sbyte op2)
        {
            sbyte result = (sbyte)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (byte val, bool ovf) SubtractOvf(byte op1, byte op2)
        {
            byte result = (byte)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static sbyte AbsSaturate(sbyte op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static sbyte AddSaturate(sbyte op1, sbyte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? sbyte.MinValue : sbyte.MaxValue) : result;
        }

        public static sbyte AddSaturate(sbyte op1, byte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? sbyte.MaxValue : result;
        }

        public static byte AddSaturate(byte op1, sbyte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? byte.MaxValue : byte.MinValue) : result;
        }

        public static byte AddSaturate(byte op1, byte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? byte.MaxValue : result;
        }

        public static sbyte NegateSaturate(sbyte op1) => SubtractSaturate((sbyte)0, op1);

        public static sbyte SubtractSaturate(sbyte op1, sbyte op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? sbyte.MinValue : sbyte.MaxValue) : result;
        }

        public static byte SubtractSaturate(byte op1, byte op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? byte.MinValue : result;
        }

        public static short ShiftArithmetic(short op1, short op2) => SignedShift(op1, op2);

        public static short ShiftArithmeticRounded(short op1, short op2) => SignedShift(op1, op2, rounding: true);

        public static short ShiftArithmeticSaturate(short op1, short op2) => SignedShift(op1, op2, saturating: true);

        public static short ShiftArithmeticRoundedSaturate(short op1, short op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static short SignedShift(short op1, short op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            short rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((short)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            short result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (short)ShiftOvf((ushort)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = short.MaxValue;
                    }
                }
            }

            return result;
        }

        public static short ShiftLeftLogical(short op1, byte op2) => UnsignedShift(op1, (short)op2);

        public static ushort ShiftLeftLogical(ushort op1, byte op2) => UnsignedShift(op1, (short)op2);

        public static short ShiftLeftLogicalSaturate(short op1, byte op2) => SignedShift(op1, (short)op2, saturating: true);

        public static ushort ShiftLeftLogicalSaturate(ushort op1, byte op2) => UnsignedShift(op1, (short)op2, saturating: true);

        public static ushort ShiftLeftLogicalSaturateUnsigned(short op1, byte op2) => (ushort)UnsignedShift(op1, (short)op2, saturating: true);

        public static short ShiftLogical(short op1, short op2) => UnsignedShift(op1, op2);

        public static ushort ShiftLogical(ushort op1, short op2) => UnsignedShift(op1, op2);

        public static ushort ShiftLogicalRounded(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true);

        public static short ShiftLogicalRounded(short op1, short op2) => UnsignedShift(op1, op2, rounding: true);

        public static ushort ShiftLogicalRoundedSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static short ShiftLogicalRoundedSaturate(short op1, short op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static short ShiftLogicalSaturate(short op1, short op2) => UnsignedShift(op1, op2, saturating: true);

        public static ushort ShiftLogicalSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, saturating: true);

        public static short ShiftRightArithmetic(short op1, byte op2) => SignedShift(op1, (short)(-op2));

        public static short ShiftRightArithmeticAdd(short op1, short op2, byte op3) =>  (short)(op1 + ShiftRightArithmetic(op2, op3));

        public static short ShiftRightArithmeticRounded(short op1, byte op2) => SignedShift(op1, (short)(-op2), rounding: true);

        public static short ShiftRightArithmeticRoundedAdd(short op1, short op2, byte op3) =>  (short)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static short ShiftRightLogical(short op1, byte op2) => UnsignedShift(op1, (short)(-op2));

        public static ushort ShiftRightLogical(ushort op1, byte op2) => UnsignedShift(op1, (short)(-op2));

        public static short ShiftRightLogicalAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightLogical(op2, op3));

        public static ushort ShiftRightLogicalAdd(ushort op1, ushort op2, byte op3) => (ushort)(op1 + ShiftRightLogical(op2, op3));

        public static short ShiftRightLogicalRounded(short op1, byte op2) => UnsignedShift(op1, (short)(-op2), rounding: true);

        public static ushort ShiftRightLogicalRounded(ushort op1, byte op2) => UnsignedShift(op1, (short)(-op2), rounding: true);

        public static short ShiftRightLogicalRoundedAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static ushort ShiftRightLogicalRoundedAdd(ushort op1, ushort op2, byte op3) => (ushort)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static ushort UnsignedShift(ushort op1, short op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            ushort rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((ushort)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            (ushort result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                ushort shiftedCarry = ShiftOvf((ushort)1, 8 * sizeof(ushort) + shift).val;
                result = (ushort)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = ushort.MaxValue;
                }
            }

            return result;
        }

        private static short UnsignedShift(short op1, short op2, bool rounding = false, bool saturating = false) => (short)UnsignedShift((ushort)op1, op2, rounding, saturating);

        private static (short val, bool ovf) AddOvf(short op1, short op2)
        {
            short result = (short)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (short val, bool ovf) AddOvf(short op1, ushort op2)
        {
            short result = (short)(op1 + (short)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (ushort val, bool ovf) AddOvf(ushort op1, short op2)
        {
            ushort result = (ushort)(op1 + (ushort)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (ushort val, bool ovf) AddOvf(ushort op1, ushort op2)
        {
            ushort result = (ushort)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (short val, bool ovf) SubtractOvf(short op1, short op2)
        {
            short result = (short)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (ushort val, bool ovf) SubtractOvf(ushort op1, ushort op2)
        {
            ushort result = (ushort)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static short AbsSaturate(short op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static short AddSaturate(short op1, short op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? short.MinValue : short.MaxValue) : result;
        }

        public static short AddSaturate(short op1, ushort op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? short.MaxValue : result;
        }

        public static ushort AddSaturate(ushort op1, short op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? ushort.MaxValue : ushort.MinValue) : result;
        }

        public static ushort AddSaturate(ushort op1, ushort op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? ushort.MaxValue : result;
        }

        public static short NegateSaturate(short op1) => SubtractSaturate((short)0, op1);

        public static short SubtractSaturate(short op1, short op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? short.MinValue : short.MaxValue) : result;
        }

        public static ushort SubtractSaturate(ushort op1, ushort op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? ushort.MinValue : result;
        }

        public static int ShiftArithmetic(int op1, int op2) => SignedShift(op1, op2);

        public static int ShiftArithmeticRounded(int op1, int op2) => SignedShift(op1, op2, rounding: true);

        public static int ShiftArithmeticSaturate(int op1, int op2) => SignedShift(op1, op2, saturating: true);

        public static int ShiftArithmeticRoundedSaturate(int op1, int op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static int SignedShift(int op1, int op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            int rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((int)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            int result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (int)ShiftOvf((uint)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = int.MaxValue;
                    }
                }
            }

            return result;
        }

        public static int ShiftLeftLogical(int op1, byte op2) => UnsignedShift(op1, (int)op2);

        public static uint ShiftLeftLogical(uint op1, byte op2) => UnsignedShift(op1, (int)op2);

        public static int ShiftLeftLogicalSaturate(int op1, byte op2) => SignedShift(op1, (int)op2, saturating: true);

        public static uint ShiftLeftLogicalSaturate(uint op1, byte op2) => UnsignedShift(op1, (int)op2, saturating: true);

        public static uint ShiftLeftLogicalSaturateUnsigned(int op1, byte op2) => (uint)UnsignedShift(op1, (int)op2, saturating: true);

        public static int ShiftLogical(int op1, int op2) => UnsignedShift(op1, op2);

        public static uint ShiftLogical(uint op1, int op2) => UnsignedShift(op1, op2);

        public static uint ShiftLogicalRounded(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true);

        public static int ShiftLogicalRounded(int op1, int op2) => UnsignedShift(op1, op2, rounding: true);

        public static uint ShiftLogicalRoundedSaturate(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static int ShiftLogicalRoundedSaturate(int op1, int op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static int ShiftLogicalSaturate(int op1, int op2) => UnsignedShift(op1, op2, saturating: true);

        public static uint ShiftLogicalSaturate(uint op1, int op2) => UnsignedShift(op1, op2, saturating: true);

        public static int ShiftRightArithmetic(int op1, byte op2) => SignedShift(op1, (int)(-op2));

        public static int ShiftRightArithmeticAdd(int op1, int op2, byte op3) =>  (int)(op1 + ShiftRightArithmetic(op2, op3));

        public static int ShiftRightArithmeticRounded(int op1, byte op2) => SignedShift(op1, (int)(-op2), rounding: true);

        public static int ShiftRightArithmeticRoundedAdd(int op1, int op2, byte op3) =>  (int)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static int ShiftRightLogical(int op1, byte op2) => UnsignedShift(op1, (int)(-op2));

        public static uint ShiftRightLogical(uint op1, byte op2) => UnsignedShift(op1, (int)(-op2));

        public static int ShiftRightLogicalAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightLogical(op2, op3));

        public static uint ShiftRightLogicalAdd(uint op1, uint op2, byte op3) => (uint)(op1 + ShiftRightLogical(op2, op3));

        public static int ShiftRightLogicalRounded(int op1, byte op2) => UnsignedShift(op1, (int)(-op2), rounding: true);

        public static uint ShiftRightLogicalRounded(uint op1, byte op2) => UnsignedShift(op1, (int)(-op2), rounding: true);

        public static int ShiftRightLogicalRoundedAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static uint ShiftRightLogicalRoundedAdd(uint op1, uint op2, byte op3) => (uint)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static uint UnsignedShift(uint op1, int op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            uint rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((uint)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            (uint result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                uint shiftedCarry = ShiftOvf((uint)1, 8 * sizeof(uint) + shift).val;
                result = (uint)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = uint.MaxValue;
                }
            }

            return result;
        }

        private static int UnsignedShift(int op1, int op2, bool rounding = false, bool saturating = false) => (int)UnsignedShift((uint)op1, op2, rounding, saturating);

        private static (int val, bool ovf) AddOvf(int op1, int op2)
        {
            int result = (int)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (int val, bool ovf) AddOvf(int op1, uint op2)
        {
            int result = (int)(op1 + (int)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (uint val, bool ovf) AddOvf(uint op1, int op2)
        {
            uint result = (uint)(op1 + (uint)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (uint val, bool ovf) AddOvf(uint op1, uint op2)
        {
            uint result = (uint)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (int val, bool ovf) SubtractOvf(int op1, int op2)
        {
            int result = (int)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (uint val, bool ovf) SubtractOvf(uint op1, uint op2)
        {
            uint result = (uint)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static int AbsSaturate(int op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static int AddSaturate(int op1, int op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? int.MinValue : int.MaxValue) : result;
        }

        public static int AddSaturate(int op1, uint op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? int.MaxValue : result;
        }

        public static uint AddSaturate(uint op1, int op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? uint.MaxValue : uint.MinValue) : result;
        }

        public static uint AddSaturate(uint op1, uint op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? uint.MaxValue : result;
        }

        public static int NegateSaturate(int op1) => SubtractSaturate((int)0, op1);

        public static int SubtractSaturate(int op1, int op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? int.MinValue : int.MaxValue) : result;
        }

        public static uint SubtractSaturate(uint op1, uint op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? uint.MinValue : result;
        }

        public static long ShiftArithmetic(long op1, long op2) => SignedShift(op1, op2);

        public static long ShiftArithmeticRounded(long op1, long op2) => SignedShift(op1, op2, rounding: true);

        public static long ShiftArithmeticSaturate(long op1, long op2) => SignedShift(op1, op2, saturating: true);

        public static long ShiftArithmeticRoundedSaturate(long op1, long op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static long SignedShift(long op1, long op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            long rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((long)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            long result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (long)ShiftOvf((ulong)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = long.MaxValue;
                    }
                }
            }

            return result;
        }

        public static long ShiftLeftLogical(long op1, byte op2) => UnsignedShift(op1, (long)op2);

        public static ulong ShiftLeftLogical(ulong op1, byte op2) => UnsignedShift(op1, (long)op2);

        public static long ShiftLeftLogicalSaturate(long op1, byte op2) => SignedShift(op1, (long)op2, saturating: true);

        public static ulong ShiftLeftLogicalSaturate(ulong op1, byte op2) => UnsignedShift(op1, (long)op2, saturating: true);

        public static ulong ShiftLeftLogicalSaturateUnsigned(long op1, byte op2) => (ulong)UnsignedShift(op1, (long)op2, saturating: true);

        public static long ShiftLogical(long op1, long op2) => UnsignedShift(op1, op2);

        public static ulong ShiftLogical(ulong op1, long op2) => UnsignedShift(op1, op2);

        public static ulong ShiftLogicalRounded(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true);

        public static long ShiftLogicalRounded(long op1, long op2) => UnsignedShift(op1, op2, rounding: true);

        public static ulong ShiftLogicalRoundedSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static long ShiftLogicalRoundedSaturate(long op1, long op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static long ShiftLogicalSaturate(long op1, long op2) => UnsignedShift(op1, op2, saturating: true);

        public static ulong ShiftLogicalSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, saturating: true);

        public static long ShiftRightArithmetic(long op1, byte op2) => SignedShift(op1, (long)(-op2));

        public static long ShiftRightArithmeticAdd(long op1, long op2, byte op3) =>  (long)(op1 + ShiftRightArithmetic(op2, op3));

        public static long ShiftRightArithmeticRounded(long op1, byte op2) => SignedShift(op1, (long)(-op2), rounding: true);

        public static long ShiftRightArithmeticRoundedAdd(long op1, long op2, byte op3) =>  (long)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static long ShiftRightLogical(long op1, byte op2) => UnsignedShift(op1, (long)(-op2));

        public static ulong ShiftRightLogical(ulong op1, byte op2) => UnsignedShift(op1, (long)(-op2));

        public static long ShiftRightLogicalAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightLogical(op2, op3));

        public static ulong ShiftRightLogicalAdd(ulong op1, ulong op2, byte op3) => (ulong)(op1 + ShiftRightLogical(op2, op3));

        public static long ShiftRightLogicalRounded(long op1, byte op2) => UnsignedShift(op1, (long)(-op2), rounding: true);

        public static ulong ShiftRightLogicalRounded(ulong op1, byte op2) => UnsignedShift(op1, (long)(-op2), rounding: true);

        public static long ShiftRightLogicalRoundedAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static ulong ShiftRightLogicalRoundedAdd(ulong op1, ulong op2, byte op3) => (ulong)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static ulong UnsignedShift(ulong op1, long op2, bool rounding = false, bool saturating = false)
        {
            int shift = (sbyte)(op2 & 0xFF);

            ulong rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((ulong)1, -shift-1);

                if (ovf)
                {
                    return 0;
                }
            }

            (ulong result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                ulong shiftedCarry = ShiftOvf((ulong)1, 8 * sizeof(ulong) + shift).val;
                result = (ulong)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = ulong.MaxValue;
                }
            }

            return result;
        }

        private static long UnsignedShift(long op1, long op2, bool rounding = false, bool saturating = false) => (long)UnsignedShift((ulong)op1, op2, rounding, saturating);

        private static (long val, bool ovf) AddOvf(long op1, long op2)
        {
            long result = (long)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (long val, bool ovf) AddOvf(long op1, ulong op2)
        {
            long result = (long)(op1 + (long)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (ulong val, bool ovf) AddOvf(ulong op1, long op2)
        {
            ulong result = (ulong)(op1 + (ulong)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (ulong val, bool ovf) AddOvf(ulong op1, ulong op2)
        {
            ulong result = (ulong)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (long val, bool ovf) SubtractOvf(long op1, long op2)
        {
            long result = (long)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (ulong val, bool ovf) SubtractOvf(ulong op1, ulong op2)
        {
            ulong result = (ulong)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static long AbsSaturate(long op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static long AddSaturate(long op1, long op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? long.MinValue : long.MaxValue) : result;
        }

        public static long AddSaturate(long op1, ulong op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? long.MaxValue : result;
        }

        public static ulong AddSaturate(ulong op1, long op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? ulong.MaxValue : ulong.MinValue) : result;
        }

        public static ulong AddSaturate(ulong op1, ulong op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? ulong.MaxValue : result;
        }

        public static long NegateSaturate(long op1) => SubtractSaturate((long)0, op1);

        public static long SubtractSaturate(long op1, long op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? long.MinValue : long.MaxValue) : result;
        }

        public static ulong SubtractSaturate(ulong op1, ulong op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? ulong.MinValue : result;
        }


        private static (sbyte val, bool ovf) ShiftOvf(sbyte value, int shift)
        {
            sbyte result = value;

            bool ovf = false;
            sbyte msb = 1;
            msb = (sbyte)(msb << (8 * sizeof(sbyte) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (byte val, bool ovf) ShiftOvf(byte value, int shift)
        {
            byte result = value;

            bool ovf = false;
            byte msb = 1;
            msb = (byte)(msb << (8 * sizeof(byte) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (short val, bool ovf) ShiftOvf(short value, int shift)
        {
            short result = value;

            bool ovf = false;
            short msb = 1;
            msb = (short)(msb << (8 * sizeof(short) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (ushort val, bool ovf) ShiftOvf(ushort value, int shift)
        {
            ushort result = value;

            bool ovf = false;
            ushort msb = 1;
            msb = (ushort)(msb << (8 * sizeof(ushort) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (int val, bool ovf) ShiftOvf(int value, int shift)
        {
            int result = value;

            bool ovf = false;
            int msb = 1;
            msb = (int)(msb << (8 * sizeof(int) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (uint val, bool ovf) ShiftOvf(uint value, int shift)
        {
            uint result = value;

            bool ovf = false;
            uint msb = 1;
            msb = (uint)(msb << (8 * sizeof(uint) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (long val, bool ovf) ShiftOvf(long value, int shift)
        {
            long result = value;

            bool ovf = false;
            long msb = 1;
            msb = (long)(msb << (8 * sizeof(long) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (ulong val, bool ovf) ShiftOvf(ulong value, int shift)
        {
            ulong result = value;

            bool ovf = false;
            ulong msb = 1;
            msb = (ulong)(msb << (8 * sizeof(ulong) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }

        public static float AbsoluteDifference(float op1, float op2) => MathF.Abs(op1 - op2);

        public static float FusedMultiplyAdd(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(op2, op3, op1);

        public static float FusedMultiplyAddNegated(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(-op2, op3, -op1);

        public static float FusedMultiplySubtract(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(-op2, op3, op1);

        public static float FusedMultiplySubtractNegated(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(op2, op3, -op1);

        public static float MaxNumber(float op1, float op2) => float.IsNaN(op1) ? op2 : (float.IsNaN(op2) ? op1 : MathF.Max(op1, op2));

        public static float MaxNumberPairwise(float[] op1, int i) => Pairwise(MaxNumber, op1, i);

        public static float MaxNumberPairwise(float[] op1, float[] op2, int i) => Pairwise(MaxNumber, op1, op2, i);

        public static float MinNumber(float op1, float op2) => float.IsNaN(op1) ? op2 : (float.IsNaN(op2) ? op1 : MathF.Min(op1, op2));

        public static float MinNumberPairwise(float[] op1, int i) => Pairwise(MinNumber, op1, i);

        public static float MinNumberPairwise(float[] op1, float[] op2, int i) => Pairwise(MinNumber, op1, op2, i);

        public static float MultiplyExtended(float op1, float op2)
        {
            bool inf1 = float.IsInfinity(op1);
            bool inf2 = float.IsInfinity(op2);

            bool zero1 = (op1 == 0);
            bool zero2 = (op2 == 0);

            if ((inf1 && zero2) || (zero1 && inf2))
            {
                return MathF.CopySign(2, (zero1 ? op2 : op1));
            }
            else
            {
                return op1 * op2;
            }
        }

        public static float FPReciprocalStepFused(float op1, float op2) => FusedMultiplySubtract(2, op1, op2);

        public static float FPReciprocalSqrtStepFused(float op1, float op2) => FusedMultiplySubtract(3, op1, op2) / 2;

        public static double AbsoluteDifference(double op1, double op2) => Math.Abs(op1 - op2);

        public static double FusedMultiplyAdd(double op1, double op2, double op3) => Math.FusedMultiplyAdd(op2, op3, op1);

        public static double FusedMultiplyAddNegated(double op1, double op2, double op3) => Math.FusedMultiplyAdd(-op2, op3, -op1);

        public static double FusedMultiplySubtract(double op1, double op2, double op3) => Math.FusedMultiplyAdd(-op2, op3, op1);

        public static double FusedMultiplySubtractNegated(double op1, double op2, double op3) => Math.FusedMultiplyAdd(op2, op3, -op1);

        public static double MaxNumber(double op1, double op2) => double.IsNaN(op1) ? op2 : (double.IsNaN(op2) ? op1 : Math.Max(op1, op2));

        public static double MaxNumberPairwise(double[] op1, int i) => Pairwise(MaxNumber, op1, i);

        public static double MaxNumberPairwise(double[] op1, double[] op2, int i) => Pairwise(MaxNumber, op1, op2, i);

        public static double MinNumber(double op1, double op2) => double.IsNaN(op1) ? op2 : (double.IsNaN(op2) ? op1 : Math.Min(op1, op2));

        public static double MinNumberPairwise(double[] op1, int i) => Pairwise(MinNumber, op1, i);

        public static double MinNumberPairwise(double[] op1, double[] op2, int i) => Pairwise(MinNumber, op1, op2, i);

        public static double MultiplyExtended(double op1, double op2)
        {
            bool inf1 = double.IsInfinity(op1);
            bool inf2 = double.IsInfinity(op2);

            bool zero1 = (op1 == 0);
            bool zero2 = (op2 == 0);

            if ((inf1 && zero2) || (zero1 && inf2))
            {
                return Math.CopySign(2, (zero1 ? op2 : op1));
            }
            else
            {
                return op1 * op2;
            }
        }

        public static double FPReciprocalStepFused(double op1, double op2) => FusedMultiplySubtract(2, op1, op2);

        public static double FPReciprocalSqrtStepFused(double op1, double op2) => FusedMultiplySubtract(3, op1, op2) / 2;

        private static uint ReciprocalEstimate(uint a)
        {
            a = a * 2 + 1;

            uint b = (1 << 19) / a;
            uint r = (b + 1) / 2;

            return r;
        }

        private static uint ReciprocalSqrtEstimate(uint a)
        {
            if (a < 256)
            {
                a = a * 2 + 1;
            }
            else
            {
                a = (a >> 1) << 1;
                a = (a + 1) * 2;
            }

            uint b = 512;

            while (a * (b + 1) * (b + 1) < (1 << 28))
            {
                b = b + 1;
            }

            uint r = (b + 1) / 2;

            return r;
        }

        public static double ReciprocalEstimate(double op1) => Math.ReciprocalEstimate(op1);
        
        public static float ReciprocalEstimate(float op1) => MathF.ReciprocalEstimate(op1);

        public static double ReciprocalExponent(double op1)
        {
            ulong bits = (ulong)BitConverter.DoubleToUInt64Bits(op1);

            // Invert the exponent
            bits ^= 0x7FF0000000000000;
            // Zero the fraction
            bits &= 0xFFF0000000000000;

            return BitConverter.UInt64BitsToDouble(bits);
        }

        public static float ReciprocalExponent(float op1)
        {
            uint bits = BitConverter.SingleToUInt32Bits(op1);

            // Invert the exponent
            bits ^= 0x7F800000;
            // Zero the fraction
            bits &= 0xFF800000;

            return BitConverter.UInt32BitsToSingle(bits);
        }

        public static double ReciprocalSqrtEstimate(double op1) => Math.ReciprocalSqrtEstimate(op1);
        
        public static float ReciprocalSqrtEstimate(float op1) => MathF.ReciprocalSqrtEstimate(op1);

        private static uint ExtractBits(uint val, byte msbPos, byte lsbPos)
        {
            uint andMask = 0;

            for (byte pos = lsbPos; pos <= msbPos; pos++)
            {
                andMask |= (uint)1 << pos;
            }

            return (val & andMask) >> lsbPos;
        }

        public static uint UnsignedReciprocalEstimate(uint op1)
        {
            uint result;

            if ((op1 & (1 << 31)) == 0)
            {
                result = ~0U;
            }
            else
            {
                uint estimate = ReciprocalEstimate(ExtractBits(op1, 31, 23));
                result = ExtractBits(estimate, 8, 0) << 31;
            }

            return result;
        }

        public static uint UnsignedReciprocalSqrtEstimate(uint op1)
        {
            uint result;

            if ((op1 & (3 << 30)) == 0)
            {
                result = ~0U;
            }
            else
            {
                uint estimate = ReciprocalSqrtEstimate(ExtractBits(op1, 31, 23));
                result = ExtractBits(estimate, 8, 0) << 31;
            }

            return result;
        }

        public static sbyte Add(sbyte op1, sbyte op2) => (sbyte)(op1 + op2);

        public static sbyte AddPairwise(sbyte[] op1, int i) => Pairwise(Add, op1, i);

        public static sbyte AddPairwise(sbyte[] op1, sbyte[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static sbyte Max(sbyte op1, sbyte op2) => Math.Max(op1, op2);

        public static sbyte MaxPairwise(sbyte[] op1, int i) => Pairwise(Max, op1, i);

        public static sbyte MaxPairwise(sbyte[] op1, sbyte[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static sbyte Min(sbyte op1, sbyte op2) => Math.Min(op1, op2);

        public static sbyte MinPairwise(sbyte[] op1, int i) => Pairwise(Min, op1, i);

        public static sbyte MinPairwise(sbyte[] op1, sbyte[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static sbyte Multiply(sbyte op1, sbyte op2) => (sbyte)(op1 * op2);

        public static sbyte MultiplyAdd(sbyte op1, sbyte op2, sbyte op3) => (sbyte)(op1 + (sbyte)(op2 * op3));

        public static sbyte MultiplySubtract(sbyte op1, sbyte op2, sbyte op3) => (sbyte)(op1 - (sbyte)(op2 * op3));

        public static sbyte Subtract(sbyte op1, sbyte op2) => (sbyte)(op1 - op2);

        private static sbyte Pairwise(Func<sbyte, sbyte, sbyte> pairOp, sbyte[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static sbyte Pairwise(Func<sbyte, sbyte, sbyte> pairOp, sbyte[] op1, sbyte[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static byte Add(byte op1, byte op2) => (byte)(op1 + op2);

        public static byte AddPairwise(byte[] op1, int i) => Pairwise(Add, op1, i);

        public static byte AddPairwise(byte[] op1, byte[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static byte Max(byte op1, byte op2) => Math.Max(op1, op2);

        public static byte MaxPairwise(byte[] op1, int i) => Pairwise(Max, op1, i);

        public static byte MaxPairwise(byte[] op1, byte[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static byte Min(byte op1, byte op2) => Math.Min(op1, op2);

        public static byte MinPairwise(byte[] op1, int i) => Pairwise(Min, op1, i);

        public static byte MinPairwise(byte[] op1, byte[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static byte Multiply(byte op1, byte op2) => (byte)(op1 * op2);

        public static byte MultiplyAdd(byte op1, byte op2, byte op3) => (byte)(op1 + (byte)(op2 * op3));

        public static byte MultiplySubtract(byte op1, byte op2, byte op3) => (byte)(op1 - (byte)(op2 * op3));

        public static byte Subtract(byte op1, byte op2) => (byte)(op1 - op2);

        private static byte Pairwise(Func<byte, byte, byte> pairOp, byte[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static byte Pairwise(Func<byte, byte, byte> pairOp, byte[] op1, byte[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static short Add(short op1, short op2) => (short)(op1 + op2);

        public static short AddPairwise(short[] op1, int i) => Pairwise(Add, op1, i);

        public static short AddPairwise(short[] op1, short[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static short Max(short op1, short op2) => Math.Max(op1, op2);

        public static short MaxPairwise(short[] op1, int i) => Pairwise(Max, op1, i);

        public static short MaxPairwise(short[] op1, short[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static short Min(short op1, short op2) => Math.Min(op1, op2);

        public static short MinPairwise(short[] op1, int i) => Pairwise(Min, op1, i);

        public static short MinPairwise(short[] op1, short[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static short Multiply(short op1, short op2) => (short)(op1 * op2);

        public static short MultiplyAdd(short op1, short op2, short op3) => (short)(op1 + (short)(op2 * op3));

        public static short MultiplySubtract(short op1, short op2, short op3) => (short)(op1 - (short)(op2 * op3));

        public static short Subtract(short op1, short op2) => (short)(op1 - op2);

        private static short Pairwise(Func<short, short, short> pairOp, short[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static short Pairwise(Func<short, short, short> pairOp, short[] op1, short[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static ushort Add(ushort op1, ushort op2) => (ushort)(op1 + op2);

        public static ushort AddPairwise(ushort[] op1, int i) => Pairwise(Add, op1, i);

        public static ushort AddPairwise(ushort[] op1, ushort[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static ushort Max(ushort op1, ushort op2) => Math.Max(op1, op2);

        public static ushort MaxPairwise(ushort[] op1, int i) => Pairwise(Max, op1, i);

        public static ushort MaxPairwise(ushort[] op1, ushort[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static ushort Min(ushort op1, ushort op2) => Math.Min(op1, op2);

        public static ushort MinPairwise(ushort[] op1, int i) => Pairwise(Min, op1, i);

        public static ushort MinPairwise(ushort[] op1, ushort[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static ushort Multiply(ushort op1, ushort op2) => (ushort)(op1 * op2);

        public static ushort MultiplyAdd(ushort op1, ushort op2, ushort op3) => (ushort)(op1 + (ushort)(op2 * op3));

        public static ushort MultiplySubtract(ushort op1, ushort op2, ushort op3) => (ushort)(op1 - (ushort)(op2 * op3));

        public static ushort Subtract(ushort op1, ushort op2) => (ushort)(op1 - op2);

        private static ushort Pairwise(Func<ushort, ushort, ushort> pairOp, ushort[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static ushort Pairwise(Func<ushort, ushort, ushort> pairOp, ushort[] op1, ushort[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static int Add(int op1, int op2) => (int)(op1 + op2);

        public static int AddPairwise(int[] op1, int i) => Pairwise(Add, op1, i);

        public static int AddPairwise(int[] op1, int[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static int Max(int op1, int op2) => Math.Max(op1, op2);

        public static int MaxPairwise(int[] op1, int i) => Pairwise(Max, op1, i);

        public static int MaxPairwise(int[] op1, int[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static int Min(int op1, int op2) => Math.Min(op1, op2);

        public static int MinPairwise(int[] op1, int i) => Pairwise(Min, op1, i);

        public static int MinPairwise(int[] op1, int[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static int Multiply(int op1, int op2) => (int)(op1 * op2);

        public static int MultiplyAdd(int op1, int op2, int op3) => (int)(op1 + (int)(op2 * op3));

        public static int MultiplySubtract(int op1, int op2, int op3) => (int)(op1 - (int)(op2 * op3));

        public static int Subtract(int op1, int op2) => (int)(op1 - op2);

        private static int Pairwise(Func<int, int, int> pairOp, int[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static int Pairwise(Func<int, int, int> pairOp, int[] op1, int[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static uint Add(uint op1, uint op2) => (uint)(op1 + op2);

        public static uint AddPairwise(uint[] op1, int i) => Pairwise(Add, op1, i);

        public static uint AddPairwise(uint[] op1, uint[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static uint Max(uint op1, uint op2) => Math.Max(op1, op2);

        public static uint MaxPairwise(uint[] op1, int i) => Pairwise(Max, op1, i);

        public static uint MaxPairwise(uint[] op1, uint[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static uint Min(uint op1, uint op2) => Math.Min(op1, op2);

        public static uint MinPairwise(uint[] op1, int i) => Pairwise(Min, op1, i);

        public static uint MinPairwise(uint[] op1, uint[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static uint Multiply(uint op1, uint op2) => (uint)(op1 * op2);

        public static uint MultiplyAdd(uint op1, uint op2, uint op3) => (uint)(op1 + (uint)(op2 * op3));

        public static uint MultiplySubtract(uint op1, uint op2, uint op3) => (uint)(op1 - (uint)(op2 * op3));

        public static uint Subtract(uint op1, uint op2) => (uint)(op1 - op2);

        private static uint Pairwise(Func<uint, uint, uint> pairOp, uint[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static uint Pairwise(Func<uint, uint, uint> pairOp, uint[] op1, uint[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static long Add(long op1, long op2) => (long)(op1 + op2);

        public static long AddPairwise(long[] op1, int i) => Pairwise(Add, op1, i);

        public static long AddPairwise(long[] op1, long[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static long Max(long op1, long op2) => Math.Max(op1, op2);

        public static long MaxPairwise(long[] op1, int i) => Pairwise(Max, op1, i);

        public static long MaxPairwise(long[] op1, long[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static long Min(long op1, long op2) => Math.Min(op1, op2);

        public static long MinPairwise(long[] op1, int i) => Pairwise(Min, op1, i);

        public static long MinPairwise(long[] op1, long[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static long Multiply(long op1, long op2) => (long)(op1 * op2);

        public static long MultiplyAdd(long op1, long op2, long op3) => (long)(op1 + (long)(op2 * op3));

        public static long MultiplySubtract(long op1, long op2, long op3) => (long)(op1 - (long)(op2 * op3));

        public static long Subtract(long op1, long op2) => (long)(op1 - op2);

        private static long Pairwise(Func<long, long, long> pairOp, long[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static long Pairwise(Func<long, long, long> pairOp, long[] op1, long[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static ulong Add(ulong op1, ulong op2) => (ulong)(op1 + op2);

        public static ulong AddPairwise(ulong[] op1, int i) => Pairwise(Add, op1, i);

        public static ulong AddPairwise(ulong[] op1, ulong[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static ulong Max(ulong op1, ulong op2) => Math.Max(op1, op2);

        public static ulong MaxPairwise(ulong[] op1, int i) => Pairwise(Max, op1, i);

        public static ulong MaxPairwise(ulong[] op1, ulong[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static ulong Min(ulong op1, ulong op2) => Math.Min(op1, op2);

        public static ulong MinPairwise(ulong[] op1, int i) => Pairwise(Min, op1, i);

        public static ulong MinPairwise(ulong[] op1, ulong[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static ulong Multiply(ulong op1, ulong op2) => (ulong)(op1 * op2);

        public static ulong MultiplyAdd(ulong op1, ulong op2, ulong op3) => (ulong)(op1 + (ulong)(op2 * op3));

        public static ulong MultiplySubtract(ulong op1, ulong op2, ulong op3) => (ulong)(op1 - (ulong)(op2 * op3));

        public static ulong Subtract(ulong op1, ulong op2) => (ulong)(op1 - op2);

        private static ulong Pairwise(Func<ulong, ulong, ulong> pairOp, ulong[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static ulong Pairwise(Func<ulong, ulong, ulong> pairOp, ulong[] op1, ulong[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static float Add(float op1, float op2) => (float)(op1 + op2);

        public static float AddPairwise(float[] op1, int i) => Pairwise(Add, op1, i);

        public static float AddPairwise(float[] op1, float[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static float Max(float op1, float op2) => Math.Max(op1, op2);

        public static float MaxPairwise(float[] op1, int i) => Pairwise(Max, op1, i);

        public static float MaxPairwise(float[] op1, float[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static float Min(float op1, float op2) => Math.Min(op1, op2);

        public static float MinPairwise(float[] op1, int i) => Pairwise(Min, op1, i);

        public static float MinPairwise(float[] op1, float[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static float Multiply(float op1, float op2) => (float)(op1 * op2);

        public static float MultiplyAdd(float op1, float op2, float op3) => (float)(op1 + (float)(op2 * op3));

        public static float MultiplySubtract(float op1, float op2, float op3) => (float)(op1 - (float)(op2 * op3));

        public static float Subtract(float op1, float op2) => (float)(op1 - op2);

        private static float Pairwise(Func<float, float, float> pairOp, float[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static float Pairwise(Func<float, float, float> pairOp, float[] op1, float[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static double Add(double op1, double op2) => (double)(op1 + op2);

        public static double AddPairwise(double[] op1, int i) => Pairwise(Add, op1, i);

        public static double AddPairwise(double[] op1, double[] op2, int i) => Pairwise(Add, op1, op2, i);

        public static double Max(double op1, double op2) => Math.Max(op1, op2);

        public static double MaxPairwise(double[] op1, int i) => Pairwise(Max, op1, i);

        public static double MaxPairwise(double[] op1, double[] op2, int i) => Pairwise(Max, op1, op2, i);

        public static double Min(double op1, double op2) => Math.Min(op1, op2);

        public static double MinPairwise(double[] op1, int i) => Pairwise(Min, op1, i);

        public static double MinPairwise(double[] op1, double[] op2, int i) => Pairwise(Min, op1, op2, i);

        public static double Multiply(double op1, double op2) => (double)(op1 * op2);

        public static double MultiplyAdd(double op1, double op2, double op3) => (double)(op1 + (double)(op2 * op3));

        public static double MultiplySubtract(double op1, double op2, double op3) => (double)(op1 - (double)(op2 * op3));

        public static double Subtract(double op1, double op2) => (double)(op1 - op2);

        private static double Pairwise(Func<double, double, double> pairOp, double[] op1, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return 0;
            }
        }

        private static double Pairwise(Func<double, double, double> pairOp, double[] op1, double[] op2, int i)
        {
            if (2 * i + 1 < op1.Length)
            {
                return pairOp(op1[2 * i], op1[2 * i + 1]);
            }
            else
            {
                return pairOp(op2[2 * i - op1.Length], op2[2 * i + 1 - op1.Length]);
            }
        }

        public static sbyte Negate(sbyte op1) => (sbyte)(-op1);

        public static short Negate(short op1) => (short)(-op1);

        public static int Negate(int op1) => (int)(-op1);

        public static long Negate(long op1) => (long)(-op1);

        public static float Negate(float op1) => (float)(-op1);

        public static double Negate(double op1) => (double)(-op1);

        public static sbyte AddAcross(sbyte[] op1) => Reduce(Add, op1);

        public static sbyte AndAcross(sbyte[] op1) => Reduce(And, op1);

        public static sbyte MaxAcross(sbyte[] op1) => Reduce(Max, op1);

        public static sbyte MinAcross(sbyte[] op1) => Reduce(Min, op1);

        public static sbyte OrAcross(sbyte[] op1) => Reduce(Or, op1);

        public static sbyte XorAcross(sbyte[] op1) => Reduce(Xor, op1);

        private static sbyte Reduce(Func<sbyte, sbyte, sbyte> reduceOp, sbyte[] op1)
        {
            sbyte acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static byte AddAcross(byte[] op1) => Reduce(Add, op1);

        public static byte AndAcross(byte[] op1) => Reduce(And, op1);

        public static byte MaxAcross(byte[] op1) => Reduce(Max, op1);

        public static byte MinAcross(byte[] op1) => Reduce(Min, op1);

        public static byte OrAcross(byte[] op1) => Reduce(Or, op1);

        public static byte XorAcross(byte[] op1) => Reduce(Xor, op1);

        private static byte Reduce(Func<byte, byte, byte> reduceOp, byte[] op1)
        {
            byte acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static short AddAcross(short[] op1) => Reduce(Add, op1);

        public static short AndAcross(short[] op1) => Reduce(And, op1);

        public static short MaxAcross(short[] op1) => Reduce(Max, op1);

        public static short MinAcross(short[] op1) => Reduce(Min, op1);

        public static short OrAcross(short[] op1) => Reduce(Or, op1);

        public static short XorAcross(short[] op1) => Reduce(Xor, op1);

        private static short Reduce(Func<short, short, short> reduceOp, short[] op1)
        {
            short acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static ushort AddAcross(ushort[] op1) => Reduce(Add, op1);

        public static ushort AndAcross(ushort[] op1) => Reduce(And, op1);

        public static ushort MaxAcross(ushort[] op1) => Reduce(Max, op1);

        public static ushort MinAcross(ushort[] op1) => Reduce(Min, op1);

        public static ushort OrAcross(ushort[] op1) => Reduce(Or, op1);

        public static ushort XorAcross(ushort[] op1) => Reduce(Xor, op1);

        private static ushort Reduce(Func<ushort, ushort, ushort> reduceOp, ushort[] op1)
        {
            ushort acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static int AddAcross(int[] op1) => Reduce(Add, op1);

        public static int AndAcross(int[] op1) => Reduce(And, op1);

        public static int MaxAcross(int[] op1) => Reduce(Max, op1);

        public static int MinAcross(int[] op1) => Reduce(Min, op1);

        public static int OrAcross(int[] op1) => Reduce(Or, op1);

        public static int XorAcross(int[] op1) => Reduce(Xor, op1);

        private static int Reduce(Func<int, int, int> reduceOp, int[] op1)
        {
            int acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static uint AddAcross(uint[] op1) => Reduce(Add, op1);

        public static uint AndAcross(uint[] op1) => Reduce(And, op1);

        public static uint MaxAcross(uint[] op1) => Reduce(Max, op1);

        public static uint MinAcross(uint[] op1) => Reduce(Min, op1);

        public static uint OrAcross(uint[] op1) => Reduce(Or, op1);

        public static uint XorAcross(uint[] op1) => Reduce(Xor, op1);

        private static uint Reduce(Func<uint, uint, uint> reduceOp, uint[] op1)
        {
            uint acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static long AddAcross(long[] op1) => Reduce(Add, op1);

        public static long AndAcross(long[] op1) => Reduce(And, op1);

        public static long MaxAcross(long[] op1) => Reduce(Max, op1);

        public static long MinAcross(long[] op1) => Reduce(Min, op1);

        public static long OrAcross(long[] op1) => Reduce(Or, op1);

        public static long XorAcross(long[] op1) => Reduce(Xor, op1);

        private static long Reduce(Func<long, long, long> reduceOp, long[] op1)
        {
            long acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static ulong AddAcross(ulong[] op1) => Reduce(Add, op1);

        public static ulong AndAcross(ulong[] op1) => Reduce(And, op1);

        public static ulong MaxAcross(ulong[] op1) => Reduce(Max, op1);

        public static ulong MinAcross(ulong[] op1) => Reduce(Min, op1);

        public static ulong OrAcross(ulong[] op1) => Reduce(Or, op1);

        public static ulong XorAcross(ulong[] op1) => Reduce(Xor, op1);

        private static ulong Reduce(Func<ulong, ulong, ulong> reduceOp, ulong[] op1)
        {
            ulong acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static float AddAcross(float[] op1) => Reduce(Add, op1);

        public static float MaxAcross(float[] op1) => Reduce(Max, op1);

        public static float MinAcross(float[] op1) => Reduce(Min, op1);

        private static float Reduce(Func<float, float, float> reduceOp, float[] op1)
        {
            float acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static float AddAcrossRecursivePairwise(float[] op1) => ReduceRecursivePairwise(Add, op1);

        private static float ReduceRecursivePairwise(Func<float, float, float> reduceOp, float[] op1)
        {
            if (op1.Length == 2)
            {
                return reduceOp(op1[0], op1[1]);
            }

            if (op1.Length % 2 != 0)
            {
                return float.NaN;
            }

            float[] l = new float[op1.Length / 2];
            Array.Copy(op1, 0, l, 0, (op1.Length / 2));
            float l_reduced = ReduceRecursivePairwise(reduceOp, l);

            float[] r = new float[op1.Length / 2];
            Array.Copy(op1, (op1.Length / 2), r, 0, (op1.Length / 2));
            float r_reduced = ReduceRecursivePairwise(reduceOp, r);

            return reduceOp(l_reduced, r_reduced);
        }

        public static double AddAcross(double[] op1) => Reduce(Add, op1);

        public static double MaxAcross(double[] op1) => Reduce(Max, op1);

        public static double MinAcross(double[] op1) => Reduce(Min, op1);

        private static double Reduce(Func<double, double, double> reduceOp, double[] op1)
        {
            double acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static double AddAcrossRecursivePairwise(double[] op1) => ReduceRecursivePairwise(Add, op1);

        private static double ReduceRecursivePairwise(Func<double, double, double> reduceOp, double[] op1)
        {
            if (op1.Length == 2)
            {
                return reduceOp(op1[0], op1[1]);
            }

            if (op1.Length % 2 != 0)
            {
                return double.NaN;
            }

            double[] l = new double[op1.Length / 2];
            Array.Copy(op1, 0, l, 0, (op1.Length / 2));
            double l_reduced = ReduceRecursivePairwise(reduceOp, l);

            double[] r = new double[op1.Length / 2];
            Array.Copy(op1, (op1.Length / 2), r, 0, (op1.Length / 2));
            double r_reduced = ReduceRecursivePairwise(reduceOp, r);

            return reduceOp(l_reduced, r_reduced);
        }

        public static float MaxNumberAcross(float[] op1) => Reduce(MaxNumber, op1);

        public static float MinNumberAcross(float[] op1) => Reduce(MinNumber, op1);

        private struct poly128_t
        {
            public ulong lo;
            public ulong hi;

            public static poly128_t operator ^(poly128_t op1, poly128_t op2)
            {
                op1.lo ^= op2.lo;
                op1.hi ^= op2.hi;

                return op1;
            }

            public static poly128_t operator <<(poly128_t val, int shiftAmount)
            {
                for (int i = 0; i < shiftAmount; i++)
                {
                    val.hi <<= 1;

                    if ((val.lo & 0x8000000000000000U) != 0)
                    {
                       val.hi |= 1;
                    }

                    val.lo <<= 1;
                }

                return val;
            }

            public static implicit operator poly128_t(ulong lo)
            {
                poly128_t result = new poly128_t();
                result.lo = lo;
                return result;
            }

            public static explicit operator poly128_t(long lo)
            {
                poly128_t result = new poly128_t();
                result.lo = (ulong)lo;
                return result;
            }
        }

        private static ushort PolynomialMult(byte op1, byte op2)
        {
            ushort result = default(ushort);
            ushort extendedOp2 = (ushort)op2;

            for (int i = 0; i < 8 * sizeof(byte); i++)
            {
                if ((op1 & ((byte)1 << i)) != 0)
                {
                    result = (ushort)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static short PolynomialMult(sbyte op1, sbyte op2)
        {
            short result = default(short);
            short extendedOp2 = (short)op2;

            for (int i = 0; i < 8 * sizeof(sbyte); i++)
            {
                if ((op1 & ((sbyte)1 << i)) != 0)
                {
                    result = (short)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static poly128_t PolynomialMult(ulong op1, ulong op2)
        {
            poly128_t result = default(poly128_t);
            poly128_t extendedOp2 = (poly128_t)op2;

            for (int i = 0; i < 8 * sizeof(ulong); i++)
            {
                if ((op1 & ((ulong)1 << i)) != 0)
                {
                    result = (poly128_t)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static poly128_t PolynomialMult(long op1, long op2)
        {
            poly128_t result = default(poly128_t);
            poly128_t extendedOp2 = (poly128_t)op2;

            for (int i = 0; i < 8 * sizeof(long); i++)
            {
                if ((op1 & ((long)1 << i)) != 0)
                {
                    result = (poly128_t)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        public static long MultiplyHigh(long op1, long op2)
        {
            ulong u0, v0, w0;
            long u1, v1, w1, w2, t;
            u0 = (ulong)op1 & 0xFFFFFFFF;
            u1 = op1 >> 32;
            v0 = (ulong)op2 & 0xFFFFFFFF;
            v1 = op2 >> 32;
            w0 = u0 * v0;
            t  = u1 * (long)v0 + (long)(w0 >> 32);
            w1 = t & 0xFFFFFFFF;
            w2 = t >> 32;
            w1 = (long)u0 * v1 + w1;
            return u1 * v1 + w2 + (w1 >> 32);
        }

        public static ulong MultiplyHigh(ulong op1, ulong op2)
        {
            ulong u0, v0, w0;
            ulong u1, v1, w1, w2, t;
            u0 = (ulong)op1 & 0xFFFFFFFF;
            u1 = op1 >> 32;
            v0 = (ulong)op2 & 0xFFFFFFFF;
            v1 = op2 >> 32;
            w0 = u0 * v0;
            t  = u1 * (ulong)v0 + (ulong)(w0 >> 32);
            w1 = t & 0xFFFFFFFF;
            w2 = t >> 32;
            w1 = (ulong)u0 * v1 + w1;
            return u1 * v1 + w2 + (w1 >> 32);
        }

        public static byte PolynomialMultiply(byte op1, byte op2) => (byte)PolynomialMult(op1, op2);

        public static ushort PolynomialMultiplyWidening(byte op1, byte op2) => PolynomialMult(op1, op2);

        public static ushort PolynomialMultiplyWideningUpper(byte[] op1, byte[] op2, int i) => PolynomialMultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static sbyte PolynomialMultiply(sbyte op1, sbyte op2) => (sbyte)PolynomialMult(op1, op2);

        public static short PolynomialMultiplyWidening(sbyte op1, sbyte op2) => PolynomialMult(op1, op2);

        public static short PolynomialMultiplyWideningUpper(sbyte[] op1, sbyte[] op2, int i) => PolynomialMultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong PolynomialMultiplyWideningLo64(ulong op1, ulong op2) => PolynomialMult(op1, op2).lo;

        public static long PolynomialMultiplyWideningLo64(long op1, long op2) => (long)PolynomialMult(op1, op2).lo;

        public static ulong PolynomialMultiplyWideningHi64(ulong op1, ulong op2) => PolynomialMult(op1, op2).hi;

        public static long PolynomialMultiplyWideningHi64(long op1, long op2) => (long)PolynomialMult(op1, op2).hi;

        public static sbyte Concat(sbyte[] op1, sbyte[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static sbyte ConcatScalar(sbyte[] op1, sbyte[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static sbyte ExtractVector(sbyte[] op1, sbyte[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static sbyte Insert(sbyte[] op1, int op2, sbyte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static byte Concat(byte[] op1, byte[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static byte ConcatScalar(byte[] op1, byte[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static byte ExtractVector(byte[] op1, byte[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static byte Insert(byte[] op1, int op2, byte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static short Concat(short[] op1, short[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static short ConcatScalar(short[] op1, short[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static short ExtractVector(short[] op1, short[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static short Insert(short[] op1, int op2, short op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ushort Concat(ushort[] op1, ushort[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static ushort ConcatScalar(ushort[] op1, ushort[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static ushort ExtractVector(ushort[] op1, ushort[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static ushort Insert(ushort[] op1, int op2, ushort op3, int i) => (op2 != i) ? op1[i] : op3;

        public static int Concat(int[] op1, int[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static int ConcatScalar(int[] op1, int[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static int ExtractVector(int[] op1, int[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static int Insert(int[] op1, int op2, int op3, int i) => (op2 != i) ? op1[i] : op3;

        public static uint Concat(uint[] op1, uint[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static uint ConcatScalar(uint[] op1, uint[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static uint ExtractVector(uint[] op1, uint[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static uint Insert(uint[] op1, int op2, uint op3, int i) => (op2 != i) ? op1[i] : op3;

        public static long Concat(long[] op1, long[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static long ConcatScalar(long[] op1, long[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static long ExtractVector(long[] op1, long[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static long Insert(long[] op1, int op2, long op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ulong Concat(ulong[] op1, ulong[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static ulong ConcatScalar(ulong[] op1, ulong[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static ulong ExtractVector(ulong[] op1, ulong[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static ulong Insert(ulong[] op1, int op2, ulong op3, int i) => (op2 != i) ? op1[i] : op3;

        public static float Concat(float[] op1, float[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static float ConcatScalar(float[] op1, float[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static float ExtractVector(float[] op1, float[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static float Insert(float[] op1, int op2, float op3, int i) => (op2 != i) ? op1[i] : op3;

        public static double Concat(double[] op1, double[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static double ConcatScalar(double[] op1, double[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static double ExtractVector(double[] op1, double[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static double Insert(double[] op1, int op2, double op3, int i) => (op2 != i) ? op1[i] : op3;

        public static int LoadPairScalar(int[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (int)0;

        public static uint LoadPairScalar(uint[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (uint)0;

        public static float LoadPairScalar(float[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (float)0;

        public static sbyte TableVectorExtension(int i, sbyte[] defaultValues, sbyte[] indices, params sbyte[][] table)
        {
            sbyte[] fullTable = table.SelectMany(x => x).ToArray();
            int index = indices[i];

            if (index < 0 || index >= fullTable.Length)
              return defaultValues[i];

            return fullTable[index];
        }

        public static sbyte TableVectorLookup(int i, sbyte[] indices, params sbyte[][] table)
        {
            sbyte[] zeros = new sbyte[indices.Length];
            Array.Fill<sbyte>(zeros, 0, 0, indices.Length);

            return TableVectorExtension(i, zeros, indices, table);
        }

        public static byte TableVectorExtension(int i, byte[] defaultValues, byte[] indices, params byte[][] table)
        {
            byte[] fullTable = table.SelectMany(x => x).ToArray();
            int index = indices[i];

            if (index < 0 || index >= fullTable.Length)
              return defaultValues[i];

            return fullTable[index];
        }

        public static byte TableVectorLookup(int i, byte[] indices, params byte[][] table)
        {
            byte[] zeros = new byte[indices.Length];
            Array.Fill<byte>(zeros, 0, 0, indices.Length);

            return TableVectorExtension(i, zeros, indices, table);
        }

        public static byte ShiftLeftAndInsert(byte left, byte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue << shift);
            byte value = (byte)(right << shift);
            byte newval = (byte)(((byte)left & mask) | value);
            return newval;
        }

        public static byte ShiftRightAndInsert(byte left, byte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue >> shift);
            byte value = (byte)(right >> shift);
            byte newval = (byte)(((byte)left & mask) | value);
            return newval;
        }

        public static short ShiftLeftAndInsert(short left, short right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue << shift);
            ushort value = (ushort)(right << shift);
            short newval = (short)(((ushort)left & mask) | value);
            return newval;
        }

        public static short ShiftRightAndInsert(short left, short right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue >> shift);
            ushort value = (ushort)(right >> shift);
            short newval = (short)(((ushort)left & mask) | value);
            return newval;
        }

        public static int ShiftLeftAndInsert(int left, int right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue << shift);
            uint value = (uint)(right << shift);
            int newval = (int)(((uint)left & mask) | value);
            return newval;
        }

        public static int ShiftRightAndInsert(int left, int right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue >> shift);
            uint value = (uint)(right >> shift);
            int newval = (int)(((uint)left & mask) | value);
            return newval;
        }

        public static long ShiftLeftAndInsert(long left, long right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue << shift);
            ulong value = (ulong)(right << shift);
            long newval = (long)(((ulong)left & mask) | value);
            return newval;
        }

        public static long ShiftRightAndInsert(long left, long right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue >> shift);
            ulong value = (ulong)(right >> shift);
            long newval = (long)(((ulong)left & mask) | value);
            return newval;
        }

        public static sbyte ShiftLeftAndInsert(sbyte left, sbyte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue << shift);
            byte value = (byte)(right << shift);
            sbyte newval = (sbyte)(((byte)left & mask) | value);
            return newval;
        }

        public static sbyte ShiftRightAndInsert(sbyte left, sbyte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue >> shift);
            byte value = (byte)(right >> shift);
            sbyte newval = (sbyte)(((byte)left & mask) | value);
            return newval;
        }

        public static ushort ShiftLeftAndInsert(ushort left, ushort right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue << shift);
            ushort value = (ushort)(right << shift);
            ushort newval = (ushort)(((ushort)left & mask) | value);
            return newval;
        }

        public static ushort ShiftRightAndInsert(ushort left, ushort right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue >> shift);
            ushort value = (ushort)(right >> shift);
            ushort newval = (ushort)(((ushort)left & mask) | value);
            return newval;
        }

        public static uint ShiftLeftAndInsert(uint left, uint right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue << shift);
            uint value = (uint)(right << shift);
            uint newval = (uint)(((uint)left & mask) | value);
            return newval;
        }

        public static uint ShiftRightAndInsert(uint left, uint right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue >> shift);
            uint value = (uint)(right >> shift);
            uint newval = (uint)(((uint)left & mask) | value);
            return newval;
        }

        public static ulong ShiftLeftAndInsert(ulong left, ulong right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue << shift);
            ulong value = (ulong)(right << shift);
            ulong newval = (ulong)(((ulong)left & mask) | value);
            return newval;
        }

        public static ulong ShiftRightAndInsert(ulong left, ulong right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue >> shift);
            ulong value = (ulong)(right >> shift);
            ulong newval = (ulong)(((ulong)left & mask) | value);
            return newval;
        }

        public static double Ceiling(double op1) => Math.Ceiling(op1);

        public static double Floor(double op1) => Math.Floor(op1);

        public static double RoundAwayFromZero(double op1) => Math.Round(op1, MidpointRounding.AwayFromZero);

        public static double RoundToNearest(double op1) => Math.Round(op1, MidpointRounding.ToEven);

        public static double RoundToNegativeInfinity(double op1) => Math.Round(op1, MidpointRounding.ToNegativeInfinity);

        public static double RoundToPositiveInfinity(double op1) => Math.Round(op1, MidpointRounding.ToPositiveInfinity);

        public static double RoundToZero(double op1) => Math.Round(op1, MidpointRounding.ToZero);

        public static float Ceiling(float op1) => MathF.Ceiling(op1);

        public static float Floor(float op1) => MathF.Floor(op1);

        public static float RoundAwayFromZero(float op1) => MathF.Round(op1, MidpointRounding.AwayFromZero);

        public static float RoundToNearest(float op1) => MathF.Round(op1, MidpointRounding.ToEven);

        public static float RoundToNegativeInfinity(float op1) => MathF.Round(op1, MidpointRounding.ToNegativeInfinity);

        public static float RoundToPositiveInfinity(float op1) => MathF.Round(op1, MidpointRounding.ToPositiveInfinity);

        public static float RoundToZero(float op1) => MathF.Round(op1, MidpointRounding.ToZero);

        public static int ConvertDoubleToInt32(double op1) => (int)Math.Clamp(op1, long.MinValue, long.MaxValue);

        public static int ConvertToInt32(float op1) => (int)Math.Clamp(op1, int.MinValue, int.MaxValue);
        
        public static long ConvertToInt64(double op1) => (long)Math.Clamp(op1, long.MinValue, long.MaxValue);

        public static long ConvertFloatToInt64(float op1) => (long)Math.Clamp(op1, int.MinValue, int.MaxValue);

        public static uint ConvertDoubleToUInt32(double op1) => (uint)Math.Clamp(op1, ulong.MinValue, ulong.MaxValue);

        public static uint ConvertToUInt32(float op1) => (uint)Math.Clamp(op1, uint.MinValue, uint.MaxValue);

        public static ulong ConvertToUInt64(double op1) => (ulong)Math.Clamp(op1, ulong.MinValue, ulong.MaxValue);

        public static ulong ConvertFloatToUInt64(float op1) => (ulong)Math.Clamp(op1, uint.MinValue, uint.MaxValue);

        public static Int32 ConvertToInt32RoundAwayFromZero(float op1) => ConvertToInt32(RoundAwayFromZero(op1));

        public static Int32 ConvertToInt32RoundToEven(float op1) => ConvertToInt32(RoundToNearest(op1));

        public static Int32 ConvertToInt32RoundToNegativeInfinity(float op1) => ConvertToInt32(RoundToNegativeInfinity(op1));

        public static Int32 ConvertToInt32RoundToPositiveInfinity(float op1) => ConvertToInt32(RoundToPositiveInfinity(op1));

        public static Int32 ConvertToInt32RoundToZero(float op1) => ConvertToInt32(RoundToZero(op1));

        public static Int64 ConvertToInt64RoundAwayFromZero(double op1) => ConvertToInt64(RoundAwayFromZero(op1));

        public static Int64 ConvertToInt64RoundToEven(double op1) => ConvertToInt64(RoundToNearest(op1));

        public static Int64 ConvertToInt64RoundToNegativeInfinity(double op1) => ConvertToInt64(RoundToNegativeInfinity(op1));

        public static Int64 ConvertToInt64RoundToPositiveInfinity(double op1) => ConvertToInt64(RoundToPositiveInfinity(op1));

        public static Int64 ConvertToInt64RoundToZero(double op1) => ConvertToInt64(RoundToZero(op1));

        public static UInt32 ConvertToUInt32RoundAwayFromZero(float op1) => ConvertToUInt32(RoundAwayFromZero(op1));

        public static UInt32 ConvertToUInt32RoundToEven(float op1) => ConvertToUInt32(RoundToNearest(op1));

        public static UInt32 ConvertToUInt32RoundToNegativeInfinity(float op1) => ConvertToUInt32(RoundToNegativeInfinity(op1));

        public static UInt32 ConvertToUInt32RoundToPositiveInfinity(float op1) => ConvertToUInt32(RoundToPositiveInfinity(op1));

        public static UInt32 ConvertToUInt32RoundToZero(float op1) => ConvertToUInt32(RoundToZero(op1));

        public static UInt64 ConvertToUInt64RoundAwayFromZero(double op1) => ConvertToUInt64(RoundAwayFromZero(op1));

        public static UInt64 ConvertToUInt64RoundToEven(double op1) => ConvertToUInt64(RoundToNearest(op1));

        public static UInt64 ConvertToUInt64RoundToNegativeInfinity(double op1) => ConvertToUInt64(RoundToNegativeInfinity(op1));

        public static UInt64 ConvertToUInt64RoundToPositiveInfinity(double op1) => ConvertToUInt64(RoundToPositiveInfinity(op1));

        public static UInt64 ConvertToUInt64RoundToZero(double op1) => ConvertToUInt64(RoundToZero(op1));

        public static float ConvertToSingle(int op1) => op1;

        public static float ConvertToSingle(uint op1) => op1;

        public static float ConvertToSingle(double op1) => (float)op1;

        public static float ConvertToSingleUpper(float[] op1, double[] op2, int i) => i < op1.Length ? op1[i] : ConvertToSingle(op2[i - op1.Length]);

        public static double ConvertToDouble(float op1) => op1;

        public static double ConvertToDoubleUpper(float[] op1, int i) => ConvertToDouble(op1[i + op1.Length / 2]);

        public static double ConvertToDouble(long op1) => op1;

        public static double ConvertToDouble(ulong op1) => op1;

        public static short ReverseElement8(short val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(short)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (short)result;
        }

        public static ushort ReverseElement8(ushort val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ushort)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ushort)result;
        }

        public static int ReverseElement8(int val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(int)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (int)result;
        }

        public static uint ReverseElement8(uint val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(uint)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (uint)result;
        }

        public static long ReverseElement8(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement8(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static int ReverseElement16(int val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(int)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (int)result;
        }

        public static uint ReverseElement16(uint val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(uint)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (uint)result;
        }

        public static long ReverseElement16(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement16(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static long ReverseElement32(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 32;
            ulong elementMask = ((1UL << 32) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (32 * i)) & elementMask;
                ulong reversedElement = element << (32 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement32(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 32;
            ulong elementMask = ((1UL << 32) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (32 * i)) & elementMask;
                ulong reversedElement = element << (32 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static uint DotProduct(uint op1, byte[] op2, int s, byte[] op3, int t)
        {
            uint result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (uint)((uint)op2[s + i] * (uint)op3[t + i]);
            }

            return result;
        }

        public static int DotProduct(int op1, sbyte[] op2, int s, sbyte[] op3, int t)
        {
            int result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (int)((int)op2[s + i] * (int)op3[t + i]);
            }

            return result;
        }

        public static ulong DotProduct(ulong op1, ushort[] op2, int s, ushort[] op3, int t)
        {
            ulong result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (ulong)((ulong)op2[s + i] * (ulong)op3[t + i]);
            }

            return result;
        }

        public static long DotProduct(long op1, short[] op2, int s, short[] op3, int t)
        {
            long result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (long)((long)op2[s + i] * (long)op3[t + i]);
            }

            return result;
        }

        public static int WhileLessThanMask(int op1, int op2)
        {
            return (op1 < op2) ? 1 : 0;
        }

        public static uint WhileLessThanMask(uint op1, uint op2)
        {
            return (uint)((op1 < op2) ? 1 : 0);
        }

        public static long WhileLessThanMask(long op1, long op2)
        {
            return (op1 < op2) ? 1 : 0;
        }

        public static ulong WhileLessThanMask(ulong op1, ulong op2)
        {
            return (ulong)((op1 < op2) ? 1 : 0);
        }

        public static int WhileLessThanOrEqualMask(int op1, int op2)
        {
            return (op1 <= op2) ? 1 : 0;
        }

        public static uint WhileLessThanOrEqualMask(uint op1, uint op2)
        {
            return (uint)((op1 <= op2) ? 1 : 0);
        }

        public static long WhileLessThanOrEqualMask(long op1, long op2)
        {
            return (op1 <= op2) ? 1 : 0;
        }

        public static ulong WhileLessThanOrEqualMask(ulong op1, ulong op2)
        {
            return (ulong)((op1 <= op2) ? 1 : 0);
        }

        public static ulong MaskBothSet(byte[] op1, byte[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(sbyte[] op1, sbyte[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(short[] op1, short[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(ushort[] op1, ushort[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(int[] op1, int[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(uint[] op1, uint[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(long[] op1, long[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(ulong[] op1, ulong[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i]==1 && op2[i]==1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(float[] op1, float[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((BitConverter.SingleToInt32Bits(op1[i]) == 1 && BitConverter.SingleToInt32Bits(op2[i]) == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(double[] op1, double[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((BitConverter.DoubleToInt64Bits(op1[i]) == 1 && BitConverter.DoubleToInt64Bits(op2[i]) == 1) ? 1 : 0);
            }
            return acc;
        }

        public static byte getMaskByte()
        {
            return (byte)(TestLibrary.Generator.GetByte()%(byte)2);
        }

        public static sbyte getMaskSByte()
        {
            return (sbyte)(TestLibrary.Generator.GetSByte()%(sbyte)2);
        }

        public static short getMaskInt16()
        {
            return (short)(TestLibrary.Generator.GetInt16()%(short)2);
        }

        public static ushort getMaskUInt16()
        {
            return (ushort)(TestLibrary.Generator.GetUInt16()%(ushort)2);
        }

        public static int getMaskInt32()
        {
            return (int)(TestLibrary.Generator.GetInt32()%(int)2);
        }

        public static uint getMaskUInt32()
        {
            return (uint)(TestLibrary.Generator.GetUInt32()%(uint)2);
        }

        public static long getMaskInt64()
        {
            return (long)(TestLibrary.Generator.GetInt64()%(long)2);
        }

        public static ulong getMaskUInt64()
        {
            return (ulong)(TestLibrary.Generator.GetUInt64()%(ulong)2);
        }

        public static float getMaskSingle()
        {
            return (float)(BitConverter.Int32BitsToSingle(TestLibrary.Generator.GetInt32()%(int)2));
        }

        public static double getMaskDouble()
        {
            return (double)(BitConverter.Int64BitsToDouble(TestLibrary.Generator.GetInt64()%(long)2));
        }

        public static int MaskNumberOfElementsVector(int elems, SveMaskPattern pattern)
        {

            switch(pattern)
            {
                // Returns elems, as this is always a power of 2.
                case SveMaskPattern.LargestPowerOf2:
                    return elems;

                // Returns N if N elements can fit in the vector. Otherwise 0.
                case SveMaskPattern.VectorCount1:
                    return elems >= 1 ? 1 : 0;
                case SveMaskPattern.VectorCount2:
                    return elems >= 2 ? 2 : 0;
                case SveMaskPattern.VectorCount3:
                    return elems >= 3 ? 3 : 0;
                case SveMaskPattern.VectorCount4:
                    return elems >= 4 ? 4 : 0;
                case SveMaskPattern.VectorCount5:
                    return elems >= 5 ? 5 : 0;
                case SveMaskPattern.VectorCount6:
                    return elems >= 6 ? 6 : 0;
                case SveMaskPattern.VectorCount7:
                    return elems >= 7 ? 7 : 0;
                case SveMaskPattern.VectorCount8:
                    return elems >= 8 ? 8 : 0;
                case SveMaskPattern.VectorCount16:
                    return elems >= 16 ? 16 : 0;
                case SveMaskPattern.VectorCount32:
                    return elems >= 32 ? 32 : 0;
                case SveMaskPattern.VectorCount64:
                    return elems >= 64 ? 64 : 0;
                case SveMaskPattern.VectorCount128:
                    return elems >= 128 ? 128 : 0;
                case SveMaskPattern.VectorCount256:
                    return elems >= 256 ? 256 : 0;

                // Number of elems rounded down to nearest multiple of 4
                case SveMaskPattern.LargestMultipleOf4:
                    return elems - (elems % 4);

                // Number of elems rounded down to nearest multiple of 3
                case SveMaskPattern.LargestMultipleOf3:
                    return elems - (elems % 3);

                case SveMaskPattern.All:
                default:
                    return elems;
            }
        }

        public static int NumberOfElementsInVectorInt8(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<byte>>() / sizeof(byte), pattern);
        }
        
        public static int NumberOfElementsInVectorInt16(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<short>>() / sizeof(short), pattern);
        }

        public static int NumberOfElementsInVectorInt32(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<int>>() / sizeof(int), pattern);
        }

        public static int NumberOfElementsInVectorInt64(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<long>>() / sizeof(long), pattern);
        }

        public static int NumberOfActiveElementsInMask(sbyte[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(short[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(int[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(long[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(byte[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(ushort[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(uint[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(ulong[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static double[] Compact(double[] op1, double[] op2)
        {
            double[] result = new double[op1.Length];
            Array.Fill<double>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static int[] Compact(int[] op1, int[] op2)
        {
            int[] result = new int[op1.Length];
            Array.Fill<int>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static long[] Compact(long[] op1, long[] op2)
        {
            long[] result = new long[op1.Length];
            Array.Fill<long>(result, 0, 0, op1.Length);

            long i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static float[] Compact(float[] op1, float[] op2)
        {
            float[] result = new float[op1.Length];
            Array.Fill<float>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static uint[] Compact(uint[] op1, uint[] op2)
        {
            uint[] result = new uint[op1.Length];
            Array.Fill<uint>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static ulong[] Compact(ulong[] op1, ulong[] op2)
        {
            ulong[] result = new ulong[op1.Length];
            Array.Fill<ulong>(result, 0, 0, op1.Length);

            ulong i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static int LoadInt32FromByteArray(byte[] array, int offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset+i];
            }
            return ret;
        }

        public static int LoadInt32FromByteArray(byte[] array, uint offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset+i];
            }
            return ret;
        }

        public static long LoadInt64FromByteArray(byte[] array, long offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset+i];
            }
            return ret;
        }

        public static long LoadInt64FromByteArray(byte[] array, ulong offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset+(ulong)i];
            }
            return ret;
        }

        public static uint LoadUInt32FromByteArray(byte[] array, int offset)
        {
            uint ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset+i];
            }
            return ret;
        }

        public static uint LoadUInt32FromByteArray(byte[] array, uint offset)
        {
            uint ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset+i];
            }
            return ret;
        }

        public static ulong LoadUInt64FromByteArray(byte[] array, long offset)
        {
            ulong ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (ulong)array[offset+i];
            }
            return ret;
        }

        public static ulong LoadUInt64FromByteArray(byte[] array, ulong offset)
        {
            ulong ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (ulong)array[offset+(ulong)i];
            }
            return ret;
        }

        public static float LoadSingleFromByteArray(byte[] array, int offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset+i];
            }
            return BitConverter.Int32BitsToSingle(ret);
        }

        public static float LoadSingleFromByteArray(byte[] array, uint offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset+i];
            }
            return BitConverter.Int32BitsToSingle(ret);
        }

        public static double LoadDoubleFromByteArray(byte[] array, long offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset+i];
            }
            return BitConverter.Int64BitsToDouble(ret);
        }

        public static double LoadDoubleFromByteArray(byte[] array, ulong offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset+(ulong)i];
            }
            return BitConverter.Int64BitsToDouble(ret);
        }

        public static Byte Splice(Byte[] first, Byte[] second, Byte[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static double Splice(double[] first, double[] second, double[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (Double.IsNaN(maskArray[i]) || maskArray[i] > 0.0d)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static float Splice(float[] first, float[] second, float[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0.0f)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static Int16 Splice(Int16[] first, Int16[] second, Int16[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static Int32 Splice(Int32[] first, Int32[] second, Int32[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static Int64 Splice(Int64[] first, Int64[] second, Int64[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static SByte Splice(SByte[] first, SByte[] second, SByte[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static UInt16 Splice(UInt16[] first, UInt16[] second, UInt16[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static UInt32 Splice(UInt32[] first, UInt32[] second, UInt32[] maskArray, Int32 index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static ulong Splice(ulong[] first, ulong[] second, ulong[] maskArray, int index)
        {
            int start = -1;
            int end   = -1;

            for(var i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] != 0)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            var rangeSize = end - start + 1;
            return (index < rangeSize) ? first[start + index] : second[index - rangeSize];
        }

        public static T LastActive<T>(T[] mask, T[] x) where T : IBinaryInteger<T>
        {
            for (var i = mask.Length - 1; i >= 0; i--)
            {
                if (mask[i] != T.Zero)
                {
                    return x[i];
                }
            }
            return T.Zero;
        }

        public static T[] CreateBreakAfterMask<T>(T[] mask, T[] op) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isBreakSet = false;
            for (var i = 0; i < count; i++)
            {
                var isElementActive = op[i] != T.Zero;
                if (mask[i] != T.Zero)
                {
                    if (isBreakSet)
                    {
                        result[i] = T.Zero;
                    }
                    else
                    {
                        result[i] = T.One;
                    }
                    isBreakSet = isBreakSet || isElementActive;
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakAfterPropagateMask<T>(T[] mask, T[] op1, T[] op2) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isLastActive = LastActive(mask, op1) != T.Zero;
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    if (isLastActive)
                    {
                        result[i] = T.One;
                    }
                    else
                    {
                        result[i] = T.Zero;
                    }
                    isLastActive = isLastActive && (op2[i] == T.Zero);
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakBeforeMask<T>(T[] mask, T[] op) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isBreakSet = false;
            for (var i = 0; i < count; i++)
            {
                var isElementActive = op[i] != T.Zero;
                if (mask[i] != T.Zero)
                {
                    isBreakSet = isBreakSet || isElementActive;
                    if (isBreakSet)
                    {
                        result[i] = T.Zero;
                    }
                    else
                    {
                        result[i] = T.One;
                    }
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakBeforePropagateMask<T>(T[] mask, T[] op1, T[] op2) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isLastActive = LastActive(mask, op1) != T.Zero;
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    isLastActive = isLastActive && (op2[i] == T.Zero);
                    if (isLastActive)
                    {
                        result[i] = T.One;
                    }
                    else
                    {
                        result[i] = T.Zero;
                    }
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }
    }
}
