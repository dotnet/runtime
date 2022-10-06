// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public class EETypeOptionalFieldsNode : ObjectNode, ISymbolDefinitionNode
    {
        private EETypeNode _owner;

        public EETypeOptionalFieldsNode(EETypeNode owner)
        {
            _owner = owner;
        }

        public override ObjectNodeSection Section
        {
            get
            {
                if (_owner.Type.Context.Target.IsWindows)
                    return ObjectNodeSection.FoldableReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__optionalfields_");
            _owner.AppendMangledName(nameMangler, sb);
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            _owner.ComputeOptionalEETypeFields(factory, relocsOnly: false);
            return _owner.ShouldSkipEmittingObjectNode(factory) || !_owner.HasOptionalFields;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialAlignment(1);
            objData.AddSymbol(this);

            if (!relocsOnly)
            {
                _owner.ComputeOptionalEETypeFields(factory, relocsOnly: false);
                objData.EmitBytes(_owner.GetOptionalFieldsData());
            }

            return objData.ToObjectData();
        }

        public override int ClassCode => 821718028;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return CompareImpl(_owner, ((EETypeOptionalFieldsNode)other)._owner, comparer);
        }
    }
}
