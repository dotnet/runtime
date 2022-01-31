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
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteHalf(Half value)
        {
            EnsureWriteCapacity(1 + sizeof(short));
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional16BitData));
            BinaryPrimitives.WriteHalfBigEndian(_buffer.AsSpan(_offset), value);
            _offset += sizeof(short);
            AdvanceDataItemCounters();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryConvertSingleToHalf(float value, out Half result)
        {
            result = (Half)value;
            return BitConverter.SingleToInt32Bits((float)result) == BitConverter.SingleToInt32Bits(value);
        }
    }
}
