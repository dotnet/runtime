// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct GenericParameter
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint rowId;

        internal GenericParameter(MetadataReader reader, GenericParameterHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            this.reader = reader;
            this.rowId = handle.RowId;
        }

        private GenericParameterHandle Handle
        {
            get { return GenericParameterHandle.FromRowId(rowId); }
        }

        /// <summary>
        /// <see cref="TypeDefinitionHandle"/> or <see cref="MethodDefinitionHandle"/>.
        /// </summary>
        /// <remarks>
        /// Corresponds to Owner field of GenericParam table in ECMA-335 Standard.
        /// </remarks>
        public Handle Parent
        {
            get
            {
                return reader.GenericParamTable.GetOwner(Handle);
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
                return reader.GenericParamTable.GetFlags(Handle);
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
                return reader.GenericParamTable.GetNumber(Handle);
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
                return reader.GenericParamTable.GetName(Handle);
            }
        }

        public GenericParameterConstraintHandleCollection GetConstraints()
        {
            return reader.GenericParamConstraintTable.FindConstraintsForGenericParam(Handle);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}