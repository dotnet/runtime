// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the TypeSpec metadata table (a constructed type).
    /// </summary>
    public sealed class TypeSpecificationNode : TokenBasedNode
    {
        public TypeSpecificationNode(EcmaModule module, TypeSpecificationHandle handle)
            : base(module, handle)
        {
        }

        private TypeSpecificationHandle Handle => (TypeSpecificationHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            TypeSpecification typeSpec = _module.MetadataReader.GetTypeSpecification(Handle);

            // TODO: report dependencies from the signature
            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            TypeSpecification typeSpec = reader.GetTypeSpecification(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the signature blob might contain references to tokens we need to rewrite
            var signatureBlob = reader.GetBlobBytes(typeSpec.Signature);

            return builder.AddTypeSpecification(
                builder.GetOrAddBlob(signatureBlob));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into
            return "TypeSpecification";
        }
    }
}
