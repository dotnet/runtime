// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.NativeFormat;

using CorElementType = System.Reflection.CorElementType;
using MdSigCallingConvention = System.Reflection.MdSigCallingConvention;

namespace Internal.Metadata.NativeFormat;

// Signature blobs follow ECMA-335 II.23.2, except that TypeDefOrRefOrSpecEncoded tokens are replaced with
// NativeFormat handles. SigPointer and SigParser mirror CoreCLR's SigPointer and SigParser (src/coreclr/inc/sigparser.h)
// so that signature parsing can be shared with CoreCLR's System.Private.CoreLib.

// A position within a signature blob. Unlike CoreCLR's SigPointer, which is a SigParser with type loading helpers,
// this only records the position; use SigParser to read from it.
#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly unsafe struct SigPointer
{
    internal readonly byte* _ptr;
    internal readonly uint _len;

    internal SigPointer(byte* ptr, uint len)
    {
        _ptr = ptr;
        _len = len;
    }
}

// A cursor over a signature blob. All reads are bounds-checked against the end of the blob.
#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public unsafe struct SigParser
{
    private byte* _ptr;
    private uint _len;

    public SigParser(SigPointer sig)
        : this(sig._ptr, sig._len)
    {
    }

    internal SigParser(byte* ptr, uint len)
    {
        _ptr = ptr;
        _len = len;
    }

    public readonly SigPointer GetSigPointer() => new SigPointer(_ptr, _len);

    internal readonly byte* GetPtr() => _ptr;

    // Like SigParser::PeekElemType, this skips a vararg sentinel and custom modifiers, and reports STRING and OBJECT as CLASS.
    public readonly CorElementType PeekElemType()
    {
        SigParser parser = this;
        CorElementType elementType = parser.GetElemType();
        return elementType is CorElementType.ELEMENT_TYPE_STRING or CorElementType.ELEMENT_TYPE_OBJECT
            ? CorElementType.ELEMENT_TYPE_CLASS
            : elementType;
    }

    // Like SigParser::GetElemType, this skips a vararg sentinel and custom modifiers.
    public CorElementType GetElemType()
    {
        SkipCustomModifiers();
        return (CorElementType)GetByte();
    }

    // ECMA-335 II.23.2.2.
    public void SkipAnyVASentinel()
    {
        if (PeekByte() == (byte)CorElementType.ELEMENT_TYPE_SENTINEL)
            SkipBytes(1);
    }

    public void SkipCustomModifiers()
    {
        SkipAnyVASentinel();
        while (TryGetCustomModifier(out _, out _))
        {
        }
    }

    // ECMA-335 II.23.2.7.
    public bool TryGetCustomModifier(out bool isOptional, out Handle modifierType)
    {
        CorElementType elementType = (CorElementType)PeekByte();
        if (elementType is not (CorElementType.ELEMENT_TYPE_CMOD_REQD or CorElementType.ELEMENT_TYPE_CMOD_OPT))
        {
            isOptional = false;
            modifierType = default;
            return false;
        }

        SkipBytes(1);
        isOptional = elementType == CorElementType.ELEMENT_TYPE_CMOD_OPT;
        modifierType = GetToken();
        return true;
    }

    // ECMA-335 II.23.2.
    public int GetData()
    {
        byte first = GetByte();
        if ((first & 0x80) == 0)
            return first;
        if ((first & 0x40) == 0)
            return ((first & 0x3f) << 8) | GetByte();
        if ((first & 0x20) == 0)
            return ((first & 0x1f) << 24) | (GetByte() << 16) | (GetByte() << 8) | GetByte();

        ThrowBadImageFormatException();
        return 0;
    }

    // Reads the NativeFormat handle that replaces an ECMA-335 TypeDefOrRefOrSpecEncoded token.
    public Handle GetToken()
    {
        byte* ptr = _ptr;
        uint rawValue = NativePrimitiveDecoder.DecodeUnsigned(ref ptr, _ptr + _len);
        SkipBytes((uint)(ptr - _ptr));
        return MdBinaryReader.DecodeHandle(rawValue);
    }

    public void SkipExactlyOne()
    {
        switch (GetElemType())
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
                GetToken();
                break;
            case CorElementType.ELEMENT_TYPE_PTR:
            case CorElementType.ELEMENT_TYPE_BYREF:
            case CorElementType.ELEMENT_TYPE_SZARRAY:
                SkipExactlyOne();
                break;
            case CorElementType.ELEMENT_TYPE_VAR:
            case CorElementType.ELEMENT_TYPE_MVAR:
                GetData();
                break;
            case CorElementType.ELEMENT_TYPE_GENERICINST:
                SkipExactlyOne();
                int arguments = GetData();
                for (int i = 0; i < arguments; i++)
                    SkipExactlyOne();
                break;
            case CorElementType.ELEMENT_TYPE_ARRAY:
                SkipExactlyOne();
                GetArrayShape();
                break;
            case CorElementType.ELEMENT_TYPE_FNPTR:
                SkipSignature();
                break;
            default:
                ThrowBadImageFormatException();
                break;
        }
    }

    // Skips a method signature, as follows ELEMENT_TYPE_FNPTR.
    internal void SkipSignature()
    {
        SkipMethodHeaderSignature(out int argCount);
        for (int i = 0; i < argCount; i++)
            SkipExactlyOne();
    }

    // ECMA-335 II.23.2.1-3.
    internal void SkipMethodHeaderSignature(out int argCount, bool skipReturnType = true)
    {
        GetMethodHeaderSignature(out _, out argCount);

        if (skipReturnType)
            SkipExactlyOne();
    }

    // Reads the method header up to (but not including) the return type.
    internal MdSigCallingConvention GetMethodHeaderSignature(out int genericParameterCount, out int argCount)
    {
        MdSigCallingConvention callConv = GetCallingConvInfo();
        if (callConv is MdSigCallingConvention.Field or MdSigCallingConvention.LocalSig)
            ThrowBadImageFormatException();

        genericParameterCount = (callConv & MdSigCallingConvention.Generic) != 0 ? GetData() : 0;
        argCount = GetData();
        return callConv;
    }

    // ECMA-335 II.23.2.13. Returns the rank; sizes and lower bounds are skipped.
    // Like CoreCLR's SigParser::SkipExactlyOne, sizes and lower bounds are only present for a nonzero rank.
    public int GetArrayShape()
    {
        int rank = GetData();
        if (rank != 0)
        {
            int sizes = GetData();
            for (int i = 0; i < sizes; i++)
                GetData();

            // Signed lower bounds use the same length encoding as unsigned integers.
            int lowerBounds = GetData();
            for (int i = 0; i < lowerBounds; i++)
                GetData();
        }
        return rank;
    }

    internal MdSigCallingConvention GetCallingConvInfo() => (MdSigCallingConvention)GetByte();

    internal MdSigCallingConvention GetCallingConv() => GetCallingConvInfo() & MdSigCallingConvention.CallConvMask;

    private readonly byte PeekByte()
    {
        if (_len == 0)
            ThrowBadImageFormatException();
        return *_ptr;
    }

    private byte GetByte()
    {
        byte value = PeekByte();
        _ptr++;
        _len--;
        return value;
    }

    private void SkipBytes(uint count)
    {
        if (count > _len)
            ThrowBadImageFormatException();
        _ptr += count;
        _len -= count;
    }

    [DoesNotReturn]
    internal static void ThrowBadImageFormatException() => throw new BadImageFormatException();
}

#if SYSTEM_PRIVATE_CORELIB
[CLSCompliant(false)]
#endif
public readonly struct SigPointerCollection
{
    private readonly SigPointer _first;
    public int Count { get; }

    internal SigPointerCollection(SigPointer first, int count) => (_first, Count) = (first, count);

    public Enumerator GetEnumerator() => new Enumerator(_first, Count);

    public struct Enumerator
    {
        private SigPointer _next;
        private int _remaining;
        public SigPointer Current { get; private set; }

        internal Enumerator(SigPointer first, int count)
        {
            _next = first;
            _remaining = count;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_remaining == 0)
                return false;
            Current = _next;
            SigParser parser = new SigParser(_next);
            parser.SkipExactlyOne();
            _next = parser.GetSigPointer();
            _remaining--;
            return true;
        }
    }
}

internal readonly struct MethodSignatureInfo
{
    public MdSigCallingConvention CallingConvention { get; }
    public int GenericParameterCount { get; }
    public int ParameterCount { get; }
    public SigPointer ReturnType { get; }

    public SigPointerCollection Parameters
    {
        get
        {
            SigParser parser = new SigParser(ReturnType);
            parser.SkipExactlyOne();
            return new SigPointerCollection(parser.GetSigPointer(), ParameterCount);
        }
    }

    public MethodSignatureInfo(SigParser parser)
    {
        CallingConvention = parser.GetMethodHeaderSignature(out int genericParameterCount, out int parameterCount);
        GenericParameterCount = genericParameterCount;
        ParameterCount = parameterCount;
        ReturnType = parser.GetSigPointer();
    }
}

public readonly partial struct MethodSignature
{
    internal MethodSignatureInfo Info => new MethodSignatureInfo(new SigParser(Blob.GetSigPointer()));
    internal MdSigCallingConvention CallingConvention => Info.CallingConvention;
    public int GenericParameterCount => Info.GenericParameterCount;
    public int ParameterCount => Info.ParameterCount;
    public SigPointer ReturnType => Info.ReturnType;
    public SigPointerCollection Parameters => Info.Parameters;
}

public readonly partial struct FieldSignature
{
    public SigPointer Type
    {
        get
        {
            SigParser parser = new SigParser(Blob.GetSigPointer());
            if (parser.GetCallingConvInfo() != MdSigCallingConvention.Field)
                SigParser.ThrowBadImageFormatException();
            return parser.GetSigPointer();
        }
    }
}

public readonly partial struct PropertySignature
{
    private MethodSignatureInfo Info
    {
        get
        {
            MethodSignatureInfo info = new MethodSignatureInfo(new SigParser(Blob.GetSigPointer()));
            if ((info.CallingConvention & MdSigCallingConvention.CallConvMask) != MdSigCallingConvention.Property)
                SigParser.ThrowBadImageFormatException();
            return info;
        }
    }

    public SigPointer Type => Info.ReturnType;
    public SigPointerCollection Parameters => Info.Parameters;
}

public readonly partial struct TypeSpecification
{
    public SigPointer Signature => Blob.GetSigPointer();
}

public readonly partial struct ByteCollection
{
    // NativeFormat metadata is always backed by unmanaged memory that doesn't move, so a blob can be addressed
    // directly, like CoreCLR's PCCOR_SIGNATURE.
    internal unsafe SigPointer GetSigPointer()
    {
        ReadOnlySpan<byte> bytes = AsSpan();
        return new SigPointer((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)), (uint)bytes.Length);
    }
}
