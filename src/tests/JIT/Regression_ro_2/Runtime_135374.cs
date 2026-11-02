// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;
using Xunit;

public unsafe class Runtime_135374
{
    private struct IFoo
    {
        public void** lpVtbl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get() => ((delegate* unmanaged<IFoo*, int>)lpVtbl[0])((IFoo*)Unsafe.AsPointer(ref this));
    }

    private static IFoo* s_foo;
    private static volatile bool s_stop;
    private static int s_sink;

    [UnmanagedCallersOnly]
    private static int GetImpl(IFoo* self) => 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IFoo* Create() => s_foo;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Check(string name, bool condition) => s_sink += condition ? name.Length : 0;

    // The hoisted string pieces take the callee-saved registers, so 'p' is spilled and was reloaded
    // as a byref into a callee-saved register that was then reported live across an unmanaged call.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(string[] names)
    {
        foreach (string name in names)
        {
            IFoo* p = Create();
            Check($"[{name}] a", p->Get() == 0);
            Check($"[{name}] b", p->Get() == 0);
            Check($"[{name}] c", p->Get() == 0);
        }
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    [SkipOnMono("CoreCLR JIT regression test")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/124219", typeof(PlatformDetection), nameof(PlatformDetection.IsWasm))]
    public static void TestEntryPoint()
    {
        void** vtbl = (void**)NativeMemory.Alloc((nuint)sizeof(void*));
        vtbl[0] = (delegate* unmanaged<IFoo*, int>)&GetImpl;
        s_foo = (IFoo*)NativeMemory.Alloc((nuint)sizeof(IFoo));
        s_foo->lpVtbl = vtbl;

        // GCs triggered by another thread scan this thread while it's inside the unmanaged call.
        Thread thread = new Thread(() =>
        {
            while (!s_stop)
            {
                GC.KeepAlive(new byte[600_000]);
            }
        });
        thread.IsBackground = true;
        thread.Start();

        int iterations = 0;
        try
        {
            string[] names = ["one", "two"];
            Stopwatch sw = Stopwatch.StartNew();
            while ((iterations < 200_000) && (sw.ElapsedMilliseconds < 2000))
            {
                Test(names);
                iterations++;
            }
        }
        finally
        {
            s_stop = true;
            thread.Join();
            NativeMemory.Free(s_foo);
            NativeMemory.Free(vtbl);
        }

        Assert.Equal(iterations * 2 * 3 * "[one] a".Length, s_sink);
    }
}
