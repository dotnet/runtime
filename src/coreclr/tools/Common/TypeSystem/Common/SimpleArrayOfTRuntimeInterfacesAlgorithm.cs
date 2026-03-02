// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public class SimpleArrayOfTRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        private DefType[] _arrayRuntimeInterfaces;
        private MetadataType[] _genericRuntimeInterfaces;
        private ModuleDesc _systemModule;

        private static readonly ReadOnlyMemory<byte>[] s_genericRuntimeInterfacesNames =
        {
            "IEnumerable`1"u8.ToArray(),
            "ICollection`1"u8.ToArray(),
            "IList`1"u8.ToArray(),
            "IReadOnlyList`1"u8.ToArray(),
            "IReadOnlyCollection`1"u8.ToArray(),
        };

        public SimpleArrayOfTRuntimeInterfacesAlgorithm(ModuleDesc systemModule)
        {
            _systemModule = systemModule;

            // initialize interfaces
            _arrayRuntimeInterfaces = _systemModule.GetType("System"u8, "Array"u8)?.RuntimeInterfaces
                ?? Array.Empty<DefType>();

            _genericRuntimeInterfaces = new MetadataType[s_genericRuntimeInterfacesNames.Length];
            int count = 0;
            for (int i = 0; i < s_genericRuntimeInterfacesNames.Length; ++i)
            {
                MetadataType runtimeInterface =_systemModule.GetType("System.Collections.Generic"u8, s_genericRuntimeInterfacesNames[i].Span, throwIfNotFound: false);
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
