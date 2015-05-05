// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// All identities will implement this interface
//

using System;

namespace System.Security.Principal
{
    public interface IIdentity
    {
        // Access to the name string
        string Name { get; }

        // Access to Authentication 'type' info
        string AuthenticationType { get; }

        // Determine if this represents the unauthenticated identity
        bool IsAuthenticated { get; }
    }
}
