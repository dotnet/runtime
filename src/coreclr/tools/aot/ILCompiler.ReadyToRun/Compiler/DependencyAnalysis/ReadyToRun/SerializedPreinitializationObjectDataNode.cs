// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class SerializedPreinitializationObjectDataNode : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly MetadataType _owningType;
        private readonly TypePreinit.ISerializableReference _data;
        private readonly int _allocationSiteId;

        public SerializedPreinitializationObjectDataNode(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
        {
            _owningType = owningType;
            _allocationSiteId = allocationSiteId;
            _data = data;
        }

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public override bool HasConditionalStaticDependencies => _data.HasConditionalDependencies;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            CombinedDependencyList result = null;
            _data.GetConditionalDependencies(ref result, factory);
            return result;
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__PreInitObj_"u8)
                .Append(nameMangler.GetMangledTypeName(_owningType))
                .Append(_allocationSiteId.ToString());
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
            => factory.Target.IsWindows ? ObjectNodeSection.ReadOnlyDataSection : ObjectNodeSection.DataSection;

        public override bool IsShareable => false;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);
            _data.WriteContent(ref builder, this, factory);
            return builder.ToObjectData();
        }

        public override int ClassCode => 214568742;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            SerializedPreinitializationObjectDataNode otherNode = (SerializedPreinitializationObjectDataNode)other;

            int result = comparer.Compare(_owningType, otherNode._owningType);
            if (result != 0)
                return result;

            return _allocationSiteId.CompareTo(otherNode._allocationSiteId);
        }
    }
}
