// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises GCFrame (GCPROTECT) root reporting.
/// Triggers AppDomain.AssemblyResolve by loading a missing assembly. The native
/// AppDomain::RaiseAssemblyResolveEvent invokes the managed handler while holding a
/// GCPROTECT frame over the requesting Assembly reference. The handler FailFasts so the
/// dump captures the thread with that GCFrame still live. The GC reports the GCFrame's protected
/// objects via GCFrame::GcScanRoots; the test verifies WalkStackReferences reports them too.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        TriggerResolve();
        Environment.FailFast("cDAC dump test: GCProtect debuggee did not hit the resolve handler");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TriggerResolve()
    {
        try
        {
            Assembly.Load("cDAC_Missing_Assembly_GCFrameMarker, Version=9.9.9.9, Culture=neutral, PublicKeyToken=null");
        }
        catch
        {
            // Resolution ultimately fails; the crash happens inside the handler below first.
        }
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        // Runs inside AppDomain::RaiseAssemblyResolveEvent's GCPROTECT(gc) scope, so the
        // requesting Assembly reference is held only by that native GCFrame at this point.
        Environment.FailFast("cDAC dump test: GCProtect debuggee intentional crash");
        return null;
    }
}
