// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;
internal static class DbiHelpers
{
    public static TypeHandle LookupTypeDefOrRefInAssembly(Target target, IRuntimeTypeSystem rts, ulong vmAssembly, uint metadataToken)
    {
        TypeHandle th = TryLookupTypeDefOrRefInAssembly(target, rts, vmAssembly, metadataToken);
        if (th.IsNull)
            throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
        return th;
    }

    public static TypeHandle TryLookupTypeDefOrRefInAssembly(Target target, IRuntimeTypeSystem rts, ulong vmAssembly, uint metadataToken)
    {
        ILoader loader = target.Contracts.Loader;
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
        ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
        TargetPointer mt;
        switch ((EcmaMetadataUtils.TokenType)(metadataToken & EcmaMetadataUtils.TokenTypeMask))
        {
            case EcmaMetadataUtils.TokenType.mdtTypeDef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeDefToMethodTable, metadataToken, out _);
                break;
            case EcmaMetadataUtils.TokenType.mdtTypeRef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeRefToMethodTable, metadataToken, out _);
                break;
            default:
                return default;
        }
        if (mt == TargetPointer.Null)
            return default;
        return rts.GetTypeHandle(mt);
    }
}
