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
            ulong arrayLength = checked((ulong)ReadUnsignedInteger(header, out int additionalBytes));
            AdvanceBuffer(1 + additionalBytes);
            _remainingDataItems--;

            PushDataItem(CborMajorType.Array, arrayLength);
            return arrayLength;
        }

        public void ReadEndArray()
        {
            PopDataItem(expectedType: CborMajorType.Array);
        }
    }
}
