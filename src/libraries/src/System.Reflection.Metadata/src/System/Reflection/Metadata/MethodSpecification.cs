// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct MethodSpecification
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal MethodSpecification(MetadataReader reader, MethodSpecificationHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private MethodSpecificationHandle Handle
        {
            get { return MethodSpecificationHandle.FromRowId(rowId); }
        }

        /// <summary>
        /// MethodDef or MemberRef handle specifying to which generic method this <see cref="MethodSpecification"/> refers,
        /// that is which generic method is it an instantiation of.
        /// </summary>
        public Handle Method
        {
            get
            {
                return reader.MethodSpecTable.GetMethod(Handle);
            }
        }

        /// <summary>
        /// Blob handle holding the signature of this instantiation.
        /// </summary>
        public BlobHandle Signature
        {
            get
            {
                return reader.MethodSpecTable.GetInstantiation(Handle);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}