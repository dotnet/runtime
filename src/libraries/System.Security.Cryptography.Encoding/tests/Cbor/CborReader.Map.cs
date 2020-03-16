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
            ulong arrayLength = checked((ulong)ReadUnsignedInteger(header, out int additionalBytes));
            AdvanceBuffer(1 + additionalBytes);
            _remainingDataItems--;

            if (arrayLength > long.MaxValue)
            {
                throw new OverflowException("Read CBOR map field count exceeds supported size.");
            }

            PushDataItem(CborMajorType.Map, 2 * arrayLength);
            return arrayLength;
        }

        public void ReadEndMap()
        {
            PopDataItem(expectedType: CborMajorType.Map);
        }
    }
}
