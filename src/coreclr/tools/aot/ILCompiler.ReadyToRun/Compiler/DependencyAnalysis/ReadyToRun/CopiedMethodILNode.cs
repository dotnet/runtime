// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedMethodILNode : ObjectNode, ISymbolDefinitionNode
    {
        // Sentinel body for stripped IL methods: tiny header (0x0A, the tiny-header encoding for
        // a 2-byte code size) followed by the two-byte opcode 0xFE 0x24, which is intentionally
        // unassigned/invalid. Invalid IL so it can never collide with a real method body; the
        // runtime detects it (in prestub.cpp) before it reaches the JIT or interpreter.
        private static readonly byte[] s_minimalILBody = [0x0A, 0xFE, 0x24];

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

            if (factory.OptimizationFlags.StripILBodies
                && factory.OptimizationFlags.CompiledMethodDefs.Contains(_method)
                && !MayNeedILAtRuntime(_method))
            {
                return new ObjectData(s_minimalILBody, Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
            }

            var rva = _method.MetadataReader.GetMethodDefinition(_method.Handle).RelativeVirtualAddress;
            var peReader = _method.Module.PEReader;
            var reader = peReader.GetSectionData(rva).GetReader();
            int size = MethodBodyBlock.Create(reader).Size;
            byte[] bodyBytes = peReader.GetSectionData(rva).GetReader().ReadBytes(size);

            return new ObjectData(bodyBytes, Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
        }

        private static bool MayNeedILAtRuntime(MethodDesc method)
        {
            if (method.HasInstantiation || method.OwningType.HasInstantiation)
            {
                // IL may be needed for new instantiations
                return true;
            }

            if (method.GetTypicalMethodDefinition().Signature.ReturnsTaskOrValueTask() && !method.IsAsync)
            {
                // IL may be needed for async version of non-async Task-returning method
                return true;
            }

            return false;
        }

        public override int ClassCode => 541651465;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((CopiedMethodILNode)other)._method);
        }
    }
}
