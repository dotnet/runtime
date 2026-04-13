// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Debug-only helpers that validate cDAC type annotations match the C# read type.
/// In release builds, all methods are completely elided by the <see cref="ConditionalAttribute"/>.
/// </summary>
public static class DataDescriptorTypeValidation
{
    /// <summary>
    /// Assert that a declared field type name is compatible with the C# primitive integer type <typeparamref name="T"/>.
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertPrimitiveType<T>(string? typeName, string context)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        Debug.Assert(
            typeName is null or "" || IsCompatiblePrimitiveType<T>(typeName),
            $"Type mismatch reading {context}: declared as '{typeName}', reading as {typeof(T).Name}");
    }

    /// <summary>
    /// Assert that a declared field type name is "pointer" (or absent).
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertPointerType(string? typeName, string context)
    {
        Debug.Assert(
            typeName is null or "" or "pointer",
            $"Type mismatch reading {context}: declared as '{typeName}', expected pointer");
    }

    /// <summary>
    /// Assert that a global's declared type is compatible with <c>ReadGlobal&lt;T&gt;</c>.
    /// Pointer-like types (nuint, nint, pointer) must use <c>ReadGlobalPointer</c> instead.
    /// String-typed globals are allowed since they may carry dual numeric/string values.
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertGlobalType<T>(string? typeName, string globalName)
        where T : struct, INumber<T>
    {
        if (typeName is null or "" or "string")
            return;

        bool compatible = typeName switch
        {
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

        Debug.Assert(compatible,
            $"Type mismatch reading global '{globalName}': declared as '{typeName}', reading as {typeof(T).Name}. " +
            $"Pointer-like globals (pointer, nuint, nint) should be read via ReadGlobalPointer.");
    }

    /// <summary>
    /// Assert that a global's declared type is compatible with <c>ReadGlobalPointer</c>.
    /// Accepts pointer, nuint, nint, or absent type information.
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertGlobalPointerType(string? typeName, string globalName)
    {
        Debug.Assert(
            typeName is null or "" or "pointer" or "nuint" or "nint" or "string",
            $"Type mismatch reading global '{globalName}' as pointer: declared as '{typeName}', expected pointer/nuint/nint");
    }

    public static bool IsCompatiblePrimitiveType<T>(string typeName)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        return typeName switch
        {
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
}
