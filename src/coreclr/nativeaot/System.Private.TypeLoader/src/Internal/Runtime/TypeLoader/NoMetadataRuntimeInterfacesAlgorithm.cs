// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using Internal.Runtime.Augments;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Gets interface information from the RuntimeTypeHandle for a type with no metadata
    /// </summary>
    internal class NoMetadataRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        public override DefType[] ComputeRuntimeInterfaces(TypeDesc type)
        {
            int numInterfaces = RuntimeAugments.GetInterfaceCount(type.RuntimeTypeHandle);
            DefType[] interfaces = new DefType[numInterfaces];
            for (int i = 0; i < numInterfaces; i++)
            {
                RuntimeTypeHandle itfHandle = RuntimeAugments.GetInterface(type.RuntimeTypeHandle, i);
                TypeDesc itfType = type.Context.ResolveRuntimeTypeHandle(itfHandle);
                interfaces[i] = (DefType)itfType;
            }
            return interfaces;
        }
    }
}
