// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using CrossModuleLib;

/// <summary>
/// Stresses the cDAC ArgIterator encoder across module boundaries.
/// Every test method's signature references a type defined in
/// CrossModuleLib.dll; the encoder must resolve those TypeRef tokens
/// against the lib's MetadataReader (not the main exe's) and walk
/// fields whose enclosing MethodTable lives in the lib module.
///
/// Coverage:
///   - Class arg from other module (REF)
///   - By-value struct with embedded ref from other module (REF inside struct)
///   - Nested struct: outer + inner both in lib (cross-module GetFieldDescApproxTypeHandle)
///   - Mixed: outer in lib, contains string ref-field
///   - ByRefLike (ref struct) defined in other module
///   - Generic class instantiated with a main-module type
///   - Generic value type instantiated with a main-module type
///   - Generic struct with embedded ref, instantiated cross-module
/// </summary>
internal static class Program
{
    private static object? s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocBurst()
    {
        for (int i = 0; i < 32; i++)
            s_sink = new object();
    }

    private static int Main()
    {
        for (int iter = 0; iter < 50; iter++)
        {
            Drive();
        }
        GC.KeepAlive(s_sink);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Drive()
    {
        TakeClass(new ManagedHolder { Ref1 = new object(), Ref2 = "abc", Pad = 1 });

        TakeStructWithRef(new StructWithRef { Header = 1, Ref = new object(), Trailer = 2 });

        TakeOuter(new OuterWithCrossModuleInner
        {
            Pre = 1,
            Inner = new StructWithRef { Header = 10, Ref = new object(), Trailer = 11 },
            Tail = "tail",
        });

        Span<byte> sp = stackalloc byte[16];
        TakeCrossModuleRefStruct(new CrossModuleRefStruct
        {
            Header = 1,
            Payload = sp,
            Trailer = 2,
        });

        TakeGenericClass(new Generic<string> { Value = "g" });
        TakeGenericClassMainType(new Generic<MainModuleClass> { Value = new MainModuleClass { R = "m" } });
        TakeGenericValue(new GenericStruct<int> { Value = 42, Tag = 1 });
        TakeGenericValueWithRef(new GenericRefStruct<int> { Ref = new object(), Value = 7 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeClass(ManagedHolder h)
    {
        AllocBurst();
        GC.KeepAlive(h.Ref1);
        GC.KeepAlive(h.Ref2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeStructWithRef(StructWithRef s)
    {
        AllocBurst();
        GC.KeepAlive(s.Ref);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeOuter(OuterWithCrossModuleInner o)
    {
        AllocBurst();
        GC.KeepAlive(o.Inner.Ref);
        GC.KeepAlive(o.Tail);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TakeCrossModuleRefStruct(CrossModuleRefStruct s)
    {
        AllocBurst();
        return s.Header + s.Payload.Length + s.Trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeGenericClass(Generic<string> g)
    {
        AllocBurst();
        GC.KeepAlive(g);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeGenericClassMainType(Generic<MainModuleClass> g)
    {
        AllocBurst();
        GC.KeepAlive(g);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TakeGenericValue(GenericStruct<int> g)
    {
        AllocBurst();
        return g.Value + g.Tag;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TakeGenericValueWithRef(GenericRefStruct<int> g)
    {
        AllocBurst();
        GC.KeepAlive(g.Ref);
    }
}

/// <summary>
/// Defined in the main module. Used as a generic type argument so the
/// closed instantiation Generic&lt;MainModuleClass&gt; combines a lib-
/// module open generic with a main-module type arg. The signature
/// TypeSpec for the parameter mixes TypeRef (Generic`1 from lib) with
/// TypeDef (MainModuleClass from main).
/// </summary>
internal class MainModuleClass
{
    public string? R;
}
