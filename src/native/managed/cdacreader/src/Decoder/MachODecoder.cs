// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

/// <summary>
/// Only supports 64-bit MachO binaries.
/// </summary>
internal sealed class MachODecoder : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly ulong _baseAddress;

    private bool _isLittleEndian;

    private bool _disposedValue;

    public bool IsValid { get; init; }

    /// <summary>
    /// Create MachODecoder with stream beginning at the base address of the module.
    /// </summary>
    public MachODecoder(Stream stream, ulong baseAddress)
    {
        _reader = new(stream, Encoding.UTF8);
        _baseAddress = baseAddress;

        IsValid = Initialize();
    }

    private bool Initialize()
    {
        _reader.BaseStream.Seek(0, SeekOrigin.Begin);
        Mach64Header header = new(_reader);

        // if the magic number is not correct, this is not a MachO file
        if (header.magic is not Mach64Header.LE_MAGIC and not Mach64Header.BE_MAGIC)
            return false;

        _isLittleEndian = header.magic == Mach64Header.LE_MAGIC;
        return true;
    }

    public static bool TryGetRelativeSymbolAddress(string symbol, out ulong address)
    {
        address = 0;

        Console.WriteLine(symbol);

        return false;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
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
