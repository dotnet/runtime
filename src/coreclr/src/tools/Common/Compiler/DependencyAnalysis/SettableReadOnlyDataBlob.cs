// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class SettableReadOnlyDataBlob : ObjectNode, ISymbolDefinitionNode
    {
        private Utf8String _name;
        private ObjectNodeSection _section;
        private ObjectData _data;

        public SettableReadOnlyDataBlob(Utf8String name, ObjectNodeSection section)
        {
            _name = name;
            _section = section;
        }

        public override ObjectNodeSection Section => _section;
        public override bool StaticDependenciesAreComputed => _data != null;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_name);
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
            return _name.CompareTo(((SettableReadOnlyDataBlob)other)._name);
        }
#endif
    }
}

