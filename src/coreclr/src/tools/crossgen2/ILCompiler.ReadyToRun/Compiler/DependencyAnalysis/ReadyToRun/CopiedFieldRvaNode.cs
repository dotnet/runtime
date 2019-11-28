// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.Text;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedFieldRvaNode : ObjectNode, ISymbolDefinitionNode
    {
        public int Rva { get; private set; }
        public byte[] Data { get; private set; }

        public CopiedFieldRvaNode(int rva)
        {
            Rva = rva;
        }

        public void SetData(byte[] data)
        {
            Data = data;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override int ClassCode => 223495;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

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

            Debug.Assert(Data != null);

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);
            builder.EmitBytes(Data);
            return builder.ToObjectData();
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"_FieldRvaData_{Rva}_{Data.Length}");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return Rva - ((CopiedFieldRvaNode)other).Rva;
        }
    }
}
