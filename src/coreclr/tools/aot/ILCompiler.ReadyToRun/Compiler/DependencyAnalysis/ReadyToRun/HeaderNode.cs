// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
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

    public abstract class HeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        struct HeaderItem
        {
            public HeaderItem(ReadyToRunSectionType id, DependencyNodeCore<NodeFactory> node, ISymbolNode startSymbol)
            {
                Id = id;
                Node = node;
                StartSymbol = startSymbol;
            }

            public readonly ReadyToRunSectionType Id;
            public readonly DependencyNodeCore<NodeFactory> Node;
            public readonly ISymbolNode StartSymbol;
        }

        private readonly List<HeaderItem> _items = new List<HeaderItem>();
        private readonly TargetDetails _target;
        private readonly ReadyToRunFlags _flags;

        public HeaderNode(TargetDetails target, ReadyToRunFlags flags)
        {
            _target = target;
            _flags = flags;
        }

        public void Add(ReadyToRunSectionType id, DependencyNodeCore<NodeFactory> node, ISymbolNode startSymbol)
        {
            _items.Add(new HeaderItem(id, node, startSymbol));
        }

        public int Offset => 0;
        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected abstract void AppendMangledHeaderName(NameMangler nameMangler, Utf8StringBuilder sb);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => AppendMangledHeaderName(nameMangler, sb);

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

            EmitHeaderPrefix(ref builder);

            // Don't bother sorting if we're not emitting the contents
            if (!relocsOnly)
                _items.MergeSort((x, y) => Comparer<int>.Default.Compare((int)x.Id, (int)y.Id));

            // ReadyToRunHeader.Flags
            builder.EmitInt((int)_flags);

            // ReadyToRunHeader.NumberOfSections
            ObjectDataBuilder.Reservation sectionCountReservation = builder.ReserveInt();

            int count = 0;
            foreach (var item in _items)
            {
                // Skip empty entries
                if (!relocsOnly && item.Node is ObjectNode on && on.ShouldSkipEmittingObjectNode(factory))
                    continue;

                // Unmarked nodes are not part of the graph
                if (!item.Node.Marked && !(item.Node is ObjectNode))
                {
                    Debug.Assert(item.Node is DelayLoadMethodCallThunkNodeRange);
                    continue;
                }

                builder.EmitInt((int)item.Id);

                builder.EmitReloc(item.StartSymbol, RelocType.IMAGE_REL_BASED_ADDR32NB);

                // The header entry for the runtime functions table should not include the 4 byte 0xffffffff sentinel
                // value in the covered range.
                int delta = item.Id == ReadyToRunSectionType.RuntimeFunctions ? RuntimeFunctionsTableNode.SentinelSizeAdjustment : 0;
                builder.EmitReloc(item.StartSymbol, RelocType.IMAGE_REL_SYMBOL_SIZE, delta);

                count++;
            }

            builder.EmitInt(sectionCountReservation, count);

            return builder.ToObjectData();
        }

        protected abstract void EmitHeaderPrefix(ref ObjectDataBuilder builder);

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
    }

    public class GlobalHeaderNode : HeaderNode
    {
        public GlobalHeaderNode(TargetDetails target, ReadyToRunFlags flags)
            : base(target, flags)
        {
        }

        protected override void AppendMangledHeaderName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunHeader");
        }

        protected override void EmitHeaderPrefix(ref ObjectDataBuilder builder)
        {
            // ReadyToRunHeader.Magic
            builder.EmitInt((int)(ReadyToRunHeaderConstants.Signature));

            // ReadyToRunHeader.MajorVersion
            builder.EmitShort((short)(ReadyToRunHeaderConstants.CurrentMajorVersion));
            builder.EmitShort((short)(ReadyToRunHeaderConstants.CurrentMinorVersion));
        }

        public override int ClassCode => (int)ObjectNodeOrder.ReadyToRunHeaderNode;
    }

    public class AssemblyHeaderNode : HeaderNode
    {
        private readonly int _index;

        public AssemblyHeaderNode(TargetDetails target, ReadyToRunFlags flags, int index)
            : base(target, flags)
        {
            _index = index;
        }

        protected override void EmitHeaderPrefix(ref ObjectDataBuilder builder)
        {
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _index - ((AssemblyHeaderNode)other)._index;
        }

        protected override void AppendMangledHeaderName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunAssemblyHeader__");
            sb.Append(_index.ToString());
        }

        public override int ClassCode => (int)ObjectNodeOrder.ReadyToRunAssemblyHeaderNode;
    }
}
