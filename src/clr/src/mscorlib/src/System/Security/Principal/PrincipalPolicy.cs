// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

//
//
//  Enum describing what type of principal to create by default (assuming no
//  principal has been set on the AppDomain).
//

namespace System.Security.Principal
{
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum PrincipalPolicy {
        // Note: it's important that the default policy has the value 0.
        UnauthenticatedPrincipal = 0,
        NoPrincipal = 1,
        WindowsPrincipal = 2,
    }
}
