// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public class BlobNode : ObjectNode, ISymbolDefinitionNode
    {
        private Utf8String _name;
        private ObjectNodeSection _section;
        private byte[] _data;
        private int _alignment;

        public BlobNode(Utf8String name, ObjectNodeSection section, byte[] data, int alignment)
        {
            _name = name;
            _section = section;
            _data = data;
            _alignment = alignment;
        }

        public override ObjectNodeSection Section => _section;
        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_name);
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return new ObjectData(_data, Array.Empty<Relocation>(), _alignment, new ISymbolDefinitionNode[] { this });
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

#if !SUPPORT_JIT
        public override int ClassCode => -470351029;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _name.CompareTo(((BlobNode)other)._name);
        }
#endif
    }
}
