// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    [Flags]
    public enum HostSecurityManagerOptions
    {
        AllFlags = 31,
        HostAppDomainEvidence = 1,
        HostAssemblyEvidence = 4,
        HostDetermineApplicationTrust = 8,
        HostPolicyLevel = 2,
        HostResolvePolicy = 16,
        None = 0,
    }
}
