// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct GenericParameterConstraint
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal GenericParameterConstraint(MetadataReader reader, GenericParameterConstraintHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private GenericParameterConstraintHandle Handle
        {
            get { return GenericParameterConstraintHandle.FromRowId(rowId); }
        }

        /// <summary>
        /// The constrained <see cref="GenericParameterHandle"/>.
        /// </summary>
        /// <remarks>
        /// Corresponds to Owner field of GenericParamConstraint table in ECMA-335 Standard.
        /// </remarks>
        public GenericParameterHandle Parameter
        {
            get { return reader.GenericParamConstraintTable.GetOwner(Handle); }
        }

        /// <summary>
        /// Handle (<see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/>) 
        /// specifying from which type this generic parameter is constrained to derive,
        /// or which interface this generic parameter is constrained to implement.
        /// </summary>
        /// <remarks>
        /// Corresponds to Constraint field of GenericParamConstraint table in ECMA-335 Standard.
        /// </remarks>
        public Handle Type
        {
            get { return reader.GenericParamConstraintTable.GetConstraint(Handle); }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}