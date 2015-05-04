// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct Constant
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal Constant(MetadataReader reader, int rowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(rowId != 0);

            _reader = reader;
            _rowId = rowId;
        }

        private ConstantHandle Handle
        {
            get
            {
                return ConstantHandle.FromRowId(_rowId);
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
                return _reader.ConstantTable.GetType(Handle);
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
                return _reader.ConstantTable.GetValue(Handle);
            }
        }

        /// <summary>
        /// The parent handle (<see cref="ParameterHandle"/>, <see cref="FieldDefinitionHandle"/>, or <see cref="PropertyDefinitionHandle"/>).
        /// </summary>
        /// <remarks>
        /// Corresponds to Parent field of Constant table in ECMA-335 Standard.
        /// </remarks>
        public EntityHandle Parent
        {
            get
            {
                return _reader.ConstantTable.GetParent(Handle);
            }
        }
    }
}