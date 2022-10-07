// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class MethodExceptionHandlingInfoNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly MethodDesc _owningMethod;
        private readonly ObjectData _data;

        public MethodDesc Method => _owningMethod;

        public MethodExceptionHandlingInfoNode(MethodDesc owningMethod, ObjectData data)
        {
            _owningMethod = owningMethod;
            Debug.Assert(data.DefinedSymbols == null || data.DefinedSymbols.Length == 0);
            _data = new ObjectData(data.Data, data.Relocs, data.Alignment, new ISymbolDefinitionNode[] { this });
        }

        public override ObjectNodeSection Section => _owningMethod.Context.Target.IsWindows
            ? ObjectNodeSection.FoldableReadOnlyDataSection
            : ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__ehinfo_" + nameMangler.GetMangledMethodName(_owningMethod));
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return _data;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

#if !SUPPORT_JIT
        public override int ClassCode => 64872398;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_owningMethod, ((MethodExceptionHandlingInfoNode)other)._owningMethod);
        }
#endif
    }
}
