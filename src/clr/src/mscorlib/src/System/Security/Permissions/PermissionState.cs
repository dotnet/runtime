// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
