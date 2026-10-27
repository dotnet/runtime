// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Webcil;

public static class WasmWebcilModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string FixupLeaf() => "transitive-precode-string";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string FixupMiddle(bool useCallee)
    {
        string value = FixupLeaf();
        return useCallee ? value : "transitive-precode-string";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string FixupOuter(bool useCallee)
    {
        string value = FixupMiddle(useCallee);
        return useCallee ? value : "transitive-precode-string";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string UncoveredFixup() => "uncovered-precode-string";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadGenericTypeToken<T>() => typeof(T);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadGenericIntToken() => ReadGenericTypeToken<int>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadIntToken() => typeof(int);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadGenericValueToken() => ReadGenericTypeToken<GenericTokenValue>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadValueToken() => typeof(GenericTokenValue);

    public struct GenericTokenValue
    {
        public int Value;
    }

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
