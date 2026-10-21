// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Operation();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Register(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong Metric(uint index);

    private sealed class Holder
    {
        public object? Scalar;
        public object? Atomic;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(object?[] fields, int worker, Operation attach)
    {
        if (attach() != 0)
        {
            throw new InvalidOperationException("RC observer thread registration failed.");
        }
        for (int index = worker; index < fields.Length; index += 4)
        {
            fields[index] = null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ScalarAndAtomic(Holder holder, object replacement, object wrong)
    {
        holder.Scalar = replacement;
        Interlocked.CompareExchange(ref holder.Atomic, replacement, wrong);
    }

    private static T Load<T>(nint library, string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    private static int ValidateBuiltIn(nint library)
    {
        if (NativeLibrary.TryGetExport(library, "GC_LxrRcVerticalObserver_Begin", out _))
        {
            Console.Error.WriteLine("Default-off runtime unexpectedly exports the RC observer.");
            return 7;
        }
        object expected = new();
        object?[] roots = new object?[4097];
        Array.Fill(roots, expected);
        for (int iteration = 0; iteration < 8; iteration++)
        {
            object?[] garbage = new object?[1025];
            Array.Fill(garbage, new object());
            Array.Clear(garbage);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
        foreach (object? root in roots)
        {
            if (!ReferenceEquals(root, expected))
            {
                Console.Error.WriteLine("Built-in GC lost a live array reference.");
                return 8;
            }
        }
        Console.WriteLine("P4-DEFAULT-OFF-SMOKE PASS; built-in GC collection and live references preserved");
        GC.KeepAlive(roots);
        return 0;
    }

    public static int Main(string[] args)
    {
        if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--builtin"))
        {
            Console.Error.WriteLine("Expected the exact CoreCLR library path and optional --builtin.");
            return 1;
        }
        nint library = NativeLibrary.Load(args[0]);
        if (args.Length == 2)
        {
            return ValidateBuiltIn(library);
        }
        Operation begin = Load<Operation>(library, "GC_LxrRcVerticalObserver_Begin");
        Register register = Load<Register>(library, "GC_LxrRcVerticalObserver_Register");
        Operation attach = Load<Operation>(library, "GC_LxrRcVerticalObserver_Attach");
        Operation end = Load<Operation>(library, "GC_LxrRcVerticalObserver_End");
        Metric metric = Load<Metric>(library, "GC_LxrRcVerticalObserver_GetMetric");
        object old = new();
        object replacement = new();
        object wrong = new();
        object?[] fields = new object?[4097];
        object?[] bulk = new object?[1025];
        Array.Fill(fields, old);
        Array.Fill(bulk, old);
        Holder holder = new() { Scalar = old, Atomic = old };
        GCHandle fieldsHandle = GCHandle.Alloc(fields);
        GCHandle bulkHandle = GCHandle.Alloc(bulk);
        GCHandle holderHandle = GCHandle.Alloc(holder);
        bool noGc = GC.TryStartNoGCRegion(64 * 1024 * 1024, disallowFullBlockingGC: false);
        if (!noGc)
        {
            Console.Error.WriteLine("Unable to establish a stable no-GC observation window.");
            fieldsHandle.Free();
            bulkHandle.Free();
            holderHandle.Free();
            return 2;
        }
        bool observing = false;
        try
        {
            if (begin() != 0)
            {
                Console.Error.WriteLine("RC observer initialization failed.");
                return 3;
            }
            observing = true;
            if (register(GCHandle.ToIntPtr(fieldsHandle)) != 0 ||
                register(GCHandle.ToIntPtr(bulkHandle)) != 0 ||
                register(GCHandle.ToIntPtr(holderHandle)) != 0 || attach() != 0)
            {
                Console.Error.WriteLine("RC observer setup failed.");
                return 3;
            }
            ScalarAndAtomic(holder, replacement, wrong);
            Array.Clear(bulk);
            Thread[] threads = new Thread[4];
            Exception? threadError = null;
            for (int worker = 0; worker < threads.Length; worker++)
            {
                int index = worker;
                threads[worker] = new Thread(() =>
                {
                    try
                    {
                        Mutate(fields, index, attach);
                    }
                    catch (InvalidOperationException error)
                    {
                        Interlocked.CompareExchange(ref threadError, error, null);
                    }
                });
                threads[worker].Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            if (threadError is not null)
            {
                Console.Error.WriteLine(threadError.Message);
                return 4;
            }
            int endResult = end();
            observing = false;
            if (endResult != 0)
            {
                Console.Error.WriteLine("RC observer window changed or root/log draining failed.");
                return 5;
            }
            const ulong Expected = 4097 + 1025 + 2;
            if (metric(0) != Expected || metric(1) != Expected || metric(2) != 2 ||
                metric(3) != 4 || metric(4) < 10 || metric(5) == 0 ||
                metric(6) == 0 || metric(7) != 0 ||
                !ReferenceEquals(holder.Atomic, old) || !ReferenceEquals(holder.Scalar, replacement))
            {
                Console.Error.WriteLine(
                    $"RC hook mismatch: inc={metric(0)} dec={metric(1)} current={metric(2)} " +
                    $"exits={metric(3)} flushes={metric(4)} births={metric(5)} roots={metric(6)} errors={metric(7)}");
                return 6;
            }
            Console.WriteLine(
                $"P4-RUNTIME-HOOK-SMOKE PASS inc={metric(0)} dec={metric(1)} " +
                $"thread-exits={metric(3)} roots={metric(6)}; observer only, built-in GC owns managed memory");
            GC.KeepAlive(old);
            GC.KeepAlive(replacement);
            GC.KeepAlive(wrong);
            return 0;
        }
        finally
        {
            if (observing)
            {
                int cleanupResult = end();
                if (cleanupResult != 0)
                {
                    Console.Error.WriteLine($"RC observer cleanup failed: {cleanupResult}.");
                }
            }
            GC.EndNoGCRegion();
            fieldsHandle.Free();
            bulkHandle.Free();
            holderHandle.Free();
        }
    }
}
