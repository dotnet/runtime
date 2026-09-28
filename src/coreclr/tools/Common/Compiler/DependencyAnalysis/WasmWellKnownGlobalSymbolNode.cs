// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents one of the well-known wasm globals referenced by JIT-generated code.
    /// These are imported globals whose final index is assigned by the ObjectWriter / wasm linker.
    /// crossgen2/R2R resolves it back to the fixed index defined in the WebCIL format, while a relocatable
    /// NativeAOT object emits it as an undefined imported global for wasm-ld to resolve.
    /// </summary>
    /// <remarks>
    /// The names deliberately match what <c>wasm-ld</c> uses, so that a composite's imports resolve
    /// against the host's exports by name and <c>wasm-merge</c> needs no renaming step.
    /// <c>__stack_pointer</c> and <c>__indirect_function_table</c> come from the linker directly;
    /// <c>__memory_base</c> and <c>__table_base</c> are wasm-ld's PIC names for exactly these two
    /// quantities (where a module's data and table slice begin) and must be defined and exported by
    /// the host, since a non-PIC main module does not produce them on its own.
    /// <para>
    /// Those last two belong to the emscripten/wasm-ld dynamic linking ABI, where they carry a
    /// per-side-module meaning. Reusing them is safe only while the host is a non-PIC main module.
    /// Building the host with <c>-sMAIN_MODULE</c> would give the linker its own definitions of both
    /// and collide with these; that would require picking runtime-specific names instead.
    /// </para>
    /// </remarks>
    public class WasmWellKnownGlobalSymbolNode(string symbolName) : ExternDataSymbolNode(new Utf8String(symbolName))
    {
        public const string StackPointerName = "__stack_pointer";
        public const string ImageBaseName = "__memory_base";
        public const string TableBaseName = "__table_base";
        public const string AsyncContinuationName = "__async_continuation";

        public override int ClassCode => 0x79046cf9;

        protected override string GetName(NodeFactory factory) => $"WasmWellKnownGlobal {this.ToString()}";
    }
}
