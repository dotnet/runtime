// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Pluggable strategy for identifying and deduplicating equivalent object data.
    /// </summary>
    public interface IObjectDataDeduplicator
    {
        /// <summary>
        /// Performs one deduplication pass, adding entries to <paramref name="symbolRemapping"/>.
        /// Called iteratively until the overall mapping converges.
        /// </summary>
        void DeduplicatePass(NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> previousSymbolRemapping, Dictionary<ISymbolNode, ISymbolNode> symbolRemapping);
    }

    public sealed class ObjectDataInterner
    {
        private readonly IObjectDataDeduplicator[] _deduplicators;
        private Dictionary<ISymbolNode, ISymbolNode> _symbolRemapping;

        public static ObjectDataInterner Null { get; } = new ObjectDataInterner() { _symbolRemapping = new() };

        public ObjectDataInterner(params IObjectDataDeduplicator[] deduplicators)
        {
            _deduplicators = deduplicators;
        }

        private void EnsureMap(NodeFactory factory)
        {
            Debug.Assert(factory.MarkingComplete);

            if (_symbolRemapping != null)
                return;

            Dictionary<ISymbolNode, ISymbolNode> previousSymbolRemapping;
            Dictionary<ISymbolNode, ISymbolNode> symbolRemapping = null;

            do
            {
                previousSymbolRemapping = symbolRemapping;
                symbolRemapping = new Dictionary<ISymbolNode, ISymbolNode>((int)(1.05 * (previousSymbolRemapping?.Count ?? 0)));

                foreach (IObjectDataDeduplicator deduplicator in _deduplicators)
                {
                    deduplicator.DeduplicatePass(factory, previousSymbolRemapping, symbolRemapping);
                }
            } while (!MappingsEqual(previousSymbolRemapping, symbolRemapping));

            _symbolRemapping = symbolRemapping;
        }

        private static bool MappingsEqual(Dictionary<ISymbolNode, ISymbolNode> a, Dictionary<ISymbolNode, ISymbolNode> b)
        {
            if (a is null)
                return false;

            if (a.Count != b.Count)
                return false;

            foreach (KeyValuePair<ISymbolNode, ISymbolNode> kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out ISymbolNode value) || value != kvp.Value)
                    return false;
            }

            return true;
        }

        public ISymbolNode GetDeduplicatedSymbol(NodeFactory factory, ISymbolNode original)
        {
            EnsureMap(factory);

            ISymbolNode target = original;
            if (target is ISymbolNodeWithLinkage symbolWithLinkage)
                target = symbolWithLinkage.NodeForLinkage(factory);

            return _symbolRemapping.TryGetValue(target, out ISymbolNode result) ? result : original;
        }
    }
}
