// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Functionality related to determinstic ordering of types
    partial class CanonBaseType
    {
        protected internal sealed override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            // These should be singletons
            Debug.Assert(this == other);
            return 0;
        }
    }

    partial class CanonType
    {
        protected internal override int ClassCode => 46114331;
    }

    partial class UniversalCanonType
    {
        protected internal override int ClassCode => 1687626054;
    }
}
