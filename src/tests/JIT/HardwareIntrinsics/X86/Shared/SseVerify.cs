// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.X86
{

    public static class SseVerify
    {
        public static bool AddSaturate(byte x, byte y, byte z)
        {
            int value = x + y;
            value = Math.Max(value, 0);
            value = Math.Min(value, byte.MaxValue);
            return value != z;
        }

        public static bool AddSaturate(sbyte x, sbyte y, sbyte z)
        {
            int value = x + y;
            value = Math.Max(value, sbyte.MinValue);
            value = Math.Min(value, sbyte.MaxValue);
            return value != z;
        }

        public static bool AddSaturate(ushort x, ushort y, ushort z)
        {
            int value = x + y;
            value = Math.Max(value, 0);
            value = Math.Min(value, ushort.MaxValue);
            return value != z;
        }

        public static bool AddSaturate(short x, short y, short z)
        {
            int value = x + y;
            value = Math.Max(value, short.MinValue);
            value = Math.Min(value, short.MaxValue);
            return value != z;
        }

        public static ushort SumAbsoluteDifferences(byte[] left, byte[] right, int i)
        {
            int b = (i / 4) * 8;

            if ((i & 3) != 0)
            {
                return 0;
            }

            ushort result = 0;

            for (int n = 0; n < 8; n++)
            {
                int tmp = int.Abs(left[b + n] - right[b + n]);
                result += (ushort)(tmp);
            }

            return result;
        }

        public static bool SubtractSaturate(byte x, byte y, byte z)
        {
            int value = (int)x - y;
            value = Math.Max(value, 0);
            value = Math.Min(value, byte.MaxValue);
            return (byte) value != z;
        }

        public static bool SubtractSaturate(sbyte x, sbyte y, sbyte z)
        {
            int value = (int)x - y;
            value = Math.Max(value, sbyte.MinValue);
            value = Math.Min(value, sbyte.MaxValue);
            return (sbyte) value != z;
        }

        public static bool SubtractSaturate(ushort x, ushort y, ushort z)
        {
            int value = (int)x - y;
            value = Math.Max(value, 0);
            value = Math.Min(value, ushort.MaxValue);
            return (ushort) value != z;
        }

        public static bool SubtractSaturate(short x, short y, short z)
        {
            int value = (int)x - y;
            value = Math.Max(value, short.MinValue);
            value = Math.Min(value, short.MaxValue);
            return (short) value != z;
        }
    }
}
