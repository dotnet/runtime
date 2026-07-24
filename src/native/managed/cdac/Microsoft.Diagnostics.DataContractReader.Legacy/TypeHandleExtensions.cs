// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class TypeHandleExtensions
{
    public static string GetName(this ITypeHandle typeHandle, Target target)
    {
        StringBuilder result = new();
        AppendName(typeHandle, target, result);
        return result.ToString();
    }

    private static void AppendName(ITypeHandle typeHandle, Target target, StringBuilder result)
    {
        IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
        if (runtimeTypeSystem.IsTypeDesc(typeHandle))
        {
            AppendTypeDescName(typeHandle, target, result);
            return;
        }

        if (runtimeTypeSystem.IsArray(typeHandle, out uint rank))
        {
            AppendName(runtimeTypeSystem.GetTypeParam(typeHandle), target, result);
            AppendParamTypeQualifier(result, runtimeTypeSystem.GetInternalCorElementType(typeHandle), rank);
        }
        else
        {
            uint typeDefToken = runtimeTypeSystem.GetTypeDefToken(typeHandle);
            EntityHandle typeDefHandle = MetadataTokens.EntityHandle((int)typeDefToken);
            if (!typeDefHandle.IsNil)
            {
                Contracts.ModuleHandle module = target.Contracts.Loader.GetModuleHandleFromModulePtr(runtimeTypeSystem.GetModule(typeHandle));
                MetadataReader reader = target.Contracts.EcmaMetadata.GetMetadata(module)!;
                TypeDefinition typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)typeDefHandle);
                string typeNamespace = reader.GetString(typeDef.Namespace);
                if (typeNamespace.Length > 0)
                {
                    result.Append(typeNamespace);
                    result.Append('.');
                }
                result.Append(reader.GetString(typeDef.Name));
            }
        }

        ReadOnlySpan<ITypeHandle> instantiation = runtimeTypeSystem.GetInstantiation(typeHandle);
        if (!instantiation.IsEmpty)
        {
            TypeNameBuilder.AppendInst(target, result, instantiation, TypeNameFormat.FormatNamespace);
        }
    }

    private static void AppendTypeDescName(ITypeHandle typeHandle, Target target, StringBuilder result)
    {
        IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
        CorElementType kind = runtimeTypeSystem.GetInternalCorElementType(typeHandle);

        if (kind is CorElementType.Byref or CorElementType.Ptr or CorElementType.SzArray or CorElementType.Array)
        {
            AppendName(runtimeTypeSystem.GetTypeParam(typeHandle), target, result);
            AppendParamTypeQualifier(result, kind, rank: 0);
            return;
        }

        if (kind is CorElementType.Var or CorElementType.MVar)
        {
            bool isGenericVariable = runtimeTypeSystem.IsGenericVariable(typeHandle, out TargetPointer modulePointer, out uint genericParamToken);
            Debug.Assert(isGenericVariable);

            Contracts.ModuleHandle module = target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePointer);
            MetadataReader reader = target.Contracts.EcmaMetadata.GetMetadata(module)!;
            GenericParameter genericParameter = reader.GetGenericParameter((GenericParameterHandle)MetadataTokens.EntityHandle((int)genericParamToken));
            result.Append(kind == CorElementType.Var ? "!" : "!!");
            result.Append(genericParameter.Index);
            return;
        }

        if (kind == CorElementType.FnPtr)
        {
            result.Append("FNPTR");
            return;
        }

        string? name = kind switch
        {
            CorElementType.Void => "Void",
            CorElementType.Boolean => "Boolean",
            CorElementType.Char => "Char",
            CorElementType.I1 => "SByte",
            CorElementType.U1 => "Byte",
            CorElementType.I2 => "Int16",
            CorElementType.U2 => "UInt16",
            CorElementType.I4 => "Int32",
            CorElementType.U4 => "UInt32",
            CorElementType.I8 => "Int64",
            CorElementType.U8 => "UInt64",
            CorElementType.R4 => "Single",
            CorElementType.R8 => "Double",
            CorElementType.String => "String",
            CorElementType.TypedByRef => "TypedReference",
            CorElementType.I => "IntPtr",
            CorElementType.U => "UIntPtr",
            CorElementType.Object => "Object",
            _ => null,
        };

        if (name is not null)
        {
            result.Append("System.");
            result.Append(name);
        }
    }

    private static void AppendParamTypeQualifier(StringBuilder result, CorElementType kind, uint rank)
    {
        switch (kind)
        {
            case CorElementType.Byref:
                result.Append('&');
                break;
            case CorElementType.Ptr:
                result.Append('*');
                break;
            case CorElementType.SzArray:
                result.Append("[]");
                break;
            case CorElementType.Array:
                result.Append('[');
                if (rank == 1)
                {
                    result.Append('*');
                }
                else
                {
                    for (uint dimension = 1; dimension < rank; dimension++)
                    {
                        result.Append(',');
                    }
                }
                result.Append(']');
                break;
        }
    }
}
