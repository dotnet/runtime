// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Indices of the Wasm globals imported from the <c>webcil</c> host module into every R2R Wasm module.
    /// </summary>
    /// <remarks>
    /// Must stay in sync with <c>WasmObjectWriter._defaultGlobalImports</c>, <c>_globalSymbolNameToGlobalIndex</c>,
    /// and the host loader (<c>libCorerun.js</c>). The JIT references these via relocatable well-known-global
    /// handles (<c>CORINFO_WASM_WELLKNOWN_GLOBALS</c>), not the indices below.
    /// </remarks>
    public static class WasmGlobalImports
    {
        /// <summary>Mutable i32, linear-memory stack pointer.</summary>
        public const int StackPointerGlobalIndex = 0;

        /// <summary>Const i32, start of the R2R image in linear memory.</summary>
        public const int ImageBaseGlobalIndex = 1;

        /// <summary>Const i32, start of this module's slice in the shared function table.</summary>
        public const int TableBaseGlobalIndex = 2;

        /// <summary>Mutable i32, runtime-async continuation return value.</summary>
        public const int AsyncContinuationGlobalIndex = 3;
    }
}
