// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Pluggable virtual method computation algorithm. Provides an abstraction to resolve
    /// virtual and interface methods on types.
    /// </summary>
    /// <remarks>
    /// The algorithms are expected to be directly used by <see cref="TypeSystemContext"/> derivatives
    /// only. The most obvious implementation of this algorithm that uses type's metadata to
    /// compute the answers is in MetadataVirtualMethodAlgorithm.
    /// </remarks>
    public abstract class VirtualMethodAlgorithm
    {
        /// <summary>
        /// Resolves interface method '<paramref name="interfaceMethod"/>' to a method on '<paramref name="currentType"/>'
        /// that implements the method.
        /// </summary>
        public abstract MethodDesc ResolveInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType);

        public abstract MethodDesc ResolveVariantInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType);

        public abstract MethodDesc ResolveInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType);

        public abstract MethodDesc ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType);

        public abstract DefaultInterfaceMethodResolution ResolveInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl);

        public abstract DefaultInterfaceMethodResolution ResolveVariantInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl);

        /// <summary>
        /// Resolves a virtual method call.
        /// </summary>
        public abstract MethodDesc FindVirtualFunctionTargetMethodOnObjectType(MethodDesc targetMethod, TypeDesc objectType);

        /// <summary>
        /// Enumerates all virtual slots on '<paramref name="type"/>'.
        /// </summary>
        public abstract IEnumerable<MethodDesc> ComputeAllVirtualSlots(TypeDesc type);
    }

    public enum DefaultInterfaceMethodResolution
    {
        /// <summary>
        /// No default implementation was found.
        /// </summary>
        None,

        /// <summary>
        /// A default implementation was found.
        /// </summary>
        DefaultImplementation,

        /// <summary>
        /// The implementation was reabstracted.
        /// </summary>
        Reabstraction,

        /// <summary>
        /// The default implementation conflicts.
        /// </summary>
        Diamond,
    }
}
