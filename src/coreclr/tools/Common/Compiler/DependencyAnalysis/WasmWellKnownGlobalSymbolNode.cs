// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents one of the well-known wasm globals referenced by JIT-generated code.
    /// These are imported globals whose final index is assigned by the ObjectWriter / wasm linker.
    /// crossgen2/R2R resolves it back to the fixed index defined in the WebCIL format, while a relocatable
    /// NativeAOT object emits it as an undefined imported global for wasm-ld to resolve.
    /// </summary>
    public class WasmWellKnownGlobalSymbolNode(string symbolName) : ExternDataSymbolNode(new Utf8String(symbolName))
    {
        public const string StackPointerName = "__stack_pointer";
        public const string ImageBaseName = "__memory_base";
        public const string TableBaseName = "__table_base";

        public override int ClassCode => 0x79046cf9;

        protected override string GetName(NodeFactory factory) => $"WasmWellKnownGlobal {this.ToString()}";
    }
}
