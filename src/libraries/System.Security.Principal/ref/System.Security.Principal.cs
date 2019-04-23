// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Principal
{
    public partial interface IIdentity
    {
        string AuthenticationType { get; }
        bool IsAuthenticated { get; }
        string Name { get; }
    }
    public partial interface IPrincipal
    {
        System.Security.Principal.IIdentity Identity { get; }
        bool IsInRole(string role);
    }
    public enum PrincipalPolicy
    {
        UnauthenticatedPrincipal = 0,
        NoPrincipal = 1,
        WindowsPrincipal = 2,
    }
    public enum TokenImpersonationLevel
    {
        None = 0,
        Anonymous = 1,
        Identification = 2,
        Impersonation = 3,
        Delegation = 4,
    }
}
