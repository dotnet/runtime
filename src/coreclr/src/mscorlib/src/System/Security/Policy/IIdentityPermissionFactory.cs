// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
