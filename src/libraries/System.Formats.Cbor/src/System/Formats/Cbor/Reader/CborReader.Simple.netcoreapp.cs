// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>Reads the next data item as a half-precision floating point number (major type 7).</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException"><para>The next data item does not have the correct major type.</para>
        /// <para>-or-</para>
        /// <para>The next simple value is not a floating-point number encoding.</para>
        /// <para>-or-</para>
        /// <para>The encoded value is a double-precision float.</para></exception>
        /// <exception cref="CborContentException"><para>The next value has an invalid CBOR encoding.</para>
        /// <para>-or-</para>
        /// <para>There was an unexpected end of CBOR encoding data.</para>
        /// <para>-or-</para>
        /// <para>The next value uses a CBOR encoding that is not valid under the current conformance mode.</para></exception>
        public Half ReadHalf()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            Half result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 1 + sizeof(short));
                    result = BinaryPrimitives.ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + sizeof(short));
                    AdvanceDataItemCounters();
                    return result;
                case CborAdditionalInfo.Additional32BitData:
                case CborAdditionalInfo.Additional64BitData:
                    throw new InvalidOperationException(SR.Cbor_Reader_ReadingAsLowerPrecision);

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);
            }
        }
    }
}
