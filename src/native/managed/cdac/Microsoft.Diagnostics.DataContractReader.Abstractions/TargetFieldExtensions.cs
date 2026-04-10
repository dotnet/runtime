// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Extension methods for <see cref="Target"/> that provide typed field reading with optional
/// type validation. When the data descriptor includes type information (debug/checked builds),
/// these methods assert that the declared field type is compatible with the C# read type.
/// </summary>
public static class TargetFieldExtensions
{
    /// <summary>
    /// Read a primitive integer field from the target with type validation.
    /// </summary>
    public static T ReadField<T>(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        AssertPrimitiveType<T>(field, fieldName);

        return target.Read<T>(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read an optional primitive integer field from the target with type validation.
    /// Returns <paramref name="defaultValue"/> if the field is not present in the descriptor.
    /// </summary>
    public static T ReadFieldOrDefault<T>(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName, T defaultValue = default)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (!typeInfo.Fields.TryGetValue(fieldName, out Target.FieldInfo field))
            return defaultValue;

        AssertPrimitiveType<T>(field, fieldName);

        return target.Read<T>(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read a pointer field from the target with type validation.
    /// </summary>
    public static TargetPointer ReadPointerField(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        AssertPointerType(field, fieldName);

        return target.ReadPointer(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read an optional pointer field from the target with type validation.
    /// Returns <see cref="TargetPointer.Null"/> if the field is not present in the descriptor.
    /// </summary>
    public static TargetPointer ReadPointerFieldOrNull(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
    {
        if (!typeInfo.Fields.TryGetValue(fieldName, out Target.FieldInfo field))
            return TargetPointer.Null;

        AssertPointerType(field, fieldName);

        return target.ReadPointer(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read a native unsigned integer field from the target with type validation.
    /// </summary>
    public static TargetNUInt ReadNUIntField(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        Debug.Assert(
            field.TypeName is null or "" or "nuint",
            $"Type mismatch reading field '{fieldName}': declared as '{field.TypeName}', expected nuint");

        return target.ReadNUInt(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read a code pointer field from the target with type validation.
    /// </summary>
    public static TargetCodePointer ReadCodePointerField(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        Debug.Assert(
            field.TypeName is null or "" or "CodePointer",
            $"Type mismatch reading field '{fieldName}': declared as '{field.TypeName}', expected CodePointer");

        return target.ReadCodePointer(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read a field that contains an inline Data struct type, with type validation.
    /// Returns the data object created by <see cref="Target.IDataCache.GetOrAdd{T}"/>.
    /// </summary>
    public static T ReadDataField<T>(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
        where T : IData<T>
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        Debug.Assert(
            field.TypeName is null or "" || field.TypeName == typeof(T).Name,
            $"Type mismatch reading field '{fieldName}': declared as '{field.TypeName}', reading as {typeof(T).Name}");

        return target.ProcessedData.GetOrAdd<T>(address + (ulong)field.Offset);
    }

    /// <summary>
    /// Read a field that contains a pointer to a Data struct type, with type validation.
    /// Reads the pointer, then creates the data object via <see cref="Target.IDataCache.GetOrAdd{T}"/>.
    /// Returns null if the pointer is null.
    /// </summary>
    public static T? ReadDataFieldPointer<T>(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
        where T : IData<T>
    {
        Target.FieldInfo field = typeInfo.Fields[fieldName];
        AssertPointerType(field, fieldName);

        TargetPointer pointer = target.ReadPointer(address + (ulong)field.Offset);
        if (pointer == TargetPointer.Null)
            return default;

        return target.ProcessedData.GetOrAdd<T>(pointer);
    }

    /// <summary>
    /// Read an optional field that contains a pointer to a Data struct type, with type validation.
    /// Returns null if the field is not present in the descriptor or the pointer is null.
    /// </summary>
    public static T? ReadDataFieldPointerOrNull<T>(this Target target, ulong address, Target.TypeInfo typeInfo, string fieldName)
        where T : IData<T>
    {
        if (!typeInfo.Fields.TryGetValue(fieldName, out Target.FieldInfo field))
            return default;

        AssertPointerType(field, fieldName);

        TargetPointer pointer = target.ReadPointer(address + (ulong)field.Offset);
        if (pointer == TargetPointer.Null)
            return default;

        return target.ProcessedData.GetOrAdd<T>(pointer);
    }

    [Conditional("DEBUG")]
    private static void AssertPrimitiveType<T>(Target.FieldInfo field, string fieldName)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        Debug.Assert(
            field.TypeName is null or "" || IsCompatiblePrimitiveType<T>(field.TypeName),
            $"Type mismatch reading field '{fieldName}': declared as '{field.TypeName}', reading as {typeof(T).Name}");
    }

    [Conditional("DEBUG")]
    private static void AssertPointerType(Target.FieldInfo field, string fieldName)
    {
        Debug.Assert(
            field.TypeName is null or "" or "pointer",
            $"Type mismatch reading field '{fieldName}': declared as '{field.TypeName}', expected pointer");
    }

    private static bool IsCompatiblePrimitiveType<T>(string typeName)
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
