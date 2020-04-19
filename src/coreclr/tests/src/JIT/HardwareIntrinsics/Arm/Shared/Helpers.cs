// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is auto-generated from template file Helpers.tt
// In order to make changes to this file, please update Helpers.tt
// and run the following command from Developer Command Prompt for Visual Studio
//   "%DevEnvDir%\TextTransform.exe" .\Helpers.tt

using System;

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
    }
}
