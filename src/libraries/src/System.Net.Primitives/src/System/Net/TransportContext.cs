// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    public abstract class TransportContext
    {
        public abstract ChannelBinding GetChannelBinding(ChannelBindingKind kind);
    }
}
