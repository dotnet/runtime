// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct InterfaceImplementation
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal InterfaceImplementation(MetadataReader reader, InterfaceImplementationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private InterfaceImplementationHandle Handle
        {
            get { return InterfaceImplementationHandle.FromRowId(rowId); }
        }


        /// <summary>
        /// The interface that is implemented
        /// <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/>
        /// </summary>
        public Handle Interface
        {
            get { return reader.InterfaceImplTable.GetInterface(rowId); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}