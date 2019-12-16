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
    }
}
