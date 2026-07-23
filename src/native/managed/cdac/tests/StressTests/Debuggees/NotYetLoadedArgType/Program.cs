// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using NotYetLoadedArgLib;

namespace NotYetLoadedArgTypeDebuggee;

/// <summary>
/// Reproduces David's cDAC GC-stack-map hole for an argument whose type is not
/// yet loaded when the containing method is scanned.
///
/// Scenario (a managed analogue of the NoPIA / EmbedInteropTypes case):
///   * <see cref="NotYetLoadedArg"/> is a reference type defined in a *separate*
///     assembly (NotYetLoadedArgLib) that is NOT on the probing path -- it is
///     provided lazily by <see cref="OnAssemblyResolve"/>.
///   * <see cref="Base"/> declares <c>abstract void Func(NotYetLoadedArg?)</c>;
///     <see cref="Derived"/> overrides it. The caller (<see cref="Drive"/>)
///     only ever sees <c>Base::Func</c>, so the JIT has no reason to load
///     <c>NotYetLoadedArg</c>, and constructing <c>Derived</c> does not load it
///     either (it appears only in a method signature, never instantiated).
///   * The first virtual dispatch to <c>Derived.Func</c> enters its prestub and
///     JIT-compiles it. <c>CEEInfo::getArgClass(NotYetLoadedArg)</c> forces the
///     NotYetLoadedArgLib bind, which -- because the assembly is missing from the
///     app base -- raises <c>AssemblyResolve</c>. Our handler runs *and
///     allocates* synchronously in that window: <c>Derived.Func</c>'s
///     <c>PrestubMethodFrame</c> is live on the stack and <c>NotYetLoadedArg</c>'s
///     TypeRef is still unresolved in the module's <c>TypeRefToMethodTable</c>.
///
/// At that allocation the cdacstress hook fires. The runtime oracle classifies
/// the <c>NotYetLoadedArg</c> argument slot as a REF without loading the type
/// (<c>MetaSig::GcScanRoots</c> uses the static element-type table for
/// <c>ELEMENT_TYPE_CLASS</c>). The cDAC's out-of-process scan looks the TypeRef
/// up in the (still null) lookup map, cannot resolve it, and drops the slot --
/// the divergence this debuggee is meant to surface.
/// </summary>
internal static class Program
{
    private static object? s_sink;

    private static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        for (int iter = 0; iter < 50; iter++)
            Drive();

        GC.KeepAlive(s_sink);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Drive()
    {
        // Base-typed reference to a Derived instance. The JIT of this method
        // only sees Base::Func, so it never resolves NotYetLoadedArg.
        Base b = DerivedFactory.Create();

        // First call (iter 0) triggers Derived.Func's prestub + JIT ->
        // getArgClass(NotYetLoadedArg) -> AssemblyResolve -> allocation -> capture.
        // Pass null: the value is irrelevant. ARGITER compares the static
        // GCRefMap blob (which encodes the slot as REF regardless of value), so
        // the cDAC's dropped slot diverges from the runtime's byte-for-byte.
        b.Func(null);
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        AssemblyName requested = new(args.Name);
        if (!string.Equals(requested.Name, "NotYetLoadedArgLib", StringComparison.Ordinal))
            return null;

        // Allocate on the GC heap *inside* the resolve callback so the
        // cdacstress allocation hook fires while NotYetLoadedArg is still
        // unresolved and Derived.Func's PrestubMethodFrame is live. The burst
        // gives the hook many chances to capture this thread mid-window.
        for (int i = 0; i < 64; i++)
            s_sink = new object();

        string path = Path.Combine(AppContext.BaseDirectory, "lazydep", "NotYetLoadedArgLib.dll");
        return Assembly.Load(File.ReadAllBytes(path));
    }
}

internal abstract class Base
{
    public abstract void Func(NotYetLoadedArg? a);
}

internal sealed class Derived : Base
{
    private static object? s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Func(NotYetLoadedArg? a)
    {
        // By the time this body runs NotYetLoadedArg is already loaded; the
        // interesting window is this method's prestub, before getArgClass.
        for (int i = 0; i < 4; i++)
            s_sink = new object();
        GC.KeepAlive(a);
    }
}

internal static class DerivedFactory
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Base Create() => new Derived();
}
