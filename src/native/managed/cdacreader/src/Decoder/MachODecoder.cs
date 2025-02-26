// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class MachODecoder : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly ulong _baseAddress;

    private bool _is64Bit;
    private GnuHashTable? _gnuHashTable;

    private bool _disposedValue;

    public bool IsValid => _gnuHashTable is not null;

    /// <summary>
    /// Create MachODecoder with stream beginning at the base address of the module.
    /// </summary>
    public MachODecoder(Stream stream, ulong baseAddress)
    {
        _reader = new(stream, Encoding.UTF8);
        _baseAddress = baseAddress;

        Initialize();
    }

    private void Initialize()
    {
        _reader.BaseStream.Seek(0, SeekOrigin.Begin);

        uint elfMagic = _reader.ReadUInt32();
        if (elfMagic != 0x464C457F) // 0x7F followed by "ELF"
            return;

        _is64Bit = _reader.ReadByte() != 1;

        if (_is64Bit)
        {
            Initialize<ulong>();
        }
        else
        {
            Initialize<uint>();
        }
    }

    public bool TryGetRelativeSymbolAddress(string symbol, out ulong address)
    {
        address = 0;

        if (_gnuHashTable is not GnuHashTable hashTable)
            return false;

        return hashTable.TryLookupRelativeSymbolAddress(symbol, out address);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _gnuHashTable?.Dispose();
                _reader.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
    }
}
