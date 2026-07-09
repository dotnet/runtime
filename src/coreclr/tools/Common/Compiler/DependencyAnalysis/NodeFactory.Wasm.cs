// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public partial class NodeFactory
    {
        // The well-known wasm globals are immutable relocation targets. They are created
        // lazily on first access (only wasm targets ever request them) so non-wasm compilations don't
        // pay the allocation.
        private FrozenDictionary<Utf8String, WasmWellKnownGlobalSymbolNode> _wasmWellKnownGlobals;

        public WasmWellKnownGlobalSymbolNode GetWellKnownWasmGlobalSymbol(Utf8String symbolName)
        {
            FrozenDictionary<Utf8String, WasmWellKnownGlobalSymbolNode> globals = _wasmWellKnownGlobals;
            if (globals is null)
            {
                globals = FrozenDictionary.Create<Utf8String, WasmWellKnownGlobalSymbolNode>([
                    new(new(WasmWellKnownGlobalSymbolNode.StackPointerName), new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobalSymbolNode.StackPointerName)),
                    new(new(WasmWellKnownGlobalSymbolNode.ImageBaseName), new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobalSymbolNode.ImageBaseName)),
                    new(new(WasmWellKnownGlobalSymbolNode.TableBaseName), new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobalSymbolNode.TableBaseName))
                ]);

                globals = Interlocked.CompareExchange(ref _wasmWellKnownGlobals, globals, null) ?? globals;
            }

            return globals[symbolName];
        }
    }
}
