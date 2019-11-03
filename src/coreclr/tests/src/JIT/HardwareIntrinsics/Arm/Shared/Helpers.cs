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

    }
}
