// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Runtime.Caching
{
    internal sealed class PerfCounters : IDisposable
    {
        internal PerfCounters(string cacheName)
        {
        }
        public void Dispose()
        {
        }
        internal void Increment(PerfCounterName name)
        {
        }
        internal void IncrementBy(PerfCounterName name, long value)
        {
        }
        internal void Decrement(PerfCounterName name)
        {
        }
    }
}
