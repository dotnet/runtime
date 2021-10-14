// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the MethodDef table.
    /// </summary>
    public sealed class MethodDefinitionNode : TokenBasedNode
    {
        public MethodDefinitionNode(EcmaModule module, MethodDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private MethodDefinitionHandle Handle => (MethodDefinitionHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MethodDefinition methodDef = _module.MetadataReader.GetMethodDefinition(Handle);
            TypeDefinitionHandle declaringType = methodDef.GetDeclaringType();

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzeMethodSignature(
                _module,
                _module.MetadataReader.GetBlobReader(methodDef.Signature),
                factory,
                dependencies);

            dependencies.Add(factory.TypeDefinition(_module, declaringType), "Method owning type");

            dependencies.Add(factory.MethodBody(_module, Handle), "Method body");

            foreach (CustomAttributeHandle customAttribute in methodDef.GetCustomAttributes())
            {
                dependencies.Add(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a method");
            }

            foreach (ParameterHandle parameter in methodDef.GetParameters())
            {
                dependencies.Add(factory.Parameter(_module, parameter), "Parameter of method");
            }

            foreach (GenericParameterHandle parameter in methodDef.GetGenericParameters())
            {
                dependencies.Add(factory.GenericParameter(_module, parameter), "Generic Parameter of method");
            }

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            MethodDefinition methodDef = reader.GetMethodDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            MethodBodyNode bodyNode = writeContext.Factory.MethodBody(_module, Handle);
            int rva = bodyNode.Write(writeContext);

            BlobBuilder signatureBlob = writeContext.GetSharedBlobBuilder();
            EcmaSignatureRewriter.RewriteMethodSignature(
                reader.GetBlobReader(methodDef.Signature),
                writeContext.TokenMap,
                signatureBlob);

            return builder.AddMethodDefinition(
                methodDef.Attributes,
                methodDef.ImplAttributes,
                builder.GetOrAddString(reader.GetString(methodDef.Name)),
                builder.GetOrAddBlob(signatureBlob),
                rva,
                writeContext.TokenMap.MapMethodParamList(Handle));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetMethodDefinition(Handle).Name);
        }
    }
}
