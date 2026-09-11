// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Provides compatibility checks for data descriptor type names.
/// </summary>
public static class TargetTypeHelpers
{
    /// <summary>
    /// Determines whether a descriptor type is compatible with a primitive integer type.
    /// </summary>
    /// <typeparam name="T">The primitive integer type.</typeparam>
    /// <param name="typeName">The descriptor type name.</param>
    /// <returns><see langword="true" /> if the types are compatible; otherwise, <see langword="false" />.</returns>
    public static bool IsCompatiblePrimitiveType<T>(string? typeName)
        where T : struct, INumber<T>
    {
        return typeName switch
        {
            null or "" => true,
            "uint8" => typeof(T) == typeof(byte),
            "int8" => typeof(T) == typeof(sbyte),
            "uint16" => typeof(T) == typeof(ushort),
            "int16" => typeof(T) == typeof(short),
            "uint32" => typeof(T) == typeof(uint),
            "int32" => typeof(T) == typeof(int),
            "uint64" => typeof(T) == typeof(ulong),
            "int64" => typeof(T) == typeof(long),
            "bool" => typeof(T) == typeof(byte),
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether a descriptor type is compatible with a pointer.
    /// </summary>
    /// <param name="typeName">The descriptor type name.</param>
    /// <returns><see langword="true" /> if the type is pointer-compatible; otherwise, <see langword="false" />.</returns>
    public static bool IsCompatiblePointerType(string? typeName)
        // Managed field signatures report IntPtr/UIntPtr as native ints, while raw pointers
        // use "pointer". All three are read unsigned at the target's pointer width.
        => typeName is null or "" or "pointer" or "nint" or "nuint";

}
