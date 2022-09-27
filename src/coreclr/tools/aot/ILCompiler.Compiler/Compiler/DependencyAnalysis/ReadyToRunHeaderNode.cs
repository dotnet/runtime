// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ReadyToRunHeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        private struct HeaderItem
        {
            public HeaderItem(ReadyToRunSectionType id, ObjectNode node, ISymbolNode startSymbol, ISymbolNode endSymbol)
            {
                Id = id;
                Node = node;
                StartSymbol = startSymbol;
                EndSymbol = endSymbol;
            }

            public readonly ReadyToRunSectionType Id;
            public readonly ObjectNode Node;
            public readonly ISymbolNode StartSymbol;
            public readonly ISymbolNode EndSymbol;
        }

        private List<HeaderItem> _items = new List<HeaderItem>();
        private TargetDetails _target;

        public ReadyToRunHeaderNode(TargetDetails target)
        {
            _target = target;
        }

        public void Add(ReadyToRunSectionType id, ObjectNode node, ISymbolNode startSymbol, ISymbolNode endSymbol = null)
        {
            _items.Add(new HeaderItem(id, node, startSymbol, endSymbol));
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunHeader");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (_target.IsWindows)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
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
                if (item.EndSymbol != null)
                {
                    flags |= ModuleInfoFlags.HasEndPointer;
                }
                builder.EmitInt((int)flags);

                builder.EmitPointerReloc(item.StartSymbol);

                if (item.EndSymbol != null)
                {
                    builder.EmitPointerReloc(item.EndSymbol);
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

        public override int ClassCode => -534800244;
    }
}
