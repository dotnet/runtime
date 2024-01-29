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
    internal sealed class DehydratedDataNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Late;

        public override int ClassCode => 0xbd80526;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        int INodeWithSize.Size => _size.Value;

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
            foreach (ObjectData o in factory.MetadataManager.GetDehydratableData())
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
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Sort the reloc targets and create reloc lookup table.
            KeyValuePair<ISymbolNode, int>[] relocSort = new List<KeyValuePair<ISymbolNode, int>>(relocOccurences).ToArray();
            Array.Sort(relocSort, (x, y) => y.Value.CompareTo(x.Value));
            int lastProfitableReloc = 0;
            for (int i = 0; i < relocSort.Length; i++)
            {
                // Stop when we reach rarely referenced targets. Those will be inlined instead of being indirected
                // through the table. Lookup table entry costs 4 bytes, a single reference to a rarely used reloc
                // in the lookup table costs about 3 bytes. Inline reference to a reloc costs 5 bytes.
                // It might be profitable from cache line utilization perspective at runtime to bump this number
                // even higher to avoid using the lookup table as much as possible.
                if (relocSort[i].Value < 3)
                {
                    lastProfitableReloc = i - 1;
                    break;
                }

                relocSort[i] = new KeyValuePair<ISymbolNode, int>(relocSort[i].Key, i);
            }
            if (lastProfitableReloc > 0)
                Array.Resize(ref relocSort, lastProfitableReloc);
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
                    {
                        int written = DehydratedDataCommand.Encode(DehydratedDataCommand.ZeroFill, dehydratedSegmentPosition - oldPosition, buff);
                        builder.EmitBytes(buff, 0, written);
                    }
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
                        Debug.Assert(sourcePosition == reloc.Offset);

                        long delta;
                        unsafe
                        {
                            fixed (byte* pData = &o.Data[reloc.Offset])
                            {
                                delta = Relocation.ReadValue(reloc.RelocType, pData);
                            }
                        }

                        // The size of the relocation is included in the ObjectData bytes. Skip the literal bytes.
                        sourcePosition += Relocation.GetSize(reloc.RelocType);

                        ISymbolNode target = reloc.Target;
                        if (target is ISymbolNodeWithLinkage withLinkage)
                            target = withLinkage.NodeForLinkage(factory);

                        if (delta == 0 && relocs.TryGetValue(target, out int targetIndex))
                        {
                            // Reloc goes through the lookup table
                            int relocCommand = reloc.RelocType switch
                            {
                                RelocType.IMAGE_REL_BASED_DIR64 => DehydratedDataCommand.PtrReloc, // 64-bit platforms
                                RelocType.IMAGE_REL_BASED_HIGHLOW => DehydratedDataCommand.PtrReloc, // 32-bit platforms
                                RelocType.IMAGE_REL_BASED_RELPTR32 => DehydratedDataCommand.RelPtr32Reloc,
                                _ => throw new NotSupportedException(),
                            };

                            int written = DehydratedDataCommand.Encode(relocCommand, targetIndex, buff);
                            builder.EmitBytes(buff, 0, written);
                        }
                        else
                        {
                            // Reloc will be generated inline. Check if we can generate a run of inline relocs.

                            // Reserve a byte for the command (the command payload will have to fit in this byte too).
                            ObjectDataBuilder.Reservation reservation = builder.ReserveByte();

                            int numRelocs = 0;
                            bool hasNextReloc;
                            do
                            {
                                builder.EmitReloc(target, RelocType.IMAGE_REL_BASED_RELPTR32, checked((int)delta));
                                numRelocs++;
                                hasNextReloc = false;

                                if (currentReloc < o.Relocs.Length)
                                {
                                    // If we wouldn't be able to fit this run into the single byte we reserved, stop.
                                    if (numRelocs == DehydratedDataCommand.MaxShortPayload)
                                        break;

                                    Relocation nextReloc = o.Relocs[currentReloc];

                                    // Does the next reloc immediately follow this one?
                                    if (nextReloc.Offset != sourcePosition)
                                        break;

                                    // Is it of the same type?
                                    if (nextReloc.RelocType != reloc.RelocType)
                                        break;

                                    ISymbolNode nextTarget = nextReloc.Target;
                                    if (nextTarget is ISymbolNodeWithLinkage nextTargetWithLinkage)
                                        nextTarget = nextTargetWithLinkage.NodeForLinkage(factory);

                                    unsafe
                                    {
                                        fixed (byte* pData = &o.Data[reloc.Offset])
                                        {
                                            delta = Relocation.ReadValue(reloc.RelocType, pData);
                                        }
                                    }

                                    // We don't have a short code for it?
                                    if (delta == 0 && relocs.ContainsKey(nextTarget))
                                        break;

                                    // This relocation is good - we'll generate it as part of the run
                                    sourcePosition += Relocation.GetSize(reloc.RelocType);
                                    hasNextReloc = true;
                                    currentReloc++;
                                    target = nextTarget;
                                }
                            } while (hasNextReloc);

                            // Now update the byte we reserved with the command to emit for the run
                            int relocCommand = reloc.RelocType switch
                            {
                                RelocType.IMAGE_REL_BASED_DIR64 => DehydratedDataCommand.InlinePtrReloc, // 64-bit platforms
                                RelocType.IMAGE_REL_BASED_HIGHLOW => DehydratedDataCommand.InlinePtrReloc, // 32-bit platforms
                                RelocType.IMAGE_REL_BASED_RELPTR32 => DehydratedDataCommand.InlineRelPtr32Reloc,
                                _ => throw new NotSupportedException(),
                            };
                            builder.EmitByte(reservation, DehydratedDataCommand.EncodeShort(relocCommand, numRelocs));
                        }
                    }
                }

                Debug.Assert(sourcePosition == o.Data.Length);
                dehydratedSegmentPosition += o.Data.Length;
            }

            _size = builder.CountBytes;

            // Dehydrated data is followed by the reloc lookup table.
            for (int i = 0; i < relocSort.Length; i++)
                builder.EmitReloc(relocSort[i].Key, RelocType.IMAGE_REL_BASED_RELPTR32);

            return builder.ToObjectData();
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
    }
}
