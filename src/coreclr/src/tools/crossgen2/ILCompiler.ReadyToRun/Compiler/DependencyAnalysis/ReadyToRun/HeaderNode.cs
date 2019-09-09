// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal struct ReadyToRunHeaderConstants
    {
        public const uint Signature = 0x00525452; // 'RTR'

        public const ushort CurrentMajorVersion = 3;
        public const ushort CurrentMinorVersion = 0;
    }

    public abstract class HeaderTableNode : ObjectNode, ISymbolDefinitionNode
    {
        public TargetDetails Target { get; private set; }
        
        public HeaderTableNode(TargetDetails target)
        {
            Target = target;
        }

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);

        public int Offset => 0;

        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (Target.IsWindows)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }
    }

    public class HeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        struct HeaderItem
        {
            public HeaderItem(ReadyToRunSectionType id, ObjectNode node, ISymbolNode startSymbol)
            {
                Id = id;
                Node = node;
                StartSymbol = startSymbol;
            }

            public readonly ReadyToRunSectionType Id;
            public readonly ObjectNode Node;
            public readonly ISymbolNode StartSymbol;
        }

        private List<HeaderItem> _items = new List<HeaderItem>();
        private TargetDetails _target;

        public HeaderNode(TargetDetails target)
        {
            _target = target;
        }

        public void Add(ReadyToRunSectionType id, ObjectNode node, ISymbolNode startSymbol)
        {
            _items.Add(new HeaderItem(id, node, startSymbol));
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
            ObjectDataBuilder.Reservation sectionCountReservation = builder.ReserveInt();
            
            int count = 0;
            foreach (var item in _items)
            {
                // Skip empty entries
                if (!relocsOnly && item.Node.ShouldSkipEmittingObjectNode(factory))
                    continue;

                builder.EmitInt((int)item.Id);
                
                builder.EmitReloc(item.StartSymbol, RelocType.IMAGE_REL_BASED_ADDR32NB);

                if (!relocsOnly)
                    builder.EmitInt(item.Node.GetData(factory).Data.Length);
                
                count++;
            }

            builder.EmitInt(sectionCountReservation, count);
            
            return builder.ToObjectData();
        }

        public override int ClassCode => 627741208;
    }
}
