// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This code is copied almost verbatim from the same-named file in CoreRT with mechanical changes to make it build outside of CoreLib.
//

namespace System
{
    internal static class DecimalDecCalc
    {
        private static uint D32DivMod1E9(uint hi32, ref uint lo32)
        {
            ulong n = (ulong)hi32 << 32 | lo32;
            lo32 = (uint)(n / 1000000000);
            return (uint)(n % 1000000000);
        }

        // Performs the equivalent of:
        //
        //    uint modulo = value % 1e9;
        //    value = value / 1e9;
        //    return modulo;
        //
        internal static uint DecDivMod1E9(ref MutableDecimal value)
        {
            return D32DivMod1E9(D32DivMod1E9(D32DivMod1E9(0, ref value.High), ref value.Mid), ref value.Low);
        }

        internal static void DecAddInt32(ref MutableDecimal value, uint i)
        {
            if (D32AddCarry(ref value.Low, i))
            {
                if (D32AddCarry(ref value.Mid, 1))
                {
                    D32AddCarry(ref value.High, 1);
                }
            }
        }

        private static bool D32AddCarry(ref uint value, uint i)
        {
            uint v = value;
            uint sum = v + i;
            value = sum;
            return (sum < v) || (sum < i);
        }

        internal static void DecMul10(ref MutableDecimal value)
        {
            MutableDecimal d = value;
            DecShiftLeft(ref value);
            DecShiftLeft(ref value);
            DecAdd(ref value, d);
            DecShiftLeft(ref value);
        }

        private static void DecShiftLeft(ref MutableDecimal value)
        {
            uint c0 = (value.Low & 0x80000000) != 0 ? 1u : 0u;
            uint c1 = (value.Mid & 0x80000000) != 0 ? 1u : 0u;
            value.Low = value.Low << 1;
            value.Mid = (value.Mid << 1) | c0;
            value.High = (value.High << 1) | c1;
        }

        private static void DecAdd(ref MutableDecimal value, MutableDecimal d)
        {
            if (D32AddCarry(ref value.Low, d.Low))
            {
                if (D32AddCarry(ref value.Mid, 1))
                {
                    D32AddCarry(ref value.High, 1);
                }
            }

            if (D32AddCarry(ref value.Mid, d.Mid))
            {
                D32AddCarry(ref value.High, 1);
            }

            D32AddCarry(ref value.High, d.High);
        }
    }
}
