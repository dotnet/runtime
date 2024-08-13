// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Helpers;

namespace Microsoft.Diagnostics.DataContractReader.Legacy
{
    internal static class SigFormat
    {
        private const uint IMAGE_CEE_CS_CALLCONV_MASK = 0xF;
        private const uint IMAGE_CEE_CS_CALLCONV_VARARG = 0x5;
        private const uint IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
        public static void AppendSigFormat(Target target,
            StringBuilder stringBuilder,
            ReadOnlySpan<byte> signature,
            EcmaMetadataReader? metadata,
            string? memberName,
            string? className,
            string? namespaceName,
            ReadOnlySpan<TypeHandle> typeInstantiation,
            ReadOnlySpan<TypeHandle> methodInstantiation,
            bool CStringParmsOnly)
        {
            byte callConv = checked((byte)GetData(ref signature));

            if ((callConv & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
            {
                GetData(ref signature); // Ignore generic parameter count
            }
            int cArgs = (int)GetData(ref signature);
            bool isVarArg = (callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_VARARG;

            if (!CStringParmsOnly)
            {
                AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                stringBuilder.Append(' ');
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    stringBuilder.Append(namespaceName);
                    stringBuilder.Append('.');
                }
                if (!string.IsNullOrEmpty(className))
                {
                    stringBuilder.Append(className);
                    stringBuilder.Append('.');
                }
                if (!string.IsNullOrEmpty(memberName))
                {
                    stringBuilder.Append(memberName);
                    stringBuilder.Append('.');
                }
            }
            else
            {
                StringBuilder sbDummy = new StringBuilder();
                AddTypeString(target, sbDummy, ref signature, typeInstantiation, methodInstantiation, metadata);
            }

            stringBuilder.Append('(');
            for (int i = 0; i < cArgs; i++)
            {
                AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                if (i != cArgs - 1)
                    stringBuilder.Append(", ");
            }

            if (isVarArg)
            {
                if (cArgs > 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append("...");
            }
            stringBuilder.Append(')');
        }

        private static Contracts.CorElementType GetElemType(ref ReadOnlySpan<byte> signature)
        {
            CorElementType result = (Contracts.CorElementType)signature[0];
            signature = signature.Slice(1);
            return result;
        }

        private static uint GetData(ref ReadOnlySpan<byte> signature)
        {
            byte headerByte = signature[0];
            if ((headerByte & 0x80) == 0)
            {
                signature = signature.Slice(1);
                return headerByte;
            }
            else if ((headerByte & 0x40) == 0)
            {
                int result = ((headerByte & 0x3f) << 8) | signature[1];
                signature = signature.Slice(2);
                return (uint)result;
            }
            else if ((headerByte & 0x20) == 0)
            {
                int result = ((headerByte & 0x1f) << 24) | (signature[1] << 16) | (signature[2] << 8) | signature[3];
                signature = signature.Slice(4);
                return (uint)result;
            }
            throw new InvalidOperationException("Invalid signature format");
        }

        private static uint GetToken(ref ReadOnlySpan<byte> signature)
        {
            uint data = GetData(ref signature);
            MetadataTable table;
            switch (data & 3)
            {
                case 0: table = MetadataTable.TypeDef; break;
                case 1: table = MetadataTable.TypeRef; break;
                case 2: table = MetadataTable.TypeSpec; break;
                default: throw new InvalidOperationException("Invalid signature format");
            }

            return EcmaMetadataReader.CreateToken(table, data >> 2);
        }

        private static void AddTypeString(Target target,
            StringBuilder stringBuilder,
            ref ReadOnlySpan<byte> signature,
            ReadOnlySpan<TypeHandle> typeInstantiation,
            ReadOnlySpan<TypeHandle> methodInstantiation,
            EcmaMetadataReader? metadata)
        {
            string _namespace;
            string name;
            EcmaMetadataCursor cursor;

            while (true)
            {
                switch (GetElemType(ref signature))
                {
                    case CorElementType.Void: stringBuilder.Append("Void"); return;
                    case CorElementType.Boolean: stringBuilder.Append("Boolean"); return;
                    case CorElementType.I: stringBuilder.Append("IntPtr"); return;
                    case CorElementType.U: stringBuilder.Append("UIntPtr"); return;
                    case CorElementType.I1: stringBuilder.Append("SByte"); return;
                    case CorElementType.U1: stringBuilder.Append("Byte"); return;
                    case CorElementType.I2: stringBuilder.Append("Int16"); return;
                    case CorElementType.U2: stringBuilder.Append("UInt16"); return;
                    case CorElementType.I4: stringBuilder.Append("Int32"); return;
                    case CorElementType.U4: stringBuilder.Append("UInt32"); return;
                    case CorElementType.I8: stringBuilder.Append("Int64"); return;
                    case CorElementType.U8: stringBuilder.Append("UInt64"); return;
                    case CorElementType.R4: stringBuilder.Append("Single"); return;
                    case CorElementType.R8: stringBuilder.Append("Double"); return;
                    case CorElementType.Char: stringBuilder.Append("Char"); return;

                    case CorElementType.Object: stringBuilder.Append("System.Object"); return;
                    case CorElementType.String: stringBuilder.Append("System.String"); return;

                    case CorElementType.ValueType:
                    case CorElementType.Class:
                        if (metadata == null)
                            throw new InvalidOperationException("Invalid signature without metadata reader");
                        uint token = GetToken(ref signature);
                        cursor = metadata.GetCursor(token);
                        switch (EcmaMetadataReader.TokenToTable(token))
                        {
                            case MetadataTable.TypeDef:
                                _namespace = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);
                                name = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);
                                break;
                            case MetadataTable.TypeRef:
                                _namespace = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeRef_TypeNamespace);
                                name = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeRef_TypeNamespace);
                                break;
                            default:
                                return;
                        }

                        if (!string.IsNullOrEmpty(_namespace))
                        {
                            stringBuilder.Append(_namespace);
                            stringBuilder.Append('.');
                        }
                        stringBuilder.Append(name);
                        return;

                    case CorElementType.Internal:
                        TargetPointer typeHandlePointer = target.ReadPointerFromSpan(signature);
                        IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
                        signature.Slice(target.PointerSize);
                        TypeHandle th = runtimeTypeSystem.GetTypeHandle(typeHandlePointer);
                        switch (runtimeTypeSystem.GetSignatureCorElementType(th))
                        {
                            case CorElementType.FnPtr:
                            case CorElementType.Ptr:
                                stringBuilder.Append("System.UIntPtr");
                                return;
                            case CorElementType.ValueType:
                                if (runtimeTypeSystem.HasTypeParam(th))
                                {
                                    th = runtimeTypeSystem.GetTypeParam(th);
                                }
                                break;

                            case CorElementType.Byref:
                            case CorElementType.Array:
                                AddType(target, stringBuilder, th);
                                return;
                        }

                        uint typeDefToken = runtimeTypeSystem.GetTypeDefToken(th);
                        TargetPointer modulePointer = target.Contracts.RuntimeTypeSystem.GetModule(th);
                        Contracts.ModuleHandle module = target.Contracts.Loader.GetModuleHandle(modulePointer);
                        EcmaMetadataReader internalTypeMetadata = target.Metadata.GetMetadata(module).EcmaMetadataReader;
                        cursor = internalTypeMetadata.GetCursor(typeDefToken);
                        _namespace = internalTypeMetadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);
                        name = internalTypeMetadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);

                        if (!string.IsNullOrEmpty(_namespace))
                        {
                            stringBuilder.Append(_namespace);
                            stringBuilder.Append('.');
                        }
                        stringBuilder.Append(name);
                        return;

                    case CorElementType.TypedByRef:
                        stringBuilder.Append("TypedReference");
                        return;

                    case CorElementType.Byref:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append(" ByRef");
                        return;

                    case CorElementType.Ptr:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append('*');
                        return;

                    case CorElementType.MVar:
                        uint mvarIndex = GetData(ref signature);
                        if (methodInstantiation.Length > (int)mvarIndex)
                        {
                            AddType(target, stringBuilder, methodInstantiation[(int)mvarIndex]);
                        }
                        else
                        {
                            stringBuilder.Append($"!!{mvarIndex}");
                        }
                        return;

                    case CorElementType.Var:
                        uint varIndex = GetData(ref signature);
                        if (methodInstantiation.Length > (int)varIndex)
                        {
                            AddType(target, stringBuilder, methodInstantiation[(int)varIndex]);
                        }
                        else
                        {
                            stringBuilder.Append($"!!{varIndex}");
                        }
                        return;

                    case CorElementType.GenericInst:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        uint genericArgCount = GetData(ref signature);
                        stringBuilder.Append('<');
                        for (uint i = 0; i < genericArgCount; i++)
                        {
                            if (i != 0)
                                stringBuilder.Append(',');
                            AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        }
                        stringBuilder.Append('>');
                        return;

                    case CorElementType.SzArray:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append("[]");
                        return;

                    case CorElementType.Array:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append('[');
                        uint rank = GetData(ref signature);
                        for (uint i = 1; i < rank; i++)
                        {
                            stringBuilder.Append(',');
                        }
                        stringBuilder.Append(']');
                        uint numSizes = GetData(ref signature);
                        for (uint i = 0; i < numSizes; i++)
                        {
                            GetData(ref signature);
                        }
                        uint numLoBounds = GetData(ref signature);
                        for (uint i = 0; i < numLoBounds; i++)
                        {
                            GetData(ref signature);
                        }
                        return;

                    case CorElementType.FnPtr:
                        uint callConv = GetData(ref signature);
                        uint cArgs = GetData(ref signature);
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append(" (");
                        for (uint i = 0; i < cArgs; i++)
                        {
                            AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                            if (i != cArgs - 1)
                                stringBuilder.Append(", ");
                        }
                        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_VARARG)
                        {
                            if (cArgs > 0)
                                stringBuilder.Append(", ");
                            stringBuilder.Append("...");
                        }
                        stringBuilder.Append(')');
                        return;

                    case CorElementType.CModOpt:
                    case CorElementType.CModReqd:
                        GetToken(ref signature);
                        break;

                    default:
                        stringBuilder.Append("**UNKNOWN TYPE**");
                        return;
                }
            }
        }

        private static void AddType(Target target, StringBuilder stringBuilder, TypeHandle typeHandle)
        {
            IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;

            if (typeHandle.IsNull)
                stringBuilder.Append("**UNKNOWN TYPE**");
            CorElementType corElementType = runtimeTypeSystem.GetSignatureCorElementType(typeHandle);
            if (corElementType == CorElementType.ValueType && runtimeTypeSystem.HasTypeParam(typeHandle))
            {
                typeHandle = runtimeTypeSystem.GetTypeParam(typeHandle);
            }

            switch (corElementType)
            {
                case CorElementType.Void: stringBuilder.Append("Void"); return;
                case CorElementType.Boolean: stringBuilder.Append("Boolean"); return;
                case CorElementType.I: stringBuilder.Append("IntPtr"); return;
                case CorElementType.U: stringBuilder.Append("UIntPtr"); return;
                case CorElementType.I1: stringBuilder.Append("SByte"); return;
                case CorElementType.U1: stringBuilder.Append("Byte"); return;
                case CorElementType.I2: stringBuilder.Append("Int16"); return;
                case CorElementType.U2: stringBuilder.Append("UInt16"); return;
                case CorElementType.I4: stringBuilder.Append("Int32"); return;
                case CorElementType.U4: stringBuilder.Append("UInt32"); return;
                case CorElementType.I8: stringBuilder.Append("Int64"); return;
                case CorElementType.U8: stringBuilder.Append("UInt64"); return;
                case CorElementType.R4: stringBuilder.Append("Single"); return;
                case CorElementType.R8: stringBuilder.Append("Double"); return;
                case CorElementType.Char: stringBuilder.Append("Char"); return;

                case CorElementType.Object: stringBuilder.Append("System.Object"); return;
                case CorElementType.String: stringBuilder.Append("System.String"); return;

                case CorElementType.ValueType:
                case CorElementType.Class:
                    uint typeDefToken = runtimeTypeSystem.GetTypeDefToken(typeHandle);
                    TargetPointer modulePointer = target.Contracts.RuntimeTypeSystem.GetModule(typeHandle);
                    Contracts.ModuleHandle module = target.Contracts.Loader.GetModuleHandle(modulePointer);
                    EcmaMetadataReader metadata = target.Metadata.GetMetadata(module).EcmaMetadataReader;
                    EcmaMetadataCursor cursor = metadata.GetCursor(typeDefToken);
                    string _namespace = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);
                    string name = metadata.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);

                    if (!string.IsNullOrEmpty(_namespace))
                    {
                        stringBuilder.Append(_namespace);
                        stringBuilder.Append('.');
                    }
                    stringBuilder.Append(name);

                    ReadOnlySpan<TypeHandle> instantiation = runtimeTypeSystem.GetInstantiation(typeHandle);
                    if (instantiation.Length > 0)
                    {
                        stringBuilder.Append('<');
                        for (int i = 0; i < instantiation.Length; i++)
                        {
                            if (i != 0)
                                stringBuilder.Append(',');
                            AddType(target, stringBuilder, instantiation[i]);
                        }
                        stringBuilder.Append('>');
                    }

                    return;

                case CorElementType.TypedByRef:
                    stringBuilder.Append("TypedReference");
                    return;

                case CorElementType.Byref:
                    AddType(target, stringBuilder, runtimeTypeSystem.GetTypeParam(typeHandle));
                    stringBuilder.Append(" ByRef");
                    return;

                case CorElementType.Ptr:
                    AddType(target, stringBuilder, runtimeTypeSystem.GetTypeParam(typeHandle));
                    stringBuilder.Append('*');
                    return;

                case CorElementType.MVar:
                case CorElementType.Var:
                    runtimeTypeSystem.IsGenericVariable(typeHandle, out TargetPointer genericVariableModulePointer, out uint typeVarToken);
                    Contracts.ModuleHandle genericVariableModule = target.Contracts.Loader.GetModuleHandle(genericVariableModulePointer);
                    EcmaMetadataReader genericVariableMetadata = target.Metadata.GetMetadata(genericVariableModule).EcmaMetadataReader;
                    EcmaMetadataCursor genericVariableCursor = genericVariableMetadata.GetCursor(typeVarToken);
                    stringBuilder.Append(genericVariableMetadata.GetColumnAsUtf8String(genericVariableCursor, MetadataColumnIndex.GenericParam_Name));
                    return;

                case CorElementType.SzArray:
                    AddType(target, stringBuilder, runtimeTypeSystem.GetTypeParam(typeHandle));
                    stringBuilder.Append("[]");
                    return;

                case CorElementType.Array:
                    AddType(target, stringBuilder, runtimeTypeSystem.GetTypeParam(typeHandle));
                    stringBuilder.Append('[');

                    runtimeTypeSystem.IsArray(typeHandle, out uint rank);
                    for (uint i = 1; i < rank; i++)
                    {
                        stringBuilder.Append(',');
                    }
                    stringBuilder.Append(']');
                    return;

                case CorElementType.FnPtr:
                    runtimeTypeSystem.IsFunctionPointer(typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv);
                    AddType(target, stringBuilder, retAndArgTypes[0]);
                    stringBuilder.Append(" (");
                    for (int i = 1; i < retAndArgTypes.Length; i++)
                    {
                        AddType(target, stringBuilder, retAndArgTypes[i]);
                        if (i != retAndArgTypes.Length - 1)
                            stringBuilder.Append(", ");
                    }
                    if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_VARARG)
                    {
                        if (retAndArgTypes.Length > 1)
                            stringBuilder.Append(", ");
                        stringBuilder.Append("...");
                    }
                    stringBuilder.Append(')');
                    return;

                default:
                    stringBuilder.Append("**UNKNOWN TYPE**");
                    return;
            }
        }
    }
}
