// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.IL;
using Internal.TypeSystem;

namespace ILVerify
{
    internal class SimpleArrayOfTRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        private DefType[] _arrayRuntimeInterfaces;
        private MetadataType[] _genericRuntimeInterfaces;
        private ModuleDesc _systemModule;

        private static readonly string[] s_genericRuntimeInterfacesNames = 
        {
            "IEnumerable`1",
            "ICollection`1",
            "IList`1",
            "IReadOnlyList`1",
            "IReadOnlyCollection`1",
        };

        public SimpleArrayOfTRuntimeInterfacesAlgorithm(ModuleDesc systemModule)
        {
            _systemModule = systemModule;

            // initialize interfaces
            _arrayRuntimeInterfaces = _systemModule.GetType("System", "Array")?.RuntimeInterfaces 
                ?? Array.Empty<DefType>();

            _genericRuntimeInterfaces = new MetadataType[s_genericRuntimeInterfacesNames.Length];
            int count = 0;
            for (int i = 0; i < s_genericRuntimeInterfacesNames.Length; ++i)
            {
                MetadataType runtimeInterface =_systemModule.GetType("System.Collections.Generic", s_genericRuntimeInterfacesNames[i], throwIfNotFound: false);
                if (runtimeInterface != null)
                    _genericRuntimeInterfaces[count++] = runtimeInterface;
            };
            Array.Resize(ref _genericRuntimeInterfaces, count);
        }

        public override DefType[] ComputeRuntimeInterfaces(TypeDesc type)
        {
            ArrayType arrayType = (ArrayType)type;
            TypeDesc elementType = arrayType.ElementType;
            Debug.Assert(arrayType.IsSzArray);

            // first copy runtime interfaces from System.Array
            var result = new DefType[_arrayRuntimeInterfaces.Length + _genericRuntimeInterfaces.Length];
            Array.Copy(_arrayRuntimeInterfaces, result, _arrayRuntimeInterfaces.Length);

            // then copy instantiated generic interfaces
            int offset = _arrayRuntimeInterfaces.Length;
            for (int i = 0; i < _genericRuntimeInterfaces.Length; ++i)
                result[i + offset] = _genericRuntimeInterfaces[i].MakeInstantiatedType(elementType);

            return result;
        }
    }
}
