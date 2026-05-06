// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Create a type association between a type and its proxy.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TypeMapAssociationAttribute<TTypeMapGroup> : Attribute
    {
        /// <summary>
        /// Create an association between two types in the type map.
        /// </summary>
        /// <param name="source">Target type.</param>
        /// <param name="proxy">Type to associated with <paramref name="source"/>.</param>
        /// <remarks>
        /// This mapping will only exist in the type map if trimming observes
        /// an allocation using the <see cref="System.Type"/> represented by <paramref name="source"/>.
        /// </remarks>
        public TypeMapAssociationAttribute(Type source, Type proxy) { }
    }
}
