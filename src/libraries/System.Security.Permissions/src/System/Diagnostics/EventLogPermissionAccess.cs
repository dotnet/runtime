// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    [Flags]
    public enum EventLogPermissionAccess
    {
        Administer = 48,
        Audit = 10,
        Browse = 2,
        Instrument = 6,
        None = 0,
        Write = 16,
    }
}
