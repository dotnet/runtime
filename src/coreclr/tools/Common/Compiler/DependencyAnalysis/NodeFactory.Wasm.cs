// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
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
                    new(new(WasmWellKnownGlobalSymbolNode.TableBaseName), new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobalSymbolNode.TableBaseName)),
                    new(new(WasmWellKnownGlobalSymbolNode.AsyncContinuationName), new WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobalSymbolNode.AsyncContinuationName))
                ]);

                globals = Interlocked.CompareExchange(ref _wasmWellKnownGlobals, globals, null) ?? globals;
            }

            return globals[symbolName];
        }

        private NodeCache<WasmFunctionEntryNodeCacheKey, WasmFunctionEntryNode> _wasmFunctionEntryCache;

        public WasmFunctionEntryNode WasmFunctionEntry(ObjectNode methodCodeNode, WasmTypeNode typeNode, int? funcletIndex = null)
        {
            return _wasmFunctionEntryCache.GetOrAdd(new WasmFunctionEntryNodeCacheKey(methodCodeNode, typeNode, funcletIndex));
        }

        public readonly struct WasmFunctionEntryNodeCacheKey : IEquatable<WasmFunctionEntryNodeCacheKey>
        {
            public readonly ObjectNode MethodCodeNode;
            public readonly WasmTypeNode Type;
            public readonly int? FuncletIndex;

            public WasmFunctionEntryNodeCacheKey(ObjectNode methodCodeNode, WasmTypeNode type, int? funcletIndex = null)
            {
                MethodCodeNode = methodCodeNode;
                Type = type;
                FuncletIndex = funcletIndex;
            }

            public bool Equals(WasmFunctionEntryNodeCacheKey other) =>
                ReferenceEquals(MethodCodeNode, other.MethodCodeNode) &&
                ReferenceEquals(Type, other.Type) &&
                FuncletIndex == other.FuncletIndex;

            public override bool Equals(object obj) =>
                obj is WasmFunctionEntryNodeCacheKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(
                    RuntimeHelpers.GetHashCode(MethodCodeNode),
                    RuntimeHelpers.GetHashCode(Type),
                    FuncletIndex);
        }
    }
}
