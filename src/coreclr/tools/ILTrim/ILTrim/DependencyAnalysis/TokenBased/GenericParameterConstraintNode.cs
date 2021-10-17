// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the GenericParamConstraint metadata table.
    /// </summary>
    public sealed class GenericParameterConstraintNode : TokenBasedNodeWithDelayedSort
    {
        public GenericParameterConstraintNode(EcmaModule module, GenericParameterConstraintHandle handle)
            : base(module, handle)
        {
        }

        private GenericParameterConstraintHandle Handle => (GenericParameterConstraintHandle)_handle;

        private int _ownerCodedIndex = -1;
        private int _index = -1;

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

        public override void PrepareForDelayedSort(TokenMap.Builder tokenMap)
        {
            GenericParameterConstraint genericParamConstraint = _module.MetadataReader.GetGenericParameterConstraint(Handle);
            GenericParameter genericParam = _module.MetadataReader.GetGenericParameter(genericParamConstraint.Parameter);
            _ownerCodedIndex = CodedIndex.TypeOrMethodDef(tokenMap.MapToken(genericParam.Parent));
            _index = genericParam.Index;
        }

        public override int CompareTo(TokenWriterNode other)
        {
            if (other is GenericParameterConstraintNode otherGenericParameterConstraint)
            {
                Debug.Assert(_ownerCodedIndex >= 0 && otherGenericParameterConstraint._ownerCodedIndex >= 0);
                if (_ownerCodedIndex == otherGenericParameterConstraint._ownerCodedIndex &&
                    _index == otherGenericParameterConstraint._index)
                {
                    return MetadataTokens.GetRowNumber(Handle).CompareTo(
                        MetadataTokens.GetRowNumber(otherGenericParameterConstraint.Handle));
                }
                else
                {
                    return GenericParameterNode.CompareGenericParameters(_ownerCodedIndex, _index,
                        otherGenericParameterConstraint._ownerCodedIndex, otherGenericParameterConstraint._index);
                }
            }
            else
            {
                return base.CompareTo(other);
            }
        }
    }
}
