// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct PropertyDefinition
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal PropertyDefinition(MetadataReader reader, PropertyDefinitionHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private PropertyDefinitionHandle Handle
        {
            get { return PropertyDefinitionHandle.FromRowId(rowId); }
        }

        public StringHandle Name
        {
            get
            {
                return reader.PropertyTable.GetName(Handle);
            }
        }

        public PropertyAttributes Attributes
        {
            get
            {
                return reader.PropertyTable.GetFlags(Handle);
            }
        }

        public BlobHandle Signature
        {
            get
            {
                return reader.PropertyTable.GetSignature(Handle);
            }
        }

        public ConstantHandle GetDefaultValue()
        {
            return reader.ConstantTable.FindConstant(Handle);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        public PropertyAccessors GetAccessors()
        {
            uint getter = 0;
            uint setter = 0;

            ushort methodCount;
            var firstRowId = reader.MethodSemanticsTable.FindSemanticMethodsForProperty(Handle, out methodCount);
            for (ushort i = 0; i < methodCount; i++)
            {
                uint rowId = firstRowId + i;
                switch (reader.MethodSemanticsTable.GetSemantics(rowId))
                {
                    case MethodSemanticsAttributes.Getter:
                        getter = reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Setter:
                        setter = reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;
                        // TODO: expose 'Other' collection on PropertyAccessors for completeness.
                }
            }

            return new PropertyAccessors(getter, setter);
        }
    }
}