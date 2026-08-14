// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private const byte NativeTypeVoid = 0x01;
    private const byte NativeTypeSysChar = 0x0D;
    private const byte NativeTypeVariant = 0x0E;
    private const byte NativeTypePointer = 0x10;
    private const byte NativeTypeDecimal = 0x11;
    private const byte NativeTypeDate = 0x12;
    private const byte NativeTypeObjectReference = 0x18;
    private const byte NativeTypeNestedStruct = 0x21;
    private const byte NativeTypeMax = 0x50;

    internal MarshallingDescriptorValue CreateEmptyMarshallingDescriptor()
        => MarshallingDescriptorValue.Empty;

    internal MarshallingDescriptorValue CompleteMarshalClause(
        MarshallingDescriptorValue value)
        => value;

    internal void SetMarshalBlobNativeType(
        CILParser.MarshalBlobBuilder builder,
        NativeTypeValue value)
        => builder.NativeType = value;

    internal void AddMarshalBlobByte(CILParser.MarshalBlobBuilder builder, byte value)
        => (builder.RawBytes ??= new BlobBuilder()).WriteByte(value);

    internal MarshallingDescriptorValue CreateMarshallingDescriptor(
        CILParser.MarshalBlobBuilder builder)
        => new(builder.RawBytes, builder.NativeType);

    internal void SetNativeTypeElement(
        CILParser.NativeTypeBuilder builder,
        NativeTypeElementValue value)
        => builder.Element = value;

    internal void AddNativeTypeArrayPointerInfo(
        CILParser.NativeTypeBuilder builder,
        NativeTypeArrayPointerInfoValue value)
        => (builder.ArrayPointerInfo ??= new List<NativeTypeArrayPointerInfoValue>())
            .Add(value);

    internal NativeTypeValue CreateNativeType(
        IToken token,
        CILParser.NativeTypeBuilder builder)
        => new(
            token,
            builder.Element,
            builder.ArrayPointerInfo?.ToImmutableArray() ?? []);

    internal NativeTypeArrayPointerInfoValue CreatePointerNativeType()
        => new NativeTypeArrayPointerInfoValue(NativeTypeArrayPointerInfoKind.Pointer);

    internal NativeTypeArrayPointerInfoValue CreatePointerArrayTypeNoSizeData()
        => new NativeTypeArrayPointerInfoValue(NativeTypeArrayPointerInfoKind.ArrayNoSizeData);

    internal NativeTypeArrayPointerInfoValue CreatePointerArrayTypeSize(IToken size)
        => new NativeTypeArrayPointerInfoValue(NativeTypeArrayPointerInfoKind.ArraySize, Size: size);

    internal NativeTypeArrayPointerInfoValue CreatePointerArrayTypeSizeParamIndex(
        IToken size,
        IToken parameterIndex)
        => new NativeTypeArrayPointerInfoValue(
            NativeTypeArrayPointerInfoKind.ArraySizeParamIndex,
            size,
            parameterIndex);

    internal NativeTypeArrayPointerInfoValue CreatePointerArrayTypeParamIndex(
        IToken parameterIndex)
        => new NativeTypeArrayPointerInfoValue(
            NativeTypeArrayPointerInfoKind.ArrayParamIndex,
            ParameterIndex: parameterIndex);

    internal NativeTypeElementValue CreateEmptyNativeType()
        => EmptyNativeTypeElementValue.Instance;

    internal NativeTypeElementValue CreateDeprecatedCustomMarshallerNativeType(
        CILParser.NativeTypeElementContext context,
        string guid,
        string nativeTypeName,
        string marshallerType,
        string cookie)
        => new CustomMarshallerNativeTypeElementValue(
            context.Start,
            guid,
            nativeTypeName,
            marshallerType,
            cookie);

    internal NativeTypeElementValue CreateCustomMarshallerNativeType(
        string marshallerType,
        string cookie)
        => new CustomMarshallerNativeTypeElementValue(
            null,
            null,
            null,
            marshallerType,
            cookie);

    internal NativeTypeElementValue CreateFixedSysStringNativeType(IToken size)
        => new FixedSysStringNativeTypeElementValue(size);

    internal NativeTypeElementValue CreateFixedArrayNativeType(
        IToken size,
        NativeTypeValue element)
        => new FixedArrayNativeTypeElementValue(size, element);

    internal NativeTypeElementValue CreateDeprecatedNativeType(
        CILParser.NativeTypeElementContext context,
        IToken nativeType)
        => new DeprecatedNativeTypeElementValue(context.Start, nativeType.Type);

    internal NativeTypeElementValue CreateSimpleNativeType(IToken nativeType)
        => new SimpleNativeTypeElementValue(nativeType.Type);

    internal NativeTypeElementValue CreateIidNativeType(
        IToken nativeType,
        IidParamIndexValue index)
        => new IidNativeTypeElementValue(nativeType.Type, index);

    internal NativeTypeElementValue CreateSafeArrayNativeType(
        VariantTypeValue variantType,
        string? userDefinedType)
        => new SafeArrayNativeTypeElementValue(variantType, userDefinedType);

    internal NativeTypeElementValue CreateUnsignedNativeType(IToken nativeType)
        => new UnsignedNativeTypeElementValue(nativeType.Type);

    internal NativeTypeElementValue CreateNestedStructNativeType(
        CILParser.NativeTypeElementContext context)
        => new NestedStructNativeTypeElementValue(context.Start);

    internal NativeTypeElementValue CreateAnsiBstrNativeType()
        => AnsiBstrNativeTypeElementValue.Instance;

    internal NativeTypeElementValue CreateVariantBoolNativeType()
        => VariantBoolNativeTypeElementValue.Instance;

    internal NativeTypeElementValue CreateNativeTypeTypedef(
        CILParser.NativeTypeElementContext context,
        string alias)
        => new NativeTypeTypedefValue(context.Start, alias);

    internal IidParamIndexValue GetIidParamIndex(IToken index)
        => new(index);

    internal void SetVariantTypeElement(
        CILParser.VariantTypeBuilder builder,
        VariantTypeElementValue value)
        => builder.Element = value;

    internal void AddVariantTypeModifier(
        CILParser.VariantTypeBuilder builder,
        IToken modifier)
    {
        builder.Modifiers |= modifier.Type switch
        {
            CILParser.ARRAY_TYPE_NO_BOUNDS => VarEnum.VT_ARRAY,
            CILParser.VECTOR => VarEnum.VT_VECTOR,
            CILParser.REF => VarEnum.VT_BYREF,
            _ => throw new UnreachableException()
        };
    }

    internal VariantTypeValue CreateVariantType(CILParser.VariantTypeBuilder builder)
        => new(builder.Element, builder.Modifiers);

    internal VariantTypeElementValue GetVariantTypeElement(IToken variantType)
        => new VariantTypeElementValue(variantType.Type);

    private BlobBuilder MaterializeMarshallingDescriptor(MarshallingDescriptorValue? value)
    {
        if (value?.RawBytes is BlobBuilder rawBytes)
        {
            return rawBytes;
        }

        return MaterializeNativeType(value?.NativeType ?? NativeTypeValue.Empty);
    }

    private BlobBuilder MaterializeNativeType(NativeTypeValue value)
    {
        if (value.Element is null)
        {
            return new BlobBuilder();
        }

        BlobBuilder element = MaterializeNativeTypeElement(value.Element);
        if (value.ArrayPointerInfo.IsDefaultOrEmpty)
        {
            return element;
        }

        BlobBuilder prefix = new(value.ArrayPointerInfo.Length);
        BlobBuilder suffix = new();

        for (int i = value.ArrayPointerInfo.Length - 1; i >= 0; i--)
        {
            NativeTypeArrayPointerInfoValue info = value.ArrayPointerInfo[i];
            if (info.Kind == NativeTypeArrayPointerInfoKind.Pointer)
            {
                if (value.Token is IToken token)
                {
                    ReportWarning(
                        DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "pointer in array"),
                        token);
                }
                prefix.WriteByte(NativeTypePointer);
            }
            else
            {
                prefix.WriteByte((byte)UnmanagedType.LPArray);
                if (element.Count == 0)
                {
                    element.WriteByte(NativeTypeMax);
                }
            }
        }

        foreach (NativeTypeArrayPointerInfoValue info in value.ArrayPointerInfo)
        {
            switch (info.Kind)
            {
                case NativeTypeArrayPointerInfoKind.ArraySize:
                    suffix.WriteCompressedInteger(0);
                    suffix.WriteCompressedInteger(ParseMarshallingInt32(info.Size));
                    suffix.WriteCompressedInteger(0);
                    break;
                case NativeTypeArrayPointerInfoKind.ArraySizeParamIndex:
                    suffix.WriteCompressedInteger(ParseMarshallingInt32(info.ParameterIndex));
                    suffix.WriteCompressedInteger(ParseMarshallingInt32(info.Size));
                    suffix.WriteCompressedInteger(1);
                    break;
                case NativeTypeArrayPointerInfoKind.ArrayParamIndex:
                    suffix.WriteCompressedInteger(ParseMarshallingInt32(info.ParameterIndex));
                    break;
            }
        }

        prefix.LinkSuffix(element);
        prefix.LinkSuffix(suffix);
        return prefix;
    }

    private BlobBuilder MaterializeNativeTypeElement(NativeTypeElementValue value)
    {
        switch (value)
        {
            case EmptyNativeTypeElementValue:
                return new BlobBuilder();
            case CustomMarshallerNativeTypeElementValue customMarshaller:
                return MaterializeCustomMarshallerNativeType(customMarshaller);
            case FixedSysStringNativeTypeElementValue fixedSysString:
                {
                    BlobBuilder blob = CreateNativeTypeBlob(UnmanagedType.ByValTStr);
                    blob.WriteCompressedInteger(ParseInt32(fixedSysString.Size));
                    return blob;
                }
            case FixedArrayNativeTypeElementValue fixedArray:
                {
                    BlobBuilder blob = CreateNativeTypeBlob(UnmanagedType.ByValArray);
                    blob.WriteCompressedInteger(ParseInt32(fixedArray.Size));
                    MaterializeNativeType(fixedArray.Element).WriteContentTo(blob);
                    return blob;
                }
            case DeprecatedNativeTypeElementValue deprecated:
                return MaterializeDeprecatedNativeType(deprecated);
            case SimpleNativeTypeElementValue simple:
                return CreateNativeTypeBlob(GetSimpleNativeType(simple.TokenType));
            case IidNativeTypeElementValue iid:
                return MaterializeIidNativeType(iid);
            case SafeArrayNativeTypeElementValue safeArray:
                return MaterializeSafeArrayNativeType(safeArray);
            case UnsignedNativeTypeElementValue unsigned:
                return CreateNativeTypeBlob(GetUnsignedNativeType(unsigned.TokenType));
            case NestedStructNativeTypeElementValue nestedStruct:
                ReportWarning(
                    DiagnosticIds.DeprecatedNativeType,
                    string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "NESTEDSTRUCT"),
                    nestedStruct.Token);
                return CreateNativeTypeBlob(NativeTypeNestedStruct);
            case AnsiBstrNativeTypeElementValue:
#pragma warning disable CS0618 // Preserve the legacy IL native type spelling.
                return CreateNativeTypeBlob(UnmanagedType.AnsiBStr);
#pragma warning restore CS0618
            case VariantBoolNativeTypeElementValue:
                return CreateNativeTypeBlob(UnmanagedType.VariantBool);
            case NativeTypeTypedefValue typedef:
                ReportError(
                    DiagnosticIds.TypedefNotFound,
                    string.Format(DiagnosticMessageTemplates.TypedefNotFound, typedef.Alias),
                    typedef.Token);
                return new BlobBuilder();
            default:
                throw new UnreachableException();
        }
    }

    private BlobBuilder MaterializeCustomMarshallerNativeType(
        CustomMarshallerNativeTypeElementValue customMarshaller)
    {
        BlobBuilder blob = CreateNativeTypeBlob(UnmanagedType.CustomMarshaler);
        if (customMarshaller.Guid is not null)
        {
            if (customMarshaller.Token is IToken token)
            {
                ReportWarning(
                    DiagnosticIds.DeprecatedCustomMarshaller,
                    DiagnosticMessageTemplates.DeprecatedCustomMarshaller,
                    token);
            }
            blob.WriteSerializedString(customMarshaller.Guid);
            blob.WriteSerializedString(customMarshaller.NativeTypeName);
        }
        else
        {
            blob.WriteCompressedInteger(0);
            blob.WriteCompressedInteger(0);
        }

        blob.WriteSerializedString(customMarshaller.MarshallerType);
        blob.WriteSerializedString(customMarshaller.Cookie);
        return blob;
    }

    private BlobBuilder MaterializeDeprecatedNativeType(DeprecatedNativeTypeElementValue deprecated)
    {
        (byte value, string name) = deprecated.TokenType switch
        {
            CILParser.VARIANT => (NativeTypeVariant, "VARIANT"),
            CILParser.SYSCHAR => (NativeTypeSysChar, "SYSCHAR"),
            CILParser.VOID => (NativeTypeVoid, "VOID"),
            CILParser.DECIMAL => (NativeTypeDecimal, "DECIMAL"),
            CILParser.DATE => (NativeTypeDate, "DATE"),
            CILParser.OBJECTREF => (NativeTypeObjectReference, "OBJECTREF"),
            _ => throw new UnreachableException()
        };

        ReportWarning(
            DiagnosticIds.DeprecatedNativeType,
            string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, name),
            deprecated.Token);
        return CreateNativeTypeBlob(value);
    }

    private BlobBuilder MaterializeIidNativeType(IidNativeTypeElementValue iid)
    {
        UnmanagedType nativeType = iid.TokenType switch
        {
            CILParser.IUNKNOWN => UnmanagedType.IUnknown,
            CILParser.IDISPATCH => UnmanagedType.IDispatch,
            CILParser.INTERFACE => UnmanagedType.Interface,
            _ => throw new UnreachableException()
        };

        BlobBuilder blob = CreateNativeTypeBlob(nativeType);
        if (MaterializeIidParamIndex(iid.IidParamIndex) is int parameterIndex)
        {
            blob.WriteCompressedInteger(parameterIndex);
        }
        return blob;
    }

    private BlobBuilder MaterializeSafeArrayNativeType(SafeArrayNativeTypeElementValue safeArray)
    {
        BlobBuilder blob = CreateNativeTypeBlob(UnmanagedType.SafeArray);
        blob.WriteCompressedInteger((int)MaterializeVariantType(safeArray.VariantType));
        if (safeArray.UserDefinedType is null)
        {
            blob.WriteCompressedInteger(0);
        }
        else
        {
            blob.WriteSerializedString(safeArray.UserDefinedType);
        }
        return blob;
    }

    private int? MaterializeIidParamIndex(IidParamIndexValue value)
        => value.Index is null ? null : ParseInt32(value.Index);

    private VarEnum MaterializeVariantType(VariantTypeValue value)
        => value.Element is null
            ? VarEnum.VT_EMPTY
            : MaterializeVariantTypeElement(value.Element) | value.Modifiers;

    private static VarEnum MaterializeVariantTypeElement(VariantTypeElementValue value)
        => value.TokenType switch
        {
            CILParser.NULL => VarEnum.VT_EMPTY,
            CILParser.VARIANT => VarEnum.VT_VARIANT,
            CILParser.CURRENCY => VarEnum.VT_CY,
            CILParser.VOID => VarEnum.VT_VOID,
            CILParser.BOOL => VarEnum.VT_BOOL,
            CILParser.INT8 => VarEnum.VT_I1,
            CILParser.INT16 => VarEnum.VT_I2,
            CILParser.INT32_ => VarEnum.VT_I4,
            CILParser.INT64_ => VarEnum.VT_I8,
            CILParser.FLOAT32 => VarEnum.VT_R4,
            CILParser.FLOAT64_ => VarEnum.VT_R8,
            CILParser.UINT8 => VarEnum.VT_UI1,
            CILParser.UINT16 => VarEnum.VT_UI2,
            CILParser.UINT32 => VarEnum.VT_UI4,
            CILParser.UINT64 => VarEnum.VT_UI8,
            CILParser.PTR => VarEnum.VT_PTR,
            CILParser.DECIMAL => VarEnum.VT_DECIMAL,
            CILParser.DATE => VarEnum.VT_DATE,
            CILParser.BSTR => VarEnum.VT_BSTR,
            CILParser.LPSTR => VarEnum.VT_LPSTR,
            CILParser.LPWSTR => VarEnum.VT_LPWSTR,
            CILParser.IUNKNOWN => VarEnum.VT_UNKNOWN,
            CILParser.IDISPATCH => VarEnum.VT_DISPATCH,
            CILParser.SAFEARRAY => VarEnum.VT_SAFEARRAY,
            CILParser.INT => VarEnum.VT_INT,
            CILParser.UINT => VarEnum.VT_UINT,
            CILParser.ERROR => VarEnum.VT_ERROR,
            CILParser.HRESULT => VarEnum.VT_HRESULT,
            CILParser.CARRAY => VarEnum.VT_CARRAY,
            CILParser.USERDEFINED => VarEnum.VT_USERDEFINED,
            CILParser.RECORD => VarEnum.VT_RECORD,
            CILParser.FILETIME => VarEnum.VT_FILETIME,
            CILParser.BLOB => VarEnum.VT_BLOB,
            CILParser.STREAM => VarEnum.VT_STREAM,
            CILParser.STORAGE => VarEnum.VT_STORAGE,
            CILParser.STREAMED_OBJECT => VarEnum.VT_STREAMED_OBJECT,
            CILParser.STORED_OBJECT => VarEnum.VT_STORED_OBJECT,
            CILParser.BLOB_OBJECT => VarEnum.VT_BLOB_OBJECT,
            CILParser.CF => VarEnum.VT_CF,
            CILParser.CLSID => VarEnum.VT_CLSID,
            TokenConstants.InvalidType => VarEnum.VT_EMPTY,
            _ => throw new UnreachableException()
        };

    private static UnmanagedType GetSimpleNativeType(int tokenType)
    {
#pragma warning disable CS0618 // Preserve the legacy IL native type spellings.
        return tokenType switch
        {
            CILParser.CURRENCY => UnmanagedType.Currency,
            CILParser.BOOL => UnmanagedType.Bool,
            CILParser.INT8 => UnmanagedType.I1,
            CILParser.INT16 => UnmanagedType.I2,
            CILParser.INT32_ => UnmanagedType.I4,
            CILParser.INT64_ => UnmanagedType.I8,
            CILParser.FLOAT32 => UnmanagedType.R4,
            CILParser.FLOAT64_ => UnmanagedType.R8,
            CILParser.ERROR => UnmanagedType.Error,
            CILParser.UINT8 => UnmanagedType.U1,
            CILParser.UINT16 => UnmanagedType.U2,
            CILParser.UINT32 => UnmanagedType.U4,
            CILParser.UINT64 => UnmanagedType.U8,
            CILParser.BSTR => UnmanagedType.BStr,
            CILParser.LPSTR => UnmanagedType.LPStr,
            CILParser.LPWSTR => UnmanagedType.LPWStr,
            CILParser.LPTSTR => UnmanagedType.LPTStr,
            CILParser.STRUCT => UnmanagedType.Struct,
            CILParser.INT => UnmanagedType.SysInt,
            CILParser.UINT => UnmanagedType.SysUInt,
            CILParser.BYVALSTR => UnmanagedType.VBByRefStr,
            CILParser.TBSTR => UnmanagedType.TBStr,
            CILParser.METHOD => UnmanagedType.FunctionPtr,
            CILParser.LPSTRUCT => UnmanagedType.LPStruct,
            CILParser.ANY => UnmanagedType.AsAny,
            _ => throw new UnreachableException()
        };
#pragma warning restore CS0618
    }

    private static UnmanagedType GetUnsignedNativeType(int tokenType)
        => tokenType switch
        {
            CILParser.INT8 => UnmanagedType.U1,
            CILParser.INT16 => UnmanagedType.U2,
            CILParser.INT32_ => UnmanagedType.U4,
            CILParser.INT64_ => UnmanagedType.U8,
            _ => throw new UnreachableException()
        };

    private static BlobBuilder CreateNativeTypeBlob(UnmanagedType value)
        => CreateNativeTypeBlob((byte)value);

    private static BlobBuilder CreateNativeTypeBlob(byte value)
    {
        BlobBuilder blob = new(1);
        blob.WriteByte(value);
        return blob;
    }

    private int ParseMarshallingInt32(IToken? token)
        => token is null ? 0 : ParseInt32(token);

}
