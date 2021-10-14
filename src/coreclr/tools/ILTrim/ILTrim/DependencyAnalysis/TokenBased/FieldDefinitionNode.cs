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

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzeFieldSignature(
                _module,
                _module.MetadataReader.GetBlobReader(fieldDef.Signature),
                factory,
                dependencies);

            dependencies.Add(factory.TypeDefinition(_module, declaringType), "Field owning type");

            if((fieldDef.Attributes & FieldAttributes.Literal) == FieldAttributes.Literal)
            {
                dependencies.Add(factory.GetNodeForToken(_module, fieldDef.GetDefaultValue()), "Constant in field definition");
            }

            foreach (CustomAttributeHandle customAttribute in fieldDef.GetCustomAttributes())
            {
                dependencies.Add(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a field");
            }

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            FieldDefinition fieldDef = reader.GetFieldDefinition(Handle);

            var builder = writeContext.MetadataBuilder;
            BlobBuilder signatureBlob = writeContext.GetSharedBlobBuilder();

            EcmaSignatureRewriter.RewriteFieldSignature(
                reader.GetBlobReader(fieldDef.Signature),
                writeContext.TokenMap,
                signatureBlob);

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
