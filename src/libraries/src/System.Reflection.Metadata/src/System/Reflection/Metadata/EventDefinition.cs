// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct EventDefinition
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal EventDefinition(MetadataReader reader, EventDefinitionHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private EventDefinitionHandle Handle
        {
            get { return EventDefinitionHandle.FromRowId(_rowId); }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.EventTable.GetName(Handle);
            }
        }

        public EventAttributes Attributes
        {
            get
            {
                return _reader.EventTable.GetFlags(Handle);
            }
        }

        public EntityHandle Type
        {
            get
            {
                return _reader.EventTable.GetEventType(Handle);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }

        public EventAccessors GetAccessors()
        {
            int adder = 0;
            int remover = 0;
            int fire = 0;

            ushort methodCount;
            int firstRowId = _reader.MethodSemanticsTable.FindSemanticMethodsForEvent(Handle, out methodCount);
            for (ushort i = 0; i < methodCount; i++)
            {
                int rowId = firstRowId + i;
                switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
                {
                    case MethodSemanticsAttributes.Adder:
                        adder = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Remover:
                        remover = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Raiser:
                        fire = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;
                        // TODO: expose 'Other' collection on EventAccessors for completeness.
                }
            }

            return new EventAccessors(adder, remover, fire);
        }
    }
}