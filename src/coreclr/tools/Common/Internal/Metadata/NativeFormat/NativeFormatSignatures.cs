// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.NativeFormat;

using CorElementType = System.Reflection.CorElementType;
using MdSigCallingConvention = System.Reflection.MdSigCallingConvention;

namespace Internal.Metadata.NativeFormat;

// Signature blobs follow ECMA-335 II.23.2, except that TypeDefOrRefOrSpecEncoded tokens are replaced with
// NativeFormat handles. The reader mirrors CoreCLR's SigParser so that signature parsing can be shared with
// CoreCLR's System.Private.CoreLib.

// A position within a signature blob, equivalent to the (Signature, offset) pairs used by CoreCLR's reflection.
#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly struct TypeSignature
{
    public MetadataReader Reader { get; }
    internal uint Offset { get; }
    internal uint EndOffset { get; }

    internal TypeSignature(MetadataReader reader, uint offset, uint endOffset)
    {
        Reader = reader;
        Offset = offset;
        EndOffset = endOffset;
    }

    public SignatureReader GetReader() => new SignatureReader(Reader, Offset, EndOffset);

    public TypeSignatureCollection GetGenericArguments()
    {
        SignatureReader reader = GetReader();
        CorElementType elementType = reader.ReadElementType();
        Debug.Assert(elementType == CorElementType.ELEMENT_TYPE_GENERICINST);
        reader.SkipType();
        int count = reader.ReadCompressedInteger();
        return new TypeSignatureCollection(reader.Position, count);
    }

    // Equivalent of CoreCLR's Signature.GetTypeParameterOffset. Index 0 of a function pointer is its return type.
    internal TypeSignature GetTypeParameter(int index)
    {
        SignatureReader reader = GetReader();
        switch (reader.ReadElementType())
        {
            case CorElementType.ELEMENT_TYPE_FNPTR:
                reader.ReadMethodHeader(out _, out int parameterCount);
                Debug.Assert((uint)index <= (uint)parameterCount);
                break;
            case CorElementType.ELEMENT_TYPE_GENERICINST:
                reader.SkipType();
                int argumentCount = reader.ReadCompressedInteger();
                Debug.Assert((uint)index < (uint)argumentCount);
                break;
            case CorElementType.ELEMENT_TYPE_ARRAY:
            case CorElementType.ELEMENT_TYPE_SZARRAY:
            case CorElementType.ELEMENT_TYPE_BYREF:
            case CorElementType.ELEMENT_TYPE_PTR:
                Debug.Assert(index == 0);
                break;
            case CorElementType.ELEMENT_TYPE_VAR:
            case CorElementType.ELEMENT_TYPE_MVAR:
                // The signature of a substituted generic variable doesn't describe the substituted type's components.
                return default;
            default:
                Debug.Fail("Unexpected element type");
                return default;
        }

        for (int i = 0; i < index; i++)
            reader.SkipType();
        return reader.Position;
    }

    // Equivalent of CoreCLR's Signature.GetCallingConventionFromFunctionPointerAtOffset.
    internal MdSigCallingConvention GetCallingConventionFromFunctionPointer()
    {
        SignatureReader reader = GetReader();
        CorElementType elementType = reader.ReadElementType();
        Debug.Assert(elementType == CorElementType.ELEMENT_TYPE_FNPTR);
        return reader.ReadMethodHeader(out _, out _) & MdSigCallingConvention.CallConvMask;
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

    internal SignatureReader(MetadataReader reader, uint offset, uint endOffset)
    {
        _reader = reader;
        _offset = offset;
        _endOffset = endOffset;
    }

    internal SignatureReader(MetadataReader reader, ByteCollection blob)
    {
        _reader = reader;
        _offset = reader._streamReader.DecodeUnsigned(blob.Offset, out uint length);
        _endOffset = _offset + length;
        if (_endOffset < _offset || _endOffset > reader._streamReader.Size)
            ThrowBadImageFormatException();
    }

    public readonly TypeSignature Position => new TypeSignature(_reader, _offset, _endOffset);

    // Like SigParser::PeekElemType and SigParser::GetElemType, these skip a vararg sentinel and custom modifiers.
    public readonly CorElementType PeekElementType()
    {
        SignatureReader reader = this;
        return reader.ReadElementType();
    }

    public CorElementType ReadElementType()
    {
        SkipCustomModifiers();
        return (CorElementType)ReadByte();
    }

    public void SkipCustomModifiers()
    {
        while (TryReadCustomModifier(out _, out _))
        {
        }
    }

    // ECMA-335 II.23.2.7. Skips a vararg sentinel (II.23.2.2) preceding the modifiers.
    public bool TryReadCustomModifier(out bool isOptional, out Handle modifierType)
    {
        if (PeekByte() == (byte)CorElementType.ELEMENT_TYPE_SENTINEL)
            _offset++;

        CorElementType elementType = (CorElementType)PeekByte();
        if (elementType is not (CorElementType.ELEMENT_TYPE_CMOD_REQD or CorElementType.ELEMENT_TYPE_CMOD_OPT))
        {
            isOptional = false;
            modifierType = default;
            return false;
        }

        _offset++;
        isOptional = elementType == CorElementType.ELEMENT_TYPE_CMOD_OPT;
        modifierType = ReadTypeHandle();
        return true;
    }

    // ECMA-335 II.23.2.
    public int ReadCompressedInteger()
    {
        byte first = ReadByte();
        if ((first & 0x80) == 0)
            return first;
        if ((first & 0x40) == 0)
            return ((first & 0x3f) << 8) | ReadByte();
        if ((first & 0x20) == 0)
            return ((first & 0x1f) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();

        ThrowBadImageFormatException();
        return 0;
    }

    public Handle ReadTypeHandle()
    {
        _offset = _reader._streamReader.Read(_offset, out Handle handle);
        if (_offset > _endOffset)
            ThrowBadImageFormatException();
        return handle;
    }

    public TypeSignature ReadType()
    {
        TypeSignature result = Position;
        SkipType();
        return result;
    }

    // Equivalent of SigParser::SkipExactlyOne.
    public void SkipType()
    {
        switch (ReadElementType())
        {
            case CorElementType.ELEMENT_TYPE_VOID:
            case CorElementType.ELEMENT_TYPE_BOOLEAN:
            case CorElementType.ELEMENT_TYPE_CHAR:
            case CorElementType.ELEMENT_TYPE_I1:
            case CorElementType.ELEMENT_TYPE_U1:
            case CorElementType.ELEMENT_TYPE_I2:
            case CorElementType.ELEMENT_TYPE_U2:
            case CorElementType.ELEMENT_TYPE_I4:
            case CorElementType.ELEMENT_TYPE_U4:
            case CorElementType.ELEMENT_TYPE_I8:
            case CorElementType.ELEMENT_TYPE_U8:
            case CorElementType.ELEMENT_TYPE_R4:
            case CorElementType.ELEMENT_TYPE_R8:
            case CorElementType.ELEMENT_TYPE_STRING:
            case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
            case CorElementType.ELEMENT_TYPE_I:
            case CorElementType.ELEMENT_TYPE_U:
            case CorElementType.ELEMENT_TYPE_OBJECT:
                break;
            case CorElementType.ELEMENT_TYPE_CLASS:
            case CorElementType.ELEMENT_TYPE_VALUETYPE:
                ReadTypeHandle();
                break;
            case CorElementType.ELEMENT_TYPE_PTR:
            case CorElementType.ELEMENT_TYPE_BYREF:
            case CorElementType.ELEMENT_TYPE_SZARRAY:
                SkipType();
                break;
            case CorElementType.ELEMENT_TYPE_VAR:
            case CorElementType.ELEMENT_TYPE_MVAR:
                ReadCompressedInteger();
                break;
            case CorElementType.ELEMENT_TYPE_GENERICINST:
                SkipType();
                int arguments = ReadCompressedInteger();
                for (int i = 0; i < arguments; i++)
                    SkipType();
                break;
            case CorElementType.ELEMENT_TYPE_ARRAY:
                SkipType();
                ReadArrayShape();
                break;
            case CorElementType.ELEMENT_TYPE_FNPTR:
                ReadMethodHeader(out _, out int parameters);
                for (int i = 0; i <= parameters; i++)
                    SkipType();
                break;
            default:
                ThrowBadImageFormatException();
                break;
        }
    }

    // ECMA-335 II.23.2.13. Returns the rank; sizes and lower bounds are skipped.
    public int ReadArrayShape()
    {
        int rank = ReadCompressedInteger();
        int sizes = ReadCompressedInteger();
        for (int i = 0; i < sizes; i++)
            ReadCompressedInteger();

        // Signed lower bounds use the same length encoding as unsigned integers.
        int lowerBounds = ReadCompressedInteger();
        for (int i = 0; i < lowerBounds; i++)
            ReadCompressedInteger();
        return rank;
    }

    // ECMA-335 II.23.2.1-3. The return type follows.
    internal MdSigCallingConvention ReadMethodHeader(out int genericParameterCount, out int parameterCount)
    {
        MdSigCallingConvention header = (MdSigCallingConvention)ReadByte();
        genericParameterCount = (header & MdSigCallingConvention.Generic) != 0 ? ReadCompressedInteger() : 0;
        parameterCount = ReadCompressedInteger();
        return header;
    }

    // ECMA-335 II.23.2.4. The field type follows.
    internal void ReadFieldHeader()
    {
        if ((MdSigCallingConvention)ReadByte() != MdSigCallingConvention.Field)
            ThrowBadImageFormatException();
    }

    private readonly byte PeekByte()
    {
        if (_offset >= _endOffset)
            ThrowBadImageFormatException();
        return _reader._streamReader.ReadUInt8(_offset);
    }

    private byte ReadByte()
    {
        byte value = PeekByte();
        _offset++;
        return value;
    }

    [DoesNotReturn]
    private static void ThrowBadImageFormatException() => throw new BadImageFormatException();
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
            Current = reader.ReadType();
            _next = reader.Position;
            _remaining--;
            return true;
        }
    }
}

internal readonly struct MethodSignatureInfo
{
    private readonly MdSigCallingConvention _header;
    public int GenericParameterCount { get; }
    public int ParameterCount { get; }
    public TypeSignature ReturnType { get; }

    public SignatureCallingConvention CallingConvention => (SignatureCallingConvention)(_header & ~MdSigCallingConvention.Generic);

    public TypeSignatureCollection Parameters
    {
        get
        {
            SignatureReader reader = ReturnType.GetReader();
            reader.SkipType();
            return new TypeSignatureCollection(reader.Position, ParameterCount);
        }
    }

    public MethodSignatureInfo(SignatureReader reader)
    {
        _header = reader.ReadMethodHeader(out int genericParameterCount, out int parameterCount);
        GenericParameterCount = genericParameterCount;
        ParameterCount = parameterCount;
        ReturnType = reader.Position;
    }
}

public readonly partial struct MethodSignature
{
    internal MethodSignatureInfo Info => new MethodSignatureInfo(new SignatureReader(_reader, Blob));
    public SignatureCallingConvention CallingConvention => Info.CallingConvention;
    public int GenericParameterCount => Info.GenericParameterCount;
    public int ParameterCount => Info.ParameterCount;
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
            reader.ReadFieldHeader();
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
