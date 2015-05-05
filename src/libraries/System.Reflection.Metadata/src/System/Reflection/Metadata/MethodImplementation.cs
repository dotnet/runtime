// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct MethodImplementation
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal MethodImplementation(MetadataReader reader, MethodImplementationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private MethodImplementationHandle Handle
        {
            get { return MethodImplementationHandle.FromRowId(_rowId); }
        }

        public TypeDefinitionHandle Type
        {
            get
            {
                return _reader.MethodImplTable.GetClass(Handle);
            }
        }

        public EntityHandle MethodBody
        {
            get
            {
                return _reader.MethodImplTable.GetMethodBody(Handle);
            }
        }

        public EntityHandle MethodDeclaration
        {
            get
            {
                return _reader.MethodImplTable.GetMethodDeclaration(Handle);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}