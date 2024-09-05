// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    [Flags]
    public enum MachSegmentFlags : uint
    {
        HighVirtualMemory = 1,
        NoRelocations = 4,
    }
}
