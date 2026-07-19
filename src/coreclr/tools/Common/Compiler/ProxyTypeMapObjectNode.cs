// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ProxyTypeMapObjectNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly TypeMapManager _manager;
        private readonly INativeFormatTypeReferenceProvider _externalReferences;

        public ProxyTypeMapObjectNode(TypeMapManager manager, INativeFormatTypeReferenceProvider externalReferences)
        {
            _manager = manager;
            _externalReferences = externalReferences;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, [this]);

            var writer = new NativeWriter();
            var typeMapGroupHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapGroupHashTable);

            foreach (IProxyTypeMapNode proxyTypeMap in _manager.GetProxyTypeMaps())
            {
                TypeDesc typeMapGroup = proxyTypeMap.TypeMapGroup;
                typeMapGroupHashTable.Append((uint)typeMapGroup.GetHashCode(), proxyTypeMap.CreateTypeMap(factory, writer, hashTableSection, _externalReferences));
            }

            byte[] hashTableBytes = writer.Save();

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, [this]);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ProxyTypeMapObjectNode otherNode = (ProxyTypeMapObjectNode)other;
            if (_manager.AssociatedModule is null)
                return 0;

            return comparer.Compare(_manager.AssociatedModule, otherNode._manager.AssociatedModule);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__proxy_type_map__"u8);
            if (_manager.AssociatedModule is not null)
                sb.Append(_manager.AssociatedModule.Assembly.GetName().Name);
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ProxyTypeMapObjectNode;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory context) => "Proxy Type Map Hash Table";
    }
}
