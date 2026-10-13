// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Webcil;

public static class WasmWebcilModule
{
    public sealed class LdtokenType
    {
    }

    public sealed class LdtokenHolder<T>
    {
    }

    public struct GenericTokenValue
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadIntrinsicTypeToken(int index) => index switch
    {
        0 => typeof(void),
        1 => typeof(bool),
        2 => typeof(char),
        3 => typeof(sbyte),
        4 => typeof(byte),
        5 => typeof(short),
        6 => typeof(ushort),
        7 => typeof(int),
        8 => typeof(uint),
        9 => typeof(long),
        10 => typeof(ulong),
        11 => typeof(float),
        12 => typeof(double),
        13 => typeof(nint),
        14 => typeof(nuint),
        15 => typeof(string),
        16 => typeof(object),
        17 => typeof(TypedReference),
        18 => typeof(bool[]),
        19 => typeof(int[,]),
        20 => typeof(int[][]),
        21 => typeof(string[]),
        22 => typeof(object[,,]),
        23 => typeof(int[,][]),
        _ => typeof(nuint[]),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadTokenDependentTypeToken(int index) => index switch
    {
        0 => typeof(GenericTokenValue),
        1 => typeof(LdtokenType),
        2 => typeof(DayOfWeek),
        3 => typeof(decimal),
        4 => typeof(Half),
        5 => typeof(int?),
        6 => typeof(LdtokenHolder<int>),
        7 => typeof(LdtokenType[]),
        _ => typeof(GenericTokenValue[,][]),
    };

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
