// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader;

public static class EcmaMetadataUtils
{
    private const int MaxTypeForwardingChainSize = 1024;

    internal const int RowIdBitCount = 24;
    internal const uint RIDMask = (1 << RowIdBitCount) - 1;

    public static uint GetRowId(uint token) => token & RIDMask;

    internal static uint MakeToken(uint rid, uint table) => rid | (table << RowIdBitCount);

    // ECMA-335 II.22
    // Metadata table index is the most significant byte of the 4-byte token
    public enum TokenType : uint
    {
        mdtTypeRef = 0x01 << 24,
        mdtTypeDef = 0x02 << 24,
        mdtFieldDef = 0x04 << 24,
        mdtMethodDef = 0x06 << 24,
        mdtSignature = 0x11 << 24,
        mdtAssemblyRef = 0x23 << 24,
    }

    public const uint TokenTypeMask = 0xff000000;

    public static uint CreateMethodDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtMethodDef | tokenParts;
    }

    public static uint CreateFieldDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtFieldDef | tokenParts;
    }

    private static bool TryFindTopLevelTypeDef(MetadataReader reader, string @namespace, string name, out TypeDefinitionHandle result)
    {
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            if (!typeDef.GetDeclaringType().IsNil)
                continue;
            if (reader.StringComparer.Equals(typeDef.Name, name) && reader.StringComparer.Equals(typeDef.Namespace, @namespace))
            {
                result = handle;
                return true;
            }
        }

        result = default;
        return false;
    }

    private static bool TryFindNestedTypeDef(MetadataReader reader, TypeDefinitionHandle declaringType, string name, out TypeDefinitionHandle result)
    {
        TypeDefinition declaring = reader.GetTypeDefinition(declaringType);
        foreach (TypeDefinitionHandle handle in declaring.GetNestedTypes())
        {
            TypeDefinition nested = reader.GetTypeDefinition(handle);
            if (reader.StringComparer.Equals(nested.Name, name))
            {
                result = handle;
                return true;
            }
        }

        result = default;
        return false;
    }

    // Resolves a TypeRef token (defined in <paramref name="referencingModule"/>'s metadata) to the
    // module that defines the type and its TypeDef token. Walks the TypeRef's resolution scope to the
    // defining/exporting module, then follows any type-forwarder chain to the module that actually
    // defines the (possibly nested) type.
    public static bool TryResolveTypeRef(
        ILoader loader,
        IEcmaMetadata ecmaMetadata,
        ModuleHandle referencingModule,
        uint typeRefToken,
        out TargetPointer targetAssembly,
        out uint targetTypeDef)
    {
        targetAssembly = TargetPointer.Null;
        targetTypeDef = 0;

        if (!TryGetTypeRefScopeAndName(loader, ecmaMetadata, referencingModule, typeRefToken, out ModuleHandle foundModule, out List<(string Namespace, string Name)> nameChain))
            return false;

        return TrySearchModulesForTypeDef(loader, ecmaMetadata, foundModule, nameChain, out targetAssembly, out targetTypeDef);
    }

    // Use metadata to walk the resolution scope of <paramref name="typeRefToken"/> to the module
    // that should define or export the type, and produce the name chain.
    private static bool TryGetTypeRefScopeAndName(
        ILoader loader,
        IEcmaMetadata ecmaMetadata,
        ModuleHandle referencingModule,
        uint typeRefToken,
        out ModuleHandle foundModule,
        out List<(string Namespace, string Name)> nameChain)
    {
        foundModule = default;
        nameChain = new List<(string Namespace, string Name)>();

        MetadataReader? reader = ecmaMetadata.GetMetadata(referencingModule);
        if (reader is null)
            return false;

        TypeReferenceHandle handle = MetadataTokens.TypeReferenceHandle((int)GetRowId(typeRefToken));
        if (handle.IsNil)
            return false;

        EntityHandle scope;
        while (true)
        {
            TypeReference typeRef = reader.GetTypeReference(handle);
            nameChain.Add((reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name)));
            scope = typeRef.ResolutionScope;
            if (scope.Kind == HandleKind.TypeReference)
            {
                handle = (TypeReferenceHandle)scope;
                continue;
            }
            break;
        }

        // Recorded innermost-to-outermost; reverse so nameChain[0] is the top-level enclosing type.
        nameChain.Reverse();

        // - Nil scope or ModuleDefinition: the type is defined/exported in the referencing module.
        // - AssemblyReference: resolve via the referencing module's AssemblyRef -> Module map.
        if (scope.IsNil || scope.Kind == HandleKind.ModuleDefinition)
        {
            foundModule = referencingModule;
            return true;
        }

        if (scope.Kind == HandleKind.AssemblyReference)
        {
            return loader.TryResolveAssemblyRefToModule(referencingModule, (AssemblyReferenceHandle)scope, out foundModule);
        }

        return false;
    }

    private static bool TrySearchModulesForTypeDef(
        ILoader loader,
        IEcmaMetadata ecmaMetadata,
        ModuleHandle module,
        List<(string Namespace, string Name)> nameChain,
        out TargetPointer targetAssembly,
        out uint targetTypeDef)
    {
        targetAssembly = TargetPointer.Null;
        targetTypeDef = 0;

        (string Namespace, string Name) topLevel = nameChain[0];

        MetadataReader? definingReader = null;
        TypeDefinitionHandle typeDefHandle = default;
        bool foundTopLevel = false;

        // Follow the type-forwarder chain to the module that defines the top-level type.
        for (int i = 0; i < MaxTypeForwardingChainSize; i++)
        {
            MetadataReader? reader = ecmaMetadata.GetMetadata(module);
            if (reader is null)
                return false;

            if (TryFindTopLevelTypeDef(reader, topLevel.Namespace, topLevel.Name, out typeDefHandle))
            {
                definingReader = reader;
                foundTopLevel = true;
                break;
            }

            if (TryFindTopLevelExportedForwarder(loader, reader, module, topLevel.Namespace, topLevel.Name, out ModuleHandle nextModule))
            {
                module = nextModule;
                continue;
            }

            // Not defined or forwarded by this module.
            return false;
        }

        if (!foundTopLevel)
            return false; // Type-forwarding chain too long.

        // Descend into nested types within the defining module.
        for (int level = 1; level < nameChain.Count; level++)
        {
            if (!TryFindNestedTypeDef(definingReader!, typeDefHandle, nameChain[level].Name, out typeDefHandle))
                return false;
        }

        targetAssembly = loader.GetAssembly(module);
        targetTypeDef = (uint)MetadataTokens.GetToken(typeDefHandle);
        return true;
    }

    private static bool TryFindTopLevelExportedForwarder(
        ILoader loader,
        MetadataReader reader,
        ModuleHandle module,
        string @namespace,
        string name,
        out ModuleHandle nextModule)
    {
        foreach (ExportedTypeHandle handle in reader.ExportedTypes)
        {
            ExportedType exportedType = reader.GetExportedType(handle);
            if (exportedType.Implementation.Kind != HandleKind.AssemblyReference)
                continue;
            if (reader.StringComparer.Equals(exportedType.Name, name) && reader.StringComparer.Equals(exportedType.Namespace, @namespace))
            {
                return loader.TryResolveAssemblyRefToModule(module, (AssemblyReferenceHandle)exportedType.Implementation, out nextModule);
            }
        }

        nextModule = default;
        return false;
    }
}
