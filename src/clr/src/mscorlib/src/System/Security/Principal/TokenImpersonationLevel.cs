// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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