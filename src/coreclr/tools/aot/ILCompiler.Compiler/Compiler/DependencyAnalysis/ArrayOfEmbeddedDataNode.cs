// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an array of <typeparamref name="TEmbedded"/> nodes. The contents of this node will be emitted
    /// by placing a starting symbol, followed by contents of <typeparamref name="TEmbedded"/> nodes (optionally
    /// sorted using provided comparer), followed by ending symbol.
    /// </summary>
    public class ArrayOfEmbeddedDataNode<TEmbedded> : EmbeddedDataContainerNode, INodeWithSize
        where TEmbedded : EmbeddedObjectNode
    {
        private int? _size;
        private HashSet<TEmbedded> _nestedNodes = new HashSet<TEmbedded>();
        private List<TEmbedded> _nestedNodesList = new List<TEmbedded>();
        private IComparer<TEmbedded> _sorter;

        int INodeWithSize.Size => _size.Value;

        public ArrayOfEmbeddedDataNode(string mangledName, IComparer<TEmbedded> nodeSorter) : base(mangledName)
        {
            _sorter = nodeSorter;
        }

        public void AddEmbeddedObject(TEmbedded symbol)
        {
            lock (_nestedNodes)
            {
                if (_nestedNodes.Add(symbol))
                {
                    _nestedNodesList.Add(symbol);
                }
            }
        }

        protected override string GetName(NodeFactory factory) => $"Region {this.GetMangledName(factory.NameMangler)}";

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.DataSection;
        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        protected IEnumerable<TEmbedded> NodesList =>  _nestedNodesList;
        private TEmbedded _nextElementToEncode;
        public TEmbedded NextElementToEncode => _nextElementToEncode;

        protected virtual void GetElementDataForNodes(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            int index = 0;
            _nextElementToEncode = null;
            for (int i = 0; i < _nestedNodesList.Count; i++)
            {
                TEmbedded node = _nestedNodesList[i];
                if ((i + 1) < _nestedNodesList.Count)
                    _nextElementToEncode = _nestedNodesList[i + 1];
                else
                    _nextElementToEncode = null;

                if (!relocsOnly)
                {
                    node.InitializeOffsetFromBeginningOfArray(builder.CountBytes);
                    node.InitializeIndexFromBeginningOfArray(index++);
                }

                node.EncodeData(ref builder, factory, relocsOnly);
                if (node is ISymbolDefinitionNode symbolDef)
                {
                    builder.AddSymbol(symbolDef);
                }
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();

            if (_sorter != null)
                _nestedNodesList.Sort(_sorter);

            builder.AddSymbol(this);

            GetElementDataForNodes(ref builder, factory, relocsOnly);

            _size = builder.CountBytes;

            ObjectData objData = builder.ToObjectData();
            return objData;
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return _nestedNodesList.Count == 0;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ArrayOfEmbeddedDataNode;
    }
}
