// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the MethodImpl table.
    /// </summary>
    public sealed class MethodImplementationNode : TokenBasedNode
    {
        public MethodImplementationNode(EcmaModule module, MethodImplementationHandle handle)
            : base(module, handle)
        {
        }

        private MethodImplementationHandle Handle => (MethodImplementationHandle)_handle;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            var methodImpl = _module.MetadataReader.GetMethodImplementation(Handle);
            sink.Add(new DependencyListEntry(factory.GetNodeForMethodToken(_module, methodImpl.MethodBody), "MethodImpl body"));
            sink.Add(new DependencyListEntry(factory.GetNodeForMethodToken(_module, methodImpl.MethodDeclaration), "MethodImpl decl"));
            sink.Add(new DependencyListEntry(factory.GetNodeForTypeToken(_module, methodImpl.Type), "MethodImpl type"));
        }

        public override string ToString()
        {
            return "MethodImpl";
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            var methodImpl = _module.MetadataReader.GetMethodImplementation(Handle);
            return writeContext.MetadataBuilder.AddMethodImplementation(
                (TypeDefinitionHandle)writeContext.TokenMap.MapToken(methodImpl.Type),
                writeContext.TokenMap.MapToken(methodImpl.MethodBody),
                writeContext.TokenMap.MapToken(methodImpl.MethodDeclaration));
        }
    }
}
