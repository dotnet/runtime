// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class ILoaderExtensions
{
    // Resolves an AssemblyRef token (defined in <paramref name="referencingModule"/>'s metadata) to
    // the referenced assembly's module via the module's AssemblyRef -> Module map.
    public static bool TryResolveAssemblyRefToModule(
        this ILoader loader,
        ModuleHandle referencingModule,
        AssemblyReferenceHandle assemblyRef,
        out ModuleHandle foundModule)
    {
        foundModule = default;

        if (MetadataTokens.GetRowNumber(assemblyRef) == 0)
            return false;

        uint token = (uint)MetadataTokens.GetToken(assemblyRef);

        ModuleLookupTables tables = loader.GetLookupTables(referencingModule);
        TargetPointer modulePtr = loader.GetModuleLookupMapElement(tables.ManifestModuleReferences, token, out _);
        if (modulePtr == TargetPointer.Null)
            return false;

        foundModule = loader.GetModuleHandleFromModulePtr(modulePtr);
        return true;
    }
}
