// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
//  All Identities will implement this interface.
//

namespace System.Security.Policy {
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IIdentityPermissionFactory
    {
        IPermission CreateIdentityPermission( Evidence evidence );
    }

}
