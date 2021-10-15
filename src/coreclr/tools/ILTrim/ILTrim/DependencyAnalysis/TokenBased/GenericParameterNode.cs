// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the GenericParam metadata table.
    /// </summary>
    public sealed class GenericParameterNode : TokenBasedNodeWithDelayedSort
    {
        public GenericParameterNode(EcmaModule module, GenericParameterHandle handle)
            : base(module, handle)
        {
        }

        private GenericParameterHandle Handle => (GenericParameterHandle)_handle;

        private int _ownerCodedIndex = -1;
        private int _index = -1;

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

        public override void PrepareForDelayedSort(TokenMap.Builder tokenMap)
        {
            GenericParameter genericParam = _module.MetadataReader.GetGenericParameter(Handle);
            _ownerCodedIndex = CodedIndex.TypeOrMethodDef(tokenMap.MapToken(genericParam.Parent));
            _index = genericParam.Index;
        }

        public override int CompareTo(TokenWriterNode other)
        {
            if (other is GenericParameterNode otherGenericParameter)
            {
                Debug.Assert(_ownerCodedIndex >= 0 && otherGenericParameter._ownerCodedIndex >= 0);

                if (_ownerCodedIndex == otherGenericParameter._ownerCodedIndex)
                    return _index.CompareTo(otherGenericParameter._index);
                else
                    return _ownerCodedIndex.CompareTo(otherGenericParameter._ownerCodedIndex);
            }
            else
            {
                return base.CompareTo(other);
            }
        }
    }
}
