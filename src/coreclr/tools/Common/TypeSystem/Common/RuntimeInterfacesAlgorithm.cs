// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Pluggable RuntimeInterfaces computation algorithm. Provides an abstraction to compute
    /// the list of interfaces effectively implemented by a type at runtime.
    /// The computed list is exposed as <see cref="TypeDesc.RuntimeInterfaces"/>.
    /// </summary>
    /// <remarks>
    /// The algorithms are expected to be directly used by <see cref="TypeSystemContext"/> derivatives
    /// only. The most obvious implementation of this algorithm that uses type's metadata to
    /// compute the answers is in MetadataRuntimeInterfacesAlgorithm.
    /// </remarks>
    public abstract class RuntimeInterfacesAlgorithm
    {
        /// <summary>
        /// Compute the RuntimeInterfaces for a TypeDesc, is permitted to depend on
        /// RuntimeInterfaces of base type, but must not depend on any other
        /// details of the base type.
        /// </summary>
        public abstract DefType[] ComputeRuntimeInterfaces(TypeDesc type);

        /// <summary>
        /// Determines whether <paramref name="type"/> implements a generic runtime interface with the same
        /// type definition as <paramref name="interfaceType"/>, ignoring the generic arguments.
        /// </summary>
        public virtual bool ImplementsGenericInterfaceDefinition(TypeDesc interfaceType, TypeDesc type)
        {
            Debug.Assert(interfaceType.IsInterface);

            if (!interfaceType.HasInstantiation)
            {
                return false;
            }

            foreach (DefType runtimeInterface in type.RuntimeInterfaces)
            {
                if (interfaceType.HasSameTypeDefinition(runtimeInterface))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
