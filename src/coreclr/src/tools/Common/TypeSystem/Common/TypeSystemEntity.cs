// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public abstract class TypeSystemEntity
    {
        /// <summary>
        /// Gets the type system context this entity belongs to.
        /// </summary>
        public abstract TypeSystemContext Context { get; }
    }
}
