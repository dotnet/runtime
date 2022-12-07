// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Abstract api to allow creation of various different types of import nodes as might be exposed through the NodeFactory
    /// </summary>
    public abstract class ImportedNodeProvider
    {
        public abstract IEETypeNode ImportedEETypeNode(NodeFactory factory, TypeDesc type);
        public abstract ISortableSymbolNode ImportedGCStaticNode(NodeFactory factory, MetadataType type);
        public abstract ISortableSymbolNode ImportedNonGCStaticNode(NodeFactory factory, MetadataType type);
        public abstract ISortableSymbolNode ImportedTypeDictionaryNode(NodeFactory factory, TypeDesc type);
        public abstract ISortableSymbolNode ImportedMethodDictionaryNode(NodeFactory factory, MethodDesc method);
        public abstract IMethodNode ImportedMethodCodeNode(NodeFactory factory, MethodDesc method, bool unboxingStub);
    }

    public class ImportedNodeProviderThrowing : ImportedNodeProvider
    {
        public override IEETypeNode ImportedEETypeNode(NodeFactory factory, TypeDesc type)
        {
            throw new NotSupportedException();
        }

        public override ISortableSymbolNode ImportedGCStaticNode(NodeFactory factory, MetadataType type)
        {
            throw new NotSupportedException();
        }

        public override ISortableSymbolNode ImportedNonGCStaticNode(NodeFactory factory, MetadataType type)
        {
            throw new NotSupportedException();
        }

        public override ISortableSymbolNode ImportedTypeDictionaryNode(NodeFactory factory, TypeDesc type)
        {
            throw new NotSupportedException();
        }

        public override ISortableSymbolNode ImportedMethodDictionaryNode(NodeFactory factory, MethodDesc method)
        {
            throw new NotSupportedException();
        }

        public override IMethodNode ImportedMethodCodeNode(NodeFactory factory, MethodDesc method, bool unboxingStub)
        {
            throw new NotSupportedException();
        }
    }
}
