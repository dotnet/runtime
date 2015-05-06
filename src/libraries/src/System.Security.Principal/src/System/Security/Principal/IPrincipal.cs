// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// All roles will implement this interface
//

using System;

namespace System.Security.Principal
{
    public interface IPrincipal
    {
        // Retrieve the identity object
        IIdentity Identity { get; }

        // Perform a check for a specific role
        bool IsInRole(string role);
    }
}
