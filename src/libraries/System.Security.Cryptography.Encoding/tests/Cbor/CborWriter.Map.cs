// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Text;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        public void WriteStartMap(int definiteLength)
        {
            if (definiteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength), "must be non-negative integer.");
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            AdvanceDataItemCounters();
            PushDataItem(CborMajorType.Map, 2 * (uint)definiteLength);
        }

        public void WriteEndMap()
        {
            if (!_isEvenNumberOfDataItemsWritten)
            {
                throw new InvalidOperationException("CBOR Map types require an even number of key/value combinations");
            }

            bool isDefiniteLengthMap = _remainingDataItems.HasValue;

            PopDataItem(CborMajorType.Map);

            if (!isDefiniteLengthMap)
            {
                // append break byte
                EnsureWriteCapacity(1);
                _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
            }
        }

        public void WriteStartMapIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            AdvanceDataItemCounters();
            PushDataItem(CborMajorType.Map, expectedNestedItems: null);
        }
    }
}
