// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Type Reference metadata table.
    /// </summary>
    public sealed class TypeReferenceNode : TokenBasedNode
    {
        public TypeReferenceNode(EcmaModule module, TypeReferenceHandle handle)
            : base(module, handle)
        {
        }

        private TypeReferenceHandle Handle => (TypeReferenceHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            TypeReference typeRef = _module.MetadataReader.GetTypeReference(Handle);

            yield return new(factory.GetNodeForToken(_module, typeRef.ResolutionScope), "Resolution Scope of a type reference");
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            TypeReference typeRef = reader.GetTypeReference(Handle);

            var builder = writeContext.MetadataBuilder;

            return builder.AddTypeReference(writeContext.TokenMap.MapToken(typeRef.ResolutionScope),
                builder.GetOrAddString(reader.GetString(typeRef.Namespace)),
                builder.GetOrAddString(reader.GetString(typeRef.Name)));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetTypeReference(Handle).Name);
        }
    }
}
