// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

//
//
// All roles will implement this interface
//

namespace System.Security.Principal
{
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface IPrincipal {
        // Retrieve the identity object
        IIdentity Identity { get; }

        // Perform a check for a specific role
        bool IsInRole (string role);
    }
}
