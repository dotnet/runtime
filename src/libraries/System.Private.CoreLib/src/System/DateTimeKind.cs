// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // This enum is used to indentify DateTime instances in cases when they are known to be in local time,
    // UTC time or if this information has not been specified or is not applicable.

    public enum DateTimeKind
    {
        Unspecified = 0,
        Utc = 1,
        Local = 2,
    }
}
