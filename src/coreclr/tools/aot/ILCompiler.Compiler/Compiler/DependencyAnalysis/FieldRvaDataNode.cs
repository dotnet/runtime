// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public class FieldRvaDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly EcmaField _field;

        public EcmaField Field => _field;

        public FieldRvaDataNode(EcmaField field)
        {
            Debug.Assert(field.HasRva);
            _field = field;
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledFieldName(_field));
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            Relocation[] relocs;

            CorHeader corHeader = _field.Module.PEReader.PEHeaders.CorHeader;
            DirectoryEntry vtableFixups = corHeader.VtableFixupsDirectory;
            byte[] data = null;
            if (vtableFixups.Size != 0)
            {
                ArrayBuilder<Relocation> relocBuilder = default;

                int fieldAddr = _field.MetadataReader.GetFieldDefinition(_field.Handle).GetRelativeVirtualAddress();
                int fieldSize = _field.FieldType.GetElementSize().AsInt;

                BlobReader reader = _field.Module.PEReader.GetSectionData(vtableFixups.RelativeVirtualAddress).GetReader(0, vtableFixups.Size);
                while (reader.Offset < reader.Length)
                {
                    int fixupAddr = reader.ReadInt32();
                    int numFixups = reader.ReadInt16();
                    int fixupType = reader.ReadInt16();

                    if ((fixupType != 1 && factory.Target.PointerSize == 4) || (fixupType != 2 && factory.Target.PointerSize == 8))
                        ThrowHelper.ThrowBadImageFormatException();

                    int fixupSize = numFixups * factory.Target.PointerSize;
                    if (fieldAddr >= fixupAddr + fixupSize || fieldAddr + fieldSize <= fixupAddr)
                        continue;

                    // Simplifying assumptions (the data blob is a vtable), we could relax this
                    if (fixupAddr != fieldAddr || fixupSize != fieldSize)
                        ThrowHelper.ThrowBadImageFormatException();

                    BlobReader fixupReader = _field.Module.PEReader.GetSectionData(fieldAddr).GetReader(0, fieldSize);
                    int relocOffset = 0;
                    while (fixupReader.Offset < fixupReader.Length)
                    {
                        var token = (int)(factory.Target.PointerSize == 4 ? (long)fixupReader.ReadInt32() : fixupReader.ReadInt64());
                        MethodDesc method = _field.Module.GetMethod(MetadataTokens.EntityHandle(token));
                        relocBuilder.Add(new Relocation(
                            (factory.Target.PointerSize == 8) ? RelocType.IMAGE_REL_BASED_DIR64 : RelocType.IMAGE_REL_BASED_HIGHLOW,
                            relocOffset,
                            factory.AddressTakenMethodEntrypoint(method, unboxingStub: false)));
                        relocOffset = fixupReader.Offset;
                    }

                    if (!relocsOnly)
                        data = new byte[fieldSize];
                    break;
                }

                relocs = relocBuilder.ToArray();
            }
            else
            {
                relocs = Array.Empty<Relocation>();
            }

            if (data == null)
            {
                if (relocsOnly)
                {
                    data = Array.Empty<byte>();
                }
                else
                {
                    bool success = _field.TryGetFieldRvaData(out data);
                    Debug.Assert(success);
                }
            }

            int fieldTypePack = (_field.FieldType as MetadataType)?.GetClassLayout().PackingSize ?? 1;
            return new ObjectData(
                data,
                relocs,
                Math.Max(factory.Target.PointerSize, fieldTypePack),
                new ISymbolDefinitionNode[] { this });
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

#if !SUPPORT_JIT
        public override int ClassCode => -456126;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_field, ((FieldRvaDataNode)other)._field);
        }
#endif
    }
}
