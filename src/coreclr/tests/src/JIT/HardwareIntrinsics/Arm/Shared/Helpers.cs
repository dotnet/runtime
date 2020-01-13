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

        public static byte AbsoluteDifference(sbyte left, sbyte right) => (byte)Math.Abs((long)left - (long)right);
        public static ushort AbsoluteDifference(short left, short right) => (ushort)Math.Abs((long)left - (long)right);
        public static uint AbsoluteDifference(int left, int right) => (uint)Math.Abs((long)left - (long)right);
        public static byte AbsoluteDifference(byte left, byte right) => (byte)Math.Abs((long)left - (long)right);
        public static ushort AbsoluteDifference(ushort left, ushort right) => (ushort)Math.Abs((long)left - (long)right);
        public static uint AbsoluteDifference(uint left, uint right) => (uint)Math.Abs((long)left - (long)right);
        public static float AbsoluteDifference(float left, float right) => Math.Abs(left - right);
        public static double AbsoluteDifference(double left, double right) => Math.Abs(left - right);
    }
}
