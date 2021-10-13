// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the GenericParam metadata table.
    /// </summary>
    public sealed class GenericParameterNode : TokenBasedNode
    {
        public GenericParameterNode(EcmaModule module, GenericParameterHandle handle)
            : base(module, handle)
        {
        }

        private GenericParameterHandle Handle => (GenericParameterHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            GenericParameter genericParam = _module.MetadataReader.GetGenericParameter(Handle);
            foreach (var genericParamConstrain in genericParam.GetConstraints())
            {
                yield return new DependencyListEntry(factory.GenericParameterConstraint(_module, genericParamConstrain), "Generic Parameter Constraint of Generic Parameter");
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            GenericParameter genericParam = reader.GetGenericParameter(Handle);

            var builder = writeContext.MetadataBuilder;

            return builder.AddGenericParameter(writeContext.TokenMap.MapToken(genericParam.Parent),
                genericParam.Attributes,
                builder.GetOrAddString(reader.GetString(genericParam.Name)),
                genericParam.Index);
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetGenericParameter(Handle).Name);
        }
    }
}
