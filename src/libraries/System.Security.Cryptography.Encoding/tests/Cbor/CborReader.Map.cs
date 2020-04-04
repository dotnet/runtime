// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        public ulong? ReadStartMap()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Map);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                AdvanceBuffer(1);
                AdvanceDataItemCounters();
                PushDataItem(CborMajorType.Map, null);
                return null;
            }
            else
            {
                ulong mapSize = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);

                if (mapSize > long.MaxValue)
                {
                    throw new OverflowException("Read CBOR map field count exceeds supported size.");
                }

                AdvanceBuffer(1 + additionalBytes);
                AdvanceDataItemCounters();
                PushDataItem(CborMajorType.Map, 2 * mapSize);
                return mapSize;
            }
        }

        public void ReadEndMap()
        {
            if (_remainingDataItems == null)
            {
                CborInitialByte value = PeekInitialByte();

                if (value.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
                {
                    throw new InvalidOperationException("Not at end of indefinite-length map.");
                }

                if (!_isEvenNumberOfDataItemsRead)
                {
                    throw new FormatException("CBOR Map types require an even number of key/value combinations");
                }

                PopDataItem(expectedType: CborMajorType.Map);
                AdvanceBuffer(1);
            }
            else
            {
                PopDataItem(expectedType: CborMajorType.Map);
            }
        }
    }
}
