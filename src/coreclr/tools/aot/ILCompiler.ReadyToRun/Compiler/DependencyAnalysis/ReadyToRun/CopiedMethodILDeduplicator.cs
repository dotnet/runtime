// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class CopiedMethodILDeduplicator : IObjectDataDeduplicator
    {
        private readonly Func<IEnumerable<CopiedMethodILNode>> _nodesProvider;
        private Dictionary<ISymbolNode, ISymbolNode> _cachedMapping;

        public CopiedMethodILDeduplicator(Func<IEnumerable<CopiedMethodILNode>> nodesProvider)
        {
            _nodesProvider = nodesProvider;
        }

        public void DeduplicatePass(NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> previousSymbolRemapping, Dictionary<ISymbolNode, ISymbolNode> symbolRemapping)
        {
            if (_cachedMapping is null)
            {
                _cachedMapping = new Dictionary<ISymbolNode, ISymbolNode>();

                var sortedNodes = new List<CopiedMethodILNode>(_nodesProvider());
                sortedNodes.Sort(CompilerComparer.Instance);

                var hashSet = new HashSet<InternKey>(new InternComparer());

                foreach (CopiedMethodILNode node in sortedNodes)
                {
                    // No need to deduplicate unmarked nodes.
                    // They won't be emitted anyway.
                    if (!node.Marked)
                    {
                        continue;
                    }
                    var key = new InternKey(node, factory);
                    if (hashSet.TryGetValue(key, out InternKey existing))
                    {
                        _cachedMapping[node] = existing.Node;
                    }
                    else
                    {
                        hashSet.Add(key);
                    }
                }
            }

            foreach (KeyValuePair<ISymbolNode, ISymbolNode> entry in _cachedMapping)
            {
                symbolRemapping.TryAdd(entry.Key, entry.Value);
            }
        }

        private sealed class InternKey
        {
            public CopiedMethodILNode Node { get; }
            public int HashCode { get; }
            public byte[] Data { get; }

            public InternKey(CopiedMethodILNode node, NodeFactory factory)
            {
                Node = node;

                Data = node.GetData(factory, relocsOnly: false).Data;
                var hashCode = new HashCode();
                hashCode.AddBytes(Data);
                HashCode = hashCode.ToHashCode();
            }
        }

        private sealed class InternComparer : IEqualityComparer<InternKey>
        {
            public int GetHashCode(InternKey key) => key.HashCode;

            public bool Equals(InternKey a, InternKey b)
            {
                if (a.HashCode != b.HashCode)
                    return false;

                return a.Data.AsSpan().SequenceEqual(b.Data);
            }
        }
    }
}
