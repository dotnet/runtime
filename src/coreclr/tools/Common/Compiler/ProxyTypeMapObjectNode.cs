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
        private readonly ModuleDesc _module;

        public ProxyTypeMapObjectNode(TypeMapManager manager, INativeFormatTypeReferenceProvider externalReferences, ModuleDesc module = null)
        {
            _manager = manager;
            _externalReferences = externalReferences;
            _module = module;
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
            return comparer.Compare(_module, otherNode._module);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__proxy_type_map__"u8);
            if (_module is not null)
                sb.Append(_module.Assembly.GetName().Name);
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
