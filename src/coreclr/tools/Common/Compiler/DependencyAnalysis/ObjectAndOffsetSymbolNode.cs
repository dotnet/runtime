// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public class ObjectAndOffsetSymbolNode : DependencyNodeCore<NodeFactory>, ISymbolDefinitionNode
    {
        private ObjectNode _object;
        private int _offset;
        private Utf8String _name;
        private bool _includeCompilationUnitPrefix;

        public ObjectAndOffsetSymbolNode(ObjectNode obj, int offset, Utf8String name, bool includeCompilationUnitPrefix)
        {
            _object = obj;
            _offset = offset;
            _name = name;
            _includeCompilationUnitPrefix = includeCompilationUnitPrefix;
        }

        protected override string GetName(NodeFactory factory) => $"Symbol {_name} at offset {_offset.ToStringInvariant()}";

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            if (_includeCompilationUnitPrefix)
                sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append(_name);
        }

        int ISymbolNode.Offset => 0;
        int ISymbolDefinitionNode.Offset => _offset;
        public bool RepresentsIndirectionCell => false;

        public void SetSymbolOffset(int offset)
        {
            _offset = offset;
        }

        public ObjectNode Target => _object;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[] { new DependencyListEntry(_object, "ObjectAndOffsetDependency") };
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
