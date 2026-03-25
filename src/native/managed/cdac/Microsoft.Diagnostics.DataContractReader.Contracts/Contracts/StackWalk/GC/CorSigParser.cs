// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Minimal signature parser for GC reference classification of method parameters.
/// Parses the ECMA-335 II.23.2.1 MethodDefSig format, classifying each parameter
/// type as a GC reference, interior pointer, value type, or non-GC primitive.
/// </summary>
internal ref struct CorSigParser
{
    private ReadOnlySpan<byte> _sig;
    private int _index;
    private readonly int _pointerSize;

    public CorSigParser(ReadOnlySpan<byte> signature, int pointerSize)
    {
        _sig = signature;
        _index = 0;
        _pointerSize = pointerSize;
    }

    public bool AtEnd => _index >= _sig.Length;

    public byte ReadByte()
    {
        if (_index >= _sig.Length)
            throw new InvalidOperationException("Unexpected end of signature.");
        return _sig[_index++];
    }

    /// <summary>
    /// Reads a compressed unsigned integer (ECMA-335 II.23.2).
    /// </summary>
    public uint ReadCompressedUInt()
    {
        byte b = ReadByte();
        if ((b & 0x80) == 0)
            return b;
        if ((b & 0xC0) == 0x80)
        {
            byte b2 = ReadByte();
            return (uint)(((b & 0x3F) << 8) | b2);
        }
        if ((b & 0xE0) == 0xC0)
        {
            byte b2 = ReadByte();
            byte b3 = ReadByte();
            byte b4 = ReadByte();
            return (uint)(((b & 0x1F) << 24) | (b2 << 16) | (b3 << 8) | b4);
        }
        throw new InvalidOperationException("Invalid compressed integer encoding.");
    }

    /// <summary>
    /// Reads the next type from the signature and classifies it for GC scanning.
    /// Advances past the full type encoding.
    /// </summary>
    public GcTypeKind ReadTypeAndClassify()
    {
        CorElementType elemType = (CorElementType)ReadCompressedUInt();

        switch (elemType)
        {
            case CorElementType.Void:
            case CorElementType.Boolean:
            case CorElementType.Char:
            case CorElementType.I1:
            case CorElementType.U1:
            case CorElementType.I2:
            case CorElementType.U2:
            case CorElementType.I4:
            case CorElementType.U4:
            case CorElementType.I8:
            case CorElementType.U8:
            case CorElementType.R4:
            case CorElementType.R8:
            case CorElementType.I:
            case CorElementType.U:
                return GcTypeKind.None;

            case CorElementType.String:
            case CorElementType.Object:
                return GcTypeKind.Ref;

            case CorElementType.Class:
                ReadCompressedUInt(); // TypeDefOrRefOrSpecEncoded
                return GcTypeKind.Ref;

            case CorElementType.ValueType:
                ReadCompressedUInt(); // TypeDefOrRefOrSpecEncoded
                return GcTypeKind.Other;

            case CorElementType.SzArray:
                SkipType(); // element type
                return GcTypeKind.Ref;

            case CorElementType.Array:
                SkipType(); // element type
                SkipArrayShape();
                return GcTypeKind.Ref;

            case CorElementType.GenericInst:
            {
                byte baseType = ReadByte(); // CLASS, VALUETYPE, or INTERNAL
                if (baseType == (byte)CorElementType.Internal)
                {
                    // ELEMENT_TYPE_INTERNAL embeds a raw pointer to a TypeHandle
                    _index += _pointerSize;
                }
                else
                {
                    ReadCompressedUInt(); // TypeDefOrRefOrSpecEncoded
                }
                uint argCount = ReadCompressedUInt();
                for (uint i = 0; i < argCount; i++)
                    SkipType();
                // Conservative: treat INTERNAL base as Ref (could be either class or valuetype).
                // CLASS-based generics are Ref; VALUETYPE-based and unknown are Other.
                return baseType == (byte)CorElementType.Class ? GcTypeKind.Ref : GcTypeKind.Other;
            }

            case CorElementType.Byref:
                SkipType(); // inner type
                return GcTypeKind.Interior;

            case CorElementType.Ptr:
                SkipType(); // pointee type
                return GcTypeKind.None;

            case CorElementType.FnPtr:
                SkipMethodSignature();
                return GcTypeKind.None;

            case CorElementType.TypedByRef:
                return GcTypeKind.Other;

            case CorElementType.Var:
            case CorElementType.MVar:
                ReadCompressedUInt(); // type parameter index
                // Conservative: generic type params could be GC refs.
                // The runtime resolves these via the generic context.
                // For now, treat as potential GC ref to avoid missing references.
                return GcTypeKind.Ref;

            case CorElementType.CModReqd:
            case CorElementType.CModOpt:
                ReadCompressedUInt(); // TypeDefOrRefOrSpecEncoded
                return ReadTypeAndClassify(); // recurse past the modifier

            case CorElementType.Sentinel:
                return ReadTypeAndClassify(); // skip sentinel, read next type

            case CorElementType.Internal:
                // Runtime-internal type: raw pointer to TypeHandle follows.
                // Skip the pointer bytes. Conservative: treat as potential GC ref.
                _index += _pointerSize;
                return GcTypeKind.Ref;

            default:
                return GcTypeKind.None;
        }
    }

    /// <summary>
    /// Skips over a complete type encoding in the signature.
    /// </summary>
    public void SkipType()
    {
        ReadTypeAndClassify(); // Same traversal, just discard the result
    }

    private void SkipArrayShape()
    {
        _ = ReadCompressedUInt(); // rank
        uint numSizes = ReadCompressedUInt();
        for (uint i = 0; i < numSizes; i++)
            ReadCompressedUInt();
        uint numLoBounds = ReadCompressedUInt();
        for (uint i = 0; i < numLoBounds; i++)
            ReadCompressedUInt(); // lo bounds are signed but encoded as unsigned
    }

    private void SkipMethodSignature()
    {
        byte callingConv = ReadByte();
        if ((callingConv & 0x10) != 0) // GENERIC
            ReadCompressedUInt(); // generic param count
        uint paramCount = ReadCompressedUInt();
        SkipType(); // return type
        for (uint i = 0; i < paramCount; i++)
            SkipType();
    }
}

/// <summary>
/// Classification of a signature type for GC scanning purposes.
/// </summary>
internal enum GcTypeKind
{
    /// <summary>Not a GC reference (primitives, pointers).</summary>
    None,
    /// <summary>Object reference (class, string, array).</summary>
    Ref,
    /// <summary>Interior pointer (byref).</summary>
    Interior,
    /// <summary>Value type that may contain embedded GC references.</summary>
    Other,
}
