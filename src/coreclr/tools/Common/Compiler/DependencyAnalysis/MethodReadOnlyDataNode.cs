// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class MethodReadOnlyDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private MethodDesc _owningMethod;
        private ObjectData _data;

        public MethodReadOnlyDataNode(MethodDesc owningMethod)
        {
            _owningMethod = owningMethod;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;
        public override bool StaticDependenciesAreComputed => _data != null;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__readonlydata_" + nameMangler.GetMangledMethodName(_owningMethod));
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        public void InitializeData(ObjectData data)
        {
            Debug.Assert(_data == null);
            _data = data;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return _data;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

#if !SUPPORT_JIT
        public override int ClassCode => 674507768;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_owningMethod, ((MethodReadOnlyDataNode)other)._owningMethod);
        }
#endif
    }
}
