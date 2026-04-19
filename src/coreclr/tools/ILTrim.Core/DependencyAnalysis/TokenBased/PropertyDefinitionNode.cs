// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the Property table.
    /// </summary>
    public sealed class PropertyDefinitionNode : TokenBasedNode
    {
        public PropertyDefinitionNode(EcmaModule module, PropertyDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private PropertyDefinitionHandle Handle => (PropertyDefinitionHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _module.MetadataReader;

            PropertyDefinition property = reader.GetPropertyDefinition(Handle);

            TypeDefinitionHandle declaringTypeHandle = property.GetDeclaringType();

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzePropertySignature(
                _module,
                reader.GetBlobReader(property.Signature),
                factory,
                dependencies);

            dependencies.Add(factory.TypeDefinition(_module, declaringTypeHandle), "Property owning type");

            CustomAttributeNode.AddDependenciesDueToCustomAttributes(ref dependencies, factory, _module, property.GetCustomAttributes());

            PropertyAccessors accessors = property.GetAccessors();
            if (!accessors.Getter.IsNil)
                dependencies.Add(factory.MethodDefinition(_module, accessors.Getter), "Property getter");
            if (!accessors.Setter.IsNil)
                dependencies.Add(factory.MethodDefinition(_module, accessors.Setter), "Property setter");
            Debug.Assert(accessors.Others.Length == 0);

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            PropertyDefinition property = reader.GetPropertyDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            BlobBuilder signatureBlob = writeContext.GetSharedBlobBuilder();
            EcmaSignatureRewriter.RewritePropertySignature(
                reader.GetBlobReader(property.Signature),
                writeContext.TokenMap,
                signatureBlob);

            PropertyDefinitionHandle targetPropertyHandle = builder.AddProperty(
                property.Attributes,
                builder.GetOrAddString(reader.GetString(property.Name)),
                builder.GetOrAddBlob(signatureBlob));

            // Add MethodSemantics rows to link properties with accessor methods.
            // MethodSemantics rows may be added in any order.
            PropertyAccessors accessors = property.GetAccessors();
            if (!accessors.Getter.IsNil)
            {
                builder.AddMethodSemantics(
                    targetPropertyHandle,
                    MethodSemanticsAttributes.Getter,
                    (MethodDefinitionHandle)writeContext.TokenMap.MapToken(accessors.Getter));
            }
            if (!accessors.Setter.IsNil)
            {
                builder.AddMethodSemantics(
                    targetPropertyHandle,
                    MethodSemanticsAttributes.Setter,
                    (MethodDefinitionHandle)writeContext.TokenMap.MapToken(accessors.Setter));
            }

            return targetPropertyHandle;
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetPropertyDefinition(Handle).Name);
        }
    }
}
