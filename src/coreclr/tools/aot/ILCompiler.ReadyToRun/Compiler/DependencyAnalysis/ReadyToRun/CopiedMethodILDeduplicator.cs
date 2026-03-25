// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class CopiedMethodILDeduplicator : IObjectDataDeduplicator
    {
        private readonly IEnumerable<CopiedMethodILNode> _nodes;

        public CopiedMethodILDeduplicator(IEnumerable<CopiedMethodILNode> nodes)
        {
            _nodes = nodes;
        }

        public void DeduplicatePass(NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> previousSymbolRemapping, Dictionary<ISymbolNode, ISymbolNode> symbolRemapping)
        {
            var hashSet = new HashSet<InternKey>(new InternComparer(factory));

            foreach (CopiedMethodILNode node in _nodes)
            {
                var key = new InternKey(node, factory);
                if (hashSet.TryGetValue(key, out InternKey existing))
                {
                    symbolRemapping.TryAdd(node, existing.Node);
                }
                else
                {
                    hashSet.Add(key);
                }
            }
        }

        private sealed class InternKey
        {
            public CopiedMethodILNode Node { get; }
            public int HashCode { get; }

            public InternKey(CopiedMethodILNode node, NodeFactory factory)
            {
                Node = node;

                ObjectNode.ObjectData data = node.GetData(factory, relocsOnly: false);
                var hashCode = new HashCode();
                hashCode.AddBytes(data.Data);
                HashCode = hashCode.ToHashCode();
            }
        }

        private sealed class InternComparer : IEqualityComparer<InternKey>
        {
            private readonly NodeFactory _factory;

            public InternComparer(NodeFactory factory) => _factory = factory;

            public int GetHashCode(InternKey key) => key.HashCode;

            public bool Equals(InternKey a, InternKey b)
            {
                if (a.HashCode != b.HashCode)
                    return false;

                ObjectNode.ObjectData aData = a.Node.GetData(_factory, relocsOnly: false);
                ObjectNode.ObjectData bData = b.Node.GetData(_factory, relocsOnly: false);

                return aData.Data.AsSpan().SequenceEqual(bData.Data);
            }
        }
    }
}
