// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace CrossModuleLib;

/// <summary>
/// Reference type with embedded refs. Used as a method-arg type in the
/// CrossModule debuggee; the cDAC encoder's REF token emission for the
/// argument slot doesn't need cross-module metadata for the arg itself,
/// but the type's identity is resolved through this assembly's
/// MetadataReader.
/// </summary>
public class ManagedHolder
{
    public object? Ref1;
    public string? Ref2;
    public int Pad;
}

/// <summary>
/// Value type with an embedded GC ref. Exercises the encoder's
/// GCDesc-driven REF emission across module boundaries: the
/// argument's ITypeHandle resolves through the main module's
/// CrossModule.exe metadata, but the field-list walk (and offset
/// arithmetic) crosses into this library's MethodTable.
/// </summary>
public struct StructWithRef
{
    public int Header;
    public object? Ref;
    public int Trailer;
}

/// <summary>
/// Nested value type whose Inner field is a value type defined in the
/// same library. Exercises GetFieldDescApproxTypeHandle's cross-module
/// resolution when the outer struct's enclosing module differs from
/// the inner field's referenced module.
/// </summary>
public struct OuterWithCrossModuleInner
{
    public int Pre;
    public StructWithRef Inner;
    public string? Tail;
}

/// <summary>
/// ByRefLike struct defined in another module. Exercises the cDAC's
/// MethodTableFlags.IsByRefLike check after metadata resolution
/// crosses module boundaries.
/// </summary>
public ref struct CrossModuleRefStruct
{
    public int Header;
    public Span<byte> Payload;
    public int Trailer;
}

/// <summary>
/// Generic class definition. The closed instantiation Generic&lt;string&gt;
/// is constructed at the use site in the main module, so the signature
/// TypeRef→TypeSpec resolution path walks both modules.
/// </summary>
public class Generic<T>
{
    public T? Value;
}

public struct GenericStruct<T>
{
    public T? Value;
    public int Tag;
}

/// <summary>
/// Generic struct with an embedded GC ref. Encoder must walk this
/// type's GCDesc when an instantiation (e.g. GenericRefStruct&lt;int&gt;)
/// is used as a by-value arg in the main module.
/// </summary>
public struct GenericRefStruct<T>
{
    public object? Ref;
    public T? Value;
}
