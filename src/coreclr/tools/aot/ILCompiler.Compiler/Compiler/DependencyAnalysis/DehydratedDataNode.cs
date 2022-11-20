// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Responsible for generating dehydrated stream of data structures that need to be rehydrated
    /// at runtime before use.
    /// </summary>
    /// <remarks>
    /// The things we're dehydrating:
    /// * Pointers. On 64bit targets, absolute pointers take up 8 bytes + reloc in the image. The
    ///   size of the reloc can be up to 24 bytes (ELF/Linux). We replace relocs with a more efficient
    ///   index into a lookup table.
    /// * Runs of zeros. These can be result of alignment or simply a result of how things ended up naturally.
    ///
    /// The dehydrated stream can be though of as an instruction stream with 3 kinds of instructions:
    /// * Copy N bytes of literal data following the instruction.
    /// * Generate N bytes of zeros.
    /// * Generate a relocation to Nth entry in the lookup table that supplements the dehydrated stream.
    /// </remarks>
    internal sealed class DehydratedDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;

        public DehydratedDataNode()
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__dehydrated_data_End", true);
        }

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Late;

        public override int ClassCode => 0xbd80526;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public ISymbolNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__dehydrated_data");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This is a summary node that doesn't contribute to the dependency graph.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            // Count the number of occurences of reloc targets. We'll sort the reloc targets by the number of
            // references to them so that we can assign the most efficient instruction encoding (1 byte) for
            // the most popular targets.
            ISymbolDefinitionNode firstSymbol = null;
            var relocOccurences = new Dictionary<ISymbolNode, int>();
            foreach (ObjectNode.ObjectData o in factory.MetadataManager.GetDehydratableData())
            {
                firstSymbol ??= o.DefinedSymbols[0];

                foreach (Relocation reloc in o.Relocs)
                {
                    ISymbolNode target = reloc.Target;
                    if (target is ISymbolNodeWithLinkage withLinkage)
                        target = withLinkage.NodeForLinkage(factory);
                    relocOccurences.TryGetValue(target, out int num);
                    relocOccurences[target] = ++num;
                }
            }

            if (firstSymbol != null)
                builder.EmitReloc(firstSymbol, RelocType.IMAGE_REL_BASED_RELPTR32, -firstSymbol.Offset);
            else
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });

            // Sort the reloc targets and create reloc lookup table.
            KeyValuePair<ISymbolNode, int>[] relocSort = new List<KeyValuePair<ISymbolNode, int>>(relocOccurences).ToArray();
            Array.Sort(relocSort, (x, y) => y.Value.CompareTo(x.Value));
            for (int i = 0; i < relocSort.Length; i++)
                relocSort[i] = new KeyValuePair<ISymbolNode, int>(relocSort[i].Key, i);
            var relocs = new Dictionary<ISymbolNode, int>(relocSort);

            // Walk all the ObjectDatas and generate the dehydrated instruction stream.
            byte[] buff = new byte[4];
            int dehydratedSegmentPosition = 0;
            foreach (ObjectData o in factory.MetadataManager.GetDehydratableData())
            {
                if (o.Alignment > 1)
                {
                    // Might need to emit a ZeroFill to align the data.
                    int oldPosition = dehydratedSegmentPosition;
                    dehydratedSegmentPosition = dehydratedSegmentPosition.AlignUp(o.Alignment);
                    if (dehydratedSegmentPosition > oldPosition)
                        builder.EmitByte(DehydratedDataCommand.EncodeShort(DehydratedDataCommand.ZeroFill, dehydratedSegmentPosition - oldPosition));
                }

                int currentReloc = 0;
                int sourcePosition = 0;

                while (sourcePosition < o.Data.Length)
                {
                    // The ObjectData can be though of as a run of bytes interrupted by relocations.
                    // We identify the next chunk of data by looking at the next relocation and
                    // emit the next chunk of data (if any) followed by a relocation (if any).
                    int bytesToCopy;
                    Relocation reloc;
                    if (currentReloc < o.Relocs.Length)
                    {
                        reloc = o.Relocs[currentReloc++];

                        // We assume relocations are sorted
                        Debug.Assert(reloc.Offset >= sourcePosition);
                        bytesToCopy = reloc.Offset - sourcePosition;
                    }
                    else
                    {
                        reloc = default;
                        bytesToCopy = o.Data.Length - sourcePosition;
                    }

                    // Emit the run of bytes before the next relocation (or end of ObjectData, whichever
                    // comes first).
                    while (bytesToCopy > 0)
                    {
                        // Try to identify a continuous run of zeros. If we find one, split this run of
                        // bytes into chunks of data interleaved with zero fill instructions.
                        int chunkLength = 0;
                        int numZeros = 0;
                        const int MinProfitableZerofill = 4;
                        for (int i = sourcePosition; i < sourcePosition + bytesToCopy; i++)
                        {
                            if (o.Data[i] == 0)
                            {
                                numZeros++;
                            }
                            else if (numZeros >= MinProfitableZerofill)
                            {
                                break;
                            }
                            else
                            {
                                chunkLength += numZeros;
                                numZeros = 0;
                                chunkLength++;
                            }
                        }

                        // If it wouldn't be profiable to emit zero fill, emit zeros at literal data.
                        if (numZeros < MinProfitableZerofill)
                        {
                            chunkLength += numZeros;
                            numZeros = 0;
                        }

                        // Emit literal data if there's any.
                        if (chunkLength > 0)
                        {
                            int written = DehydratedDataCommand.Encode(DehydratedDataCommand.Copy, chunkLength, buff);
                            builder.EmitBytes(buff, 0, written);
                            builder.EmitBytes(o.Data, sourcePosition, chunkLength);
                            sourcePosition += chunkLength;
                            bytesToCopy -= chunkLength;
                        }

                        // Emit a ZeroFill instruction if there's any zeros.
                        if (numZeros > 0)
                        {
                            int written = DehydratedDataCommand.Encode(DehydratedDataCommand.ZeroFill, numZeros, buff);
                            builder.EmitBytes(buff, 0, written);
                            sourcePosition += numZeros;
                            bytesToCopy -= numZeros;
                        }
                    }

                    // Generate the next relocation if there's any.
                    if (reloc.Target != null)
                    {
#if DEBUG
                        unsafe
                        {
                            fixed (byte* pData = &o.Data[reloc.Offset])
                            {
                                long delta = Relocation.ReadValue(reloc.RelocType, pData);
                                // Extra work needed to be able to encode/decode relocs with deltas
                                Debug.Assert(delta == 0);
                            }
                        }
#endif

                        // The size of the relocation is included in the ObjectData bytes. Skip the literal bytes.
                        sourcePosition += Relocation.GetSize(reloc.RelocType);

                        ISymbolNode target = reloc.Target;
                        if (target is ISymbolNodeWithLinkage withLinkage)
                            target = withLinkage.NodeForLinkage(factory);

                        int targetIndex = relocs[target];

                        int relocCommand = reloc.RelocType switch
                        {
                            RelocType.IMAGE_REL_BASED_DIR64 => DehydratedDataCommand.PtrReloc,
                            RelocType.IMAGE_REL_BASED_RELPTR32 => DehydratedDataCommand.RelPtr32Reloc,
                            _ => throw new NotSupportedException(),
                        };

                        int written = DehydratedDataCommand.Encode(relocCommand, targetIndex, buff);
                        builder.EmitBytes(buff, 0, written);
                    }
                }

                Debug.Assert(sourcePosition == o.Data.Length);
                dehydratedSegmentPosition += o.Data.Length;
            }

            _endSymbol.SetSymbolOffset(builder.CountBytes);
            builder.AddSymbol(_endSymbol);

            // Dehydrated data is followed by the reloc lookup table.
            for (int i = 0; i < relocSort.Length; i++)
                builder.EmitReloc(relocSort[i].Key, RelocType.IMAGE_REL_BASED_RELPTR32);

            return builder.ToObjectData();
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
    }
}
