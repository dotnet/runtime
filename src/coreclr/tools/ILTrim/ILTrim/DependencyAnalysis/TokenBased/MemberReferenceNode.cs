// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the MemberRef metadata table.
    /// </summary>
    public sealed class MemberReferenceNode : TokenBasedNode
    {
        public MemberReferenceNode(EcmaModule module, MemberReferenceHandle handle)
            : base(module, handle)
        {
        }

        private MemberReferenceHandle Handle => (MemberReferenceHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MemberReference memberRef = _module.MetadataReader.GetMemberReference(Handle);

            // TODO: MemberReference has other dependencies from signature

            if (!memberRef.Parent.IsNil)
                yield return new DependencyListEntry(factory.GetNodeForToken(_module, memberRef.Parent), "Parent of member reference");
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            MemberReference memberRef = reader.GetMemberReference(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the signature blob might contain references to tokens we need to rewrite
            var signatureBlob = reader.GetBlobBytes(memberRef.Signature);

            return builder.AddMemberReference(writeContext.TokenMap.MapToken(memberRef.Parent),
                builder.GetOrAddString(reader.GetString(memberRef.Name)),
                builder.GetOrAddBlob(signatureBlob));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetMemberReference(Handle).Name);
        }
    }
}
