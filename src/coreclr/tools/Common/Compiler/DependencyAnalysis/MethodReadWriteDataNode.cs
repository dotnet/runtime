// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class MethodReadWriteDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private MethodDesc _owningMethod;
        private ObjectData _data;

        public MethodReadWriteDataNode(MethodDesc owningMethod)
        {
            _owningMethod = owningMethod;
        }

#if !READYTORUN
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            IMethodNode owningBody = factory.MethodEntrypoint(_owningMethod);
            return factory.ObjectInterner.GetDeduplicatedSymbol(factory, owningBody) != owningBody;
        }
#endif

        public override ObjectNodeSection GetSection(NodeFactory factory)
            => ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => _data != null;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__readwritedata_"u8).Append(nameMangler.GetMangledMethodName(_owningMethod));
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
        public override int ClassCode => 689723708;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_owningMethod, ((MethodReadWriteDataNode)other)._owningMethod);
        }
#endif
    }
}
