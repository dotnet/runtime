// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct PropertyDefinition
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal PropertyDefinition(MetadataReader reader, PropertyDefinitionHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private PropertyDefinitionHandle Handle
        {
            get { return PropertyDefinitionHandle.FromRowId(_rowId); }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.PropertyTable.GetName(Handle);
            }
        }

        public PropertyAttributes Attributes
        {
            get
            {
                return _reader.PropertyTable.GetFlags(Handle);
            }
        }

        public BlobHandle Signature
        {
            get
            {
                return _reader.PropertyTable.GetSignature(Handle);
            }
        }

        public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
            var blobReader = _reader.GetBlobReader(Signature);
            return decoder.DecodeMethodSignature(ref blobReader);
        }

        public ConstantHandle GetDefaultValue()
        {
            return _reader.ConstantTable.FindConstant(Handle);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }

        public PropertyAccessors GetAccessors()
        {
            int getter = 0;
            int setter = 0;
            ImmutableArray<MethodDefinitionHandle>.Builder? other = null;

            ushort methodCount;
            int firstRowId = _reader.MethodSemanticsTable.FindSemanticMethodsForProperty(Handle, out methodCount);
            for (ushort i = 0; i < methodCount; i++)
            {
                int rowId = firstRowId + i;
                switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
                {
                    case MethodSemanticsAttributes.Getter:
                        getter = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Setter:
                        setter = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Other:
                        other ??= ImmutableArray.CreateBuilder<MethodDefinitionHandle>();

                        other.Add(_reader.MethodSemanticsTable.GetMethod(rowId));
                        break;
                }
            }

            var otherAccessors = other?.ToImmutable() ?? ImmutableArray<MethodDefinitionHandle>.Empty;
            return new PropertyAccessors(getter, setter, otherAccessors);
        }
    }
}
