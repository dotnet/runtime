// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the StandaloneSignature table.
    /// </summary>
    public sealed class StandaloneSignatureNode : TokenBasedNode
    {
        public StandaloneSignatureNode(EcmaModule module, StandaloneSignatureHandle handle)
            : base(module, handle)
        {
        }

        private StandaloneSignatureHandle Handle => (StandaloneSignatureHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _module.MetadataReader;

            StandaloneSignature standaloneSig = reader.GetStandaloneSignature(Handle);

            BlobReader signatureReader = reader.GetBlobReader(standaloneSig.Signature);

            return EcmaSignatureAnalyzer.AnalyzeLocalVariableBlob(
                _module,
                signatureReader,
                factory
                );
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            StandaloneSignature standaloneSig = reader.GetStandaloneSignature(Handle);
            BlobBuilder blobBuilder = writeContext.GetSharedBlobBuilder();

            EcmaSignatureRewriter.RewriteLocalVariableBlob(
                reader.GetBlobReader(standaloneSig.Signature),
                writeContext.TokenMap,
                blobBuilder);

            var builder = writeContext.MetadataBuilder;
            return builder.AddStandaloneSignature(
                builder.GetOrAddBlob(blobBuilder));
        }

        public override string ToString()
        {
            return "Standalone signature";
        }
    }
}
