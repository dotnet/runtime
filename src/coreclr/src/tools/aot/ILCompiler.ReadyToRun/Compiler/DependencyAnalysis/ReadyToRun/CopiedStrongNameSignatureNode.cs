// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.PortableExecutable;

using Internal.Text;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Container node for emitting an input MSIL image's strong name signature blob
    /// </summary>
    public class CopiedStrongNameSignatureNode : ObjectNode, ISymbolDefinitionNode
    {
        private EcmaModule _module;

        public CopiedStrongNameSignatureNode(EcmaModule module)
        {
            _module = module;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override int ClassCode => 932489234;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__StrongNameSignature");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public int Size => _module.PEReader.PEHeaders.CorHeader.StrongNameSignatureDirectory.Size;

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

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(4);
            builder.AddSymbol(this);

            DirectoryEntry strongNameDirectory = _module.PEReader.PEHeaders.CorHeader.StrongNameSignatureDirectory;
            PEMemoryBlock block = _module.PEReader.GetSectionData(strongNameDirectory.RelativeVirtualAddress);
            builder.EmitBytes(block.GetReader().ReadBytes(strongNameDirectory.Size));

            return builder.ToObjectData();
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _module.CompareTo(((CopiedStrongNameSignatureNode)other)._module);
        }
    }
}
