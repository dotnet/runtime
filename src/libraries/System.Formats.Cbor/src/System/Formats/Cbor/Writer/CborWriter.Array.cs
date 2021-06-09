// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 4 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Writes the start of a definite or indefinite-length array (major type 4).</summary>
        /// <param name="definiteLength">The length of the definite-length array, or <see langword="null" /> for an indefinite-length array.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="definiteLength" /> parameter cannot be negative.</exception>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        /// <remarks>
        /// In canonical conformance modes, the writer will reject indefinite-length writes unless
        /// the <see cref="ConvertIndefiniteLengthEncodings" /> flag is enabled.
        /// </remarks>
        public void WriteStartArray(int? definiteLength)
        {
            if (definiteLength is null)
            {
                WriteStartArrayIndefiniteLength();
            }
            else
            {
                WriteStartArrayDefiniteLength(definiteLength.Value);
            }
        }

        /// <summary>Writes the end of an array (major type 4).</summary>
        /// <exception cref="InvalidOperationException">The written data is not accepted under the current conformance mode.
        /// -or-
        /// The definite-length array anticipates more data items.</exception>
        public void WriteEndArray()
        {
            PopDataItem(CborMajorType.Array);
            AdvanceDataItemCounters();
        }

        private void WriteStartArrayDefiniteLength(int definiteLength)
        {
            if (definiteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength));
            }

            WriteUnsignedInteger(CborMajorType.Array, (ulong)definiteLength);
            PushDataItem(CborMajorType.Array, definiteLength);
        }

        private void WriteStartArrayIndefiniteLength()
        {
            if (!ConvertIndefiniteLengthEncodings && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Array, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Array, definiteLength: null);
        }

        // perform an in-place conversion of an indefinite-length encoding into an equivalent definite-length
        private void PatchIndefiniteLengthCollection(CborMajorType majorType, int count)
        {
            Debug.Assert(majorType == CborMajorType.Array || majorType == CborMajorType.Map);

            int currentOffset = _offset;
            int bytesToShift = GetIntegerEncodingLength((ulong)count) - 1;

            if (bytesToShift > 0)
            {
                // length encoding requires more than 1 byte, need to shift encoded elements to the right
                EnsureWriteCapacity(bytesToShift);

                ReadOnlySpan<byte> elementEncoding = _buffer.AsSpan(_frameOffset, currentOffset - _frameOffset);
                Span<byte> target = _buffer.AsSpan(_frameOffset + bytesToShift, currentOffset - _frameOffset);
                elementEncoding.CopyTo(target);
            }

            // rewind to the start of the collection and write a new initial byte
            _offset = _frameOffset - 1;
            WriteUnsignedInteger(majorType, (ulong)count);
            _offset = currentOffset + bytesToShift;
        }
    }
}
