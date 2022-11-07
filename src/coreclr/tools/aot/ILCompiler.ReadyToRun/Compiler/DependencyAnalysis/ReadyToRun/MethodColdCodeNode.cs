// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodColdCodeNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectData _methodColdCode;
        private MethodDesc _owningMethod;

        public MethodColdCodeNode(MethodDesc owningMethod)
        {
            _owningMethod = owningMethod;
        }

        public int Offset => 0;

        public override ObjectNodeSection Section
        {
            get
            {
                return _owningMethod.Context.Target.IsWindows ? ObjectNodeSection.ManagedCodeWindowsContentSection : ObjectNodeSection.ManagedCodeUnixContentSection;            
                
            }
        }

        public override bool IsShareable => false;

        public override int ClassCode => 788492408;

        public override bool StaticDependenciesAreComputed => _methodColdCode != null;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__coldcode_" + nameMangler.GetMangledMethodName(_owningMethod));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            MethodColdCodeNode otherNode = (MethodColdCodeNode)other;
            return comparer.Compare(_owningMethod, otherNode._owningMethod);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) => _methodColdCode;

        protected override string GetName(NodeFactory context) => throw new NotImplementedException();

        public void SetCode(ObjectData data)
        {
            Debug.Assert(_methodColdCode == null);
            _methodColdCode = data;
        }

        public int GetColdCodeSize()
        {
            return _methodColdCode.Data.Length;
        }
    }
}
