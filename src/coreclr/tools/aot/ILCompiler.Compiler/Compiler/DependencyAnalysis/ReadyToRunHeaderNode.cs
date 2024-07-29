// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public class ReadyToRunHeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        private struct HeaderItem
        {
            public HeaderItem(ReadyToRunSectionType id, ObjectNode node)
            {
                Id = id;
                Node = node;
            }

            public readonly ReadyToRunSectionType Id;
            public readonly ObjectNode Node;
        }

        private List<HeaderItem> _items = new List<HeaderItem>();

        public void Add<T>(ReadyToRunSectionType id, T node) where T : ObjectNode, ISymbolDefinitionNode
        {
            _items.Add(new HeaderItem(id, node));
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunHeader"u8);
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            // Don't bother sorting if we're not emitting the contents
            if (!relocsOnly)
                _items.Sort((x, y) => Comparer<int>.Default.Compare((int)x.Id, (int)y.Id));

            // ReadyToRunHeader.Magic
            builder.EmitInt((int)(ReadyToRunHeaderConstants.Signature));

            // ReadyToRunHeader.MajorVersion
            builder.EmitShort((short)(ReadyToRunHeaderConstants.CurrentMajorVersion));
            builder.EmitShort((short)(ReadyToRunHeaderConstants.CurrentMinorVersion));

            // ReadyToRunHeader.Flags
            builder.EmitInt(0);

            // ReadyToRunHeader.NumberOfSections
            var sectionCountReservation = builder.ReserveShort();

            // ReadyToRunHeader.EntrySize
            builder.EmitByte((byte)(8 + 2 * factory.Target.PointerSize));

            // ReadyToRunHeader.EntryType
            builder.EmitByte(1);

            int count = 0;
            foreach (var item in _items)
            {
                // Skip empty entries
                if (!relocsOnly && item.Node.ShouldSkipEmittingObjectNode(factory))
                    continue;

                builder.EmitInt((int)item.Id);

                ModuleInfoFlags flags = 0;
                if (item.Node is INodeWithSize)
                {
                    flags |= ModuleInfoFlags.HasEndPointer;
                }
                builder.EmitInt((int)flags);

                builder.EmitPointerReloc((ISymbolNode)item.Node);

                if (!relocsOnly && item.Node is INodeWithSize nodeWithSize)
                {
                    builder.EmitPointerReloc((ISymbolNode)item.Node, nodeWithSize.Size);
                }
                else
                {
                    builder.EmitZeroPointer();
                }

                count++;
            }
            builder.EmitShort(sectionCountReservation, checked((short)count));

            return builder.ToObjectData();
        }

        protected internal override int Phase => (int)ObjectNodePhase.Late;
        public override int ClassCode => 0x7db08464;
    }

    public interface INodeWithSize
    {
        public int Size { get; }
    }
}
