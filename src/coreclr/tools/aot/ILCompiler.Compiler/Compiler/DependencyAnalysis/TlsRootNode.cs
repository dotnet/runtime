// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public class TlsRootNode : ObjectNode, ISymbolDefinitionNode
    {
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            // tls_InlinedThreadStatics
            sb.Append(nameMangler.CompilationUnitPrefix).Append("tls_InlinedThreadStatics");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TLSSection;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.AddSymbol(this);
            objData.RequireInitialPointerAlignment();

            // root
            objData.EmitZeroPointer();

            // next
            objData.EmitZeroPointer();

            // type manager
            objData.EmitPointerReloc(factory.TypeManagerIndirection);

            return objData.ToObjectData();
        }

        // TODO: VS where this should come from?
        public override int ClassCode => -985742028;
    }
}
