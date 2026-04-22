// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Superset of SRM's <see cref="ISignatureTypeProvider{TType, TGenericContext}"/>
/// that adds support for runtime-internal type codes (<c>ELEMENT_TYPE_INTERNAL</c>).
/// </summary>
/// <remarks>
/// Providers implementing this interface automatically satisfy SRM's
/// <see cref="ISignatureTypeProvider{TType, TGenericContext}"/> and can be used
/// with both SRM's <c>SignatureDecoder</c> and our
/// <see cref="RuntimeSignatureDecoder{TType, TGenericContext, TReader}"/>.
/// </remarks>
internal interface IRuntimeSignatureTypeProvider<TType, TGenericContext>
    : ISignatureTypeProvider<TType, TGenericContext>
{
    /// <summary>
    /// Classify an <c>ELEMENT_TYPE_INTERNAL</c> (0x21) type by resolving the
    /// embedded TypeHandle pointer via the target's runtime type system.
    /// </summary>
    TType GetInternalType(Target target, TargetPointer typeHandlePointer);

    /// <summary>
    /// Classify an <c>ELEMENT_TYPE_CMOD_INTERNAL</c> (0x22) custom modifier by
    /// resolving the embedded TypeHandle pointer via the target's runtime type system.
    /// </summary>
    TType GetInternalModifiedType(Target target, TargetPointer typeHandlePointer, TType unmodifiedType, bool isRequired);
}

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
/// Decodes method and local variable signatures, handling both standard ECMA-335
/// types and runtime-internal types like <c>ELEMENT_TYPE_INTERNAL</c> (0x21).
/// </summary>
/// <remarks>
/// <para>
/// Handles the same ECMA-335 type codes as SRM's
/// <see cref="SignatureDecoder{TType, TGenericContext}"/>, plus runtime-internal
/// types (<c>ELEMENT_TYPE_INTERNAL</c> 0x21 and <c>ELEMENT_TYPE_CMOD_INTERNAL</c> 0x22).
/// </para>
/// <para>
/// Internal custom modifiers (<c>ELEMENT_TYPE_CMOD_INTERNAL</c>) are skipped since
/// they carry runtime TypeHandle pointers that are not meaningful for type classification.
/// Standard custom modifiers (<c>modreq</c>/<c>modopt</c>) are decoded and dispatched
/// to <see cref="ISignatureTypeProvider{TType, TGenericContext}.GetModifiedType"/>.
/// </para>
/// </remarks>
internal ref struct RuntimeSignatureDecoder<TType, TGenericContext, TReader>
    where TReader : ISignatureReader, allows ref struct
{
    private const byte ELEMENT_TYPE_PTR = 0x0f;
    private const byte ELEMENT_TYPE_BYREF = 0x10;
    private const byte ELEMENT_TYPE_VALUETYPE = 0x11;
    private const byte ELEMENT_TYPE_CLASS = 0x12;
    private const byte ELEMENT_TYPE_VAR = 0x13;
    private const byte ELEMENT_TYPE_ARRAY = 0x14;
    private const byte ELEMENT_TYPE_GENERICINST = 0x15;
    private const byte ELEMENT_TYPE_FNPTR = 0x1b;
    private const byte ELEMENT_TYPE_SZARRAY = 0x1d;
    private const byte ELEMENT_TYPE_MVAR = 0x1e;
    private const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
    private const byte ELEMENT_TYPE_CMOD_OPT = 0x20;
    private const byte ELEMENT_TYPE_INTERNAL = 0x21;
    private const byte ELEMENT_TYPE_CMOD_INTERNAL = 0x22;
    private const byte ELEMENT_TYPE_SENTINEL = 0x41;
    private const byte ELEMENT_TYPE_PINNED = 0x45;

    private readonly IRuntimeSignatureTypeProvider<TType, TGenericContext> _provider;
    private readonly Target _target;
    private readonly TGenericContext _genericContext;
    private TReader _reader;

    public RuntimeSignatureDecoder(
        IRuntimeSignatureTypeProvider<TType, TGenericContext> provider,
        Target target,
        TGenericContext genericContext,
        TReader reader)
    {
        _provider = provider;
        _target = target;
        _genericContext = genericContext;
        _reader = reader;
    }

    /// <summary>Decodes a method signature (MethodDefSig/MethodRefSig).</summary>
    public MethodSignature<TType> DecodeMethodSignature()
    {
        byte rawHeader = _reader.ReadByte();
        SignatureHeader header = new(rawHeader);

        if (header.Kind is not SignatureKind.Method and not SignatureKind.Property)
            throw new BadImageFormatException($"Unexpected signature header kind: {header.Kind}");

        int genericParameterCount = 0;
        if (header.IsGeneric)
            genericParameterCount = ReadCompressedUInt();

        int parameterCount = ReadCompressedUInt();
        if (parameterCount > _reader.Remaining)
            throw new BadImageFormatException($"Parameter count {parameterCount} exceeds remaining signature bytes");
        TType returnType = DecodeType();

        var parameterTypes = ImmutableArray.CreateBuilder<TType>(parameterCount);
        int requiredParameterCount = parameterCount;
        bool sentinelSeen = false;

        for (int i = 0; i < parameterCount; i++)
        {
            if (_reader.Remaining > 0 && _reader.PeekByte() == ELEMENT_TYPE_SENTINEL)
            {
                if (sentinelSeen)
                    throw new BadImageFormatException("Multiple sentinels in method signature");
                sentinelSeen = true;
                requiredParameterCount = i;
                _reader.ReadByte();
            }
            parameterTypes.Add(DecodeType());
        }

        return new MethodSignature<TType>(
            header, returnType, requiredParameterCount, genericParameterCount,
            parameterTypes.MoveToImmutable());
    }

    /// <summary>Decodes a local variable signature (LocalVarSig).</summary>
    public ImmutableArray<TType> DecodeLocalSignature()
    {
        byte header = _reader.ReadByte();
        if (header != 0x07) // IMAGE_CEE_CS_CALLCONV_LOCAL_SIG
            throw new BadImageFormatException($"Expected LocalVarSig header (0x07), got 0x{header:X2}");

        int count = ReadCompressedUInt();
        if (count == 0)
            throw new BadImageFormatException("Local variable signature must have at least one entry");
        if (count > _reader.Remaining)
            throw new BadImageFormatException($"Local count {count} exceeds remaining signature bytes");
        var locals = ImmutableArray.CreateBuilder<TType>(count);
        for (int i = 0; i < count; i++)
            locals.Add(DecodeType());
        return locals.MoveToImmutable();
    }

    /// <summary>Decodes a single type embedded in a signature.</summary>
    public TType DecodeType()
    {
        // Handle custom modifiers (standard and internal)
        while (_reader.Remaining > 0)
        {
            byte peek = _reader.PeekByte();
            if (peek is ELEMENT_TYPE_CMOD_REQD or ELEMENT_TYPE_CMOD_OPT)
            {
                bool isRequired = peek == ELEMENT_TYPE_CMOD_REQD;
                _reader.ReadByte();
                TType modifier = DecodeTypeDefOrRefOrSpec(0);
                TType unmodifiedType = DecodeType();
                return _provider.GetModifiedType(modifier, unmodifiedType, isRequired);
            }
            else if (peek == ELEMENT_TYPE_CMOD_INTERNAL)
            {
                _reader.ReadByte();
                bool isRequired = _reader.ReadByte() != 0;
                ulong val = _reader.ReadPointerSized(_target.PointerSize);
                TType unmodifiedType = DecodeType();
                return _provider.GetInternalModifiedType(
                    _target, new TargetPointer(val), unmodifiedType, isRequired);
            }
            else
            {
                break;
            }
        }

        byte typeCode = _reader.ReadByte();

        switch (typeCode)
        {
            case (byte)SignatureTypeCode.Boolean:
            case (byte)SignatureTypeCode.Char:
            case (byte)SignatureTypeCode.SByte:
            case (byte)SignatureTypeCode.Byte:
            case (byte)SignatureTypeCode.Int16:
            case (byte)SignatureTypeCode.UInt16:
            case (byte)SignatureTypeCode.Int32:
            case (byte)SignatureTypeCode.UInt32:
            case (byte)SignatureTypeCode.Int64:
            case (byte)SignatureTypeCode.UInt64:
            case (byte)SignatureTypeCode.Single:
            case (byte)SignatureTypeCode.Double:
            case (byte)SignatureTypeCode.IntPtr:
            case (byte)SignatureTypeCode.UIntPtr:
            case (byte)SignatureTypeCode.Object:
            case (byte)SignatureTypeCode.String:
            case (byte)SignatureTypeCode.Void:
            case (byte)SignatureTypeCode.TypedReference:
                return _provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);

            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_VALUETYPE:
                return DecodeTypeDefOrRefOrSpec(typeCode);

            case ELEMENT_TYPE_PTR:
                return _provider.GetPointerType(DecodeType());

            case ELEMENT_TYPE_BYREF:
                return _provider.GetByReferenceType(DecodeType());

            case ELEMENT_TYPE_SZARRAY:
                return _provider.GetSZArrayType(DecodeType());

            case ELEMENT_TYPE_ARRAY:
            {
                TType elementType = DecodeType();
                ArrayShape shape = DecodeArrayShape();
                return _provider.GetArrayType(elementType, shape);
            }

            case ELEMENT_TYPE_GENERICINST:
            {
                TType baseType = DecodeType();
                int count = ReadCompressedUInt();
                if (count == 0)
                    throw new BadImageFormatException("Generic instantiation must have at least one type argument");
                if (count > _reader.Remaining)
                    throw new BadImageFormatException($"Generic argument count {count} exceeds remaining signature bytes");
                var args = ImmutableArray.CreateBuilder<TType>(count);
                for (int i = 0; i < count; i++)
                    args.Add(DecodeType());
                return _provider.GetGenericInstantiation(baseType, args.MoveToImmutable());
            }

            case ELEMENT_TYPE_VAR:
                return _provider.GetGenericTypeParameter(_genericContext, ReadCompressedUInt());

            case ELEMENT_TYPE_MVAR:
                return _provider.GetGenericMethodParameter(_genericContext, ReadCompressedUInt());

            case ELEMENT_TYPE_FNPTR:
            {
                MethodSignature<TType> fnSig = DecodeMethodSignature();
                return _provider.GetFunctionPointerType(fnSig);
            }

            case ELEMENT_TYPE_PINNED:
                return _provider.GetPinnedType(DecodeType());

            case ELEMENT_TYPE_INTERNAL:
            {
                ulong val = _reader.ReadPointerSized(_target.PointerSize);
                return _provider.GetInternalType(_target, new TargetPointer(val));
            }

            default:
                throw new BadImageFormatException($"Unexpected signature type code: 0x{typeCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a TypeDefOrRefOrSpecEncoded token (ECMA-335 II.23.2.8).
    /// The compressed value encodes tag in the low 2 bits and RID in the upper bits.
    /// </summary>
    private TType DecodeTypeDefOrRefOrSpec(byte rawTypeKind)
    {
        int coded = ReadCompressedUInt();
        int tag = coded & 0x3;
        int rid = coded >> 2;

        if (rid == 0)
            throw new BadImageFormatException("Nil TypeDefOrRefOrSpecEncoded handle in signature");

        if (rid > 0x00FFFFFF)
            throw new BadImageFormatException($"TypeDefOrRefOrSpecEncoded RID out of range: {rid}");

        return tag switch
        {
            0 => _provider.GetTypeFromDefinition(null!, MetadataTokens.TypeDefinitionHandle(rid), rawTypeKind),
            1 => _provider.GetTypeFromReference(null!, MetadataTokens.TypeReferenceHandle(rid), rawTypeKind),
            2 => _provider.GetTypeFromSpecification(null!, _genericContext, MetadataTokens.TypeSpecificationHandle(rid), rawTypeKind),
            _ => _provider.GetPrimitiveType(PrimitiveTypeCode.Object), // tag=3 is BaseType in native
        };
    }

    private ArrayShape DecodeArrayShape()
    {
        int rank = ReadCompressedUInt();
        int numSizes = ReadCompressedUInt();
        if (numSizes > _reader.Remaining)
            throw new BadImageFormatException($"Array size count {numSizes} exceeds remaining signature bytes");
        var sizes = ImmutableArray.CreateBuilder<int>(numSizes);
        for (int i = 0; i < numSizes; i++)
            sizes.Add(ReadCompressedUInt());
        int numLoBounds = ReadCompressedUInt();
        if (numLoBounds > _reader.Remaining)
            throw new BadImageFormatException($"Array lower bound count {numLoBounds} exceeds remaining signature bytes");
        var loBounds = ImmutableArray.CreateBuilder<int>(numLoBounds);
        for (int i = 0; i < numLoBounds; i++)
            loBounds.Add(ReadCompressedSignedInt());
        return new ArrayShape(rank, sizes.MoveToImmutable(), loBounds.MoveToImmutable());
    }

    /// <summary>
    /// Reads a compressed unsigned integer per ECMA-335 II.23.2.
    /// </summary>
    private int ReadCompressedUInt()
    {
        byte first = _reader.ReadByte();
        if ((first & 0x80) == 0)
            return first;
        if ((first & 0xC0) == 0x80)
            return ((first & 0x3F) << 8) | _reader.ReadByte();
        if ((first & 0xE0) == 0xC0)
            return ((first & 0x1F) << 24) | (_reader.ReadByte() << 16) | (_reader.ReadByte() << 8) | _reader.ReadByte();

        throw new BadImageFormatException("Invalid compressed integer encoding");
    }

    /// <summary>
    /// Reads a compressed signed integer per ECMA-335 II.23.2.
    /// Uses sign extension based on encoded width, matching SRM's BlobReader.ReadCompressedSignedInteger.
    /// </summary>
    private int ReadCompressedSignedInt()
    {
        byte first = _reader.ReadByte();

        if ((first & 0x80) == 0)
        {
            // 1-byte: 7 bits, sign bit is bit 0 of the encoded value
            int value = first >> 1;
            return (first & 1) != 0 ? value - 0x40 : value;
        }

        if ((first & 0xC0) == 0x80)
        {
            // 2-byte: 14 bits
            int raw = ((first & 0x3F) << 8) | _reader.ReadByte();
            int value = raw >> 1;
            return (raw & 1) != 0 ? value - 0x2000 : value;
        }

        if ((first & 0xE0) == 0xC0)
        {
            // 4-byte: 29 bits
            int raw = ((first & 0x1F) << 24) | (_reader.ReadByte() << 16) | (_reader.ReadByte() << 8) | _reader.ReadByte();
            int value = raw >> 1;
            return (raw & 1) != 0 ? value - 0x10000000 : value;
        }

        throw new BadImageFormatException("Invalid compressed signed integer encoding");
    }
}
