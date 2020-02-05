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

        public static byte AbsoluteDifference(sbyte left, sbyte right) => (byte)Math.Abs((long)left - (long)right);

        public static ushort AbsoluteDifference(short left, short right) => (ushort)Math.Abs((long)left - (long)right);

        public static uint AbsoluteDifference(int left, int right) => (uint)Math.Abs((long)left - (long)right);

        public static byte AbsoluteDifference(byte left, byte right) => (byte)Math.Abs((long)left - (long)right);

        public static ushort AbsoluteDifference(ushort left, ushort right) => (ushort)Math.Abs((long)left - (long)right);

        public static uint AbsoluteDifference(uint left, uint right) => (uint)Math.Abs((long)left - (long)right);

        public static float AbsoluteDifference(float left, float right) => Math.Abs(left - right);

        public static double AbsoluteDifference(double left, double right) => Math.Abs(left - right);

        public static sbyte Add(sbyte op1, sbyte op2) => (sbyte)(op1 + op2);

        public static sbyte Max(sbyte op1, sbyte op2) => Math.Max(op1, op2);

        public static sbyte Min(sbyte op1, sbyte op2) => Math.Min(op1, op2);

        public static sbyte Multiply(sbyte op1, sbyte op2) => (sbyte)(op1 * op2);

        public static sbyte Subtract(sbyte op1, sbyte op2) => (sbyte)(op1 - op2);

        public static byte Add(byte op1, byte op2) => (byte)(op1 + op2);

        public static byte Max(byte op1, byte op2) => Math.Max(op1, op2);

        public static byte Min(byte op1, byte op2) => Math.Min(op1, op2);

        public static byte Multiply(byte op1, byte op2) => (byte)(op1 * op2);

        public static byte Subtract(byte op1, byte op2) => (byte)(op1 - op2);

        public static short Add(short op1, short op2) => (short)(op1 + op2);

        public static short Max(short op1, short op2) => Math.Max(op1, op2);

        public static short Min(short op1, short op2) => Math.Min(op1, op2);

        public static short Multiply(short op1, short op2) => (short)(op1 * op2);

        public static short Subtract(short op1, short op2) => (short)(op1 - op2);

        public static ushort Add(ushort op1, ushort op2) => (ushort)(op1 + op2);

        public static ushort Max(ushort op1, ushort op2) => Math.Max(op1, op2);

        public static ushort Min(ushort op1, ushort op2) => Math.Min(op1, op2);

        public static ushort Multiply(ushort op1, ushort op2) => (ushort)(op1 * op2);

        public static ushort Subtract(ushort op1, ushort op2) => (ushort)(op1 - op2);

        public static int Add(int op1, int op2) => (int)(op1 + op2);

        public static int Max(int op1, int op2) => Math.Max(op1, op2);

        public static int Min(int op1, int op2) => Math.Min(op1, op2);

        public static int Multiply(int op1, int op2) => (int)(op1 * op2);

        public static int Subtract(int op1, int op2) => (int)(op1 - op2);

        public static uint Add(uint op1, uint op2) => (uint)(op1 + op2);

        public static uint Max(uint op1, uint op2) => Math.Max(op1, op2);

        public static uint Min(uint op1, uint op2) => Math.Min(op1, op2);

        public static uint Multiply(uint op1, uint op2) => (uint)(op1 * op2);

        public static uint Subtract(uint op1, uint op2) => (uint)(op1 - op2);

        public static long Add(long op1, long op2) => (long)(op1 + op2);

        public static long Max(long op1, long op2) => Math.Max(op1, op2);

        public static long Min(long op1, long op2) => Math.Min(op1, op2);

        public static long Multiply(long op1, long op2) => (long)(op1 * op2);

        public static long Subtract(long op1, long op2) => (long)(op1 - op2);

        public static ulong Add(ulong op1, ulong op2) => (ulong)(op1 + op2);

        public static ulong Max(ulong op1, ulong op2) => Math.Max(op1, op2);

        public static ulong Min(ulong op1, ulong op2) => Math.Min(op1, op2);

        public static ulong Multiply(ulong op1, ulong op2) => (ulong)(op1 * op2);

        public static ulong Subtract(ulong op1, ulong op2) => (ulong)(op1 - op2);

        public static float Add(float op1, float op2) => (float)(op1 + op2);

        public static float Max(float op1, float op2) => Math.Max(op1, op2);

        public static float Min(float op1, float op2) => Math.Min(op1, op2);

        public static float Multiply(float op1, float op2) => (float)(op1 * op2);

        public static float Subtract(float op1, float op2) => (float)(op1 - op2);

        public static double Add(double op1, double op2) => (double)(op1 + op2);

        public static double Max(double op1, double op2) => Math.Max(op1, op2);

        public static double Min(double op1, double op2) => Math.Min(op1, op2);

        public static double Multiply(double op1, double op2) => (double)(op1 * op2);

        public static double Subtract(double op1, double op2) => (double)(op1 - op2);

        public static sbyte Negate(sbyte op1) => (sbyte)(-op1);

        public static short Negate(short op1) => (short)(-op1);

        public static int Negate(int op1) => (int)(-op1);

        public static long Negate(long op1) => (long)(-op1);

        public static float Negate(float op1) => (float)(-op1);

        public static double Negate(double op1) => (double)(-op1);

    }
}
