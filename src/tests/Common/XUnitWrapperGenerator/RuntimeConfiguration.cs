// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Xunit
{
    [Flags]
    public enum RuntimeConfiguration
    {
        Any = ~0,
        Checked = 1,
        Debug = 1 << 1,
        Release = 1 << 2
    }
}
