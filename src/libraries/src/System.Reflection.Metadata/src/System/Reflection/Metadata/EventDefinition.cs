// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct EventDefinition
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal EventDefinition(MetadataReader reader, EventDefinitionHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private EventDefinitionHandle Handle
        {
            get { return EventDefinitionHandle.FromRowId(rowId); }
        }

        public StringHandle Name
        {
            get
            {
                return reader.EventTable.GetName(Handle);
            }
        }

        public EventAttributes Attributes
        {
            get
            {
                return reader.EventTable.GetFlags(Handle);
            }
        }

        public Handle Type
        {
            get
            {
                return reader.EventTable.GetEventType(Handle);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        public EventAccessors GetAccessors()
        {
            uint adder = 0;
            uint remover = 0;
            uint fire = 0;

            ushort methodCount;
            var firstRowId = reader.MethodSemanticsTable.FindSemanticMethodsForEvent(Handle, out methodCount);
            for (ushort i = 0; i < methodCount; i++)
            {
                uint rowId = firstRowId + i;
                switch (reader.MethodSemanticsTable.GetSemantics(rowId))
                {
                    case MethodSemanticsAttributes.Adder:
                        adder = reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Remover:
                        remover = reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;

                    case MethodSemanticsAttributes.Raiser:
                        fire = reader.MethodSemanticsTable.GetMethod(rowId).RowId;
                        break;
                        // TODO: expose 'Other' collection on EventAccessors for completeness.
                }
            }

            return new EventAccessors(adder, remover, fire);
        }
    }
}