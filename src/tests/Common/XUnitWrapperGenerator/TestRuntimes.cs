// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Xunit
{
    [Flags]
    public enum TestRuntimes
    {
        CoreCLR = 1,
        Mono = 2,
        Any = ~0
    }
}
