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
        /// passed to <see name="Type.GetType(String)"/>. When unbound generics are involved they
        /// should follow the IL syntax of referencing a type or method generic variables using
        /// the syntax of <c>!N</c> or <c>!!N</c> respectively, where N is the zero-based index of the
        /// generic parameters. The generic rules defined for <see cref="UnsafeAccessorAttribute"/>
        /// apply to this attribute as well, meaning the arity and type of generic parameter must match
        /// the target type.
        ///
        /// This attribute only has behavior on parameters or return values of methods marked with <see cref="UnsafeAccessorAttribute"/>.
        ///
        /// This attribute should only be applied to parameters or return types of methods that are
        /// typed as follows:
        ///
        /// <ul>
        ///   <li>References should be typed as <lang>object</lang>.</li>
        ///   <li>Byref arguments should be typed with <lang>in</lang>, <lang>ref</lang>, or <lang>out</lang> to <lang>object</lang>.</li>
        ///   <li>Unmanaged pointers should be typed as <lang>void*</lang>.</li>
        ///   <li>Byref arguments to reference types or arrays should be typed with <lang>in</lang>, <lang>ref</lang>, or <lang>out</lang> to <lang>object</lang>.</li>
        ///   <li>Byref arguments to unmanaged pointer types should be typed with <lang>in</lang>, <lang>ref</lang>, or <lang>out</lang> to <lang>void*</lang>.</li>
        /// </ul>
        ///
        /// Value types are not supported.
        ///
        /// Due to lack of variance for byrefs, returns involving byrefs are not supported. This
        /// specifically means that accessors for fields of inaccessible types are not supported.
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
