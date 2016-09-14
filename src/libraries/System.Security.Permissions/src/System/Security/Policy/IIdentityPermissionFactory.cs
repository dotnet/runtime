// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public partial interface IIdentityPermissionFactory
    {
        System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence);
    }
}
