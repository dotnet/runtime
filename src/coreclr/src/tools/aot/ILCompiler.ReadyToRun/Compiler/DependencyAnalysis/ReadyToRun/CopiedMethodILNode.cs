// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Internal.Text;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedMethodILNode : ObjectNode, ISymbolDefinitionNode
    {
        EcmaMethod _method;

        public CopiedMethodILNode(EcmaMethod method)
        {
            Debug.Assert(!method.IsAbstract);

            _method = (EcmaMethod)method.GetTypicalMethodDefinition();
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ILMethod_");
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(
                    data: Array.Empty<byte>(),
                    relocs: Array.Empty<Relocation>(),
                    alignment: 1,
                    definedSymbols: new ISymbolDefinitionNode[] { this });
            }

            var rva = _method.MetadataReader.GetMethodDefinition(_method.Handle).RelativeVirtualAddress;
            var reader = _method.Module.PEReader.GetSectionData(rva).GetReader();
            int size = MethodBodyBlock.Create(reader).Size;
            
            return new ObjectData(reader.ReadBytes(size), Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
        }

        public override int ClassCode => 541651465;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((CopiedMethodILNode)other)._method);
        }
    }
}
