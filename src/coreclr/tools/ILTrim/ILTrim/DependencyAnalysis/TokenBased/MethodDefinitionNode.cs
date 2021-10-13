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

            // TODO: MethodDefinition has other references such as parameter metadata, signature, etc.

            yield return new DependencyListEntry(factory.TypeDefinition(_module, declaringType), "Method owning type");

            yield return new DependencyListEntry(factory.MethodBody(_module, Handle), "Method body");

            foreach (CustomAttributeHandle customAttribute in methodDef.GetCustomAttributes())
            {
                yield return new(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a method");
            }

            foreach (ParameterHandle parameter in methodDef.GetParameters())
            {
                yield return new(factory.Parameter(_module, parameter), "Parameter of method");
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            MethodDefinition methodDef = reader.GetMethodDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the signature blob might contain references to tokens we need to rewrite
            var signatureBlob = reader.GetBlobBytes(methodDef.Signature);

            MethodBodyNode bodyNode = writeContext.Factory.MethodBody(_module, Handle);
            int rva = bodyNode.Write(writeContext);

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
