// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class ELFDecoder : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly ulong _baseAddress;

    private bool _is64Bit;
    private GnuHashTable? _gnuHashTable;

    private bool _disposedValue;

    public bool IsValid => _gnuHashTable is not null;

    /// <summary>
    /// Create ELFReader with stream beginning at the base address of the module.
    /// </summary>
    public ELFDecoder(Stream stream, ulong baseAddress)
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

    /// <summary>
    /// Initializes the ElfDecoder with specific type.
    /// Supports ELF32 with uint and ELF64 with ulong.
    /// </summary>
    private void Initialize<T>()
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>, IConvertible
    {
        // read the full Elf_Ehdr
        _reader.BaseStream.Seek(0, SeekOrigin.Begin);
        Elf_Ehdr<T> elfHeader = new(_reader);

        // read the list of Elf_Phdr starting at e_phoff
        _reader.BaseStream.Seek(Convert.ToInt64(elfHeader.e_phoff), SeekOrigin.Begin);
        List<Elf_Phdr<T>> programHeaders = [];
        for (int i = 0; i < elfHeader.e_phnum; i++)
        {
            programHeaders.Add(new Elf_Phdr<T>(_reader));
        }

        // Calculate the load bias from the PT_LOAD program headers.
        // PT_LOAD program headers map the executable file regions to virtual memory regions.
        // p_offset: offset into the executable file for first byte of segment
        // p_vaddr: virtual address of first byte of segment in memory
        // p_filesz: number of bytes in the file image of the segment
        // p_memsz: number of bytes in the virtual memory region of the segment
        //
        // For a given PT_LOAD header, it maps a virtual memory region at the requested p_vaddr
        // to the data beginning at p_offset in the file. While the OS can move the virtual memory segments,
        // Within an executable, all of the mapped segments must maintain their relative spacing.
        // We compute this "load bias" to correctly map RVAs to memory.
        //
        // Since the Elf Header is always at the beginning of the executable file it is mapped in the PT_LOAD, `firstLoad`, with p_offset = 0.
        // We read the ElfHeader at memory _baseAddress, therefore the load bias is `_baseAddress - (firstLoad.p_vaddr)`
        // Because we are working relative to _baseAddress load bias is stored as a just `firstLoad.p_vaddr` then subtracted.
        ulong relativeLoadBias = 0;
        foreach (Elf_Phdr<T> programHeader in programHeaders)
        {
            if (programHeader.Type == HeaderType.PT_LOAD &&
                Convert.ToUInt64(programHeader.p_offset) == 0)
            {
                relativeLoadBias = Convert.ToUInt64(programHeader.p_vaddr);
                break;
            }
        }
        ulong loadBias = _baseAddress - relativeLoadBias;

        long dynamicOffset = 0;
        foreach (Elf_Phdr<T> programHeader in programHeaders)
        {
            if (programHeader.Type == HeaderType.PT_DYNAMIC)
            {
                dynamicOffset = (long)(Convert.ToUInt64(programHeader.p_vaddr) - relativeLoadBias);
                break;
            }
        }
        if (dynamicOffset == 0) return;

        long gnuHashTableOffset = 0;
        long symbolTableOffset = 0;
        long stringTableOffset = 0;
        int stringTableSize = 0;
        _reader.BaseStream.Seek(dynamicOffset, SeekOrigin.Begin);
        while (true)
        {
            Elf_Dyn<T> dynamicEntry = new Elf_Dyn<T>(_reader);

            if (dynamicEntry.Type == DynamicType.DT_NULL)
                break;
            if (dynamicEntry.Type == DynamicType.DT_GNU_HASH)
                gnuHashTableOffset = (long)(Convert.ToUInt64(dynamicEntry.d_val) - loadBias);
            if (dynamicEntry.Type == DynamicType.DT_SYMTAB)
                symbolTableOffset = (long)(Convert.ToUInt64(dynamicEntry.d_val) - loadBias);
            if (dynamicEntry.Type == DynamicType.DT_STRTAB)
                stringTableOffset = (long)(Convert.ToUInt64(dynamicEntry.d_val) - loadBias);
            if (dynamicEntry.Type == DynamicType.DT_STRSZ)
                stringTableSize = Convert.ToInt32(dynamicEntry.d_val);
        }

        if (gnuHashTableOffset == 0) return;
        if (symbolTableOffset == 0) return;
        if (stringTableOffset == 0) return;

        _gnuHashTable = new GnuHashTable(
            _reader.BaseStream,
            gnuHashTableOffset,
            symbolTableOffset,
            stringTableOffset,
            stringTableSize,
            _is64Bit,
            leaveStreamOpen: true);
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
