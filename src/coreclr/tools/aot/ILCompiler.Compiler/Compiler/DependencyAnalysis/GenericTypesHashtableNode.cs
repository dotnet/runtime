// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hashtable of all compiled generic type instantiations
    /// </summary>
    public sealed class GenericTypesHashtableNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public GenericTypesHashtableNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__generic_types_hashtable_End", true);
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__generic_types_hashtable");
        }

        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            NativeWriter nativeWriter = new NativeWriter();
            VertexHashtable hashtable = new VertexHashtable();
            Section nativeSection = nativeWriter.NewSection();
            nativeSection.Place(hashtable);

            // We go over constructed EETypes only. The places that need to consult this hashtable at runtime
            // all need constructed EETypes. Placing unconstructed EETypes into this hashtable could make us
            // accidentally satisfy e.g. MakeGenericType for something that was only used in a cast. Those
            // should throw MissingRuntimeArtifact instead.
            //
            // We already make sure "necessary" EETypes that could potentially be loaded at runtime through
            // the dynamic type loader get upgraded to constructed EETypes at AOT compile time.
            foreach (var type in factory.MetadataManager.GetTypesWithConstructedEETypes())
            {
                // If this is an instantiated non-canonical generic type, add it to the generic instantiations hashtable
                if (!type.HasInstantiation || type.IsGenericDefinition || type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    continue;

                var typeSymbol = factory.NecessaryTypeSymbol(type);
                uint instantiationId = _externalReferences.GetIndex(typeSymbol);
                Vertex hashtableEntry = nativeWriter.GetUnsignedConstant(instantiationId);

                hashtable.Append((uint)type.GetHashCode(), nativeSection.Place(hashtableEntry));
            }

            byte[] streamBytes = nativeWriter.Save();

            _endSymbol.SetSymbolOffset(streamBytes.Length);

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.GenericTypesHashtableNode;
    }
}
