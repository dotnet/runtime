// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class DebugStreamReader
{
    private readonly uint _size;

    private byte[] _buffer;
    private int _currentNibble;

    public uint NextByteIndex => (uint)((_currentNibble + 1) / 2);

    public DebugStreamReader(Target target, TargetPointer address, uint size)
    {
        _size = size;

        _buffer = new byte[size];
        target.ReadBuffer(address, _buffer);
    }

    public uint ReadEncodedU32()
    {
        uint result = 0;
        uint nibsRead = 0;
        Nibble nib;
        do
        {
            if (nibsRead > 11)
            {
                throw new InvalidOperationException("Corrupt nibble stream, too many nibbles read for encoded uint32.");
            }

            nib = ReadNibble();
            result = (result << 3) + nib.Data;

            nibsRead++;
        } while (!nib.IsEndOfWord);
        return result;
    }

    private Nibble ReadNibble()
    {
        if (_currentNibble / 2 >= _size) throw new InvalidOperationException("No more nibbles to read.");

        byte b = _buffer[_currentNibble / 2];
        Nibble nib;
        if (_currentNibble % 2 == 0)
        {
            // Read the low nibble first
            nib = new Nibble((byte)(b & 0xF));
        }
        else
        {
            // Read the high nibble after the low nibble has been read
            nib = new Nibble((byte)((b >> 4) & 0xF));
        }
        _currentNibble++;
        return nib;
    }

    private readonly struct Nibble(byte data)
    {
        private readonly byte _data = data;

        public readonly byte Data => (byte)(_data & 0x7);
        public readonly bool IsEndOfWord => (_data & 0x8) == 0;
    }
}
