// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
