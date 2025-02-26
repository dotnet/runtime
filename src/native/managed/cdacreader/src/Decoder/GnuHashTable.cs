// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal sealed class GnuHashTable : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly bool _is64Bit;

    private readonly long _symbolTableOffset;
    private readonly long _stringTableOffset;
    private readonly int _stringTableSize;

    private readonly int[] _hashBuckets;
    private readonly ulong _chainsOffset;
    private readonly int _symbolOffset;

    private bool _disposedValue;

    public GnuHashTable(
        Stream stream,
        long gnuHashTableOffset,
        long symbolTableOffset,
        long stringTableOffset,
        int stringTableSize,
        bool is64Bit,
        bool leaveStreamOpen)
    {
        _reader = new(stream, Encoding.UTF8, leaveStreamOpen);

        _symbolTableOffset = symbolTableOffset;
        _stringTableOffset = stringTableOffset;
        _stringTableSize = stringTableSize;
        _is64Bit = is64Bit;

        _reader.BaseStream.Seek(gnuHashTableOffset, SeekOrigin.Begin);

        int bucketCount = _reader.ReadInt32();
        _symbolOffset = _reader.ReadInt32();
        int bloomSize = _reader.ReadInt32();
        int _ /*bloomShift*/ = _reader.ReadInt32();

        // skip bloom filter
        _reader.BaseStream.Seek(bloomSize * (is64Bit ? 8 : 4), SeekOrigin.Current);

        // populate hash buckets
        _hashBuckets = new int[bucketCount];
        for (int i = 0; i < bucketCount; i++)
        {
            _hashBuckets[i] = _reader.ReadInt32();
        }

        // chains begin at end of hash buckets
        _chainsOffset = (ulong)_reader.BaseStream.Position;
    }

    public bool TryLookupRelativeSymbolAddress(string symbol, out ulong address)
    {
        address = 0;

        uint hash = GnuHash(symbol);

        List<int> symbolIndexes = GetPossibleSymbolIndex(hash);

        foreach (int possibleLocation in symbolIndexes)
        {
            string possibleString = _is64Bit ? GetSymbolName<ulong>(possibleLocation) : GetSymbolName<uint>(possibleLocation);

            if (symbol == possibleString)
            {
                address = _is64Bit ? GetSymbolValue<ulong>(possibleLocation) : GetSymbolValue<uint>(possibleLocation);
                return true;
            }
        }
        return false;
    }

    private T GetSymbolValue<T>(int index)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>, IConvertible
    {
        _reader.BaseStream.Seek(_symbolTableOffset + Elf_Sym<T>.GetPackedSize() * index, SeekOrigin.Begin);
        Elf_Sym<T> elfSymbol = new(_reader);
        return elfSymbol.st_value;
    }

    private string GetSymbolName<T>(int index)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>, IConvertible
    {
        _reader.BaseStream.Seek(_symbolTableOffset + Elf_Sym<T>.GetPackedSize() * index, SeekOrigin.Begin);
        Elf_Sym<T> elfSymbol = new(_reader);
        return GetStringAtIndex(elfSymbol.st_name);
    }

    private List<int> GetPossibleSymbolIndex(uint hash)
    {
        List<int> symbolIndexes = [];

        int i = _hashBuckets[(int)(hash % _hashBuckets.Length)] - _symbolOffset;
        while (true)
        {
            _reader.BaseStream.Seek((long)_chainsOffset + i * sizeof(int), SeekOrigin.Begin);
            int chainVal = _reader.ReadInt32();

            // LSB denotes end of chain. Compare hash with chain value ignoring LSB.
            if ((chainVal & 0xfffffffe) == (hash & 0xfffffffe))
            {
                symbolIndexes.Add(i + _symbolOffset);
            }
            // If LSB is set, this is the last element in the chain.
            if ((chainVal & 0x1) == 0x1)
            {
                break;
            }
            i++;
        }
        return symbolIndexes;
    }

    private string GetStringAtIndex(uint index)
    {
        if (index > _stringTableSize)
        {
            throw new InvalidOperationException("String table index out of bounds.");
        }

        _reader.BaseStream.Seek(_stringTableOffset + index, SeekOrigin.Begin);
        return _reader.ReadZString();
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
