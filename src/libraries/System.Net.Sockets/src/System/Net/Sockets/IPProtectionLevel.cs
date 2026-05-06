// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    public enum IPProtectionLevel
    {
        Unspecified = -1,
        Unrestricted = 10,
        EdgeRestricted = 20,
        Restricted = 30,
    }
}
