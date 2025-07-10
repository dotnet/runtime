// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class NativeArray
{
    private const uint BLOCK_SIZE = 16;

    private readonly NativeReader _reader;
    private readonly uint _count;
    private readonly uint _entryIndexSize;
    private readonly uint _baseOffset;

    public NativeArray(NativeReader reader, uint offset)
    {
        _reader = reader;

        _baseOffset = _reader.DecodeUnsigned(offset, out uint val);
        _count = val >> 2;
        _entryIndexSize = val & 3;
    }

    public bool TryGetAt(uint index, out uint value)
    {
        value = 0;
        if (index >= _count)
            return false;

        uint offset = _entryIndexSize switch
        {
            0 => _reader.ReadUInt8(_baseOffset + (index / BLOCK_SIZE)),
            1 => _reader.ReadUInt16(_baseOffset + 2 * (index / BLOCK_SIZE)),
            _ => _reader.ReadUInt32(_baseOffset + 4 * (index / BLOCK_SIZE)),
        };
        offset += _baseOffset;

        for (uint bit = BLOCK_SIZE >> 1; bit > 0; bit >>= 1)
        {
            uint offset2 = _reader.DecodeUnsigned(offset, out uint val);

            if ((index & bit) != 0)
            {
                if ((val & 2) != 0)
                {
                    offset += val >> 2;
                    continue;
                }
            }
            else
            {
                if ((val & 1) != 0)
                {
                    offset = offset2;
                    continue;
                }
            }

            // Not found
            if ((val & 3) == 0)
            {
                // Matching special leaf node?
                if ((val >> 2) == (index & (BLOCK_SIZE - 1)))
                {
                    offset = offset2;
                    break;
                }
            }
            return false;
        }

        value = offset;
        return true;
    }
}
