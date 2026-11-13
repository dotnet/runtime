// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.Metadata.NativeFormat;

// ECMA-335 II.23.1.16 and II.23.2, with native handles replacing encoded tokens.
public enum SignatureTypeCode : byte
{
    Void = 0x01,
    Boolean = 0x02,
    Char = 0x03,
    SByte = 0x04,
    Byte = 0x05,
    Int16 = 0x06,
    UInt16 = 0x07,
    Int32 = 0x08,
    UInt32 = 0x09,
    Int64 = 0x0a,
    UInt64 = 0x0b,
    Single = 0x0c,
    Double = 0x0d,
    String = 0x0e,
    Pointer = 0x0f,
    ByReference = 0x10,
    ValueType = 0x11,
    Class = 0x12,
    GenericTypeParameter = 0x13,
    Array = 0x14,
    GenericTypeInstance = 0x15,
    TypedReference = 0x16,
    IntPtr = 0x18,
    UIntPtr = 0x19,
    FunctionPointer = 0x1b,
    Object = 0x1c,
    SZArray = 0x1d,
    GenericMethodParameter = 0x1e,
    RequiredModifier = 0x1f,
    OptionalModifier = 0x20,
    Sentinel = 0x41,
}

#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly struct TypeSignature
{
    public MetadataReader Reader { get; }
    public Handle Handle { get; }
    internal uint EndOffset { get; }
    public bool IsHandle => Handle.HandleType != HandleType.Null;

    public TypeSignature(MetadataReader reader, Handle handle)
    {
        if (handle.HandleType == HandleType.TypeSpecification)
        {
            this = reader.GetTypeSpecification(handle.ToTypeSpecificationHandle(reader)).Signature;
            return;
        }
        Debug.Assert(handle.HandleType is HandleType.TypeDefinition or HandleType.TypeReference);
        Reader = reader;
        Handle = handle;
        EndOffset = 0;
    }

    internal TypeSignature(MetadataReader reader, uint offset, uint endOffset)
    {
        Reader = reader;
        // A null tag distinguishes a blob position from a metadata record handle.
        Handle = new Handle(HandleType.Null, checked((int)offset));
        EndOffset = endOffset;
    }

    public SignatureReader GetReader()
    {
        Debug.Assert(!IsHandle);
        return new SignatureReader(this);
    }

    public TypeSignature SkipCustomModifiers()
    {
        if (IsHandle)
            return this;

        SignatureReader reader = GetReader();
        while (reader.TypeCode is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
        {
            reader.ReadByte();
            reader.ReadTypeHandle();
        }
        return reader.Position;
    }

    public TypeSignatureCollection GetGenericArguments()
    {
        SignatureReader reader = SkipCustomModifiers().GetReader();
        Debug.Assert(reader.TypeCode == SignatureTypeCode.GenericTypeInstance);
        reader.ReadByte();
        reader.ReadType();
        int count = reader.ReadCompressedInteger();
        return new TypeSignatureCollection(reader.Position, count);
    }
}

#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public struct SignatureReader
{
    private readonly MetadataReader _reader;
    private readonly uint _endOffset;
    private uint _offset;

    internal SignatureReader(TypeSignature position)
    {
        _reader = position.Reader;
        _offset = (uint)position.Handle.Offset;
        _endOffset = position.EndOffset;
    }

    internal SignatureReader(MetadataReader reader, ByteCollection blob)
    {
        _reader = reader;
        _offset = reader._streamReader.DecodeUnsigned(blob.Offset, out uint length);
        _endOffset = checked(_offset + length);
    }

    public readonly TypeSignature Position => new TypeSignature(_reader, _offset, _endOffset);
    public readonly SignatureTypeCode TypeCode => (SignatureTypeCode)_reader._streamReader.ReadUInt8(_offset);

    public byte ReadByte()
    {
        Debug.Assert(_offset < _endOffset);
        return _reader._streamReader.ReadUInt8(_offset++);
    }

    private readonly unsafe BlobReader GetBlobReader()
        => new BlobReader(new ReadOnlySpan<byte>((void*)_reader._streamReader.OffsetToAddress(_offset), checked((int)(_endOffset - _offset))));

    public int ReadCompressedInteger()
    {
        BlobReader reader = GetBlobReader();
        int value = reader.ReadCompressedInteger();
        _offset += (uint)reader.Offset;
        return value;
    }

    public int ReadCompressedSignedInteger()
    {
        BlobReader reader = GetBlobReader();
        int value = reader.ReadCompressedSignedInteger();
        _offset += (uint)reader.Offset;
        return value;
    }

    public Handle ReadTypeHandle()
    {
        _offset = _reader._streamReader.Read(_offset, out Handle handle);
        Debug.Assert(_offset <= _endOffset);
        return handle;
    }

    public TypeSignature ReadType()
    {
        TypeSignature result = Position;
        SkipType();
        return result;
    }

    public void SkipType()
    {
        switch ((SignatureTypeCode)ReadByte())
        {
            case SignatureTypeCode.Class:
            case SignatureTypeCode.ValueType:
                ReadTypeHandle();
                break;
            case SignatureTypeCode.RequiredModifier:
            case SignatureTypeCode.OptionalModifier:
                ReadTypeHandle();
                SkipType();
                break;
            case SignatureTypeCode.Pointer:
            case SignatureTypeCode.ByReference:
            case SignatureTypeCode.SZArray:
                SkipType();
                break;
            case SignatureTypeCode.GenericTypeParameter:
            case SignatureTypeCode.GenericMethodParameter:
                ReadCompressedInteger();
                break;
            case SignatureTypeCode.GenericTypeInstance:
                SkipType();
                int arguments = ReadCompressedInteger();
                for (int i = 0; i < arguments; i++)
                    SkipType();
                break;
            case SignatureTypeCode.Array:
                SkipType();
                ReadCompressedInteger();
                int sizes = ReadCompressedInteger();
                for (int i = 0; i < sizes; i++)
                    ReadCompressedInteger();
                int bounds = ReadCompressedInteger();
                for (int i = 0; i < bounds; i++)
                    ReadCompressedSignedInteger();
                break;
            case SignatureTypeCode.FunctionPointer:
                ReadMethodHeader(out _, out int count);
                SkipType();
                for (int i = 0; i < count; i++)
                {
                    if (TypeCode == SignatureTypeCode.Sentinel)
                        ReadByte();
                    SkipType();
                }
                break;
        }
    }

    public byte ReadMethodHeader(out int genericParameterCount, out int parameterCount)
    {
        byte header = ReadByte();
        genericParameterCount = (header & 0x10) != 0 ? ReadCompressedInteger() : 0;
        parameterCount = ReadCompressedInteger();
        return header;
    }
}

#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly struct TypeSignatureCollection
{
    private readonly TypeSignature _first;
    public int Count { get; }

    internal TypeSignatureCollection(TypeSignature first, int count) => (_first, Count) = (first, count);

    public Enumerator GetEnumerator() => new Enumerator(_first, Count);

    public struct Enumerator
    {
        private TypeSignature _next;
        private int _remaining;
        public TypeSignature Current { get; private set; }

        internal Enumerator(TypeSignature first, int count)
        {
            _next = first;
            _remaining = count;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_remaining == 0)
                return false;
            SignatureReader reader = _next.GetReader();
            if (reader.TypeCode == SignatureTypeCode.Sentinel)
                reader.ReadByte();
            Current = reader.ReadType();
            _next = reader.Position;
            _remaining--;
            return true;
        }
    }
}

#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly struct MethodSignatureInfo
{
    private readonly int _parameterCount;
    public SignatureCallingConvention CallingConvention { get; }
    public int GenericParameterCount { get; }
    public TypeSignature ReturnType { get; }
    public TypeSignatureCollection Parameters
    {
        get
        {
            SignatureReader reader = ReturnType.GetReader();
            reader.SkipType();
            return new TypeSignatureCollection(reader.Position, _parameterCount);
        }
    }

    public MethodSignatureInfo(SignatureReader reader)
    {
        byte header = reader.ReadMethodHeader(out int arity, out int count);
        CallingConvention = (SignatureCallingConvention)(header & ~0x10);
        GenericParameterCount = arity;
        ReturnType = reader.Position;
        _parameterCount = count;
    }
}

public readonly partial struct MethodSignature
{
    private MethodSignatureInfo Info => new MethodSignatureInfo(new SignatureReader(_reader, Blob));
    public SignatureCallingConvention CallingConvention => Info.CallingConvention;
    public int GenericParameterCount => Info.GenericParameterCount;
    public TypeSignature ReturnType => Info.ReturnType;
    public TypeSignatureCollection Parameters => Info.Parameters;
}

public readonly partial struct FieldSignature
{
    public TypeSignature Type
    {
        get
        {
            SignatureReader reader = new SignatureReader(_reader, Blob);
            byte header = reader.ReadByte();
            Debug.Assert(header == 0x06);
            return reader.Position;
        }
    }
}

public readonly partial struct PropertySignature
{
    private MethodSignatureInfo Info => new MethodSignatureInfo(new SignatureReader(_reader, Blob));
    public TypeSignature Type => Info.ReturnType;
    public TypeSignatureCollection Parameters => Info.Parameters;
}

public readonly partial struct TypeSpecification
{
    public TypeSignature Signature => new SignatureReader(_reader, Blob).Position;
}

public readonly partial struct ByteCollection
{
    internal uint Offset => _offset;
}
