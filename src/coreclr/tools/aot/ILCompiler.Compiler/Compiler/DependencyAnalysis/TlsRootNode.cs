// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public class TlsRootNode : ObjectNode, ISymbolDefinitionNode
    {
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("tls_InlinedThreadStatics"u8);
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TLSSection;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            // storage for InlinedThreadStaticRoot instances

            // m_threadStaticsBase
            objData.EmitZeroPointer();

            // m_next
            objData.EmitZeroPointer();

            // m_typeManager
            objData.EmitZeroPointer();

            return objData.ToObjectData();
        }

        public override int ClassCode => -985742028;
    }
}
