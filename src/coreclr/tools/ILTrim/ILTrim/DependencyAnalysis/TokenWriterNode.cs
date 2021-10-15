// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Base class for all nodes that output a row in one of the metadata tables,
    /// but don't necessarily have an input token.
    /// </summary>
    public abstract class TokenWriterNode : DependencyNodeCore<NodeFactory>, IComparable<TokenWriterNode>
    {
        protected readonly EcmaModule _module;

        /// <summary>
        /// Gets the module associated with this node.
        /// </summary>
        public EcmaModule Module => _module;

        /// <summary>
        /// Each metadata table index should have a unique node type. Nodes with
        /// the same table index must be the same node type.
        /// </summary>
        public abstract TableIndex TableIndex { get; }

        public TokenWriterNode(EcmaModule module)
        {
            _module = module;
        }

        /// <summary>
        /// Writes the node to the output using the specified writing context.
        /// </summary>
        public virtual void Write(ModuleWritingContext writeContext)
        {
            WriteInternal(writeContext);
        }

        protected abstract EntityHandle WriteInternal(ModuleWritingContext writeContext);

        public abstract void BuildTokens(TokenMap.Builder builder);

        public abstract int CompareTo(TokenWriterNode other);

        protected int CompareToHelper(TokenWriterNode other)
        {
            return TableIndex.CompareTo(other.TableIndex);
        }

        protected abstract override string GetName(NodeFactory context);

        public abstract override string ToString();

        public sealed override bool InterestingForDynamicDependencyAnalysis => false;
        public sealed override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public sealed override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public sealed override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
