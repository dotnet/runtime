// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Webcil;

public static class WasmWebcilModule
{
    private static readonly int[] s_primes = new int[] { 3, 5, 7, 11, 13 };
    private static int s_counter;

    public static int AddIntegers(int left, int right)
    {
        return left + right;
    }

    // Reads static data, which forces the JIT to materialize the image-base address via a
    // 'global.get' of the wasm image-base well-known global. That global is referenced through a
    // WASM_GLOBAL_INDEX_LEB relocation the R2R object writer must self-resolve back to the fixed
    // image-base global index; if that resolution regresses, the emitted 'global.get' encoding
    // changes (or crossgen2 throws while emitting this method).
    public static int SumStaticData(int index)
    {
        s_counter += 1;
        return s_primes[index] + s_counter;
    }

    // A try/finally makes the JIT emit a call to the 'finally' funclet (genCallFinally), which
    // computes the funclet's address from the wasm table-base well-known global via a 'global.get'.
    // Like the image base, that table-base global is referenced through a WASM_GLOBAL_INDEX_LEB
    // relocation the R2R object writer must self-resolve back to the fixed table-base global
    // index; if that resolution regresses, the emitted 'global.get' encoding changes (or
    // crossgen2 throws while emitting this method).
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
}
