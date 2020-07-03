// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Encodings.Web
{
    internal static class AdvSimdHelper
    {
        private static readonly Vector128<byte> s_bitMask128 = BitConverter.IsLittleEndian ?
                                                Vector128.Create(0x80402010_08040201).AsByte() :
                                                Vector128.Create(0x01020408_10204080).AsByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort MoveMask(Vector128<byte> value)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            Vector128<byte> mostSignficantBitMask = Vector128.Create((byte)0x80);
            Vector128<byte> mostSignificantBitIsSet = AdvSimd.CompareEqual(AdvSimd.And(value, mostSignficantBitMask), mostSignficantBitMask);

            // pairwise-add until all flags have moved to the first two bytes of the vector
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, s_bitMask128);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            return extractedBits.AsUInt16().ToScalar();
        }
    }
}
#endif
