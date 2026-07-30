// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DataGenerator;

/// <summary>
/// The kind of read the generator should emit for a <c>[Field]</c> property.
/// </summary>
internal enum FieldReadKind
{
    /// <summary><c>target.ReadField&lt;T&gt;(...)</c> -- primitive integer.</summary>
    Primitive,

    /// <summary><c>target.ReadField&lt;byte&gt;(...) != 0</c> -- bool stored as byte.</summary>
    Bool,

    /// <summary><c>target.ReadPointerField(...)</c>.</summary>
    Pointer,

    /// <summary><c>target.ReadNUIntField(...)</c>.</summary>
    NUInt,
    /// <summary><c>target.ReadNIntField(...)</c>.</summary>
    NInt,

    /// <summary><c>target.ReadCodePointerField(...)</c>.</summary>
    CodePointer,

    /// <summary><c>target.ReadDataField&lt;T&gt;(...)</c> -- in-place IData.</summary>
    DataInPlace,

    /// <summary><c>target.ReadDataFieldPointer&lt;T&gt;(...)</c> -- pointer to IData.</summary>
    DataPointer,
}

/// <summary>The kind of generated member.</summary>
internal enum MemberKind
{
    Field,
    FieldAddress,
    InstanceDataStart,
    StaticAddress,
    StaticReference,
    ThreadStaticAddress,
}

/// <summary>
/// The setter a generated <c>[Field]</c> property exposes. A field is either
/// read-only, privately settable (populated internally, e.g. by <c>OnInit</c>
/// or a hand-written constructor), or writable (a <c>Write{Name}</c> method
/// writes the value back to the target). A <c>[Field]</c> never exposes a
/// public setter -- mutation always goes through <c>Write{Name}</c>.
/// </summary>
internal enum SetterKind
{
    None,
    Private,
    Writable,
}

/// <summary>
/// A single member (property or method) of a <c>[CdacType]</c> class that
/// the generator must emit code for.
/// </summary>
internal sealed record MemberModel(
    string Name,
    MemberKind Kind,
    string DescriptorOrFieldName,
    string? DescriptorNativeType,
    string PropertyOrReturnTypeFqn,
    FieldReadKind ReadKind,
    string? DataTypeArgumentFqn,
    bool IsOptional,
    bool IsNullable,
    int? RawOffset,
    bool LittleEndian,
    SetterKind Setter,
    string? BoolUnderlyingType,
    EquatableArray<string> Names) : IEquatable<MemberModel>;

/// <summary>
/// A <c>[CdacType]</c>-annotated class to be emitted.
/// </summary>
internal sealed record CdacTypeModel(
    string Namespace,
    string ClassName,
    string Accessibility,
    bool IsSealed,
    bool IsPartial,
    EquatableArray<string> Names,
    bool HasTypeHandle,
    EquatableArray<MemberModel> Members) : IEquatable<CdacTypeModel>;
