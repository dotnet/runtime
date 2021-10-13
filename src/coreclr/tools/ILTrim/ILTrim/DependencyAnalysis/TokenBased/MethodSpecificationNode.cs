// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the MethodSpec metadata table (an instantiated generic method).
    /// </summary>
    public sealed class MethodSpecificationNode : TokenBasedNode
    {
        public MethodSpecificationNode(EcmaModule module, MethodSpecificationHandle handle)
            : base(module, handle)
        {
        }

        private MethodSpecificationHandle Handle => (MethodSpecificationHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MethodSpecification methodSpec = _module.MetadataReader.GetMethodSpecification(Handle);

            // TODO: report dependencies from the signature

            yield return new(factory.GetNodeForToken(_module, methodSpec.Method), "Instantiated method");
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            MethodSpecification methodSpec = reader.GetMethodSpecification(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the signature blob might contain references to tokens we need to rewrite
            var signatureBlob = reader.GetBlobBytes(methodSpec.Signature);

            return builder.AddMethodSpecification(
                writeContext.TokenMap.MapToken(methodSpec.Method),
                builder.GetOrAddBlob(signatureBlob));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into
            return "MethodSpecification";
        }
    }
}
