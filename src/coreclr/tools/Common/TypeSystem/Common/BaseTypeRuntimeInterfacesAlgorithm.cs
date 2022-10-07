// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// RuntimeInterfaces algorithm for types known to have no explicitly defined interfaces
    /// but which do have a base type. (For instance multidimensional arrays)
    /// </summary>
    public sealed class BaseTypeRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        private static RuntimeInterfacesAlgorithm _singleton = new BaseTypeRuntimeInterfacesAlgorithm();

        private BaseTypeRuntimeInterfacesAlgorithm() { }

        public static RuntimeInterfacesAlgorithm Instance
        {
            get
            {
                return _singleton;
            }
        }

        public override DefType[] ComputeRuntimeInterfaces(TypeDesc _type)
        {
            return _type.BaseType.RuntimeInterfaces;
        }
    }
}
