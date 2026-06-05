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

    // Reads static data, which forces the JIT to materialize the image-base and table-base
    // addresses via 'global.get' of the wasm base globals. Those are emitted as
    // WASM_GLOBAL_INDEX_LEB relocations that the R2R object writer must self-resolve back to
    // their fixed global indices; if that resolution regresses, crossgen2 throws while emitting
    // this method.
    public static int SumStaticData(int index)
    {
        s_counter += 1;
        return s_primes[index] + s_counter;
    }
}
