// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ClonedConstructedEETypeNode : ConstructedEETypeNode, ISymbolDefinitionNode
    {
        public ClonedConstructedEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler) + " cloned";

        public override ISymbolNode NodeForLinkage(NodeFactory factory) => this;

        //
        // A cloned type must be named differently than the type it is a clone of so the linker
        // will have an unambiguous symbol to resolve
        //
        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__Cloned_EEType_").Append(nameMangler.GetMangledTypeName(_type));
        }
        public override bool IsShareable => true;

        protected override void OutputRelatedType(NodeFactory factory, ref ObjectDataBuilder objData)
        {
            //
            // Cloned types use the related type field to point via an IAT slot at their true implementation
            //
            objData.EmitPointerReloc(factory.NecessaryTypeSymbol(_type));
        }

        public override int ClassCode => -288888778;
    }
}
