// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop;

/// <summary>Identifies types with special marshalling or code-generation rules.</summary>
public enum WellKnownType
{
    /// <summary>No special type classification.</summary>
    None,
    /// <summary>The base type of enumeration types.</summary>
    System_Enum,
    /// <summary>The void type.</summary>
    System_Void,
    /// <summary>The Boolean type.</summary>
    System_Boolean,
    /// <summary>The character type.</summary>
    System_Char,
    /// <summary>The signed byte type.</summary>
    System_SByte,
    /// <summary>The unsigned byte type.</summary>
    System_Byte,
    /// <summary>The signed 16-bit integer type.</summary>
    System_Int16,
    /// <summary>The unsigned 16-bit integer type.</summary>
    System_UInt16,
    /// <summary>The signed 32-bit integer type.</summary>
    System_Int32,
    /// <summary>The unsigned 32-bit integer type.</summary>
    System_UInt32,
    /// <summary>The signed 64-bit integer type.</summary>
    System_Int64,
    /// <summary>The unsigned 64-bit integer type.</summary>
    System_UInt64,
    /// <summary>The single-precision floating-point type.</summary>
    System_Single,
    /// <summary>The double-precision floating-point type.</summary>
    System_Double,
    /// <summary>The string type.</summary>
    System_String,
    /// <summary>The signed native-sized integer type.</summary>
    System_IntPtr,
    /// <summary>The unsigned native-sized integer type.</summary>
    System_UIntPtr
}

/// <summary>Classifies the primitive types used in marshalling.</summary>
public static class WellKnownTypeExtensions
{
    /// <summary>Determines whether a type can represent a collection element count.</summary>
    /// <param name="type">The type to classify.</param>
    /// <returns><see langword="true"/> if the type is integral; otherwise, <see langword="false"/>.</returns>
    public static bool IsIntegralType(this WellKnownType type) => type is
        WellKnownType.System_SByte or WellKnownType.System_Byte
        or WellKnownType.System_Int16 or WellKnownType.System_UInt16
        or WellKnownType.System_Int32 or WellKnownType.System_UInt32
        or WellKnownType.System_Int64 or WellKnownType.System_UInt64
        or WellKnownType.System_IntPtr or WellKnownType.System_UIntPtr;

    /// <summary>Determines whether a type is always blittable.</summary>
    /// <param name="type">The type to classify.</param>
    /// <returns><see langword="true"/> if the type is always blittable; otherwise, <see langword="false"/>.</returns>
    public static bool IsAlwaysBlittable(this WellKnownType type) => type.IsIntegralType()
        || type is WellKnownType.System_Void or WellKnownType.System_Single or WellKnownType.System_Double;
}
