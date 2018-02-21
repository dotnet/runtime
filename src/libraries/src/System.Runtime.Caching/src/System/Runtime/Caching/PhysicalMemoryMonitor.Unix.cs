// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Caching
{
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        protected override int GetCurrentPressure()
        {
            return 0;
        }
    }
}
