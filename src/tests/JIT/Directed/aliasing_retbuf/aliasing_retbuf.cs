// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class AliasingRetBuf
{
    public static int Main()
    {
        int failures = 0;

        Foo f = new Foo { A = 1, B = 2, C = 3 };
        CallPtrPInvoke(&f);
        if (f.A != 2 || f.B != 3 || f.C != 1)
        {
            Console.WriteLine("FAIL: After CallPtrPInvoke: {0}", f);
            failures |= 1;
        }

        f = new Foo { A = 1, B = 2, C = 3 };
        CallRefPInvoke(ref f);
        if (f.A != 2 || f.B != 3 || f.C != 1)
        {
            Console.WriteLine("FAIL: After CallRefPInvoke: {0}", f);
            failures |= 2;
        }

        f = new Foo { A = 1, B = 2, C = 3 };
        CallStructFieldPInvoke(ref f);
        if (f.A != 2 || f.B != 3 || f.C != 1)
        {
            Console.WriteLine("FAIL: After CallStructFieldPInvoke: {0}", f);
            failures |= 4;
        }

        IntPtr lib = NativeLibrary.Load(nameof(AliasingRetBufNative), typeof(AliasingRetBufNative).Assembly, null);
        IntPtr export = NativeLibrary.GetExport(lib, "TransposeRetBuf");

        f = new Foo { A = 3, B = 2, C = 1 };
        CallPtrFPtr(&f, (delegate* unmanaged[Cdecl, SuppressGCTransition]<Foo*, Foo>)export);
        if (f.A != 2 || f.B != 1 || f.C != 3)
        {
            Console.WriteLine("FAIL: After CallPtrFPtr: {0}", f);
            failures |= 8;
        }

        f = new Foo { A = 3, B = 2, C = 1 };
        CallStructFieldFPtr(ref f, (delegate* unmanaged[Cdecl, SuppressGCTransition]<Foo*, Foo>)export);
        if (f.A != 2 || f.B != 1 || f.C != 3)
        {
            Console.WriteLine("FAIL: After CallStructFieldFPtr: {0}", f);
            failures |= 16;
        }

        f = new Foo { A = 3, B = 2, C = 1 };
        CallRefFPtr(ref f, (delegate* unmanaged[Cdecl, SuppressGCTransition]<ref Foo, Foo>)export);
        if (f.A != 2 || f.B != 1 || f.C != 3)
        {
            Console.WriteLine("FAIL: After CallRefFPtr: {0}", f);
            failures |= 32;
        }

        if (failures == 0)
        {
            Console.WriteLine("PASS");
        }

        return 100 + failures;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallPtrPInvoke(Foo* fi)
    {
        *fi = AliasingRetBufNative.TransposeRetBufPtr(fi);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallRefPInvoke(ref Foo fi)
    {
        fi = AliasingRetBufNative.TransposeRetBufRef(ref fi);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallStructFieldPInvoke(ref Foo fi)
    {
        Fooer fooer = new() { F = fi };
        fooer.F = AliasingRetBufNative.TransposeRetBufPtr(&fooer.F);
        fi = fooer.F;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallPtrFPtr(Foo* fi, delegate* unmanaged[Cdecl, SuppressGCTransition]<Foo*, Foo> fptr)
    {
        *fi = fptr(fi);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallRefFPtr(ref Foo fi, delegate* unmanaged[Cdecl, SuppressGCTransition]<ref Foo, Foo> fptr)
    {
        fi = fptr(ref fi);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CallStructFieldFPtr(ref Foo fi, delegate* unmanaged[Cdecl, SuppressGCTransition]<Foo*, Foo> fptr)
    {
        Fooer fooer = new() { F = fi };
        fooer.F = fi;
        fooer.F = fptr(&fooer.F);
        fi = fooer.F;
    }

    private record struct Foo(nint A, nint B, nint C);
    private struct Fooer
    {
        public Foo F;
    }

    static class AliasingRetBufNative
    {
        [DllImport(nameof(AliasingRetBufNative), EntryPoint = "TransposeRetBuf", CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        public static unsafe extern Foo TransposeRetBufPtr(Foo* fi);

        [DllImport(nameof(AliasingRetBufNative), EntryPoint = "TransposeRetBuf", CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        public static unsafe extern Foo TransposeRetBufRef(ref Foo fi);
    }
}
