// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Interface used to participate in a type cast failure.
    /// </summary>
    public interface ICastableObject
    {
        /// <summary>
        /// Called when an implementing class instance is cast to an interface type that
        /// is not contained in the class's metadata.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="throwIfNotFound">Indicates if the function should throw an exception rather than default(RuntimeTypeHandle).</param>
        /// <returns>The type that should be used to dispatch for <paramref name="interfaceType"/> on the current object.</returns>
        /// <remarks>
        /// This is called if casting this object to the given interface type would
        /// otherwise fail. Casting here means the IL isinst and castclass instructions
        /// in the case where they are given an interface type as the target type. This
        /// function may also be called during interface dispatch.
        ///
        /// The returned type must be an interface type, otherwise <see cref="System.InvalidProgramException" />
        /// will be thrown. When the <paramref name="throwIfNotFound" /> is set to false,
        /// a return value of default(RuntimeTypeHandle) is permitted. If <paramref name="throwIfNotFound" />
        /// is true and default(RuntimeTypeHandle) is returned then <see cref="System.InvalidCastException" />
        /// will be thrown unless an exception is thrown by the implementation.
        /// </remarks>
        RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType, bool throwIfNotFound);
    }

    /// <summary>
    /// Attribute required by any type that is returned by <see cref="ICastableObject.GetInterfaceImplementation(RuntimeTypeHandle, bool)"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used to enforce policy in the runtime and make
    /// <see cref="ICastableObject" /> scenarios linker friendly.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class CastableObjectImplementationAttribute : Attribute
    {
        public CastableObjectImplementationAttribute()
        {
        }
    }
}
