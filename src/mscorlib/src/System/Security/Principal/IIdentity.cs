// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

//
//
// All identities will implement this interface
//

namespace System.Security.Principal
{
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface IIdentity {
        // Access to the name string
        string Name { get; }

        // Access to Authentication 'type' info
        string AuthenticationType { get; }

        // Determine if this represents the unauthenticated identity
        bool IsAuthenticated { get; }
    }
}
