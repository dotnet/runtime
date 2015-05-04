// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct PropertyDefinition
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

            ushort methodCount;
            int firstRowId = (int)_reader.MethodSemanticsTable.FindSemanticMethodsForProperty(Handle, out methodCount);
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
                        // TODO: expose 'Other' collection on PropertyAccessors for completeness.
                }
            }

            return new PropertyAccessors(getter, setter);
        }
    }
}