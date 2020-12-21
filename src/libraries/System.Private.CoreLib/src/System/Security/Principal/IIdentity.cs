// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// All identities will implement this interface
//

namespace System.Security.Principal
{
    public interface IIdentity
    {
        // Access to the name string
        string? Name { get; }

        // Access to Authentication 'type' info
        string? AuthenticationType { get; }

        // Determine if this represents the unauthenticated identity
        bool IsAuthenticated { get; }
    }
}
