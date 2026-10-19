// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Webcil;

public static class WasmWebcilModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nint ReadIntPtrZero() => IntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nuint ReadUIntPtrZero() => UIntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string ReadStringEmpty() => string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ReadIsLittleEndian() => BitConverter.IsLittleEndian;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly nint AddressIntPtrZero() => ref IntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly nuint AddressUIntPtrZero() => ref UIntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly string AddressStringEmpty() => ref string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly bool AddressIsLittleEndian() => ref BitConverter.IsLittleEndian;

    private static readonly int[] s_primes = new int[] { 3, 5, 7, 11, 13 };
    private static int s_counter;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AddIntegers(int left, int right)
    {
        return left + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FoldableBodyOne()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FoldableBodyTwo()
    {
    }

    // Explicit construction avoids distinct compiler-generated delegate cache fields.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MethodInfo SelfMethodOne() => new Func<MethodInfo>(SelfMethodOne).Method;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MethodInfo SelfMethodTwo() => new Func<MethodInfo>(SelfMethodTwo).Method;

    // Reads static data, which forces the JIT to materialize the imageBase address via a
    // 'global.get' of the wasm imageBase well-known global.
    public static int SumStaticData(int index)
    {
        s_counter += 1;
        return AddIntegers(s_primes[index], s_counter);
    }

    // An unoptimized try/finally makes the JIT emit a call to the 'finally'
    // funclet (genCallFinally), which computes the funclet's address from the
    // wasm table-base well-known global via a 'global.get'. Like the image
    // base, that table-base global is referenced through a
    // WASM_GLOBAL_INDEX_LEB relocation the R2R object writer must self-resolve
    // back to the fixed table-base global index; if that resolution regresses,
    // the emitted 'global.get' encoding changes (or crossgen2 throws while
    // emitting this method).
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int SumWithFinally(int index)
    {
        int total = 0;
        try
        {
            total = s_primes[index];
        }
        finally
        {
            s_counter++;
        }
        return total + s_counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CatchExceptionOne(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CatchExceptionTwo(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }
}

public class WasmSelfForwarder
{
    private readonly WasmSelfForwarder _target;

    public WasmSelfForwarder(WasmSelfForwarder? target = null)
    {
        _target = target ?? this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int BeforeEvent(int value) => _target.BeforeEvent(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int AfterEvent(int value) => _target.AfterEvent(value);
}
