// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct GenericParameter
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal GenericParameter(MetadataReader reader, GenericParameterHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private GenericParameterHandle Handle
        {
            get { return GenericParameterHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// <see cref="TypeDefinitionHandle"/> or <see cref="MethodDefinitionHandle"/>.
        /// </summary>
        /// <remarks>
        /// Corresponds to Owner field of GenericParam table in ECMA-335 Standard.
        /// </remarks>
        public EntityHandle Parent
        {
            get
            {
                return _reader.GenericParamTable.GetOwner(Handle);
            }
        }

        /// <summary>
        /// Attributes specifying variance and constraints.
        /// </summary>
        /// <remarks>
        /// Corresponds to Flags field of GenericParam table in ECMA-335 Standard.
        /// </remarks>
        public GenericParameterAttributes Attributes
        {
            get
            {
                return _reader.GenericParamTable.GetFlags(Handle);
            }
        }

        /// <summary>
        /// Zero-based index of the parameter within the declaring generic type or method declaration.
        /// </summary>
        /// <remarks>
        /// Corresponds to Number field of GenericParam table in ECMA-335 Standard.
        /// </remarks>
        public int Index
        {
            get
            {
                return _reader.GenericParamTable.GetNumber(Handle);
            }
        }

        /// <summary>
        /// The name of the generic parameter.
        /// </summary>
        /// <remarks>
        /// Corresponds to Name field of GenericParam table in ECMA-335 Standard.
        /// </remarks>
        public StringHandle Name
        {
            get
            {
                return _reader.GenericParamTable.GetName(Handle);
            }
        }

        public GenericParameterConstraintHandleCollection GetConstraints()
        {
            return _reader.GenericParamConstraintTable.FindConstraintsForGenericParam(Handle);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
