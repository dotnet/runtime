// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace System.Net.Http
{
    internal static class CurrentUserIdentityProvider
    {
        public static string GetIdentity()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.Name;
        }
    }
}
