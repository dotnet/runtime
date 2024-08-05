// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 7 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Writes a half-precision floating point number (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        private void WriteHalf(ushort value)
        {
            EnsureWriteCapacity(1 + sizeof(ushort));
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional16BitData));
            if (HalfHelpers.HalfIsNaN(value) && !CborConformanceModeHelpers.RequiresPreservingFloatPrecision(ConformanceMode))
            {
                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_offset), PositiveQNaNBitsHalf);
            }
            else
            {
                CborHelpers.WriteHalfBigEndian(_buffer.AsSpan(_offset), value);
            }
            _offset += sizeof(ushort);
            AdvanceDataItemCounters();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryConvertSingleToHalf(float value, out ushort result)
        {
            result = HalfHelpers.FloatToHalf(value);
            return float.IsNaN(value) || CborHelpers.SingleToInt32Bits(HalfHelpers.HalfToFloat(result)) == CborHelpers.SingleToInt32Bits(value);
        }
    }
}
