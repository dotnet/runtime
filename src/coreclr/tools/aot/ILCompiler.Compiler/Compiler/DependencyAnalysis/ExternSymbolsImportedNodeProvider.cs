// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ExternSymbolsImportedNodeProvider : ImportedNodeProvider
    {
        public override IEETypeNode ImportedEETypeNode(NodeFactory factory, TypeDesc type)
        {
            return new ExternEETypeSymbolNode(factory, type);
        }

        public override ISortableSymbolNode ImportedGCStaticNode(NodeFactory factory, MetadataType type)
        {
            return new ExternSymbolNode(GCStaticsNode.GetMangledName(type, factory.NameMangler));
        }

        public override ISortableSymbolNode ImportedNonGCStaticNode(NodeFactory factory, MetadataType type)
        {
            return new ExternSymbolNode(NonGCStaticsNode.GetMangledName(type, factory.NameMangler));
        }

        public override ISortableSymbolNode ImportedMethodDictionaryNode(NodeFactory factory, MethodDesc method)
        {
            throw new NotImplementedException();
        }

        public override IMethodNode ImportedMethodCodeNode(NodeFactory factory, MethodDesc method, bool unboxingStub)
        {
            return new ExternMethodSymbolNode(factory, method, unboxingStub);
        }
    }
}
