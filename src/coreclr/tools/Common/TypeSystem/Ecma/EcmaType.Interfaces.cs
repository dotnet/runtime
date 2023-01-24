// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem.Ecma
{
    // This file has implementations of the .Interfaces.cs logic from its base type.

    public sealed partial class EcmaType : MetadataType
    {
        private DefType[] _implementedInterfaces;

        public override DefType[] ExplicitlyImplementedInterfaces
        {
            get
            {
                if (_implementedInterfaces == null)
                    return InitializeImplementedInterfaces();
                return _implementedInterfaces;
            }
        }

        private DefType[] InitializeImplementedInterfaces()
        {
            var interfaceHandles = _typeDefinition.GetInterfaceImplementations();

            int count = interfaceHandles.Count;
            if (count == 0)
                return (_implementedInterfaces = Array.Empty<DefType>());

            DefType[] implementedInterfaces = new DefType[count];
            int i = 0;
            foreach (var interfaceHandle in interfaceHandles)
            {
                var interfaceImplementation = this.MetadataReader.GetInterfaceImplementation(interfaceHandle);
                DefType interfaceType = _module.GetType(interfaceImplementation.Interface) as DefType;
                if (interfaceType == null)
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, this);

                implementedInterfaces[i++] = interfaceType;
            }

            return (_implementedInterfaces = implementedInterfaces);
        }
    }
}
