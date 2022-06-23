// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// RuntimeInterfaces algorithm for array types which are similar to a generic type
    /// </summary>
    public sealed class ArrayOfTRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        /// <summary>
        /// Open type to instantiate to get the interfaces associated with an array.
        /// </summary>
        private MetadataType _arrayOfTType;

        /// <summary>
        /// RuntimeInterfaces algorithm for array types which are similar to a generic type
        /// </summary>
        /// <param name="arrayOfTType">Open type to instantiate to get the interfaces associated with an array.</param>
        public ArrayOfTRuntimeInterfacesAlgorithm(MetadataType arrayOfTType)
        {
            _arrayOfTType = arrayOfTType;
            Debug.Assert(!(arrayOfTType is InstantiatedType));
        }

        public override DefType[] ComputeRuntimeInterfaces(TypeDesc _type)
        {
            ArrayType arrayType = (ArrayType)_type;
            Debug.Assert(arrayType.IsSzArray);
            TypeDesc arrayOfTInstantiation = _arrayOfTType.MakeInstantiatedType(arrayType.ElementType);

            return arrayOfTInstantiation.RuntimeInterfaces;
        }
    }
}
