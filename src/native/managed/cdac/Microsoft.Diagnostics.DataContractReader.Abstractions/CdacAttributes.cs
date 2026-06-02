// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Marks a class as a cdac data type. The cdac source generator emits an
/// <c>IData&lt;T&gt;.Create</c> factory that reads the declared
/// <see cref="FieldAttribute"/>-marked properties from the descriptor-resolved
/// <c>Target.TypeInfo</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CdacTypeAttribute : Attribute
{
    /// <summary>
    /// One or more candidate type names tried in priority order. Each name
    /// is tried against the native cdac descriptor first; if no match is
    /// found, it is then tried against the managed type metadata via
    /// <c>IManagedTypeSource</c>. This allows a type to be migrated from
    /// managed to native descriptor (or vice versa) in the runtime without
    /// changing the CDAC code.
    /// </summary>
    public CdacTypeAttribute(params string[] names)
    {
        Names = names;
    }

    /// <summary>Candidate type names in priority order.</summary>
    public string[] Names { get; }

    /// <summary>
    /// When <c>true</c>, the generator emits a <c>TypeHandle(Target)</c>
    /// accessor that resolves the runtime <c>TypeHandle</c> by trying each
    /// candidate name against <c>IManagedTypeSource</c>.
    /// </summary>
    public bool HasTypeHandle { get; set; }
}

/// <summary>
/// Marks a property as a descriptor field. The cdac source generator emits a
/// read in <c>IData&lt;T&gt;.Create</c> with the read kind inferred from the
/// property type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FieldAttribute : Attribute
{
    /// <summary>Field name(s) default to the C# property name.</summary>
    public FieldAttribute()
    {
    }

    /// <summary>
    /// One or more candidate descriptor field names tried in priority order.
    /// The cdac LayoutSet cascade tries each name against the native cdac
    /// descriptor first; if none match (or the native descriptor isn't
    /// available), it then tries each name against the managed type metadata.
    /// </summary>
    public FieldAttribute(params string[] names)
    {
        Names = names;
    }

    /// <summary>
    /// Candidate field names tried in priority order against the native
    /// descriptor first, then the managed descriptor. The C# property name
    /// is appended as a lowest-priority candidate when
    /// <see cref="UsePropertyName"/> is <c>true</c> (the default).
    /// </summary>
    public string[]? Names { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), the C# property name is appended to
    /// the candidate name list as the lowest-priority entry. Set to
    /// <c>false</c> to suppress the fallback -- the cascade will then only
    /// try the names explicitly listed in <see cref="Names"/>.
    /// </summary>
    public bool UsePropertyName { get; set; } = true;

    /// <summary>
    /// For <c>IData&lt;T&gt;</c>-typed properties: read a pointer field then
    /// dereference via <c>target.ProcessedData.GetOrAdd&lt;T&gt;</c>. Without
    /// this, <see cref="FieldAttribute"/> on an <c>IData</c> property does an
    /// in-place <c>ReadDataField&lt;T&gt;</c>.
    /// </summary>
    public bool Pointer { get; set; }

    /// <summary>
    /// When <c>true</c>, the generator emits a
    /// <c>Write{PropName}(Target, T)</c> method that writes the value back
    /// to the target's memory and updates the property snapshot. The
    /// property must have a settable accessor (typically
    /// <c>{ get; private set; }</c>) and a primitive type
    /// (<c>uint</c>, <c>int</c>, ..., <c>bool</c>).
    /// </summary>
    public bool Writable { get; set; }

    /// <summary>
    /// For <c>bool</c> properties: specifies the underlying type in the data
    /// descriptor when the runtime stores a boolean as a wider integer type
    /// (e.g., <c>int32</c>).
    /// </summary>
    public Type UnderlyingBoolType { get; set; } = typeof(byte);
}

/// <summary>
/// Marks a property as read from a hardcoded byte offset relative to the
/// instance address, bypassing the cdac type-info descriptor. Used for
/// well-known external layouts such as PE/COFF and Webcil headers where the
/// offsets are fixed by file format rather than by the runtime descriptor.
/// </summary>
/// <remarks>
/// The read kind is inferred from the property type, the same way as
/// <see cref="FieldAttribute"/>: primitives use <c>target.Read&lt;T&gt;</c>
/// (or <c>ReadLittleEndian&lt;T&gt;</c> when <see cref="LittleEndian"/> is
/// set), <c>TargetPointer</c> uses <c>ReadPointer</c>, <c>TargetNUInt</c>
/// uses <c>ReadNUInt</c>, <c>TargetNInt</c> uses <c>ReadNInt</c>,
/// <c>TargetCodePointer</c> uses <c>ReadCodePointer</c>,
/// and <c>IData&lt;T&gt;</c>-typed properties use
/// <c>target.ProcessedData.GetOrAdd&lt;T&gt;</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class RawOffsetAttribute : Attribute
{
    public RawOffsetAttribute(int offset)
    {
        Offset = offset;
    }

    public int Offset { get; }

    /// <summary>
    /// When <c>true</c>, primitive reads use <c>target.ReadLittleEndian&lt;T&gt;</c>.
    /// Required for file-format layouts that are always little-endian (PE/COFF).
    /// </summary>
    public bool LittleEndian { get; set; }
}

/// <summary>
/// Marks a <c>TargetPointer</c> property as the address of the descriptor
/// field with the given name(s). Generator emits
/// <c>address + (ulong)type.Fields[name].Offset</c> for the first name that
/// resolves in either source; no read is performed.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FieldAddressAttribute : Attribute
{
    public FieldAddressAttribute()
    {
    }

    public FieldAddressAttribute(params string[] names)
    {
        Names = names;
    }

    /// <summary>
    /// Candidate field names tried in priority order against the native
    /// descriptor first, then the managed descriptor. The C# property name
    /// is appended as a lowest-priority candidate when
    /// <see cref="UsePropertyName"/> is <c>true</c> (the default).
    /// </summary>
    public string[]? Names { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), the C# property name is appended to
    /// the candidate name list as the lowest-priority entry.
    /// </summary>
    public bool UsePropertyName { get; set; } = true;
}

/// <summary>
/// Marks a <c>TargetPointer</c> property as the start of the instance data
/// region: <c>address + type.Size</c>. Used by <c>Array</c> and <c>Object</c>
/// for the post-header / post-MT byte.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class InstanceDataStartAttribute : Attribute
{
}

/// <summary>
/// Marks a static partial method as a static-field-address accessor. Method
/// must return <c>TargetPointer</c> and take a <c>Target</c> parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StaticAddressAttribute : Attribute
{
    public StaticAddressAttribute(string fieldName)
    {
        FieldName = fieldName;
    }

    public string FieldName { get; }
}

/// <summary>
/// Marks a static partial method as a managed-object-static accessor: looks
/// up the static slot, dereferences it as a pointer, and returns
/// <c>TargetPointer.Null</c> when the field is missing or the static base is
/// not yet allocated.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StaticReferenceAttribute : Attribute
{
    public StaticReferenceAttribute(string fieldName)
    {
        FieldName = fieldName;
    }

    public string FieldName { get; }
}

/// <summary>
/// Marks a static partial method as a thread-static-field-address accessor.
/// Method must return <c>TargetPointer</c> and take
/// <c>(Target target, TargetPointer thread)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ThreadStaticAddressAttribute : Attribute
{
    public ThreadStaticAddressAttribute(string fieldName)
    {
        FieldName = fieldName;
    }

    public string FieldName { get; }
}
