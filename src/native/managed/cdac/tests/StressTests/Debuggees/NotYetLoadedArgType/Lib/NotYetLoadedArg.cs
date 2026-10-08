// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NotYetLoadedArgLib;

/// <summary>
/// Reference type used ONLY as the parameter type of the debuggee's
/// <c>Derived.Func</c> override. It lives in its own assembly, which the
/// debuggee provides lazily via an <c>AssemblyResolve</c> handler. As a
/// result its TypeRef stays unresolved in the main module's
/// <c>TypeRefToMethodTable</c> lookup map until <c>Derived.Func</c> is first
/// JIT-compiled -- and the resolve handler (which allocates) runs synchronously
/// inside that window, while <c>Derived.Func</c>'s <c>PrestubMethodFrame</c> is
/// live on the stack.
///
/// The instance fields are GC references so a fully-resolved scan would report
/// the argument slot as a REF; the cDAC's out-of-process scan, unable to
/// resolve the not-yet-loaded type, drops the slot instead.
/// </summary>
public sealed class NotYetLoadedArg
{
    public object? Ref1;
    public string? Ref2;
}
