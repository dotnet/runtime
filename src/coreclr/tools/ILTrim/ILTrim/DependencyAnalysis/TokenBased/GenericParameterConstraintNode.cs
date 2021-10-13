// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the GenericParamConstraint metadata table.
    /// </summary>
    public sealed class GenericParameterConstraintNode : TokenBasedNode
    {
        public GenericParameterConstraintNode(EcmaModule module, GenericParameterConstraintHandle handle)
            : base(module, handle)
        {
        }

        private GenericParameterConstraintHandle Handle => (GenericParameterConstraintHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            GenericParameterConstraint genericParamConstraint = _module.MetadataReader.GetGenericParameterConstraint(Handle);
            yield return new DependencyListEntry(factory.GetNodeForToken(_module, genericParamConstraint.Type), "Parameter constrained to type");
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            GenericParameterConstraint genericParamConstraint = reader.GetGenericParameterConstraint(Handle);

            var builder = writeContext.MetadataBuilder;

            return builder.AddGenericParameterConstraint(
                (GenericParameterHandle)writeContext.TokenMap.MapToken(genericParamConstraint.Parameter),
                writeContext.TokenMap.MapToken(genericParamConstraint.Type));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into
            return "GenericParameterConstraint";
        }
    }
}
