// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Metadata.NativeFormat.Writer;
using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;
using SignatureTypeCode = Internal.Metadata.NativeFormat.SignatureTypeCode;

namespace ILCompiler.Metadata;

internal partial class Transform<TPolicy>
{
    private void WriteMethodSignature(MetadataBlob blob, Cts.MethodSignature signature)
    {
        WriteMethodHeader(blob, (byte)GetSignatureCallingConvention(signature), signature.GenericParameterCount, signature.Length);
        WriteTypeSignature(blob, signature.ReturnType);
        for (int i = 0; i < signature.Length; i++)
            WriteTypeSignature(blob, signature[i]);
    }

    private void WriteMethodSignature(MetadataBlob blob, Cts.Ecma.EcmaModule module, ref Ecma.BlobReader reader)
    {
        Ecma.SignatureHeader header = reader.ReadSignatureHeader();
        int arity = header.IsGeneric ? reader.ReadCompressedInteger() : 0;
        int count = reader.ReadCompressedInteger();
        WriteMethodHeader(blob, (byte)GetSignatureCallingConvention(header), arity, count);
        WriteTypeSignature(blob, module, ref reader);
        for (int i = 0; i < count; i++)
            WriteTypeSignature(blob, module, ref reader);
    }

    private static void WriteMethodHeader(MetadataBlob blob, byte convention, int arity, int count)
    {
        blob.WriteByte((byte)(convention | (arity != 0 ? 0x10 : 0)));
        if (arity != 0)
            blob.WriteCompressedInteger(arity);
        blob.WriteCompressedInteger(count);
    }

    private void WriteTypeSignature(MetadataBlob blob, Cts.TypeDesc type)
    {
        // ECMA-335 II.23.2.12. Compound expressions are inline; only named types relocate.
        SignatureTypeCode code;
        switch (type.Category)
        {
            case Cts.TypeFlags.Void: code = SignatureTypeCode.Void; break;
            case Cts.TypeFlags.Boolean: code = SignatureTypeCode.Boolean; break;
            case Cts.TypeFlags.Char: code = SignatureTypeCode.Char; break;
            case Cts.TypeFlags.SByte: code = SignatureTypeCode.SByte; break;
            case Cts.TypeFlags.Byte: code = SignatureTypeCode.Byte; break;
            case Cts.TypeFlags.Int16: code = SignatureTypeCode.Int16; break;
            case Cts.TypeFlags.UInt16: code = SignatureTypeCode.UInt16; break;
            case Cts.TypeFlags.Int32: code = SignatureTypeCode.Int32; break;
            case Cts.TypeFlags.UInt32: code = SignatureTypeCode.UInt32; break;
            case Cts.TypeFlags.Int64: code = SignatureTypeCode.Int64; break;
            case Cts.TypeFlags.UInt64: code = SignatureTypeCode.UInt64; break;
            case Cts.TypeFlags.Single: code = SignatureTypeCode.Single; break;
            case Cts.TypeFlags.Double: code = SignatureTypeCode.Double; break;
            case Cts.TypeFlags.IntPtr: code = SignatureTypeCode.IntPtr; break;
            case Cts.TypeFlags.UIntPtr: code = SignatureTypeCode.UIntPtr; break;
            case Cts.TypeFlags.SzArray:
            case Cts.TypeFlags.Array:
            case Cts.TypeFlags.ByRef:
            case Cts.TypeFlags.Pointer:
                code = type.Category switch
                {
                    Cts.TypeFlags.SzArray => SignatureTypeCode.SZArray,
                    Cts.TypeFlags.Array => SignatureTypeCode.Array,
                    Cts.TypeFlags.ByRef => SignatureTypeCode.ByReference,
                    _ => SignatureTypeCode.Pointer,
                };
                blob.WriteByte((byte)code);
                WriteTypeSignature(blob, ((Cts.ParameterizedType)type).ParameterType);
                if (type is Cts.ArrayType array && !array.IsSzArray)
                {
                    blob.WriteCompressedInteger(array.Rank);
                    blob.WriteCompressedInteger(0);
                    blob.WriteCompressedInteger(0);
                }
                return;
            case Cts.TypeFlags.FunctionPointer:
                blob.WriteByte((byte)SignatureTypeCode.FunctionPointer);
                WriteMethodSignature(blob, ((Cts.FunctionPointerType)type).Signature);
                return;
            case Cts.TypeFlags.SignatureTypeVariable:
            case Cts.TypeFlags.SignatureMethodVariable:
                blob.WriteByte((byte)(type.Category == Cts.TypeFlags.SignatureTypeVariable
                    ? SignatureTypeCode.GenericTypeParameter : SignatureTypeCode.GenericMethodParameter));
                blob.WriteCompressedInteger(((Cts.SignatureVariable)type).Index);
                return;
            case Cts.TypeFlags.GenericParameter:
                Cts.GenericParameterDesc parameter = (Cts.GenericParameterDesc)type;
                blob.WriteByte((byte)(parameter.Kind == Cts.GenericParameterKind.Type
                    ? SignatureTypeCode.GenericTypeParameter : SignatureTypeCode.GenericMethodParameter));
                blob.WriteCompressedInteger(parameter.Index);
                return;
            default:
                if (type.IsString)
                    code = SignatureTypeCode.String;
                else if (type.IsObject)
                    code = SignatureTypeCode.Object;
                else if (type == type.Context.GetWellKnownType(Cts.WellKnownType.TypedReference))
                    code = SignatureTypeCode.TypedReference;
                else if (type.HasInstantiation && !type.IsGenericDefinition)
                {
                    blob.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
                    WriteTypeSignature(blob, type.GetTypeDefinition());
                    blob.WriteCompressedInteger(type.Instantiation.Length);
                    foreach (Cts.TypeDesc argument in type.Instantiation)
                        WriteTypeSignature(blob, argument);
                    return;
                }
                else
                {
                    blob.WriteByte((byte)(type.IsValueType ? SignatureTypeCode.ValueType : SignatureTypeCode.Class));
                    blob.WriteType(HandleType(type));
                    return;
                }
                break;
        }
        blob.WriteByte((byte)code);
    }

    private void WriteTypeSignature(MetadataBlob blob, Cts.Ecma.EcmaModule module, ref Ecma.BlobReader reader)
    {
        Ecma.SignatureTypeCode code = reader.ReadSignatureTypeCode();
        switch (code)
        {
            case Ecma.SignatureTypeCode.TypeHandle:
                WriteTypeSignature(blob, module.GetType(reader.ReadTypeHandle()));
                return;
            case Ecma.SignatureTypeCode.OptionalModifier:
            case Ecma.SignatureTypeCode.RequiredModifier:
                blob.WriteByte((byte)code);
                blob.WriteType(HandleType(module.GetType(reader.ReadTypeHandle())));
                WriteTypeSignature(blob, module, ref reader);
                return;
            case Ecma.SignatureTypeCode.Pointer:
            case Ecma.SignatureTypeCode.ByReference:
            case Ecma.SignatureTypeCode.SZArray:
                blob.WriteByte((byte)code);
                WriteTypeSignature(blob, module, ref reader);
                return;
            case Ecma.SignatureTypeCode.Array:
                blob.WriteByte((byte)code);
                WriteTypeSignature(blob, module, ref reader);
                blob.WriteCompressedInteger(reader.ReadCompressedInteger());
                int sizes = reader.ReadCompressedInteger();
                for (int i = 0; i < sizes; i++)
                    reader.ReadCompressedInteger();
                int bounds = reader.ReadCompressedInteger();
                for (int i = 0; i < bounds; i++)
                    reader.ReadCompressedSignedInteger();
                // Preserve the existing rank-only normalization of array shapes.
                blob.WriteCompressedInteger(0);
                blob.WriteCompressedInteger(0);
                return;
            case Ecma.SignatureTypeCode.GenericTypeParameter:
            case Ecma.SignatureTypeCode.GenericMethodParameter:
                blob.WriteByte((byte)code);
                blob.WriteCompressedInteger(reader.ReadCompressedInteger());
                return;
            case Ecma.SignatureTypeCode.GenericTypeInstance:
                blob.WriteByte((byte)code);
                WriteTypeSignature(blob, module, ref reader);
                int count = reader.ReadCompressedInteger();
                blob.WriteCompressedInteger(count);
                for (int i = 0; i < count; i++)
                    WriteTypeSignature(blob, module, ref reader);
                return;
            case Ecma.SignatureTypeCode.FunctionPointer:
                blob.WriteByte((byte)code);
                WriteMethodSignature(blob, module, ref reader);
                return;
            default:
                if (code is < Ecma.SignatureTypeCode.Void or > Ecma.SignatureTypeCode.Object)
                    throw new BadImageFormatException();
                blob.WriteByte((byte)code);
                return;
        }
    }
}
