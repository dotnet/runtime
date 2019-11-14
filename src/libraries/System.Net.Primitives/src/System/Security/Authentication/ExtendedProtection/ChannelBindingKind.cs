// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Authentication.ExtendedProtection
{
    // These should match the native SEC_ATTR_*_BINDINGS defines
    public enum ChannelBindingKind
    {
        Unknown = 0,
        Unique = 0x19,
        Endpoint = 0x1A
    }
}
