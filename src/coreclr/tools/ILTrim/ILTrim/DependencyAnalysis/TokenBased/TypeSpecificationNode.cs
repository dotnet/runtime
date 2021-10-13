// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

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

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzeTypeSpecSignature(
                _module,
                _module.MetadataReader.GetBlobReader(typeSpec.Signature),
                factory,
                dependencies);

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            TypeSpecification typeSpec = reader.GetTypeSpecification(Handle);

            var signatureBlob = writeContext.GetSharedBlobBuilder();

            EcmaSignatureRewriter.RewriteTypeSpecSignature(
                reader.GetBlobReader(typeSpec.Signature),
                writeContext.TokenMap,
                signatureBlob);

            var builder = writeContext.MetadataBuilder;

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
