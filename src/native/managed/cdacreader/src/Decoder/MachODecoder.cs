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
    private Mach64SymTabCommand _symTabCommand;
    private Mach64DySymTabCommand _dySymTabCommand;
    private List<Mach64SegmentCommand> _segments = [];
    private ulong _loadBias;

    private List<NList64> _symbols = [];
    private long _strTabOffset;

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

        // load commands
        long commandOffset = _reader.BaseStream.Position;
        for (int i = 0; i < header.nCmds; i++)
        {
            _reader.BaseStream.Seek(commandOffset, SeekOrigin.Begin);
            Mach64LoadCommand command = new(_reader);
            _reader.BaseStream.Seek(commandOffset, SeekOrigin.Begin);
            switch (command.cmd)
            {
                case (uint)Mach64LoadCommand.Type.LC_SYMTAB:
                {
                    _symTabCommand = new(_reader);
                    Console.WriteLine($"LC_SYMTAB: {command.cmdSize} {commandOffset} {_symTabCommand.nsyms} {_symTabCommand.strsize}");
                    break;
                }
                case (uint)Mach64LoadCommand.Type.LC_DYNSYM:
                {
                    _dySymTabCommand = new(_reader);
                    Console.WriteLine($"LC_DYNSYM: {command.cmdSize} {commandOffset} {_dySymTabCommand.iextdefsym} {_dySymTabCommand.nextdefsym}");
                    break;
                }
                case (uint)Mach64LoadCommand.Type.LC_SEGMENT_64:
                {
                    Mach64SegmentCommand segment = new(_reader);
                    if (segment.segname == Mach64SegmentCommand.SEG_TEXT)
                    {
                        _loadBias = segment.vmaddr;
                    }
                    _segments.Add(segment);
                    Console.WriteLine($"Segment: {segment.segname} {segment.vmaddr} {segment.vmsize} {segment.fileoff} {segment.filesize}");
                    break;
                }
            }

            commandOffset += command.cmdSize;
        }

        // read symbol table
        long symbolStreamOffset = GetStreamOffsetFromFileOffset(_symTabCommand.symoff);
        _reader.BaseStream.Seek(symbolStreamOffset, SeekOrigin.Begin);
        for (int i = 0; i < _symTabCommand.nsyms; i++)
        {
            NList64 symbol = new(_reader);
            _symbols.Add(symbol);
            Console.WriteLine($"Symbol: {symbol.n_strx} {symbol.n_type} {symbol.n_sect} {symbol.n_desc} {symbol.n_value}");
        }
        _strTabOffset = GetStreamOffsetFromFileOffset(_symTabCommand.stroff);

        return true;
    }

    private long GetStreamOffsetFromFileOffset(uint offset)
    {
        foreach (Mach64SegmentCommand segment in _segments)
        {
            if (offset >= segment.fileoff && offset < segment.fileoff + segment.filesize)
            {
                return (long)(offset - segment.fileoff + segment.vmaddr - _loadBias);
            }
        }
        return (long)(offset - _loadBias);
    }

    private string GetSymbolName(uint index)
    {
        if (index >= _symbols.Count)
            return string.Empty;
        NList64 symbol = _symbols[(int)index];
        _reader.BaseStream.Seek(_strTabOffset + symbol.n_strx, SeekOrigin.Begin);

        // read 0-terminated string
        string symbolName = _reader.ReadZString();

        // trim leading underscore to match Linux externs
        if (symbolName.Length > 0 && symbolName[0] == '_')
            symbolName = symbolName.Substring(1);
        return symbolName;
    }

    public bool TryLookupSymbol(uint start, uint nSymbols, string symbolName, out ulong address)
    {
        address = 0;
        for (uint i = 0; i < nSymbols; i++)
        {
            string name = GetSymbolName(start + i);
            Console.WriteLine(name);
            if (name == symbolName)
            {
                NList64 symbol = _symbols[(int)(start + i)];
                address = symbol.n_value - _loadBias;
                return true;
            }
        }
        if (_symbols.Count == 0)
            Console.WriteLine(symbolName);
        return false;
    }

    public bool TryGetRelativeSymbolAddress(string symbol, out ulong address)
    {
        Console.WriteLine($"{_dySymTabCommand.iextdefsym} {_dySymTabCommand.nextdefsym}");

        return TryLookupSymbol(_dySymTabCommand.iextdefsym, _dySymTabCommand.nextdefsym, symbol, out address);
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
