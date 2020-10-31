// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Interface used to participate in a type cast failure.
    /// </summary>
    /// <remarks>
    /// Implementation of this interface on a value type will be ignored. Only non-value types are allowed
    /// to participate in a type cast failure through this interface.
    /// </remarks>
    public interface IDynamicInterfaceCastable
    {
        /// <summary>
        /// Called when an implementing class instance is cast to an interface type that
        /// is not contained in the class's metadata.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="throwIfNotImplemented">Indicates if the function should throw an exception instead of returning false.</param>
        /// <returns>Whether or not this object can be cast to the given interface</returns>
        /// <remarks>
        /// This is called if casting this object to the given interface type would
        /// otherwise fail. Casting here means the IL isinst and castclass instructions
        /// in the case where they are given an interface type as the target type.
        ///
        /// If <paramref name="throwIfNotImplemented" /> is false, this function should
        /// avoid throwing exceptions. If <paramref name="throwIfNotImplemented" /> is
        /// true and this function returns false, then <see cref="System.InvalidCastException" />
        /// will be thrown unless an exception is thrown by the implementation.
        /// </remarks>
        bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented);

        /// <summary>
        /// Called during interface dispatch when the given interface type cannot be found
        /// in the class's metadata.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns>The type that should be used to dispatch for <paramref name="interfaceType"/> on the current object.</returns>
        /// <remarks>
        /// When this function is called, the cast of this object to the given interface
        /// should already have been verified through the castclass/isinst instructions.
        ///
        /// The returned type must be an interface type and be marked with the
        /// <see cref="DynamicInterfaceCastableImplementationAttribute"/>. Otherwise,
        /// <see cref="System.InvalidOperationException" /> will be thrown.
        /// </remarks>
        RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType);
    }

    /// <summary>
    /// Attribute required by any type that is returned by <see cref="IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle)"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used to enforce policy in the runtime and make
    /// <see cref="IDynamicInterfaceCastable" /> scenarios linker friendly.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class DynamicInterfaceCastableImplementationAttribute : Attribute
    {
        public DynamicInterfaceCastableImplementationAttribute()
        {
        }
    }
}
