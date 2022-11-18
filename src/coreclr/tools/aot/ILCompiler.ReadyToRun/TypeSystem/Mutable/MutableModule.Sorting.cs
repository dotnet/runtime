// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    partial class MutableModule
    {
        // This isn't deterministic, but it is functional. At this time, since only 1 MutableModule is contained in a build, it will be deterministic as it will always return 0.
        static int s_globalIndex = 0;

        int _index = Interlocked.Increment(ref s_globalIndex);
        public int CompareTo(MutableModule other)
        {
            return _index.CompareTo(_index);
        }
    }
}
