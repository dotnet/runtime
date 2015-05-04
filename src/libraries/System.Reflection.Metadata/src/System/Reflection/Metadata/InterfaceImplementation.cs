// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct InterfaceImplementation
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal InterfaceImplementation(MetadataReader reader, InterfaceImplementationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private InterfaceImplementationHandle Handle
        {
            get { return InterfaceImplementationHandle.FromRowId(_rowId); }
        }


        /// <summary>
        /// The interface that is implemented
        /// <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/>
        /// </summary>
        public EntityHandle Interface
        {
            get { return _reader.InterfaceImplTable.GetInterface(_rowId); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}