// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class JumpStubNode : AssemblyStubNode, INodeWithWasmSignature
    {
        private ISymbolNode _target;

        public ISymbolNode Target
        {
            get
            {
                return _target;
            }
        }

        public JumpStubNode(ISymbolNode target)
        {
            _target = target;
        }

        public abstract WasmSignature WasmSignature { get; }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => 737788182;
    }
}
