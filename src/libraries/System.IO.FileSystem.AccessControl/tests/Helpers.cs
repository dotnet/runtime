// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.Security.Principal;

namespace System.Security.AccessControl
{
    public static class Helpers
    {
        public static IdentityReference s_LocalSystemNTAccount = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount));
        public static IdentityReference s_NetworkServiceNTAccount = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount));
        public static IdentityReference s_WorldSidNTAccount = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
    }
}
