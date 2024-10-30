// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.RuntimeTypeSystem_1_NS;

internal static class RuntimeTypeSystem_1_Helpers
{
    public static bool IsTypeDesc(this TypeHandle type)
    {
        return type.Address != 0 && ((ulong)type.Address & (ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask) == (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
    }

    public static bool IsMethodTable(this TypeHandle type)
    {
        return type.Address != 0 && ((ulong)type.Address & (ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask) == (ulong)RuntimeTypeSystem_1.TypeHandleBits.MethodTable;
    }

    public static TargetPointer TypeDescAddress(this TypeHandle type)
    {
        if (!type.IsTypeDesc())
            return 0;

        return (ulong)type.Address & ~(ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask;
    }
}
