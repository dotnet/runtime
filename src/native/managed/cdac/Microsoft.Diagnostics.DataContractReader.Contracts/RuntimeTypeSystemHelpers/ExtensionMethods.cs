// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

internal static class ExtensionMethods
{
    public static bool IsTypeDesc(this ITypeHandle type)
    {
        return type.Address != TargetPointer.Null && ((ulong)type.Address & (ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask) == (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
    }

    public static bool IsMethodTable(this ITypeHandle type)
    {
        return type.Address != TargetPointer.Null && ((ulong)type.Address & (ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask) == (ulong)RuntimeTypeSystem_1.TypeHandleBits.MethodTable;
    }

    public static TargetPointer TypeDescAddress(this ITypeHandle type)
    {
        if (!type.IsTypeDesc())
            return TargetPointer.Null;

        return (ulong)type.Address & ~(ulong)RuntimeTypeSystem_1.TypeHandleBits.ValidMask;
    }
}
