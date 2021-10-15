// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the ModuleRef table.
    /// </summary>
    public sealed class ModuleReferenceNode : TokenBasedNode
    {
        public ModuleReferenceNode(EcmaModule module, ModuleReferenceHandle handle)
            : base(module, handle)
        {
        }

        private ModuleReferenceHandle Handle => (ModuleReferenceHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return null;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            ModuleReference moduleRef = reader.GetModuleReference(Handle);

            var builder = writeContext.MetadataBuilder;

            return builder.AddModuleReference(
                builder.GetOrAddString(reader.GetString(moduleRef.Name)));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetModuleReference(Handle).Name);
        }
    }
}
