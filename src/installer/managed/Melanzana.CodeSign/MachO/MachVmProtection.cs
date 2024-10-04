// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Melanzana.MachO
{
    [Flags]
    public enum MachVmProtection : uint
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Execute = 0x4,
    }
}
