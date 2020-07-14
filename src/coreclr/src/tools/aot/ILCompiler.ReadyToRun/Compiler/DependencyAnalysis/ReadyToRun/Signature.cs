// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public abstract class Signature : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        public override ObjectNodeSection Section => ObjectNodeSection.DataSection;
        public override bool IsShareable => true;
        public override bool StaticDependenciesAreComputed => true;

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);
        public int Offset => 0;
        public override int ClassCode => ClassCode;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
    }
}
