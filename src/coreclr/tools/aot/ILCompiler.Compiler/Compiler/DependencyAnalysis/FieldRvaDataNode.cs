// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public class FieldRvaDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly EcmaField _field;

        public EcmaField Field => _field;

        public FieldRvaDataNode(EcmaField field)
        {
            Debug.Assert(field.HasRva);
            _field = field;
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledFieldName(_field));
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            int fieldTypePack = (_field.FieldType as MetadataType)?.GetClassLayout().PackingSize ?? 1;
            byte[] data = relocsOnly ? Array.Empty<byte>() : _field.GetFieldRvaData();
            return new ObjectData(
                data,
                Array.Empty<Relocation>(),
                Math.Max(factory.Target.PointerSize, fieldTypePack),
                new ISymbolDefinitionNode[] { this });
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

#if !SUPPORT_JIT
        public override int ClassCode => -456126;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_field, ((FieldRvaDataNode)other)._field);
        }
#endif
    }
}
