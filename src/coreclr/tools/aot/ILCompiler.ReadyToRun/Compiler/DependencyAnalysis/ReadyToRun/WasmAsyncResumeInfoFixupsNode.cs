// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class WasmAsyncResumeInfoFixupsNode : HeaderTableNode
    {
        private static readonly ObjectNodeSection FixupSection =
            new ObjectNodeSection("wasm.asyncresumeinfo", SectionType.ReadOnly);

        private readonly List<Fixup> _fixups = new List<Fixup>();

        public override ObjectNodeSection GetSection(NodeFactory factory) => FixupSection;

        public void AddFixup(ISymbolNode location, int offset)
        {
            lock (_fixups)
            {
                _fixups.Add(new Fixup(location, offset));
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__ReadyToRun_WasmAsyncResumeInfoFixups"u8);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            Fixup[] fixups;
            lock (_fixups)
            {
                fixups = _fixups.ToArray();
            }

            Array.Sort(fixups, CompareFixups);

            int stride = factory.Target.PointerSize * 2;
            List<FixupChunk> chunks = new List<FixupChunk>();
            for (int i = 0; i < fixups.Length;)
            {
                Fixup first = fixups[i];
                int count = 1;
                while ((i + count) < fixups.Length &&
                       fixups[i + count].Location == first.Location &&
                       fixups[i + count].Offset == first.Offset + count * stride)
                {
                    count++;
                }

                chunks.Add(new FixupChunk(first.Location, first.Offset, count, stride));
                i += count;
            }

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            EmitULEB128(ref builder, checked((uint)chunks.Count));

            foreach (FixupChunk chunk in chunks)
            {
                builder.EmitReloc(chunk.Location, RelocType.WASM_ASYNC_RESUME_INFO_DELTA_ULEB, chunk.Offset);
                EmitULEB128(ref builder, checked((uint)chunk.Count));
                EmitULEB128(ref builder, checked((uint)chunk.Stride));
            }

            return builder.ToObjectData();
        }

        public override int ClassCode => 1794382631;

        private static int CompareFixups(Fixup left, Fixup right)
        {
            int result = CompilerComparer.Instance.Compare((ISortableNode)left.Location, (ISortableNode)right.Location);
            return result != 0 ? result : left.Offset.CompareTo(right.Offset);
        }

        private static void EmitULEB128(ref ObjectDataBuilder builder, uint value)
        {
            do
            {
                byte current = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0)
                {
                    current |= 0x80;
                }
                builder.EmitByte(current);
            } while (value != 0);
        }

        private readonly struct Fixup
        {
            public Fixup(ISymbolNode location, int offset)
            {
                Location = location;
                Offset = offset;
            }

            public ISymbolNode Location { get; }
            public int Offset { get; }
        }

        private readonly struct FixupChunk
        {
            public FixupChunk(ISymbolNode location, int offset, int count, int stride)
            {
                Location = location;
                Offset = offset;
                Count = count;
                Stride = stride;
            }

            public ISymbolNode Location { get; }
            public int Offset { get; }
            public int Count { get; }
            public int Stride { get; }
        }
    }
}
