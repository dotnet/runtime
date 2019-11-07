// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    public enum ReplicationSecurityLevel : int
    {
        MutualAuthentication = 2,
        Negotiate = 1,
        NegotiatePassThrough = 0
    }
}
