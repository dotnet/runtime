// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Implements method canonicalization
    partial class MethodDesc
    {
        public virtual bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            return false;
        }

        /// <summary>
        /// Returns the result of canonicalizing this method over the given kind of Canon
        /// </summary>
        public virtual MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return this;
        }
    }
}
