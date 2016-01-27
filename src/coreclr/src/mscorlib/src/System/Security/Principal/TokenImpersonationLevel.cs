// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security.Principal
{
#if !FEATURE_CORECLR
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
#endif
    public enum TokenImpersonationLevel {
        None            = 0,
        Anonymous       = 1,
        Identification  = 2,
        Impersonation   = 3,
        Delegation      = 4
    }
}
