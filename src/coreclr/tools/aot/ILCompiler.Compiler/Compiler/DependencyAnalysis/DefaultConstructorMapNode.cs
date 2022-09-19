// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// DefaultConstructorMap blob, containing information on default constructor entrypoints of all types used
    /// by lazy generic instantiations.
    /// </summary>
    internal sealed class DefaultConstructorMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public DefaultConstructorMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__DefaultConstructor_Map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__DefaultConstructor_Map");
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => false;
        public override bool StaticDependenciesAreComputed => true;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.DefaultConstructorMapNode;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            var defaultConstructorHashtable = new VertexHashtable();

            Section defaultConstructorHashtableSection = writer.NewSection();
            defaultConstructorHashtableSection.Place(defaultConstructorHashtable);

            foreach (var type in factory.MetadataManager.GetTypesWithConstructedEETypes())
            {
                MethodDesc defaultCtor = type.GetDefaultConstructor();
                if (defaultCtor == null)
                    continue;

                // We only place default constructors of reflection-blocked types in this table.
                // At runtime, the type loader will search both this table and the invoke map
                // for default constructor info. If the ctor is reflectable, we would have
                // expected dataflow analysis to ensure there's a reflectable method for it.
                // If we don't find a reflectable method for the ctor of a non-blocked type
                // there would have to be a dataflow analysis warning.
                if (!factory.MetadataManager.IsReflectionBlocked(defaultCtor))
                    continue;

                defaultCtor = defaultCtor.GetCanonMethodTarget(CanonicalFormKind.Specific);

                ISymbolNode typeNode = factory.NecessaryTypeSymbol(type);
                ISymbolNode defaultCtorNode = factory.MethodEntrypoint(defaultCtor, false);

                Vertex vertex = writer.GetTuple(
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(typeNode)),
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(defaultCtorNode)));

                int hashCode = type.GetHashCode();
                defaultConstructorHashtable.Append((uint)hashCode, defaultConstructorHashtableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }
    }
}
