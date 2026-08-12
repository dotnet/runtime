// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        GrammarResult ICILVisitor<GrammarResult>.VisitIidParamIndex(CILParser.IidParamIndexContext context) => VisitIidParamIndex(context);
        public GrammarResult.Literal<int?> VisitIidParamIndex(CILParser.IidParamIndexContext context)
            => context.int32() is CILParser.Int32Context int32 ? new(VisitInt32(int32).Value) : new(null);

        GrammarResult ICILVisitor<GrammarResult>.VisitMarshalBlob(CILParser.MarshalBlobContext context) => VisitMarshalBlob(context);
        public GrammarResult.FormattedBlob VisitMarshalBlob(CILParser.MarshalBlobContext context)
        {
            var hexBytes = context.hexbyte();
            if (hexBytes.Length > 0)
            {
                var blob = new BlobBuilder(hexBytes.Length);
                foreach (var hb in hexBytes)
                {
                    blob.WriteByte(hb.Value);
                }
                return new(blob);
            }

            return VisitNativeType(context.nativeType());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMarshalClause(CILParser.MarshalClauseContext context) => VisitMarshalClause(context);
        public GrammarResult.FormattedBlob VisitMarshalClause(CILParser.MarshalClauseContext context)
        {
            if (context.ChildCount == 0)
            {
                return new(new BlobBuilder(0));
            }

            return VisitMarshalBlob(context.marshalBlob());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNativeType(CILParser.NativeTypeContext context) => VisitNativeType(context);
        public GrammarResult.FormattedBlob VisitNativeType(CILParser.NativeTypeContext context)
        {
            if (context.nativeTypeElement() is not CILParser.NativeTypeElementContext element)
            {
                return new(new BlobBuilder());
            }

            CILParser.NativeTypeArrayPointerInfoContext[] arrayPointerInfo = context.nativeTypeArrayPointerInfo();
            if (arrayPointerInfo.Length == 0)
            {
                return VisitNativeTypeElement(element);
            }
            var prefix = new BlobBuilder(arrayPointerInfo.Length);
            var elementType = VisitNativeTypeElement(element).Value;
            var suffix = new BlobBuilder();

            for (int i = arrayPointerInfo.Length - 1; i >= 0; i--)
            {
                if (arrayPointerInfo[i] is CILParser.PointerNativeTypeContext)
                {
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "pointer in array"),
                        context);
                    const int NATIVE_TYPE_PTR = 0x10;
                    prefix.WriteByte(NATIVE_TYPE_PTR);
                }
                else
                {
                    prefix.WriteByte((byte)UnmanagedType.LPArray);
                    if (elementType.Count == 0)
                    {
                        // We need to have an element type for arrays,
                        // so write the invalid NATIVE_TYPE_MAX value so we have something parsable.
                        const int NATIVE_TYPE_MAX = 0x50;
                        elementType.WriteByte(NATIVE_TYPE_MAX);
                    }
                }
            }

            for (int i = 0; i < arrayPointerInfo.Length; i++)
            {
                if (arrayPointerInfo[i] is CILParser.PointerArrayTypeSizeContext size)
                {
                    suffix.WriteCompressedInteger(0);
                    suffix.WriteCompressedInteger(VisitInt32(size.int32()).Value);
                    suffix.WriteCompressedInteger(0);
                }
                else if (arrayPointerInfo[i] is CILParser.PointerArrayTypeSizeParamIndexContext sizeParamIndex)
                {
                    var ints = sizeParamIndex.int32();
                    suffix.WriteCompressedInteger(VisitInt32(ints[1]).Value);
                    suffix.WriteCompressedInteger(VisitInt32(ints[0]).Value);
                    suffix.WriteCompressedInteger(1); // Write that the paramIndex parameter was specified
                }
                else if (arrayPointerInfo[i] is CILParser.PointerArrayTypeParamIndexContext paramIndex)
                {
                    suffix.WriteCompressedInteger(VisitInt32(paramIndex.int32()).Value);
                }
            }

            prefix.LinkSuffix(elementType);
            prefix.LinkSuffix(suffix);
            return new(prefix);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNativeTypeElement(CILParser.NativeTypeElementContext context) => VisitNativeTypeElement(context);
        public GrammarResult.FormattedBlob VisitNativeTypeElement(CILParser.NativeTypeElementContext context)
        {
            var blob = new BlobBuilder();
            if (context.dottedName() is CILParser.DottedNameContext typedef)
            {
                // Native type typedefs are not yet fully supported
                // For now, report an error and return empty blob
                string alias = VisitDottedName(typedef).Value;
                ReportError(DiagnosticIds.TypedefNotFound, string.Format(DiagnosticMessageTemplates.TypedefNotFound, alias), context);
                return new(blob);
            }

            if (context.marshalType is null)
            {
                if (context.marshalBool is not null)
                {
                    blob.WriteByte((byte)UnmanagedType.VariantBool);
                }
                else if (context.unsignedMarshalType is not null)
                {
                    blob.WriteByte(context.unsignedMarshalType.Type switch
                    {
                        CILParser.INT8 => (byte)UnmanagedType.U1,
                        CILParser.INT16 => (byte)UnmanagedType.U2,
                        CILParser.INT32_ => (byte)UnmanagedType.U4,
                        CILParser.INT64_ => (byte)UnmanagedType.U8,
                        _ => throw new UnreachableException(),
                    });
                }
                return new(blob);
            }

            switch (context.marshalType.Type)
            {
                case CILParser.CUSTOM:
                    {
                        blob.WriteByte((byte)UnmanagedType.CustomMarshaler);
                        CILParser.CompQstringContext[] strings = context.compQstring();
                        if (strings.Length == 4)
                        {
                            ReportWarning(DiagnosticIds.DeprecatedCustomMarshaller,
                                DiagnosticMessageTemplates.DeprecatedCustomMarshaller,
                                context);
                            blob.WriteSerializedString(VisitCompQstring(strings[0]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[1]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[2]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[3]).Value);
                        }
                        else
                        {
                            Debug.Assert(strings.Length == 2);
                            blob.WriteCompressedInteger(0);
                            blob.WriteCompressedInteger(0);
                            blob.WriteSerializedString(VisitCompQstring(strings[0]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[1]).Value);
                        }
                        break;
                    }
                case CILParser.SYSSTRING:
                    blob.WriteByte((byte)UnmanagedType.ByValTStr);
                    blob.WriteCompressedInteger(VisitInt32(context.int32()).Value);
                    break;
                case CILParser.ARRAY:
                    blob.WriteByte((byte)UnmanagedType.ByValArray);
                    blob.WriteCompressedInteger(VisitInt32(context.int32()).Value);
                    VisitNativeType(context.nativeType()).Value.WriteContentTo(blob);
                    break;
                case CILParser.VARIANT:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "VARIANT"),
                        context);
                    const int NATIVE_TYPE_VARIANT = 0xe;
                    blob.WriteByte(NATIVE_TYPE_VARIANT);
                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case CILParser.CURRENCY:
                    blob.WriteByte((byte)UnmanagedType.Currency);
                    break;
#pragma warning restore CS0618 // Type or member is obsolete
                case CILParser.SYSCHAR:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "SYSCHAR"),
                        context);
                    const int NATIVE_TYPE_SYSCHAR = 0xd;
                    blob.WriteByte(NATIVE_TYPE_SYSCHAR);
                    break;
                case CILParser.VOID:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "VOID"),
                        context);
                    const int NATIVE_TYPE_VOID = 0x1;
                    blob.WriteByte(NATIVE_TYPE_VOID);
                    break;
                case CILParser.BOOL:
                    blob.WriteByte((byte)UnmanagedType.Bool);
                    break;
                case CILParser.INT8:
                    blob.WriteByte((byte)UnmanagedType.I1);
                    break;
                case CILParser.INT16:
                    blob.WriteByte((byte)UnmanagedType.I2);
                    break;
                case CILParser.INT32_:
                    blob.WriteByte((byte)UnmanagedType.I4);
                    break;
                case CILParser.INT64_:
                    blob.WriteByte((byte)UnmanagedType.I8);
                    break;
                case CILParser.FLOAT32:
                    blob.WriteByte((byte)UnmanagedType.R4);
                    break;
                case CILParser.FLOAT64_:
                    blob.WriteByte((byte)UnmanagedType.R8);
                    break;
                case CILParser.ERROR:
                    blob.WriteByte((byte)UnmanagedType.Error);
                    break;
                case CILParser.UINT8:
                    blob.WriteByte((byte)UnmanagedType.U1);
                    break;
                case CILParser.UINT16:
                    blob.WriteByte((byte)UnmanagedType.U2);
                    break;
                case CILParser.UINT32:
                    blob.WriteByte((byte)UnmanagedType.U4);
                    break;
                case CILParser.UINT64:
                    blob.WriteByte((byte)UnmanagedType.U8);
                    break;
                case CILParser.DECIMAL:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "DECIMAL"),
                        context);
                    const int NATIVE_TYPE_DECIMAL = 0x11;
                    blob.WriteByte(NATIVE_TYPE_DECIMAL);
                    break;
                case CILParser.DATE:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "DATE"),
                        context);
                    const int NATIVE_TYPE_DATE = 0x12;
                    blob.WriteByte(NATIVE_TYPE_DATE);
                    break;
                case CILParser.BSTR:
                    // Distinguish 'ansi bstr' (AnsiBStr) from plain 'bstr' (BStr)
                    if (context.ANSI() is not null)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        blob.WriteByte((byte)UnmanagedType.AnsiBStr);
#pragma warning restore CS0618
                    }
                    else
                    {
                        blob.WriteByte((byte)UnmanagedType.BStr);
                    }
                    break;
                case CILParser.LPSTR:
                    blob.WriteByte((byte)UnmanagedType.LPStr);
                    break;
                case CILParser.LPWSTR:
                    blob.WriteByte((byte)UnmanagedType.LPWStr);
                    break;
                case CILParser.LPTSTR:
                    blob.WriteByte((byte)UnmanagedType.LPTStr);
                    break;
                case CILParser.OBJECTREF:
                    ReportWarning(DiagnosticIds.DeprecatedNativeType,
                        string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "OBJECTREF"),
                        context);
                    const int NATIVE_TYPE_OBJECTREF = 0x18;
                    blob.WriteByte(NATIVE_TYPE_OBJECTREF);
                    break;
                case CILParser.IUNKNOWN:
                    {
                        blob.WriteByte((byte)UnmanagedType.IUnknown);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.IDISPATCH:
                    {
                        blob.WriteByte((byte)UnmanagedType.IDispatch);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.STRUCT:
                    // Distinguish 'nested struct' from plain 'struct'
                    if (context.GetChild(0)?.GetText() == "nested")
                    {
                        ReportWarning(DiagnosticIds.DeprecatedNativeType,
                            string.Format(DiagnosticMessageTemplates.DeprecatedNativeType, "NESTEDSTRUCT"),
                            context);
                        const int NATIVE_TYPE_NESTEDSTRUCT = 0x21;
                        blob.WriteByte(NATIVE_TYPE_NESTEDSTRUCT);
                    }
                    else
                    {
                        blob.WriteByte((byte)UnmanagedType.Struct);
                    }
                    break;
                case CILParser.INTERFACE:
                    {
                        blob.WriteByte((byte)UnmanagedType.Interface);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.SAFEARRAY:
                    blob.WriteByte((byte)UnmanagedType.SafeArray);
                    blob.WriteCompressedInteger((int)VisitVariantType(context.variantType()).Value);
                    if (context.compQstring() is { Length: 1 } safeArrayCustomType)
                    {
                        string str = VisitCompQstring(safeArrayCustomType[0]).Value;
                        blob.WriteSerializedString(str);
                    }
                    else
                    {
                        blob.WriteCompressedInteger(0);
                    }
                    break;
                case CILParser.INT:
                    blob.WriteByte((byte)UnmanagedType.SysInt);
                    break;
                case CILParser.UINT:
                    blob.WriteByte((byte)UnmanagedType.SysUInt);
                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case CILParser.BYVALSTR:
                    blob.WriteByte((byte)UnmanagedType.VBByRefStr);
                    break;
                case CILParser.TBSTR:
                    blob.WriteByte((byte)UnmanagedType.TBStr);
                    break;
#pragma warning restore CS0618 // Type or member is obsolete
                case CILParser.METHOD:
                    blob.WriteByte((byte)UnmanagedType.FunctionPtr);
                    break;
                case CILParser.LPSTRUCT:
                    blob.WriteByte((byte)UnmanagedType.LPStruct);
                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case CILParser.ANY:
                    blob.WriteByte((byte)UnmanagedType.AsAny);
                    break;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPinvAttr(CILParser.PinvAttrContext context) => VisitPinvAttr(context);
        public GrammarResult.Flag<MethodImportAttributes> VisitPinvAttr(CILParser.PinvAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodImportAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            switch (context.GetText())
            {
                case "nomangle":
                    return new(MethodImportAttributes.ExactSpelling);
                case "ansi":
                    return new(MethodImportAttributes.CharSetAnsi);
                case "unicode":
                    return new(MethodImportAttributes.CharSetUnicode);
                case "autochar":
                    return new(MethodImportAttributes.CharSetAuto);
                case "lasterr":
                    return new(MethodImportAttributes.SetLastError);
                case "winapi":
                    return new(MethodImportAttributes.CallingConventionWinApi);
                case "cdecl":
                    return new(MethodImportAttributes.CallingConventionCDecl);
                case "stdcall":
                    return new(MethodImportAttributes.CallingConventionStdCall);
                case "thiscall":
                    return new(MethodImportAttributes.CallingConventionThisCall);
                case "fastcall":
                    return new(MethodImportAttributes.CallingConventionFastCall);
                case "bestfit:on":
                    return new(MethodImportAttributes.BestFitMappingEnable);
                case "bestfit:off":
                    return new(MethodImportAttributes.BestFitMappingDisable);
                case "charmaperror:on":
                    return new(MethodImportAttributes.ThrowOnUnmappableCharEnable);
                case "charmaperror:off":
                    return new(MethodImportAttributes.ThrowOnUnmappableCharDisable);
                default:
                    throw new UnreachableException();
            }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPinvImpl(CILParser.PinvImplContext context) => VisitPinvImpl(context);
        public GrammarResult.Literal<(string? ModuleName, string? EntryPointName, MethodImportAttributes Attributes)> VisitPinvImpl(CILParser.PinvImplContext context)
        {
            MethodImportAttributes attrs = MethodImportAttributes.None;
            foreach (var attr in context.pinvAttr())
            {
                attrs |= VisitPinvAttr(attr);
            }
            var names = context.compQstring();
            string? moduleName = names.Length > 0 ? VisitCompQstring(names[0]).Value : null;
            string? entryPointName = names.Length > 1 ? VisitCompQstring(names[1]).Value : null;
            return new((moduleName, entryPointName, attrs));
        }


        GrammarResult ICILVisitor<GrammarResult>.VisitVariantType(CILParser.VariantTypeContext context) => VisitVariantType(context);
        public GrammarResult.Literal<VarEnum> VisitVariantType(CILParser.VariantTypeContext context)
        {
            if (context.variantTypeElement() is not CILParser.VariantTypeElementContext element)
            {
                return new(VarEnum.VT_EMPTY);
            }

            VarEnum variant = VisitVariantTypeElement(element).Value;
            // The 0th child is the variant element type.
            for (int i = 1; i < context.ChildCount; i++)
            {
                ITerminalNode childToken = (ITerminalNode)context.children[i];
                if (childToken.Symbol.Type == CILParser.ARRAY_TYPE_NO_BOUNDS)
                {
                    variant |= VarEnum.VT_ARRAY;
                }
                else if (childToken.Symbol.Type == CILParser.VECTOR)
                {
                    variant |= VarEnum.VT_VECTOR;
                }
                else
                {
                    Debug.Assert(childToken.Symbol.Type == CILParser.REF);
                    variant |= VarEnum.VT_BYREF;
                }
            }
            return new(variant);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitVariantTypeElement(CILParser.VariantTypeElementContext context) => VisitVariantTypeElement(context);
        public GrammarResult.Literal<VarEnum> VisitVariantTypeElement(CILParser.VariantTypeElementContext context)
        {
            return new(context.GetChild<ITerminalNode>(0).Symbol.Type switch
            {
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
                _ => throw new UnreachableException()
            });
        }
        public GrammarResult VisitNativeTypeArrayPointerInfo(CILParser.NativeTypeArrayPointerInfoContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeSize(CILParser.PointerArrayTypeSizeContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeParamIndex(CILParser.PointerArrayTypeParamIndexContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerNativeType(CILParser.PointerNativeTypeContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeSizeParamIndex(CILParser.PointerArrayTypeSizeParamIndexContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeNoSizeData(CILParser.PointerArrayTypeNoSizeDataContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    }
}
