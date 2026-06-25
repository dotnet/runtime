// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace ILCompiler.DependencyAnalysis
{
    public partial class NodeFactory
    {
        // The well-known wasm globals are canonical, immutable relocation targets. They are created
        // lazily on first access (only wasm targets ever request them) so non-wasm compilations don't
        // pay the allocation. Indexed by WasmWellKnownGlobal.
        private WasmWellKnownGlobalSymbolNode[] _wasmWellKnownGlobals;

        public WasmWellKnownGlobalSymbolNode GetWasmGlobal(WasmWellKnownGlobal global)
        {
            WasmWellKnownGlobalSymbolNode[] globals = _wasmWellKnownGlobals;
            if (globals is null)
            {
                globals =
                [
                    new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobal.StackPointer),
                    new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobal.ImageBase),
                    new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobal.TableBase),
                ];

                // Ensure a single canonical array wins across threads; a loser's nodes are never handed out.
                globals = Interlocked.CompareExchange(ref _wasmWellKnownGlobals, globals, null) ?? globals;
            }

            return globals[(int)global];
        }
    }
}
