// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// All roles will implement this interface
//

namespace System.Security.Principal
{
    public interface IPrincipal
    {
        // Retrieve the identity object
        IIdentity? Identity { get; }

        // Perform a check for a specific role
        bool IsInRole(string role);
    }
}
