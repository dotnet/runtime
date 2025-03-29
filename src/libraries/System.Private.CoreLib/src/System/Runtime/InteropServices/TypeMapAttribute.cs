// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Type mapping between a string and a type.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TypeMapAttribute<TTypeMapGroup> : Attribute
    {
        /// <summary>
        /// Create a mapping between a value and a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="value">String representation of key</param>
        /// <param name="target">Type value</param>
        /// <remarks>
        /// This mapping is unconditionally inserted into the type map.
        /// </remarks>
        public TypeMapAttribute(string value, Type target) { }

        /// <summary>
        /// Create a mapping between a value and a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="value">String representation of key</param>
        /// <param name="target">Type value</param>
        /// <param name="trimTarget">Type used by Trimmer to determine type map inclusion.</param>
        /// <remarks>
        /// This mapping is only included in the type map if trimming observes a type check
        /// using the <see cref="System.Type"/> represented by <paramref name="trimTarget"/>.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public TypeMapAttribute(string value, Type target, Type trimTarget) { }
    }
}
