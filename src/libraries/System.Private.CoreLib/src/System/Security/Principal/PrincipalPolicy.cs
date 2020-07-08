// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//  Enum describing what type of principal to create by default (assuming no
//  principal has been set on the AppDomain).
//

namespace System.Security.Principal
{
    public enum PrincipalPolicy
    {
        // Note: it's important that the default policy has the value 0.
        UnauthenticatedPrincipal = 0,
        NoPrincipal = 1,
        WindowsPrincipal = 2,
    }
}
