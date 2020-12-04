// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.Protocols
{
    [Flags]
    public enum ReferralChasingOptions
    {
        None = 0,
        Subordinate = 0x20,
        External = 0x40,
        All = Subordinate | External
    }
}
