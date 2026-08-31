// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal static class CurrentUserIdentityProvider
    {
        public static string GetIdentity()
        {
            return string.Empty;
        }
    }
}
