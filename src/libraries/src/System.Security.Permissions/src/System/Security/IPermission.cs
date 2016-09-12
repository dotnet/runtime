// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial interface IPermission : System.Security.ISecurityEncodable
    {
        System.Security.IPermission Copy();
        void Demand();
        System.Security.IPermission Intersect(System.Security.IPermission target);
        bool IsSubsetOf(System.Security.IPermission target);
        System.Security.IPermission Union(System.Security.IPermission target);
    }
}
