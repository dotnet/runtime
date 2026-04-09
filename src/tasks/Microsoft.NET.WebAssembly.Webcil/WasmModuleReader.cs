// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.WebAssembly.Webcil;

internal class WasmModuleReader : IDisposable
{
    public enum Section : byte
    {
        // order matters: enum values must match the WebAssembly spec
        Custom,
        Type,
        Import,
        Function,
        Table,
        Memory,
        Global,
        Export,
        Start,
        Element,
        Code,
        Data,
        DataCount,
    }

    private readonly BinaryReader _reader;

    private readonly Lazy<bool> _isWasmModule;

    public bool IsWasmModule => _isWasmModule.Value;

    public WasmModuleReader(Stream stream)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
         _isWasmModule = new Lazy<bool>(this.GetIsWasmModule);
    }


    public void Dispose()
    {
        Dispose(true);
    }


    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader.Dispose();
        }
    }

    protected virtual bool VisitSection (Section sec, out bool shouldStop)
    {
        shouldStop = false;
        return true;
    }

    private const uint WASM_MAGIC = 0x6d736100u; // "\0asm"

    private bool GetIsWasmModule()
    {
        _reader.BaseStream.Seek(0, SeekOrigin.Begin);
        try
        {
            uint magic = _reader.ReadUInt32();
            if (magic == WASM_MAGIC)
                return true;
        } catch (EndOfStreamException) {}
        return false;
    }

    public bool Visit()
    {
        if (!IsWasmModule)
            return false;
        _reader.BaseStream.Seek(4L, SeekOrigin.Begin); // skip magic

        uint version = _reader.ReadUInt32();
        if (version != 1)
            return false;

        bool success = true;
        while (success) {
            success = DoVisitSection (out bool shouldStop);
            if (shouldStop)
                break;
        }
        return success;
    }

    private bool DoVisitSection(out bool shouldStop)
    {
        shouldStop = false;
        byte code = _reader.ReadByte();
        Section section = (Section)code;
        if (!Enum.IsDefined(typeof(Section), section))
            return false;
        uint sectionSize = ReadULEB128();

        long savedPos = _reader.BaseStream.Position;
        try
        {
            return VisitSection(section, out shouldStop);
        }
        finally
        {
            _reader.BaseStream.Seek(savedPos + (long)sectionSize, SeekOrigin.Begin);
        }
    }

    protected uint ReadULEB128()
    {
        uint val = 0;
        int shift = 0;
        while (true)
        {
            byte b = _reader.ReadByte();
            val |= (b & 0x7fu) << shift;
            if ((b & 0x80u) == 0) break;
            shift += 7;
            if (shift >= 35)
                throw new OverflowException();
        }
        return val;
    }

    protected bool TryReadPassiveDataSegment (out long segmentLength, out long segmentStart)
    {
        segmentLength = 0;
        segmentStart = 0;
        byte code = _reader.ReadByte();
        if (code != 1)
            return false; // not passive
        segmentLength = ReadULEB128();
        segmentStart = _reader.BaseStream.Position;
        // skip over the data
        _reader.BaseStream.Seek (segmentLength, SeekOrigin.Current);
        return true;
    }
}
