// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

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
            // TODO: the signature might depend on other tokens
            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            StandaloneSignature standaloneSig = reader.GetStandaloneSignature(Handle);

            // TODO: the signature might have tokens we need to rewrite
            var builder = writeContext.MetadataBuilder;
            return builder.AddStandaloneSignature(
                builder.GetOrAddBlob(reader.GetBlobBytes(standaloneSig.Signature))
                );
        }

        public override string ToString()
        {
            return "Standalone signature";
        }
    }
}
