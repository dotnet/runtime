// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public abstract class Signature : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.DataSection;
        public override bool IsShareable => true;
        public override bool StaticDependenciesAreComputed => true;

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);
        public virtual int Offset => 0;
        int ISymbolNode.Offset => 0;
        public override int ClassCode => ClassCode;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
    }

    // A signature that can be preceded by a GCRefMap describing the call frame of its target method.
    public abstract class GCRefMapSignature : Signature
    {
        private byte[] _gcRefMap;

        internal bool EmitGCRefMap { get; set; }

        public override int Offset => _gcRefMap?.Length ?? 0;

        protected abstract void BuildGCRefMap(GCRefMapBuilder builder);

        internal byte[] GetGCRefMap(NodeFactory factory)
        {
            if (_gcRefMap is null)
            {
                GCRefMapBuilder builder = new GCRefMapBuilder(factory.Target, relocsOnly: false);
                BuildGCRefMap(builder);
                _gcRefMap = builder.Builder.ToObjectData().Data;
            }
            return _gcRefMap;
        }
    }
}
