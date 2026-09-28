// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    partial class MutableModule
    {
        // There is only ever a single MutableModule in a build, so any comparison is against itself.
        public int CompareTo(MutableModule other)
        {
            Debug.Assert(this == other);
            return 0;
        }
    }
}
