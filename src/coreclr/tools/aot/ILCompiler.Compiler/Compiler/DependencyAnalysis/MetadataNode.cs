// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a blob of native metadata describing assemblies, the types in them, and their members.
    /// The data is used at runtime to e.g. support reflection.
    /// </summary>
    public sealed class MetadataNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;

        int INodeWithSize.Size => _size.Value;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__embedded_metadata");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node has no relocations.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            byte[] blob = factory.MetadataManager.GetMetadataBlob(factory);
            _size = blob.Length;

            return new ObjectData(
                blob,
                Array.Empty<Relocation>(),
                1,
                new ISymbolDefinitionNode[]
                {
                    this
                });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.MetadataNode;
    }
}
