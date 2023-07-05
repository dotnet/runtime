// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext
    {
        /// <summary>
        /// Abstraction to allow the type system context to affect the field layout
        /// algorithm used by types to lay themselves out.
        /// </summary>
        public virtual FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            // Type system contexts that support computing field layout need to override this.
            throw new NotSupportedException();
        }
    }
}
