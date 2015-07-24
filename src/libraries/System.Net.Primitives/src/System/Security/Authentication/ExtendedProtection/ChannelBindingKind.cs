// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
