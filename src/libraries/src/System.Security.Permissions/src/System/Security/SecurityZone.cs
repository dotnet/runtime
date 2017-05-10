// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public enum SecurityZone
    {
        Internet = 3,
        Intranet = 1,
        MyComputer = 0,
        NoZone = -1,
        Trusted = 2,
        Untrusted = 4,
    }
}
