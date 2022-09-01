// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Metadata based RuntimeInterfaces algorithm which can be used to compute the
    /// RuntimeInterfaces for any MetadataType based on the base types RuntimeInterfaces
    /// and the MetadataTypes explicit interfaces.
    /// </summary>
    public sealed class MetadataRuntimeInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        public override DefType[] ComputeRuntimeInterfaces(TypeDesc _type)
        {
            MetadataType type = (MetadataType)_type;

            // TODO: need to implement deduplication
            // https://github.com/dotnet/corert/issues/208

            if (type is InstantiatedType)
            {
                return ComputeRuntimeInterfacesForInstantiatedType((InstantiatedType)type);
            }
            else
            {
                return ComputeRuntimeInterfacesForNonInstantiatedMetadataType(type);
            }
        }

        /// <summary>
        /// Instantiated type computation for runtime interfaces. Instantiated types
        /// must have the same count of interfaces across all possible instantiations
        /// so the algorithm works by computing the uninstantiated form, and then
        /// specializing each interface as needed.
        /// </summary>
        private static DefType[] ComputeRuntimeInterfacesForInstantiatedType(InstantiatedType instantiatedType)
        {
            MetadataType uninstantiatedType = (MetadataType)instantiatedType.GetTypeDefinition();

            return InstantiatedType.InstantiateTypeArray(uninstantiatedType.RuntimeInterfaces, instantiatedType.Instantiation, default(Instantiation));
        }

        /// <summary>
        /// Metadata based computation of interfaces.
        /// </summary>
        private DefType[] ComputeRuntimeInterfacesForNonInstantiatedMetadataType(MetadataType type)
        {
            DefType[] explicitInterfaces = type.ExplicitlyImplementedInterfaces;
            DefType[] baseTypeInterfaces = (type.BaseType != null) ? (type.BaseType.RuntimeInterfaces) : Array.Empty<DefType>();

            // Optimized case for no interfaces newly defined.
            if (explicitInterfaces.Length == 0)
                return baseTypeInterfaces;

            ArrayBuilder<DefType> interfacesArray = default(ArrayBuilder<DefType>);
            interfacesArray.Append(baseTypeInterfaces);

            foreach (DefType iface in explicitInterfaces)
            {
                BuildPostOrderInterfaceList(iface, ref interfacesArray);
            }

            return interfacesArray.ToArray();
        }

        /// <summary>
        /// Add an interface and its required interfaces to the interfacesArray
        /// </summary>
        private void BuildPostOrderInterfaceList(DefType iface, ref ArrayBuilder<DefType> interfacesArray)
        {
            if (interfacesArray.Contains(iface))
                return;

            foreach (DefType implementedInterface in iface.RuntimeInterfaces)
            {
                BuildPostOrderInterfaceList(implementedInterface, ref interfacesArray);
            }

            if (interfacesArray.Contains(iface))
                return;

            interfacesArray.Add(iface);
        }
    }
}
