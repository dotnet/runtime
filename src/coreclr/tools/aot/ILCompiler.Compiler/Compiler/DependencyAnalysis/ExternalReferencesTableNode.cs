// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.Runtime;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node that points to various symbols and can be sequentially addressed.
    /// </summary>
    public sealed class ExternalReferencesTableNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly ObjectAndOffsetSymbolNode _endSymbol;
        private readonly string _blobName;
        private readonly NodeFactory _nodeFactory;

        private Dictionary<SymbolAndDelta, uint> _insertedSymbolsDictionary = new Dictionary<SymbolAndDelta, uint>();
        private List<SymbolAndDelta> _insertedSymbols = new List<SymbolAndDelta>();

        public ExternalReferencesTableNode(string blobName, NodeFactory nodeFactory)
        {
            _blobName = blobName;
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__external_" + blobName + "_references_End", true);
            _nodeFactory = nodeFactory;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__external_" + _blobName + "_references");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        /// <summary>
        /// Adds a new entry to the table. Thread safety: not thread safe. Expected to be called at the final
        /// object data emission phase from a single thread.
        /// </summary>
        public uint GetIndex(ISymbolNode symbol, int delta = 0)
        {
#if DEBUG
            if (_nodeFactory.MarkingComplete)
            {
                var node = symbol as ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<NodeFactory>;
                if (node != null)
                    Debug.Assert(node.Marked);
            }
#endif

            SymbolAndDelta key = new SymbolAndDelta(symbol, delta);

            uint index;
            if (!_insertedSymbolsDictionary.TryGetValue(key, out index))
            {
                index = (uint)_insertedSymbols.Count;
                _insertedSymbolsDictionary[key] = index;
                _insertedSymbols.Add(key);
            }

            return index;
        }

        public override ObjectNodeSection Section
        {
            get
            {
                if (_nodeFactory.Target.IsWindows || _nodeFactory.Target.SupportsRelativePointers)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Zero out the dictionary so that we AV if someone tries to insert after we're done.
            _insertedSymbolsDictionary = null;

            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(factory.Target.SupportsRelativePointers ? 4 : factory.Target.PointerSize);

            foreach (SymbolAndDelta symbolAndDelta in _insertedSymbols)
            {
                if (factory.Target.SupportsRelativePointers)
                {
                    // TODO: set low bit if the linkage of the symbol is IAT_PVALUE.
                    builder.EmitReloc(symbolAndDelta.Symbol, RelocType.IMAGE_REL_BASED_RELPTR32, symbolAndDelta.Delta);
                }
                else
                {
                    builder.EmitPointerReloc(symbolAndDelta.Symbol, symbolAndDelta.Delta);
                }
            }

            _endSymbol.SetSymbolOffset(builder.CountBytes);
            
            builder.AddSymbol(this);
            builder.AddSymbol(_endSymbol);

            return builder.ToObjectData();
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ExternalReferencesTableNode;
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return string.Compare(_blobName, ((ExternalReferencesTableNode)other)._blobName);
        }

        struct SymbolAndDelta : IEquatable<SymbolAndDelta>
        {
            public readonly ISymbolNode Symbol;
            public readonly int Delta;

            public SymbolAndDelta(ISymbolNode symbol, int delta)
            {
                Symbol = symbol;
                Delta = delta;
            }

            public bool Equals(SymbolAndDelta other)
            {
                return Symbol == other.Symbol && Delta == other.Delta;
            }

            public override bool Equals(object obj)
            {
                return Equals((SymbolAndDelta)obj);
            }

            public override int GetHashCode()
            {
                return Symbol.GetHashCode();
            }
        }
    }
}
