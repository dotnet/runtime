// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    //
    // Represents one of the well-known wasm "base globals" referenced by JIT-generated code:
    // the shadow stack pointer, the image base (__memory_base) and the table base (__table_base).
    // These are imported globals whose final index is assigned by the linker, so the JIT references
    // them via WASM_GLOBAL_INDEX_LEB relocations rather than bare immediates. This node is the
    // relocation target carrying the well-known symbol name; the object writer maps it to the final
    // wasm global index (crossgen2/R2R self-resolves it back to the fixed index, while a relocatable
    // NativeAOT object emits it as an undefined imported global for wasm-ld to resolve).
    //
    public sealed class WasmBaseGlobalSymbolNode : SortableDependencyNode, ISortableSymbolNode
    {
        // Fixed wasm global indices, matching the ABI shared with the object writer
        // (see WasmAbiConstants / WasmObjectWriter and the JIT's emitwasm.cpp, as well as the WebCIL spec).
        public const int StackPointerGlobalIndex = 0;
        public const int ImageBaseGlobalIndex = 1;
        public const int TableBaseGlobalIndex = 2;

        // Well-known symbol names for the base globals (standard wasm tool-conventions names).
        public const string StackPointerSymbolName = "__stack_pointer";
        public const string ImageBaseSymbolName = "__memory_base";
        public const string TableBaseSymbolName = "__table_base";

        private static readonly WasmBaseGlobalSymbolNode s_stackPointer = new(StackPointerGlobalIndex);
        private static readonly WasmBaseGlobalSymbolNode s_imageBase = new(ImageBaseGlobalIndex);
        private static readonly WasmBaseGlobalSymbolNode s_tableBase = new(TableBaseGlobalIndex);

        private readonly int _globalIndex;

        private WasmBaseGlobalSymbolNode(int globalIndex)
        {
            _globalIndex = globalIndex;
        }

        private static readonly Dictionary<string, int> s_symbolNameToGlobalIndex = new()
        {
            { StackPointerSymbolName, StackPointerGlobalIndex },
            { ImageBaseSymbolName, ImageBaseGlobalIndex },
            { TableBaseSymbolName, TableBaseGlobalIndex },
        };

        public static WasmBaseGlobalSymbolNode GetForIndex(int globalIndex) => globalIndex switch
        {
            StackPointerGlobalIndex => s_stackPointer,
            ImageBaseGlobalIndex => s_imageBase,
            TableBaseGlobalIndex => s_tableBase,
            _ => throw new ArgumentOutOfRangeException(nameof(globalIndex))
        };

        // Maps a well-known base-global symbol name back to its fixed wasm global index. This is the
        // authoritative mapping used by the object writer to resolve WASM_GLOBAL_INDEX_LEB relocations.
        public static bool TryGetGlobalIndexForSymbol(string symbolName, out int globalIndex)
            => s_symbolNameToGlobalIndex.TryGetValue(symbolName, out globalIndex);

        public int GlobalIndex => _globalIndex;

        public string SymbolName => _globalIndex switch
        {
            StackPointerGlobalIndex => StackPointerSymbolName,
            ImageBaseGlobalIndex => ImageBaseSymbolName,
            TableBaseGlobalIndex => TableBaseSymbolName,
            _ => throw new InvalidOperationException()
        };

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(SymbolName);

        public int Offset => 0;
        public bool RepresentsIndirectionCell => false;

        public override int ClassCode => 0x57_42_47_53; // "WBGS"

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => $"Wasm Base Global: {SymbolName}";

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            => _globalIndex.CompareTo(((WasmBaseGlobalSymbolNode)other)._globalIndex);
    }
}
