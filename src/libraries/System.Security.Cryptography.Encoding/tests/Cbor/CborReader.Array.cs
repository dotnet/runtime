// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        public int? ReadStartArray()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Array);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException("Indefinite-length items are not supported under the current conformance level.");
                }

                AdvanceBuffer(1);
                PushDataItem(CborMajorType.Array, null);
                return null;
            }
            else
            {
                ulong arrayLength = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);

                if (arrayLength > (ulong)_buffer.Length)
                {
                    throw new FormatException("Insufficient buffer size for declared definite length in CBOR data item.");
                }

                AdvanceBuffer(1 + additionalBytes);
                PushDataItem(CborMajorType.Array, (int)arrayLength);
                return (int)arrayLength;
            }
        }

        public void ReadEndArray()
        {
            if (_remainingDataItems == null)
            {
                CborInitialByte value = PeekInitialByte();

                if (value.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
                {
                    throw new InvalidOperationException("Not at end of indefinite-length array.");
                }

                PopDataItem(expectedType: CborMajorType.Array);
                AdvanceDataItemCounters();
                AdvanceBuffer(1);
            }
            else
            {
                PopDataItem(expectedType: CborMajorType.Array);
                AdvanceDataItemCounters();
            }
        }
    }
}
