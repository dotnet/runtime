// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class ELFDecoder : IDisposable
{
    private readonly Stream _stream;
    private ulong _baseAddress;
    private bool is64Bit;
    private ulong dynamicOffset;
    private ulong gnuHashTableOffset;
    private ulong symbolTableOffset;
    private ulong stringTableOffset;
    private int stringTableSize;

    private GnuHashTable _gnuHashTable;
    private List<int> _hashBuckets = [];
    private ulong _chainsOffset;

    private bool _disposedValue;

    public bool IsValid { get; init; }

    /// <summary>
    /// Create ELFReader with stream beginning at the base address of the module.
    /// </summary>
    public ELFDecoder(Stream stream, ulong baseAddress)
    {
        _stream = stream;
        _baseAddress = baseAddress;

        IsValid = Initialize();
        if (IsValid)
        {
            IsValid = is64Bit ? InitializeHashTable<ulong>() : InitializeHashTable<uint>();
        }
    }

    private bool Initialize()
    {
        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);

        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        uint elfMagic = reader.ReadUInt32();
        if (elfMagic != 0x464C457F) // 0x7F followed by "ELF"
            return false;

        is64Bit = reader.ReadByte() != 1;

        return is64Bit ? Initialize<ulong>(reader) : Initialize<uint>(reader);
    }

    private bool Initialize<T>(BinaryReader reader)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>, IConvertible
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        Elf_Ehdr<T> elfHeader = new(reader);

        reader.BaseStream.Seek(Convert.ToInt64(elfHeader.e_phoff), SeekOrigin.Begin);

        List<Elf_Phdr<T>> programHeaders = [];
        for (int i = 0; i < elfHeader.e_phnum; i++)
        {
            programHeaders.Add(new Elf_Phdr<T>(reader));
        }

        // Calculate the load bais from the PT_LOAD program headers.
        ulong loadBias = 0;
        foreach (Elf_Phdr<T> programHeader in programHeaders)
        {
            if (programHeader.Type == HeaderType.PT_LOAD &&
                programHeader.p_offset == default)
            {
                loadBias = Convert.ToUInt64(programHeader.p_vaddr);
                break;
            }
        }

        foreach (Elf_Phdr<T> programHeader in programHeaders)
        {
            if (programHeader.Type == HeaderType.PT_DYNAMIC)
            {
                dynamicOffset = Convert.ToUInt64(programHeader.p_vaddr) - loadBias;
                break;
            }
        }

        if (dynamicOffset == 0) return false;

        reader.BaseStream.Seek((long)dynamicOffset, SeekOrigin.Begin);
        List<Elf_Dyn<T>> dynamicEntries = [];
        while (true)
        {
            Elf_Dyn<T> dynamicEntry = new Elf_Dyn<T>(reader);

            if (dynamicEntry.Type == DynamicType.DT_NULL)
                break;
            if (dynamicEntry.Type == DynamicType.DT_GNU_HASH)
                gnuHashTableOffset = Convert.ToUInt64(dynamicEntry.d_val) - _baseAddress;
            if (dynamicEntry.Type == DynamicType.DT_SYMTAB)
                symbolTableOffset = Convert.ToUInt64(dynamicEntry.d_val) - _baseAddress;
            if (dynamicEntry.Type == DynamicType.DT_STRTAB)
                stringTableOffset = Convert.ToUInt64(dynamicEntry.d_val) - _baseAddress;
            if (dynamicEntry.Type == DynamicType.DT_STRSZ)
                stringTableSize = Convert.ToInt32(dynamicEntry.d_val);
            dynamicEntries.Add(dynamicEntry);
        }

        if (gnuHashTableOffset == 0) return false;
        if (symbolTableOffset == 0) return false;
        if (stringTableOffset == 0) return false;

        return true;
    }

    private bool InitializeHashTable<T>()
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>, IConvertible
    {
        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);
        reader.BaseStream.Seek((long)gnuHashTableOffset, SeekOrigin.Begin);
        _gnuHashTable = new(reader);

        ulong bucketsOffset = gnuHashTableOffset + 16ul /*sizeof(GnuHashTable)*/ +
            (ulong)_gnuHashTable.bloomSize * (is64Bit ? 8ul : 4ul);

        reader.BaseStream.Seek((long)bucketsOffset, SeekOrigin.Begin);
        for (int i = 0; i < _gnuHashTable.bucketCount; i++)
        {
            _hashBuckets.Add(reader.ReadInt32());
        }

        _chainsOffset = bucketsOffset + (ulong)(_gnuHashTable.bucketCount * sizeof(int));

        return true;
    }

    public bool TryGetRelativeSymbolAddress(string symbol, out ulong address)
    {
        address = 0;

        if (!IsValid)
            return false;
        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);

        uint hash = GnuHash(symbol);

        List<int> symbolIndexes = [];
        int i = _hashBuckets[(int)(hash % _hashBuckets.Count)] - _gnuHashTable.symbolOffset;
        while (true)
        {
            reader.BaseStream.Seek((long)_chainsOffset + i * sizeof(int), SeekOrigin.Begin);
            int chainVal = reader.ReadInt32();
            if ((chainVal & 0xfffffffe) == (hash & 0xfffffffe))
            {
                symbolIndexes.Add(i + _gnuHashTable.symbolOffset);
            }
            if ((chainVal & 0x1) == 0x1)
            {
                break;
            }
            i++;
        }

        foreach (int possibleLocation in symbolIndexes)
        {
            reader.BaseStream.Seek((long)symbolTableOffset + Elf_Sym<ulong>.GetPackedSize() * possibleLocation, SeekOrigin.Begin);
            Elf_Sym<ulong> elfSymbol = new(reader);
            string possibleString = GetStringAtIndex(elfSymbol.st_name);

            if (symbol == possibleString)
            {
                address = elfSymbol.st_value;
                return true;
            }
        }
        return false;
    }

    private string GetStringAtIndex(uint index)
    {
        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);

        if (index > stringTableSize)
        {
            throw new InvalidOperationException("String table index out of bounds.");
        }

        reader.BaseStream.Seek((long)stringTableOffset + index, SeekOrigin.Begin);
        return reader.ReadZString();
    }

    private static uint GnuHash(string symbolName)
    {
        uint h = 5381;
        foreach (char c in symbolName)
        {
            h = (h << 5) + h + c;
        }
        return h;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stream.Close();
            }

            _disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
