// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Assembly Reference metadata table.
    /// </summary>
    public sealed class AssemblyReferenceNode : TokenBasedNode
    {
        public AssemblyReferenceNode(EcmaModule module, AssemblyReferenceHandle handle)
            : base(module, handle)
        {
        }

        private AssemblyReferenceHandle Handle => (AssemblyReferenceHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            AssemblyReference assemblyRef = reader.GetAssemblyReference(Handle);

            var builder = writeContext.MetadataBuilder;

            return builder.AddAssemblyReference(builder.GetOrAddString(reader.GetString(assemblyRef.Name)),
                assemblyRef.Version,
                builder.GetOrAddString(reader.GetString(assemblyRef.Culture)),
                builder.GetOrAddBlob(reader.GetBlobBytes(assemblyRef.PublicKeyOrToken)),
                assemblyRef.Flags,
                builder.GetOrAddBlob(reader.GetBlobBytes(assemblyRef.HashValue)));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetAssemblyReference(Handle).Name);
        }
    }
}
