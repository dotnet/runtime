// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Declare a type that holds type map entries and should be inspected during type map building.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
    /// <remarks>
    /// The entries (<see cref="TypeMapAttribute{TTypeMapGroup}"/> and <see cref="TypeMapAssociationAttribute{TTypeMapGroup}"/>)
    /// declared on the target type are part of the type map of <typeparamref name="TTypeMapGroup"/>, as if they were declared on the assembly.
    /// This attribute can also be applied to the target type, to chain other types that hold entries.
    /// The target type is only a container for attributes: it is not constructed and not exposed by the type map.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TypeMapTypeTargetAttribute<TTypeMapGroup> : Attribute
    {
        /// <summary>
        /// Provide the type that holds type map entries.
        /// </summary>
        /// <param name="type">Type to look for type mapping attributes.</param>
        public TypeMapTypeTargetAttribute(Type type) { }
    }
}
