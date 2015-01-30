// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
//  The Runtime policy manager.  Maintains a set of IdentityMapper objects that map 
//  inbound evidence to groups.  Resolves an identity into a set of permissions
//

namespace System.Security.Permissions {
    
    using System;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum PermissionState
    {
        Unrestricted = 1,
        None = 0,
    } 
    
}
