// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        public ulong? ReadStartArray()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Array);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                PushDataItem(CborMajorType.Array, null);
                AdvanceBuffer(1);
                return null;
            }
            else
            {
                ulong arrayLength = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);
                PushDataItem(CborMajorType.Array, arrayLength);
                AdvanceBuffer(1 + additionalBytes);
                return arrayLength;
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
