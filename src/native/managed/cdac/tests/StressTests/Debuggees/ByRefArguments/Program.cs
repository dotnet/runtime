// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NotYetLoadedArgLib;

namespace ByRefArgumentsDebuggee;

internal static class Program
{
    private static object? s_sink;
    private static bool s_resolved;

    private static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        bool success = RunCase(heap: false) && RunCase(heap: true);
        GC.KeepAlive(s_sink);
        return success && s_resolved ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RunCase(bool heap)
    {
        Span<char> stackValues = stackalloc char[] { 'a', 'b' };
        char[] heapValues = { 'a', 'b' };
        Base receiver = CreateReceiver();
        int result = receiver.Read(heap ? heapValues : stackValues, null, out bool valid);
        GC.KeepAlive(heapValues);
        return valid && result == 'a' + 'b';
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Base CreateReceiver() => new Derived();

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        if (new AssemblyName(args.Name).Name != "NotYetLoadedArgLib")
            return null;

        // Keep the Span and out-bool arguments in a live prestub frame while its signature type is resolved.
        for (int i = 0; i < 16; i++)
            s_sink = new object();

        string path = Path.Combine(AppContext.BaseDirectory, "lazydep", "NotYetLoadedArgLib.dll");
        Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
        s_resolved = true;
        return assembly;
    }
}

internal abstract class Base
{
    public abstract int Read(ReadOnlySpan<char> values, NotYetLoadedArg? arg, out bool valid);
}

internal sealed class Derived : Base
{
    private static object? s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Read(ReadOnlySpan<char> values, NotYetLoadedArg? arg, out bool valid)
    {
        for (int i = 0; i < 4; i++)
            s_sink = new object();
        GC.Collect();
        GC.KeepAlive(arg);
        GC.KeepAlive(s_sink);
        valid = values.Length == 2;
        return values[0] + values[1];
    }
}
