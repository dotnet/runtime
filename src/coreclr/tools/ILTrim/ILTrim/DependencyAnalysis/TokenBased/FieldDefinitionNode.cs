// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the Field table.
    /// </summary>
    public sealed class FieldDefinitionNode : TokenBasedNode
    {
        public FieldDefinitionNode(EcmaModule module, FieldDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private FieldDefinitionHandle Handle => (FieldDefinitionHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            FieldDefinition fieldDef = _module.MetadataReader.GetFieldDefinition(Handle);
            TypeDefinitionHandle declaringType = fieldDef.GetDeclaringType();

            // TODO: Check if FieldDefinition has other references that needed to be added
            yield return new DependencyListEntry(factory.TypeDefinition(_module, declaringType), "Field owning type");

        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            FieldDefinition fieldDef = reader.GetFieldDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the signature blob might contain references to tokens we need to rewrite
            var signatureBlob = reader.GetBlobBytes(fieldDef.Signature);

            return builder.AddFieldDefinition(
                fieldDef.Attributes,
                builder.GetOrAddString(reader.GetString(fieldDef.Name)),
                builder.GetOrAddBlob(signatureBlob));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetFieldDefinition(Handle).Name);
        }
    }
}
