// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    public partial struct Decimal
    {
        internal static uint DecDivMod1E9(ref decimal value)
        {
            return D32DivMod1E9(D32DivMod1E9(D32DivMod1E9(0,
                                                          ref Unsafe.As<int, uint>(ref value.hi)),
                                             ref Unsafe.As<int, uint>(ref value.mid)),
                                ref Unsafe.As<int, uint>(ref value.lo));

            uint D32DivMod1E9(uint hi32, ref uint lo32)
            {
                ulong n = (ulong)hi32 << 32 | lo32;
                lo32 = (uint)(n / 1000000000);
                return (uint)(n % 1000000000);
            }
        }
    }
}
