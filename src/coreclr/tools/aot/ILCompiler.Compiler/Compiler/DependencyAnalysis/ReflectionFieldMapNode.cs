// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

using FieldTableFlags = Internal.Runtime.FieldTableFlags;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a map between reflection metadata and native field offsets.
    /// </summary>
    internal sealed class ReflectionFieldMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public ReflectionFieldMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__field_to_offset_map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__field_to_offset_map");
        }

        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => _externalReferences.GetSection(factory);

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            var fieldMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(fieldMapHashTable);

            foreach (var fieldMapping in factory.MetadataManager.GetFieldMapping(factory))
            {
                FieldDesc field = fieldMapping.Entity;

                if (field.IsLiteral || field.HasRva)
                    continue;

                // CppCodegen: implement thread statics
                if (factory.Target.Abi == TargetAbi.CppCodegen && field.IsThreadStatic)
                    continue;

                FieldTableFlags flags;
                if (field.IsStatic)
                {
                    if (field.IsThreadStatic)
                        flags = FieldTableFlags.ThreadStatic;
                    else if (field.HasGCStaticBase)
                        flags = FieldTableFlags.GCStatic;
                    else
                        flags = FieldTableFlags.NonGCStatic;

                    if (field.OwningType.HasInstantiation)
                        flags |= FieldTableFlags.FieldOffsetEncodedDirectly;
                }
                else
                {
                    flags = FieldTableFlags.Instance | FieldTableFlags.FieldOffsetEncodedDirectly;
                }

                if (fieldMapping.MetadataHandle != 0)
                    flags |= FieldTableFlags.HasMetadataHandle;

                if (field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    flags |= FieldTableFlags.IsAnyCanonicalEntry;

                if (field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    flags |= FieldTableFlags.IsUniversalCanonicalEntry;

                if (field.IsInitOnly)
                    flags |= FieldTableFlags.IsInitOnly;

                // Grammar of a hash table entry:
                // Flags + DeclaringType + MdHandle or Name + Cookie or Ordinal or Offset

                Vertex vertex = writer.GetUnsignedConstant((uint)flags);

                uint declaringTypeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(field.OwningType));
                vertex = writer.GetTuple(vertex,
                    writer.GetUnsignedConstant(declaringTypeId));

                if ((flags & FieldTableFlags.HasMetadataHandle) != 0)
                {
                    // Only store the offset portion of the metadata handle to get better integer compression
                    vertex = writer.GetTuple(vertex,
                        writer.GetUnsignedConstant((uint)(fieldMapping.MetadataHandle & MetadataManager.MetadataOffsetMask)));
                }
                else
                {
                    // No metadata handle means we need to store name
                    vertex = writer.GetTuple(vertex,
                        writer.GetStringConstant(field.Name));
                }

                if ((flags & FieldTableFlags.IsUniversalCanonicalEntry) != 0)
                {
                    vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant(checked((uint)field.GetFieldOrdinal())));
                }
                else
                {
                    switch (flags & FieldTableFlags.StorageClass)
                    {
                        case FieldTableFlags.ThreadStatic:
                        case FieldTableFlags.GCStatic:
                        case FieldTableFlags.NonGCStatic:
                            {
                                uint fieldOffset = (uint)field.Offset.AsInt;
                                if (field.IsThreadStatic && field.OwningType is MetadataType mt)
                                {
                                    fieldOffset += factory.ThreadStaticBaseOffset(mt);
                                }

                                if (field.OwningType.HasInstantiation)
                                {
                                    vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant(fieldOffset));
                                }
                                else
                                {
                                    MetadataType metadataType = (MetadataType)field.OwningType;

                                    ISymbolNode staticsNode;
                                    if (field.IsThreadStatic)
                                    {
                                        staticsNode = factory.TypeThreadStaticIndex(metadataType);
                                    }
                                    else if (field.HasGCStaticBase)
                                    {
                                        staticsNode = factory.TypeGCStaticsSymbol(metadataType);
                                    }
                                    else
                                    {
                                        staticsNode = factory.TypeNonGCStaticsSymbol(metadataType);
                                    }

                                    if (!field.IsThreadStatic && !field.HasGCStaticBase)
                                    {
                                        uint index = _externalReferences.GetIndex(staticsNode, (int)fieldOffset);
                                        vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant(index));
                                    }
                                    else
                                    {
                                        uint index = _externalReferences.GetIndex(staticsNode);
                                        vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant(index));
                                        vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant(fieldOffset));
                                    }
                                }
                            }
                            break;

                        case FieldTableFlags.Instance:
                            vertex = writer.GetTuple(vertex, writer.GetUnsignedConstant((uint)field.Offset.AsInt));
                            break;
                    }
                }

                int hashCode = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific).GetHashCode();
                fieldMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ReflectionFieldMapNode;
    }
}
