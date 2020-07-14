// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Type that is logically equivalent to a type which is defined by a TypeDef
    /// record in an ECMA 335 metadata stream - a class, an interface, or a value type.
    /// </summary>
    public abstract partial class DefType : TypeDesc
    {
        /// <summary>
        /// Gets the namespace of the type.
        /// </summary>
        public virtual string Namespace => null;

        /// <summary>
        /// Gets the name of the type as represented in the metadata.
        /// </summary>
        public virtual string Name => null;

        /// <summary>
        /// Gets the containing type of this type or null if the type is not nested.
        /// </summary>
        public virtual DefType ContainingType => null;
    }
}
