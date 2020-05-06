// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        public void WriteStartArray(int definiteLength)
        {
            if (definiteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength), "must be non-negative integer.");
            }

            WriteUnsignedInteger(CborMajorType.Array, (ulong)definiteLength);
            PushDataItem(CborMajorType.Array, definiteLength);
        }

        public void WriteStartArray()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Array, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Array, definiteLength: null);
        }

        public void WriteEndArray()
        {
            PopDataItem(CborMajorType.Array);
            AdvanceDataItemCounters();
        }

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
