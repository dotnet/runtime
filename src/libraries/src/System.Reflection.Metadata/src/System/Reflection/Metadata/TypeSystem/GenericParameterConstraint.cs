// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct GenericParameterConstraint
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal GenericParameterConstraint(MetadataReader reader, GenericParameterConstraintHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private GenericParameterConstraintHandle Handle
        {
            get { return GenericParameterConstraintHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// The constrained <see cref="GenericParameterHandle"/>.
        /// </summary>
        /// <remarks>
        /// Corresponds to Owner field of GenericParamConstraint table in ECMA-335 Standard.
        /// </remarks>
        public GenericParameterHandle Parameter
        {
            get { return _reader.GenericParamConstraintTable.GetOwner(Handle); }
        }

        /// <summary>
        /// Handle (<see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/>) 
        /// specifying from which type this generic parameter is constrained to derive,
        /// or which interface this generic parameter is constrained to implement.
        /// </summary>
        /// <remarks>
        /// Corresponds to Constraint field of GenericParamConstraint table in ECMA-335 Standard.
        /// </remarks>
        public EntityHandle Type
        {
            get { return _reader.GenericParamConstraintTable.GetConstraint(Handle); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }
    }
}
