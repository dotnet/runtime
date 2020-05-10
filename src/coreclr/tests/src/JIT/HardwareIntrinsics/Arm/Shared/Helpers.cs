// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is auto-generated from template file Helpers.tt
// In order to make changes to this file, please update Helpers.tt
// and run the following command from Developer Command Prompt for Visual Studio
//   "%DevEnvDir%\TextTransform.exe" .\Helpers.tt

using System;
using System.Linq;
using System.Numerics;

namespace JIT.HardwareIntrinsics.Arm
{
    static class Helpers
    {
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

        public static float Sqrt(float value) => MathF.Sqrt(value);

        public static double Sqrt(double value) => Math.Sqrt(value);

        public static byte AbsoluteDifference(sbyte op1, sbyte op2) => op1 < op2 ? (byte)(op2 - op1) : (byte)(op1 - op2);

        public static sbyte AbsoluteDifferenceAdd(sbyte op1, sbyte op2, sbyte op3) => (sbyte)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifference(short op1, short op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static short AbsoluteDifferenceAdd(short op1, short op2, short op3) => (short)(op1 + AbsoluteDifference(op2, op3));

        public static uint AbsoluteDifference(int op1, int op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static int AbsoluteDifferenceAdd(int op1, int op2, int op3) => (int)(op1 + AbsoluteDifference(op2, op3));

        public static byte AbsoluteDifference(byte op1, byte op2) => op1 < op2 ? (byte)(op2 - op1) : (byte)(op1 - op2);

        public static byte AbsoluteDifferenceAdd(byte op1, byte op2, byte op3) => (byte)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifference(ushort op1, ushort op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceAdd(ushort op1, ushort op2, ushort op3) => (ushort)(op1 + AbsoluteDifference(op2, op3));

        public static uint AbsoluteDifference(uint op1, uint op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceAdd(uint op1, uint op2, uint op3) => (uint)(op1 + AbsoluteDifference(op2, op3));

        public static ushort AbsoluteDifferenceWidening(sbyte op1, sbyte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AbsoluteDifferenceWideningAndAdd(short op1, sbyte op2, sbyte op3) => (short)(op1 + (short)AbsoluteDifferenceWidening(op2, op3));

        public static short AbsoluteDifferenceWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short AddPairwiseWidening(sbyte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static short AddPairwiseWideningAndAdd(short[] op1, sbyte[] op2, int i) => (short)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static sbyte HighNarrow(short op1, bool round)
        {
            ushort roundConst = 0;
            if (round)
            {
                roundConst = (ushort)1 << (8 * sizeof(sbyte) - 1);
            }
            return (sbyte)(((ushort)op1 + roundConst) >> (8 * sizeof(sbyte)));
        }

        public static sbyte AddHighNarrow(short op1, short op2) => HighNarrow((short)(op1 + op2), round: false);

        public static sbyte AddHighNarrowUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static sbyte AddRoundedHighNarrow(short op1, short op2) => HighNarrow((short)(op1 + op2), round: true);

        public static short AddRoundedHighNarrowUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddWidening(sbyte op1, sbyte op2) => (short)((short)op1 + (short)op2);

        public static short AddWidening(short op1, sbyte op2) => (short)(op1 + op2);

        public static short AddWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AddWideningUpper(short[] op1, sbyte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static sbyte FusedAddHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2) >> 1);

        public static sbyte FusedAddRoundedHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2 + 1) >> 1);

        public static sbyte FusedSubtractHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 - (short)op2) >> 1);

        public static short MultiplyWidening(sbyte op1, sbyte op2) => (short)((short)op1 * (short)op2);

        public static short MultiplyWideningAndAdd(short op1, sbyte op2, sbyte op3) => (short)(op1 + MultiplyWidening(op2, op3));

        public static short MultiplyWideningAndSubtract(short op1, sbyte op2, sbyte op3) => (short)(op1 - MultiplyWidening(op2, op3));

        public static short MultiplyWideningUpper(sbyte[] op1, sbyte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short MultiplyWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short MultiplyWideningUpperAndSubtract(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static sbyte SubtractHighNarrow(short op1, short op2) => HighNarrow((short)(op1 - op2), round: false);

        public static short SubtractHighNarrowUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static sbyte SubtractRoundedHighNarrow(short op1, short op2) => HighNarrow((short)(op1 - op2), round: true);

        public static short SubtractRoundedHighNarrowUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static short SubtractWidening(sbyte op1, sbyte op2) => (short)((short)op1 - (short)op2);

        public static short SubtractWidening(short op1, sbyte op2) => (short)(op1 - op2);

        public static short SubtractWideningUpper(sbyte[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short SubtractWideningUpper(short[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint AbsoluteDifferenceWidening(short op1, short op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(short[] op1, short[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AbsoluteDifferenceWideningAndAdd(int op1, short op2, short op3) => (int)(op1 + (int)AbsoluteDifferenceWidening(op2, op3));

        public static int AbsoluteDifferenceWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int AddPairwiseWidening(short[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static int AddPairwiseWideningAndAdd(int[] op1, short[] op2, int i) => (int)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static short HighNarrow(int op1, bool round)
        {
            uint roundConst = 0;
            if (round)
            {
                roundConst = (uint)1 << (8 * sizeof(short) - 1);
            }
            return (short)(((uint)op1 + roundConst) >> (8 * sizeof(short)));
        }

        public static short AddHighNarrow(int op1, int op2) => HighNarrow((int)(op1 + op2), round: false);

        public static short AddHighNarrowUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddRoundedHighNarrow(int op1, int op2) => HighNarrow((int)(op1 + op2), round: true);

        public static int AddRoundedHighNarrowUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddWidening(short op1, short op2) => (int)((int)op1 + (int)op2);

        public static int AddWidening(int op1, short op2) => (int)(op1 + op2);

        public static int AddWideningUpper(short[] op1, short[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AddWideningUpper(int[] op1, short[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static short FusedAddHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2) >> 1);

        public static short FusedAddRoundedHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2 + 1) >> 1);

        public static short FusedSubtractHalving(short op1, short op2) => (short)((uint)((int)op1 - (int)op2) >> 1);

        public static int MultiplyWidening(short op1, short op2) => (int)((int)op1 * (int)op2);

        public static int MultiplyWideningAndAdd(int op1, short op2, short op3) => (int)(op1 + MultiplyWidening(op2, op3));

        public static int MultiplyWideningAndSubtract(int op1, short op2, short op3) => (int)(op1 - MultiplyWidening(op2, op3));

        public static int MultiplyWideningUpper(short[] op1, short[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int MultiplyWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int MultiplyWideningUpperAndSubtract(int[] op1, short[] op2, short[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short SubtractHighNarrow(int op1, int op2) => HighNarrow((int)(op1 - op2), round: false);

        public static int SubtractHighNarrowUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static short SubtractRoundedHighNarrow(int op1, int op2) => HighNarrow((int)(op1 - op2), round: true);

        public static int SubtractRoundedHighNarrowUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractWidening(short op1, short op2) => (int)((int)op1 - (int)op2);

        public static int SubtractWidening(int op1, short op2) => (int)(op1 - op2);

        public static int SubtractWideningUpper(short[] op1, short[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int SubtractWideningUpper(int[] op1, short[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ulong AbsoluteDifferenceWidening(int op1, int op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(int[] op1, int[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AbsoluteDifferenceWideningAndAdd(long op1, int op2, int op3) => (long)(op1 + (long)AbsoluteDifferenceWidening(op2, op3));

        public static long AbsoluteDifferenceWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long AddPairwiseWidening(int[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static long AddPairwiseWideningAndAdd(long[] op1, int[] op2, int i) => (long)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static int HighNarrow(long op1, bool round)
        {
            ulong roundConst = 0;
            if (round)
            {
                roundConst = (ulong)1 << (8 * sizeof(int) - 1);
            }
            return (int)(((ulong)op1 + roundConst) >> (8 * sizeof(int)));
        }

        public static int AddHighNarrow(long op1, long op2) => HighNarrow((long)(op1 + op2), round: false);

        public static int AddHighNarrowUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddRoundedHighNarrow(long op1, long op2) => HighNarrow((long)(op1 + op2), round: true);

        public static long AddRoundedHighNarrowUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static long AddWidening(int op1, int op2) => (long)((long)op1 + (long)op2);

        public static long AddWidening(long op1, int op2) => (long)(op1 + op2);

        public static long AddWideningUpper(int[] op1, int[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AddWideningUpper(long[] op1, int[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static int FusedAddHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2) >> 1);

        public static int FusedAddRoundedHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2 + 1) >> 1);

        public static int FusedSubtractHalving(int op1, int op2) => (int)((ulong)((long)op1 - (long)op2) >> 1);

        public static long MultiplyWidening(int op1, int op2) => (long)((long)op1 * (long)op2);

        public static long MultiplyWideningAndAdd(long op1, int op2, int op3) => (long)(op1 + MultiplyWidening(op2, op3));

        public static long MultiplyWideningAndSubtract(long op1, int op2, int op3) => (long)(op1 - MultiplyWidening(op2, op3));

        public static long MultiplyWideningUpper(int[] op1, int[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long MultiplyWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyWideningUpperAndSubtract(long[] op1, int[] op2, int[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int SubtractHighNarrow(long op1, long op2) => HighNarrow((long)(op1 - op2), round: false);

        public static long SubtractHighNarrowUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractRoundedHighNarrow(long op1, long op2) => HighNarrow((long)(op1 - op2), round: true);

        public static long SubtractRoundedHighNarrowUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static long SubtractWidening(int op1, int op2) => (long)((long)op1 - (long)op2);

        public static long SubtractWidening(long op1, int op2) => (long)(op1 - op2);

        public static long SubtractWideningUpper(int[] op1, int[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long SubtractWideningUpper(long[] op1, int[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort AbsoluteDifferenceWidening(byte op1, byte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(byte[] op1, byte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AbsoluteDifferenceWideningAndAdd(ushort op1, byte op2, byte op3) => (ushort)(op1 + (ushort)AbsoluteDifferenceWidening(op2, op3));

        public static ushort AbsoluteDifferenceWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort AddPairwiseWidening(byte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ushort AddPairwiseWideningAndAdd(ushort[] op1, byte[] op2, int i) => (ushort)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static byte HighNarrow(ushort op1, bool round)
        {
            ushort roundConst = 0;
            if (round)
            {
                roundConst = (ushort)1 << (8 * sizeof(byte) - 1);
            }
            return (byte)(((ushort)op1 + roundConst) >> (8 * sizeof(byte)));
        }

        public static byte AddHighNarrow(ushort op1, ushort op2) => HighNarrow((ushort)(op1 + op2), round: false);

        public static byte AddHighNarrowUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static byte AddRoundedHighNarrow(ushort op1, ushort op2) => HighNarrow((ushort)(op1 + op2), round: true);

        public static ushort AddRoundedHighNarrowUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddWidening(byte op1, byte op2) => (ushort)((ushort)op1 + (ushort)op2);

        public static ushort AddWidening(ushort op1, byte op2) => (ushort)(op1 + op2);

        public static ushort AddWideningUpper(byte[] op1, byte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AddWideningUpper(ushort[] op1, byte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static byte FusedAddHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2) >> 1);

        public static byte FusedAddRoundedHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2 + 1) >> 1);

        public static byte FusedSubtractHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 - (ushort)op2) >> 1);

        public static ushort MultiplyWidening(byte op1, byte op2) => (ushort)((ushort)op1 * (ushort)op2);

        public static ushort MultiplyWideningAndAdd(ushort op1, byte op2, byte op3) => (ushort)(op1 + MultiplyWidening(op2, op3));

        public static ushort MultiplyWideningAndSubtract(ushort op1, byte op2, byte op3) => (ushort)(op1 - MultiplyWidening(op2, op3));

        public static ushort MultiplyWideningUpper(byte[] op1, byte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort MultiplyWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort MultiplyWideningUpperAndSubtract(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static byte SubtractHighNarrow(ushort op1, ushort op2) => HighNarrow((ushort)(op1 - op2), round: false);

        public static ushort SubtractHighNarrowUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static byte SubtractRoundedHighNarrow(ushort op1, ushort op2) => HighNarrow((ushort)(op1 - op2), round: true);

        public static ushort SubtractRoundedHighNarrowUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractWidening(byte op1, byte op2) => (ushort)((ushort)op1 - (ushort)op2);

        public static ushort SubtractWidening(ushort op1, byte op2) => (ushort)(op1 - op2);

        public static ushort SubtractWideningUpper(byte[] op1, byte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort SubtractWideningUpper(ushort[] op1, byte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint AbsoluteDifferenceWidening(ushort op1, ushort op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(ushort[] op1, ushort[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AbsoluteDifferenceWideningAndAdd(uint op1, ushort op2, ushort op3) => (uint)(op1 + (uint)AbsoluteDifferenceWidening(op2, op3));

        public static uint AbsoluteDifferenceWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint AddPairwiseWidening(ushort[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static uint AddPairwiseWideningAndAdd(uint[] op1, ushort[] op2, int i) => (uint)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static ushort HighNarrow(uint op1, bool round)
        {
            uint roundConst = 0;
            if (round)
            {
                roundConst = (uint)1 << (8 * sizeof(ushort) - 1);
            }
            return (ushort)(((uint)op1 + roundConst) >> (8 * sizeof(ushort)));
        }

        public static ushort AddHighNarrow(uint op1, uint op2) => HighNarrow((uint)(op1 + op2), round: false);

        public static ushort AddHighNarrowUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddRoundedHighNarrow(uint op1, uint op2) => HighNarrow((uint)(op1 + op2), round: true);

        public static uint AddRoundedHighNarrowUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddWidening(ushort op1, ushort op2) => (uint)((uint)op1 + (uint)op2);

        public static uint AddWidening(uint op1, ushort op2) => (uint)(op1 + op2);

        public static uint AddWideningUpper(ushort[] op1, ushort[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AddWideningUpper(uint[] op1, ushort[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort FusedAddHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2) >> 1);

        public static ushort FusedAddRoundedHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2 + 1) >> 1);

        public static ushort FusedSubtractHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 - (uint)op2) >> 1);

        public static uint MultiplyWidening(ushort op1, ushort op2) => (uint)((uint)op1 * (uint)op2);

        public static uint MultiplyWideningAndAdd(uint op1, ushort op2, ushort op3) => (uint)(op1 + MultiplyWidening(op2, op3));

        public static uint MultiplyWideningAndSubtract(uint op1, ushort op2, ushort op3) => (uint)(op1 - MultiplyWidening(op2, op3));

        public static uint MultiplyWideningUpper(ushort[] op1, ushort[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint MultiplyWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint MultiplyWideningUpperAndSubtract(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort SubtractHighNarrow(uint op1, uint op2) => HighNarrow((uint)(op1 - op2), round: false);

        public static uint SubtractHighNarrowUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractRoundedHighNarrow(uint op1, uint op2) => HighNarrow((uint)(op1 - op2), round: true);

        public static uint SubtractRoundedHighNarrowUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractWidening(ushort op1, ushort op2) => (uint)((uint)op1 - (uint)op2);

        public static uint SubtractWidening(uint op1, ushort op2) => (uint)(op1 - op2);

        public static uint SubtractWideningUpper(ushort[] op1, ushort[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint SubtractWideningUpper(uint[] op1, ushort[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ulong AbsoluteDifferenceWidening(uint op1, uint op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(uint[] op1, uint[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AbsoluteDifferenceWideningAndAdd(ulong op1, uint op2, uint op3) => (ulong)(op1 + (ulong)AbsoluteDifferenceWidening(op2, op3));

        public static ulong AbsoluteDifferenceWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong AddPairwiseWidening(uint[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ulong AddPairwiseWideningAndAdd(ulong[] op1, uint[] op2, int i) => (ulong)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        private static uint HighNarrow(ulong op1, bool round)
        {
            ulong roundConst = 0;
            if (round)
            {
                roundConst = (ulong)1 << (8 * sizeof(uint) - 1);
            }
            return (uint)(((ulong)op1 + roundConst) >> (8 * sizeof(uint)));
        }

        public static uint AddHighNarrow(ulong op1, ulong op2) => HighNarrow((ulong)(op1 + op2), round: false);

        public static uint AddHighNarrowUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddRoundedHighNarrow(ulong op1, ulong op2) => HighNarrow((ulong)(op1 + op2), round: true);

        public static ulong AddRoundedHighNarrowUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong AddWidening(uint op1, uint op2) => (ulong)((ulong)op1 + (ulong)op2);

        public static ulong AddWidening(ulong op1, uint op2) => (ulong)(op1 + op2);

        public static ulong AddWideningUpper(uint[] op1, uint[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AddWideningUpper(ulong[] op1, uint[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint FusedAddHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2) >> 1);

        public static uint FusedAddRoundedHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2 + 1) >> 1);

        public static uint FusedSubtractHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 - (ulong)op2) >> 1);

        public static ulong MultiplyWidening(uint op1, uint op2) => (ulong)((ulong)op1 * (ulong)op2);

        public static ulong MultiplyWideningAndAdd(ulong op1, uint op2, uint op3) => (ulong)(op1 + MultiplyWidening(op2, op3));

        public static ulong MultiplyWideningAndSubtract(ulong op1, uint op2, uint op3) => (ulong)(op1 - MultiplyWidening(op2, op3));

        public static ulong MultiplyWideningUpper(uint[] op1, uint[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong MultiplyWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong MultiplyWideningUpperAndSubtract(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint SubtractHighNarrow(ulong op1, ulong op2) => HighNarrow((ulong)(op1 - op2), round: false);

        public static ulong SubtractHighNarrowUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractRoundedHighNarrow(ulong op1, ulong op2) => HighNarrow((ulong)(op1 - op2), round: true);

        public static ulong SubtractRoundedHighNarrowUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrow(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong SubtractWidening(uint op1, uint op2) => (ulong)((ulong)op1 - (ulong)op2);

        public static ulong SubtractWidening(ulong op1, uint op2) => (ulong)(op1 - op2);

        public static ulong SubtractWideningUpper(uint[] op1, uint[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong SubtractWideningUpper(ulong[] op1, uint[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        private static bool SatQ(BigInteger i, out sbyte result)
        {
            bool saturated = false;

            if (i > sbyte.MaxValue)
            {
                result = sbyte.MaxValue;
                saturated = true;
            }
            else if (i < sbyte.MinValue)
            {
                result = sbyte.MinValue;
                saturated = true;
            }
            else
            {
                result = (sbyte)i;
            }

            return saturated;
        }

        public static sbyte AddSaturate(sbyte op1, sbyte op2)
        {
            sbyte result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static sbyte SubtractSaturate(sbyte op1, sbyte op2)
        {
            sbyte result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out byte result)
        {
            bool saturated = false;

            if (i > byte.MaxValue)
            {
                result = byte.MaxValue;
                saturated = true;
            }
            else if (i < byte.MinValue)
            {
                result = byte.MinValue;
                saturated = true;
            }
            else
            {
                result = (byte)i;
            }

            return saturated;
        }

        public static byte AddSaturate(byte op1, byte op2)
        {
            byte result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static byte SubtractSaturate(byte op1, byte op2)
        {
            byte result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out short result)
        {
            bool saturated = false;

            if (i > short.MaxValue)
            {
                result = short.MaxValue;
                saturated = true;
            }
            else if (i < short.MinValue)
            {
                result = short.MinValue;
                saturated = true;
            }
            else
            {
                result = (short)i;
            }

            return saturated;
        }

        public static short AddSaturate(short op1, short op2)
        {
            short result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static short SubtractSaturate(short op1, short op2)
        {
            short result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out ushort result)
        {
            bool saturated = false;

            if (i > ushort.MaxValue)
            {
                result = ushort.MaxValue;
                saturated = true;
            }
            else if (i < ushort.MinValue)
            {
                result = ushort.MinValue;
                saturated = true;
            }
            else
            {
                result = (ushort)i;
            }

            return saturated;
        }

        public static ushort AddSaturate(ushort op1, ushort op2)
        {
            ushort result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static ushort SubtractSaturate(ushort op1, ushort op2)
        {
            ushort result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out int result)
        {
            bool saturated = false;

            if (i > int.MaxValue)
            {
                result = int.MaxValue;
                saturated = true;
            }
            else if (i < int.MinValue)
            {
                result = int.MinValue;
                saturated = true;
            }
            else
            {
                result = (int)i;
            }

            return saturated;
        }

        public static int AddSaturate(int op1, int op2)
        {
            int result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static int SubtractSaturate(int op1, int op2)
        {
            int result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out uint result)
        {
            bool saturated = false;

            if (i > uint.MaxValue)
            {
                result = uint.MaxValue;
                saturated = true;
            }
            else if (i < uint.MinValue)
            {
                result = uint.MinValue;
                saturated = true;
            }
            else
            {
                result = (uint)i;
            }

            return saturated;
        }

        public static uint AddSaturate(uint op1, uint op2)
        {
            uint result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static uint SubtractSaturate(uint op1, uint op2)
        {
            uint result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out long result)
        {
            bool saturated = false;

            if (i > long.MaxValue)
            {
                result = long.MaxValue;
                saturated = true;
            }
            else if (i < long.MinValue)
            {
                result = long.MinValue;
                saturated = true;
            }
            else
            {
                result = (long)i;
            }

            return saturated;
        }

        public static long AddSaturate(long op1, long op2)
        {
            long result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static long SubtractSaturate(long op1, long op2)
        {
            long result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
        }

        private static bool SatQ(BigInteger i, out ulong result)
        {
            bool saturated = false;

            if (i > ulong.MaxValue)
            {
                result = ulong.MaxValue;
                saturated = true;
            }
            else if (i < ulong.MinValue)
            {
                result = ulong.MinValue;
                saturated = true;
            }
            else
            {
                result = (ulong)i;
            }

            return saturated;
        }

        public static ulong AddSaturate(ulong op1, ulong op2)
        {
            ulong result;
            SatQ(new BigInteger(op1) + new BigInteger(op2), out result);
            return result;
        }

        public static ulong SubtractSaturate(ulong op1, ulong op2)
        {
            ulong result;
            SatQ(new BigInteger(op1) - new BigInteger(op2), out result);
            return result;
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

        public static float FPRecipStepFused(float op1, float op2) => FusedMultiplySubtract(2, op1, op2);

        public static float FPRSqrtStepFused(float op1, float op2) => FusedMultiplySubtract(3, op1, op2) / 2;

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

        public static double FPRecipStepFused(double op1, double op2) => FusedMultiplySubtract(2, op1, op2);

        public static double FPRSqrtStepFused(double op1, double op2) => FusedMultiplySubtract(3, op1, op2) / 2;

        private static uint RecipEstimate(uint a)
        {
            a = a * 2 + 1;

            uint b = (1 << 19) / a;
            uint r = (b + 1) / 2;

            return r;
        }

        private static uint RecipSqrtEstimate(uint a)
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

        private static uint ExtractBits(uint val, byte msbPos, byte lsbPos)
        {
            uint andMask = 0;

            for (byte pos = lsbPos; pos <= msbPos; pos++)
            {
                andMask |= (uint)1 << pos;
            }

            return (val & andMask) >> lsbPos;
        }

        public static uint UnsignedRecipEstimate(uint op1)
        {
            uint result;

            if ((op1 & (1 << 31)) == 0)
            {
                result = ~0U;
            }
            else
            {
                uint estimate = RecipEstimate(ExtractBits(op1, 31, 23));
                result = ExtractBits(estimate, 8, 0) << 31;
            }

            return result;
        }

        public static uint UnsignedRSqrtEstimate(uint op1)
        {
            uint result;

            if ((op1 & (3 << 30)) == 0)
            {
                result = ~0U;
            }
            else
            {
                uint estimate = RecipSqrtEstimate(ExtractBits(op1, 31, 23));
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

        public static sbyte MaxAcross(sbyte[] op1) => Reduce(Max, op1);

        public static sbyte MinAcross(sbyte[] op1) => Reduce(Min, op1);

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

        public static byte MaxAcross(byte[] op1) => Reduce(Max, op1);

        public static byte MinAcross(byte[] op1) => Reduce(Min, op1);

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

        public static short MaxAcross(short[] op1) => Reduce(Max, op1);

        public static short MinAcross(short[] op1) => Reduce(Min, op1);

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

        public static ushort MaxAcross(ushort[] op1) => Reduce(Max, op1);

        public static ushort MinAcross(ushort[] op1) => Reduce(Min, op1);

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

        public static int MaxAcross(int[] op1) => Reduce(Max, op1);

        public static int MinAcross(int[] op1) => Reduce(Min, op1);

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

        public static uint MaxAcross(uint[] op1) => Reduce(Max, op1);

        public static uint MinAcross(uint[] op1) => Reduce(Min, op1);

        private static uint Reduce(Func<uint, uint, uint> reduceOp, uint[] op1)
        {
            uint acc = op1[0];

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

        public static float MaxNumberAcross(float[] op1) => Reduce(MaxNumber, op1);

        public static float MinNumberAcross(float[] op1) => Reduce(MinNumber, op1);

        private static ulong PolynomialMult(sbyte op1, sbyte op2)
        {
            ulong result = 0;
            ulong extendedOp2 = (ulong)op2;

            for (int i = 0; i < 8 * sizeof(sbyte); i++)
            {
                if ((op1 & (1 << i)) != 0)
                {
                    result ^= (extendedOp2 << i);
                }
            }

            return result;
        }

        public static sbyte PolynomialMultiply(sbyte op1, sbyte op2) => (sbyte)PolynomialMult(op1, op2);

        private static ulong PolynomialMult(byte op1, byte op2)
        {
            ulong result = 0;
            ulong extendedOp2 = (ulong)op2;

            for (int i = 0; i < 8 * sizeof(byte); i++)
            {
                if ((op1 & (1 << i)) != 0)
                {
                    result ^= (extendedOp2 << i);
                }
            }

            return result;
        }

        public static byte PolynomialMultiply(byte op1, byte op2) => (byte)PolynomialMult(op1, op2);

        public static bool ExtractAndNarrowHigh(int i, sbyte[] left,
                                                short[] right,
                                                sbyte[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (sbyte)right[i - left.Length] != result[i];
        }
        public static bool ExtractAndNarrowHigh(int i, short[] left,
                                                int[] right,
                                                short[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (short)right[i - left.Length] != result[i];
        }
        public static bool ExtractAndNarrowHigh(int i, int[] left,
                                                long[] right,
                                                int[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (int)right[i - left.Length] != result[i];
        }
        public static bool ExtractAndNarrowHigh(int i, byte[] left,
                                                ushort[] right,
                                                byte[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (byte)right[i - left.Length] != result[i];
        }
        public static bool ExtractAndNarrowHigh(int i, ushort[] left,
                                                uint[] right,
                                                ushort[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (ushort)right[i - left.Length] != result[i];
        }
        public static bool ExtractAndNarrowHigh(int i, uint[] left,
                                                ulong[] right,
                                                uint[] result)
        {
            if (i < left.Length)
              return left[i] != result[i];
            else
              return (uint)right[i - left.Length] != result[i];
        }
        public static sbyte ExtractVector(sbyte[] op1, sbyte[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static sbyte Insert(sbyte[] op1, int op2, sbyte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static byte ExtractVector(byte[] op1, byte[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static byte Insert(byte[] op1, int op2, byte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static short ExtractVector(short[] op1, short[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static short Insert(short[] op1, int op2, short op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ushort ExtractVector(ushort[] op1, ushort[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static ushort Insert(ushort[] op1, int op2, ushort op3, int i) => (op2 != i) ? op1[i] : op3;

        public static int ExtractVector(int[] op1, int[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static int Insert(int[] op1, int op2, int op3, int i) => (op2 != i) ? op1[i] : op3;

        public static uint ExtractVector(uint[] op1, uint[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static uint Insert(uint[] op1, int op2, uint op3, int i) => (op2 != i) ? op1[i] : op3;

        public static long ExtractVector(long[] op1, long[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static long Insert(long[] op1, int op2, long op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ulong ExtractVector(ulong[] op1, ulong[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static ulong Insert(ulong[] op1, int op2, ulong op3, int i) => (op2 != i) ? op1[i] : op3;

        public static float ExtractVector(float[] op1, float[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static float Insert(float[] op1, int op2, float op3, int i) => (op2 != i) ? op1[i] : op3;

        public static double ExtractVector(double[] op1, double[] op2, int op3, int i) => (op3 + i < op1.Length) ? op1[op3 + i] : op2[op3 + i - op1.Length];

        public static double Insert(double[] op1, int op2, double op3, int i) => (op2 != i) ? op1[i] : op3;

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

    }
}
