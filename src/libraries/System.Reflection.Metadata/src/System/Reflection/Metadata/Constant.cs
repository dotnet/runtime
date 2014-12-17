// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct Constant
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal Constant(MetadataReader reader, uint rowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(rowId != 0);

            this.reader = reader;
            this.rowId = rowId;
        }

        private ConstantHandle Handle
        {
            get
            {
                return ConstantHandle.FromRowId(rowId);
            }
        }

        /// <summary>
        /// The type of the constant value.
        /// </summary>
        /// <remarks>
        /// Corresponds to Type field of Constant table in ECMA-335 Standard.
        /// </remarks>
        public ConstantTypeCode TypeCode
        {
            get
            {
                return reader.ConstantTable.GetType(Handle);
            }
        }

        /// <summary>
        /// The constant value.
        /// </summary>
        /// <remarks>
        /// Corresponds to Value field of Constant table in ECMA-335 Standard.
        /// </remarks>
        public BlobHandle Value
        {
            get
            {
                return reader.ConstantTable.GetValue(Handle);
            }
        }

        /// <summary>
        /// The parent handle (<see cref="ParameterHandle"/>, <see cref="FieldDefinitionHandle"/>, or <see cref="PropertyDefinitionHandle"/>).
        /// </summary>
        /// <remarks>
        /// Corresponds to Parent field of Constant table in ECMA-335 Standard.
        /// </remarks>
        public Handle Parent
        {
            get
            {
                return reader.ConstantTable.GetParent(Handle);
            }
        }
    }
}