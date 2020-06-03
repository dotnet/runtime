// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Interface used to participate in a type cast failure.
    /// </summary>
    public interface IDynamicInterfaceCastable
    {
        /// <summary>
        /// Called when an implementing class instance is cast to an interface type that
        /// is not contained in the class's metadata.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="isinstTest">Indicates the function is being used to test if this object is an instance of the given interface</param>
        /// <returns>The type that should be used to dispatch for <paramref name="interfaceType"/> on the current object.</returns>
        /// <remarks>
        /// This is called if casting this object to the given interface type would
        /// otherwise fail. Casting here means the IL isinst and castclass instructions
        /// in the case where they are given an interface type as the target type. This
        /// function may also be called during interface dispatch.
        ///
        /// The returned type must be an interface type and have the <see cref="DynamicInterfaceCastableImplementationAttribute"/>.
        /// Otherwise <see cref="System.InvalidOperationException" />, will be thrown.
        ///
        /// When <paramref name="isinstTest" /> is set to true, this function should avoid
        /// throwing exceptions. A return return value of default(RuntimeTypeHandle) will
        /// indicate that the implementing class is not an instance of the given interface.
        /// If <paramref name="isinstTest" /> is false and default(RuntimeTypeHandle) is
        /// returned, then <see cref="System.InvalidCastException" /> will be thrown unless
        /// an exception is thrown by the implementation.
        /// </remarks>
        RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType, bool isinstTest);
    }

    /// <summary>
    /// Attribute required by any type that is returned by <see cref="IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle, bool)"/>.
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
