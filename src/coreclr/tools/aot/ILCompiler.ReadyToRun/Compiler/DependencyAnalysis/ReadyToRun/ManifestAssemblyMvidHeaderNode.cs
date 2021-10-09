// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.Text;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ManifestAssemblyMvidHeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        private ManifestMetadataTableNode _manifestNode;

        public ManifestAssemblyMvidHeaderNode(ManifestMetadataTableNode manifestNode)
        {
            _manifestNode = manifestNode;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => 735231445;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public int Size => _manifestNode.ManifestAssemblyMvidTableSize;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ManifestAssemblyMvids");
        }

        protected override string GetName(NodeFactory nodeFactory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(nodeFactory.NameMangler, sb);
            return sb.ToString();
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), null, 0, null);
            }

            byte[] manifestAssemblyMvidTable = _manifestNode.GetManifestAssemblyMvidTableData();
            return new ObjectData(manifestAssemblyMvidTable, Array.Empty<Relocation>(), alignment: 0, new ISymbolDefinitionNode[] { this });
        }
    }
}
