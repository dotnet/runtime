// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides access to an inaccessible type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class UnsafeAccessorTypeAttribute : Attribute
    {
        /// <summary>
        /// Instantiates an <see cref="UnsafeAccessorTypeAttribute"/> providing access to a type supplied by <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">A fully qualified or partially qualified type name.</param>
        /// <remarks>
        /// <paramref name="typeName"/> is expected to follow the same rules as if it were being
        /// passed to <see name="Type.GetType(String)"/>.
        ///
        /// This attribute only has behavior on parameters or return values of methods marked with <see cref="UnsafeAccessorAttribute"/>.
        ///
        /// This attribute should only be applied to parameters or return types of methods that are
        /// typed as <see langword="object"/>. Modifiers such as <see langword="ref"/>, <see langword="in"/>,
        /// <see langword="out"/>, and <see langword="readonly"/> are supported.
        ///
        /// Only reference types are supported to be looked up by this attribute.
        /// Value types are not supported.
        /// </remarks>
        public UnsafeAccessorTypeAttribute(string typeName)
        {
            TypeName = typeName;
        }

        /// <summary>
        /// Fully qualified or partially qualified type name to target.
        /// </summary>
        public string TypeName { get; }
    }
}
