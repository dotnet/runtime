// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

/// <summary>
/// Abstraction for reading bytes from a signature blob.
/// </summary>
/// <remarks>
/// Allows the decoder to read from different sources (in-memory spans,
/// target process memory) without allocating intermediate byte arrays.
/// </remarks>
internal interface ISignatureReader
{
    byte ReadByte();
    byte PeekByte();
    int Remaining { get; }

    /// <summary>Reads a pointer-sized unsigned value (4 or 8 bytes).</summary>
    ulong ReadPointerSized(int pointerSize);
}

/// <summary>
/// Reads signature bytes from a <see cref="ReadOnlySpan{T}"/>.
/// </summary>
internal ref struct SpanSignatureReader : ISignatureReader
{
    private readonly ReadOnlySpan<byte> _blob;
    private readonly bool _isLittleEndian;
    private int _offset;

    public SpanSignatureReader(ReadOnlySpan<byte> blob, bool isLittleEndian = true)
    {
        _blob = blob;
        _isLittleEndian = isLittleEndian;
        _offset = 0;
    }

    public int Remaining => _blob.Length - _offset;

    public byte ReadByte()
    {
        if (_offset >= _blob.Length)
            throw new BadImageFormatException("Unexpected end of signature blob");
        return _blob[_offset++];
    }

    public byte PeekByte()
    {
        if (_offset >= _blob.Length)
            throw new BadImageFormatException("Unexpected end of signature blob");
        return _blob[_offset];
    }

    public ulong ReadPointerSized(int pointerSize)
    {
        if (_offset + pointerSize > _blob.Length)
            throw new BadImageFormatException("Unexpected end of signature blob");

        ReadOnlySpan<byte> slice = _blob.Slice(_offset, pointerSize);
        ulong val = pointerSize == 8
            ? (_isLittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(slice) : BinaryPrimitives.ReadUInt64BigEndian(slice))
            : (_isLittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(slice) : BinaryPrimitives.ReadUInt32BigEndian(slice));
        _offset += pointerSize;
        return val;
    }
}

/// <summary>
/// Reads signature bytes lazily from a <see cref="BlobHandle"/> via a <see cref="MetadataReader"/>.
/// </summary>
internal ref struct BlobHandleSignatureReader : ISignatureReader
{
    private BlobReader _blobReader;
    private readonly bool _isLittleEndian;

    public BlobHandleSignatureReader(MetadataReader metadataReader, BlobHandle blobHandle, bool isLittleEndian = true)
    {
        _blobReader = metadataReader.GetBlobReader(blobHandle);
        _isLittleEndian = isLittleEndian;
    }

    public int Remaining => _blobReader.RemainingBytes;

    public byte ReadByte() => _blobReader.ReadByte();

    public byte PeekByte()
    {
        if (_blobReader.RemainingBytes == 0)
            throw new BadImageFormatException("Unexpected end of signature blob");
        byte value = _blobReader.ReadByte();
        _blobReader.Offset--;
        return value;
    }

    public ulong ReadPointerSized(int pointerSize)
    {
        if (_blobReader.RemainingBytes < pointerSize)
            throw new BadImageFormatException("Unexpected end of signature blob");

        return pointerSize == 8
            ? (_isLittleEndian ? _blobReader.ReadUInt64() : BinaryPrimitives.ReverseEndianness(_blobReader.ReadUInt64()))
            : (_isLittleEndian ? _blobReader.ReadUInt32() : BinaryPrimitives.ReverseEndianness(_blobReader.ReadUInt32()));
    }
}
