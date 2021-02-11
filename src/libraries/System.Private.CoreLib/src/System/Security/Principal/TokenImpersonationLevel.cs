// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Principal
{
    public enum TokenImpersonationLevel
    {
        None = 0,
        Anonymous = 1,
        Identification = 2,
        Impersonation = 3,
        Delegation = 4
    }
}
