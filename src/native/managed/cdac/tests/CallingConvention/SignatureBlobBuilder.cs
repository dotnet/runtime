// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Builds a method signature blob (ECMA-335 §II.23.2.1) for tests. Supports
/// primitive element types and the cDAC-extension <c>ELEMENT_TYPE_INTERNAL</c>
/// (0x21) for referencing mock value-type method tables directly without a
/// metadata reader.
/// </summary>
internal sealed class SignatureBlobBuilder
{
    private const byte HASTHIS_FLAG = 0x20;
    private const byte VARARG_CC = 0x05; // IMAGE_CEE_CS_CALLCONV_VARARG
    private const byte ELEMENT_TYPE_INTERNAL = 0x21;

    private readonly bool _hasThis;
    private readonly int _pointerSize;
    private bool _isVarArg;
    private readonly List<ParamSpec> _params = new();
    private ParamSpec _return = new(CorElementType.Void, default);

    public SignatureBlobBuilder(int pointerSize, bool hasThis = false)
    {
        _pointerSize = pointerSize;
        _hasThis = hasThis;
    }

    public SignatureBlobBuilder VarArg()
    {
        _isVarArg = true;
        return this;
    }

    public SignatureBlobBuilder Return(CorElementType primitive)
    {
        _return = new ParamSpec(primitive, default);
        return this;
    }

    public SignatureBlobBuilder ReturnValueType(TargetPointer methodTablePtr)
    {
        _return = new ParamSpec(CorElementType.ValueType, methodTablePtr);
        return this;
    }

    public SignatureBlobBuilder Param(CorElementType primitive)
    {
        _params.Add(new ParamSpec(primitive, default));
        return this;
    }

    public SignatureBlobBuilder ParamValueType(TargetPointer methodTablePtr)
    {
        _params.Add(new ParamSpec(CorElementType.ValueType, methodTablePtr));
        return this;
    }

    /// <summary>
    /// Adds a parameter of <c>ELEMENT_TYPE_CLASS</c> with a dummy TypeDef token.
    /// The token is encoded per ECMA-335 §II.23.2.8 (TypeDefOrRefOrSpecEncoded).
    /// The cDAC signature decoder resolves this via the Loader's lookup tables;
    /// when those aren't populated (as in most tests), it falls back to a
    /// pointer-sized <c>CorElementType.Class</c> placeholder -- which is the
    /// correct calling-convention shape for any managed reference type.
    /// </summary>
    public SignatureBlobBuilder ParamClass()
    {
        _params.Add(new ParamSpec(CorElementType.Class, default, true));
        return this;
    }

    public byte[] Build()
    {
        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);

        byte callingConvention = (byte)((_hasThis ? HASTHIS_FLAG : 0) | (_isVarArg ? VARARG_CC : 0));
        bw.Write(callingConvention);
        WriteCompressedUInt(bw, (uint)_params.Count);
        WriteParam(bw, _return);
        foreach (ParamSpec p in _params)
            WriteParam(bw, p);

        return ms.ToArray();
    }

    private void WriteParam(BinaryWriter bw, ParamSpec p)
    {
        if (p.Type == CorElementType.ValueType && p.TypeHandle != TargetPointer.Null)
        {
            // Use ELEMENT_TYPE_INTERNAL + raw TypeHandle pointer to bypass metadata.
            bw.Write(ELEMENT_TYPE_INTERNAL);
            if (_pointerSize == 8)
                bw.Write(p.TypeHandle.Value);
            else
                bw.Write((uint)p.TypeHandle.Value);
            return;
        }
        if (p.IsDummyClassToken)
        {
            // ELEMENT_TYPE_CLASS followed by a TypeDefOrRefOrSpecEncoded token.
            // We use TypeDef row 1 (the <Module> pseudo-type) encoded as (1 << 2 | 0) = 4.
            bw.Write((byte)CorElementType.Class);
            WriteCompressedUInt(bw, 4); // TypeDef row 1
            return;
        }
        bw.Write((byte)p.Type);
    }

    private static void WriteCompressedUInt(BinaryWriter bw, uint value)
    {
        // ECMA-335 §II.23.2 compressed unsigned int
        if (value < 0x80)
        {
            bw.Write((byte)value);
        }
        else if (value < 0x4000)
        {
            bw.Write((byte)((value >> 8) | 0x80));
            bw.Write((byte)(value & 0xFF));
        }
        else
        {
            bw.Write((byte)((value >> 24) | 0xC0));
            bw.Write((byte)((value >> 16) & 0xFF));
            bw.Write((byte)((value >> 8) & 0xFF));
            bw.Write((byte)(value & 0xFF));
        }
    }

    private readonly record struct ParamSpec(CorElementType Type, TargetPointer TypeHandle, bool IsDummyClassToken = false);
}
