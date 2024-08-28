// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy
{
    internal static class SigFormat
    {
        public static unsafe void AppendSigFormat(Target target,
            StringBuilder stringBuilder,
            ReadOnlySpan<byte> signature,
            MetadataReader? metadata,
            string? memberName,
            string? className,
            string? namespaceName,
            ReadOnlySpan<TypeHandle> typeInstantiation,
            ReadOnlySpan<TypeHandle> methodInstantiation,
            bool CStringParmsOnly)
        {
            fixed (byte* pSignature = signature)
            {
                BlobReader blobReader = new BlobReader(pSignature, signature.Length);
                AppendSigFormat(target, stringBuilder, blobReader, metadata, memberName, className, namespaceName, typeInstantiation, methodInstantiation, CStringParmsOnly);
            }
        }

        public static void AppendSigFormat(Target target,
            StringBuilder stringBuilder,
            BlobReader signature,
            MetadataReader? metadata,
            string? memberName,
            string? className,
            string? namespaceName,
            ReadOnlySpan<TypeHandle> typeInstantiation,
            ReadOnlySpan<TypeHandle> methodInstantiation,
            bool CStringParmsOnly)
        {
            SignatureHeader header = signature.ReadSignatureHeader();

            if (header.IsGeneric)
            {
                signature.ReadCompressedInteger(); // Ignore generic parameter count
            }
            int cArgs = (int)signature.ReadCompressedInteger();
            bool isVarArg = header.CallingConvention == SignatureCallingConvention.VarArgs;

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

        private static unsafe void AddTypeString(Target target,
            StringBuilder stringBuilder,
            ref BlobReader signature,
            ReadOnlySpan<TypeHandle> typeInstantiation,
            ReadOnlySpan<TypeHandle> methodInstantiation,
            MetadataReader? metadata)
        {
            string _namespace;
            string name;

            while (true)
            {
                switch ((CorElementType)signature.ReadByte())
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
                        EntityHandle handle = signature.ReadTypeHandle();
                        switch (handle.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                TypeDefinition typeDef = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
                                _namespace = metadata.GetString(typeDef.Namespace);
                                name = metadata.GetString(typeDef.Name);
                                break;
                            case HandleKind.TypeReference:
                                TypeReference typeRef = metadata.GetTypeReference((TypeReferenceHandle)handle);
                                _namespace = metadata.GetString(typeRef.Namespace);
                                name = metadata.GetString(typeRef.Name);
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
                        TargetPointer typeHandlePointer = target.ReadPointerFromSpan(signature.ReadBytes(target.PointerSize));
                        IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
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
                        MetadataReader internalTypeMetadata = target.Contracts.EcmaMetadata.GetMetadata(module)!;

                        TypeDefinition internalTypeDef = internalTypeMetadata.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.Handle((int)typeDefToken));
                        _namespace = internalTypeMetadata.GetString(internalTypeDef.Namespace);
                        name = internalTypeMetadata.GetString(internalTypeDef.Name);

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
                        int mvarIndex = signature.ReadCompressedInteger();
                        if (methodInstantiation.Length > mvarIndex)
                        {
                            AddType(target, stringBuilder, methodInstantiation[mvarIndex]);
                        }
                        else
                        {
                            stringBuilder.Append($"!!{mvarIndex}");
                        }
                        return;

                    case CorElementType.Var:
                        int varIndex = signature.ReadCompressedInteger();
                        if (typeInstantiation.Length > varIndex)
                        {
                            AddType(target, stringBuilder, typeInstantiation[varIndex]);
                        }
                        else
                        {
                            stringBuilder.Append($"!!{varIndex}");
                        }
                        return;

                    case CorElementType.GenericInst:
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        int genericArgCount = signature.ReadCompressedInteger();
                        stringBuilder.Append('<');
                        for (int i = 0; i < genericArgCount; i++)
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
                        int rank = signature.ReadCompressedInteger();
                        for (uint i = 1; i < rank; i++)
                        {
                            stringBuilder.Append(',');
                        }
                        stringBuilder.Append(']');
                        int numSizes = signature.ReadCompressedInteger();
                        for (int i = 0; i < numSizes; i++)
                        {
                            _ = signature.ReadCompressedInteger();
                        }
                        int numLoBounds = signature.ReadCompressedInteger();
                        for (int i = 0; i < numLoBounds; i++)
                        {
                            _ = signature.ReadCompressedSignedInteger();
                        }
                        return;

                    case CorElementType.FnPtr:
                        SignatureHeader fnPtrHeader = signature.ReadSignatureHeader();
                        int cArgs = signature.ReadCompressedInteger();
                        AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                        stringBuilder.Append(" (");
                        for (uint i = 0; i < cArgs; i++)
                        {
                            AddTypeString(target, stringBuilder, ref signature, typeInstantiation, methodInstantiation, metadata);
                            if (i != cArgs - 1)
                                stringBuilder.Append(", ");
                        }
                        if (fnPtrHeader.CallingConvention == SignatureCallingConvention.VarArgs)
                        {
                            if (cArgs > 0)
                                stringBuilder.Append(", ");
                            stringBuilder.Append("...");
                        }
                        stringBuilder.Append(')');
                        return;

                    case CorElementType.CModOpt:
                    case CorElementType.CModReqd:
                        _ = signature.ReadTypeHandle();
                        break;

                    case CorElementType.CModInternal:
                        _ = signature.ReadByte();
                        _ = signature.ReadBytes(target.PointerSize);
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
                    MetadataReader metadata = target.Contracts.EcmaMetadata.GetMetadata(module)!;
                    TypeDefinition typeDef = metadata.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.Handle((int)typeDefToken));
                    string _namespace = metadata.GetString(typeDef.Namespace);
                    string name = metadata.GetString(typeDef.Name);

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
                    MetadataReader generatedVariableMetadata = target.Contracts.EcmaMetadata.GetMetadata(genericVariableModule)!;
                    GenericParameter genericVariable = generatedVariableMetadata.GetGenericParameter((GenericParameterHandle)MetadataTokens.Handle((int)typeVarToken));
                    stringBuilder.Append(generatedVariableMetadata.GetString(genericVariable.Name));
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
                    SignatureHeader header = new SignatureHeader(callConv);
                    AddType(target, stringBuilder, retAndArgTypes[0]);
                    stringBuilder.Append(" (");
                    for (int i = 1; i < retAndArgTypes.Length; i++)
                    {
                        AddType(target, stringBuilder, retAndArgTypes[i]);
                        if (i != retAndArgTypes.Length - 1)
                            stringBuilder.Append(", ");
                    }
                    if (header.CallingConvention == SignatureCallingConvention.VarArgs)
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
