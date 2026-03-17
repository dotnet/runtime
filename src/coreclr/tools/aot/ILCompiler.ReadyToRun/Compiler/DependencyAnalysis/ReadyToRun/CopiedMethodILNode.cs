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
        // Minimal IL method body: tiny header (0x02 flags | 0x01 size << 2 = 0x06) + ret opcode (0x2A).
        private static readonly byte[] s_minimalILBody = new byte[] { 0x06, 0x2A };

        EcmaMethod _method;

        public CopiedMethodILNode(EcmaMethod method)
        {
            Debug.Assert(!method.IsAbstract);

            _method = method.GetTypicalMethodDefinition();
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ILMethod_"u8);
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
            var peReader = _method.Module.PEReader;
            byte[] bodyBytes;
            {
                var reader = peReader.GetSectionData(rva).GetReader();
                int size = MethodBodyBlock.Create(reader).Size;
                bodyBytes = peReader.GetSectionData(rva).GetReader().ReadBytes(size);
            }

            if (factory.OptimizationFlags.StripILBodies
                && factory.OptimizationFlags.CompiledMethodDefs is not null
                && factory.OptimizationFlags.CompiledMethodDefs.Contains(_method)
                && !_method.HasInstantiation
                && !_method.OwningType.HasInstantiation)
            {
                return new ObjectData(s_minimalILBody, Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
            }

            return new ObjectData(bodyBytes, Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
        }

        public override int ClassCode => 541651465;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((CopiedMethodILNode)other)._method);
        }
    }
}
