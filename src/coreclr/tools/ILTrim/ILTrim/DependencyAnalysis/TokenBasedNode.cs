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
    /// Base class for all nodes that have an associated row in one of the metadata tables.
    /// </summary>
    public abstract class TokenBasedNode : DependencyNodeCore<NodeFactory>, IComparable<TokenBasedNode>
    {
        protected readonly EntityHandle _handle;
        protected readonly EcmaModule _module;

        /// <summary>
        /// Gets the module associated with this node.
        /// </summary>
        public EcmaModule Module => _module;

        public TokenBasedNode(EcmaModule module, EntityHandle handle)
        {
            _module = module;
            _handle = handle;
        }

        /// <summary>
        /// Writes the node to the output using the specified writing context.
        /// </summary>
        public void Write(ModuleWritingContext writeContext)
        {
            EntityHandle writtenHandle = WriteInternal(writeContext);
            Debug.Assert(writeContext.TokenMap.MapToken(_handle) == writtenHandle);
        }

        protected abstract EntityHandle WriteInternal(ModuleWritingContext writeContext);

        public void BuildTokens(TokenMap.Builder builder)
        {
            builder.AddTokenMapping(_handle);
        }

        public int CompareTo(TokenBasedNode other)
        {
            int result = MetadataTokens.GetToken(_handle).CompareTo(MetadataTokens.GetToken(other._handle));

            // It's only valid to compare these within the same module
            Debug.Assert(result != 0 || this == other);

            return result;
        }

        protected sealed override string GetName(NodeFactory context)
        {
            MetadataReader reader = _module.MetadataReader;
            int tokenRaw = MetadataTokens.GetToken(_handle);
            string moduleName = reader.GetString(reader.GetModuleDefinition().Name);
            return $"{this.ToString()} ({moduleName}:{tokenRaw:X8})";
        }

        public abstract override string ToString();

        public sealed override bool InterestingForDynamicDependencyAnalysis => false;
        public sealed override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public sealed override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public sealed override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
