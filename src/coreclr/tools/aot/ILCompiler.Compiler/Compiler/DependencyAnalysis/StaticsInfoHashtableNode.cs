// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hashtable with information about all statics regions for all compiled generic types.
    /// </summary>
    internal sealed class StaticsInfoHashtableNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;
        private ExternalReferencesTableNode _nativeStaticsReferences;

        public StaticsInfoHashtableNode(ExternalReferencesTableNode externalReferences, ExternalReferencesTableNode nativeStaticsReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "_StaticsInfoHashtableNode_End", true);
            _externalReferences = externalReferences;
            _nativeStaticsReferences = nativeStaticsReferences;
        }

        public ISymbolNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("_StaticsInfoHashtableNode");
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        

        /// <summary>
        /// Helper method to compute the dependencies that would be needed by a hashtable entry for statics info lookup.
        /// This helper is used by EETypeNode, which is used by the dependency analysis to compute the statics hashtable
        /// entries for the compiled types.
        /// </summary>
        public static void AddStaticsInfoDependencies(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            if (type is MetadataType && type.HasInstantiation && !type.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                MetadataType metadataType = (MetadataType)type;

                // The StaticsInfoHashtable entries only exist for static fields on generic types.

                if (metadataType.GCStaticFieldSize.AsInt > 0)
                {
                    dependencies.Add(factory.TypeGCStaticsSymbol(metadataType), "GC statics indirection for StaticsInfoHashtable");
                }

                if (metadataType.NonGCStaticFieldSize.AsInt > 0 || factory.PreinitializationManager.HasLazyStaticConstructor(type))
                {
                    // The entry in the StaticsInfoHashtable points at the beginning of the static fields data, rather than the cctor 
                    // context offset.
                    dependencies.Add(factory.TypeNonGCStaticsSymbol(metadataType), "Non-GC statics indirection for StaticsInfoHashtable");
                }

                if (metadataType.ThreadGcStaticFieldSize.AsInt > 0)
                {
                    dependencies.Add(factory.TypeThreadStaticIndex(metadataType), "Threadstatics indirection for StaticsInfoHashtable");
                }
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            NativeWriter writer = new NativeWriter();
            VertexHashtable hashtable = new VertexHashtable();
            Section section = writer.NewSection();

            section.Place(hashtable);

            foreach (var type in factory.MetadataManager.GetTypesWithConstructedEETypes())
            {
                if (!type.HasInstantiation || type.IsCanonicalSubtype(CanonicalFormKind.Any) || type.IsGenericDefinition)
                    continue;

                MetadataType metadataType = type as MetadataType;
                if (metadataType == null)
                    continue;

                VertexBag bag = new VertexBag();

                if (metadataType.GCStaticFieldSize.AsInt > 0)
                {
                    bag.AppendUnsigned(BagElementKind.GcStaticData, _nativeStaticsReferences.GetIndex(factory.TypeGCStaticsSymbol(metadataType)));
                }
                if (metadataType.NonGCStaticFieldSize.AsInt > 0 || factory.PreinitializationManager.HasLazyStaticConstructor(type))
                {
                    bag.AppendUnsigned(BagElementKind.NonGcStaticData, _nativeStaticsReferences.GetIndex(factory.TypeNonGCStaticsSymbol(metadataType)));
                }
                if (metadataType.ThreadGcStaticFieldSize.AsInt > 0)
                {
                    bag.AppendUnsigned(BagElementKind.ThreadStaticIndex, _nativeStaticsReferences.GetIndex(factory.TypeThreadStaticIndex(metadataType)));
                }

                if (bag.ElementsCount > 0)
                {
                    uint typeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(type));
                    Vertex staticsInfo = writer.GetTuple(writer.GetUnsignedConstant(typeId), bag);

                    hashtable.Append((uint)type.GetHashCode(), section.Place(staticsInfo));
                }
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.StaticsInfoHashtableNode;
    }
}
