// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
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

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var methodImpl = _module.MetadataReader.GetMethodImplementation(Handle);
            yield return new(factory.GetNodeForToken(_module, methodImpl.MethodBody), "MethodImpl body");
            yield return new(factory.GetNodeForToken(_module, methodImpl.MethodDeclaration), "MethodImpl decl");
            yield return new(factory.GetNodeForToken(_module, methodImpl.Type), "MethodImpl type");
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
