// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class ILoaderExtensions
{
    private const uint mdtAssemblyRef = 0x23 << 24;

    // Resolves an AssemblyRef token (defined in <paramref name="referencingModule"/>'s metadata) to
    // the referenced assembly's module via the module's AssemblyRef -> Module map.
    public static bool TryResolveAssemblyRefToModule(
        this ILoader loader,
        ModuleHandle referencingModule,
        AssemblyReferenceHandle assemblyRef,
        out ModuleHandle foundModule)
    {
        foundModule = default;

        uint rid = (uint)MetadataTokens.GetRowNumber(assemblyRef);
        if (rid == 0)
            return false;

        ModuleLookupTables tables = loader.GetLookupTables(referencingModule);
        TargetPointer modulePtr = loader.GetModuleLookupMapElement(tables.ManifestModuleReferences, mdtAssemblyRef | rid, out _);
        if (modulePtr == TargetPointer.Null)
            return false;

        foundModule = loader.GetModuleHandleFromModulePtr(modulePtr);
        return true;
    }
}
