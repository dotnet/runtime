// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    [Flags]
    public enum PerformanceCounterPermissionAccess
    {
        Administer = 7,
        Browse = 1,
        Instrument = 3,
        None = 0,
        Read = 1,
        Write = 2,
    }
}
