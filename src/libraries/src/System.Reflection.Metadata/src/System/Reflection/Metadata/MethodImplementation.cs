// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct MethodImplementation
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal MethodImplementation(MetadataReader reader, MethodImplementationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private MethodImplementationHandle Handle
        {
            get { return MethodImplementationHandle.FromRowId(rowId); }
        }

        public TypeDefinitionHandle Type
        {
            get
            {
                return reader.MethodImplTable.GetClass(Handle);
            }
        }

        public Handle MethodBody
        {
            get
            {
                return reader.MethodImplTable.GetMethodBody(Handle);
            }
        }

        public Handle MethodDeclaration
        {
            get
            {
                return reader.MethodImplTable.GetMethodDeclaration(Handle);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}