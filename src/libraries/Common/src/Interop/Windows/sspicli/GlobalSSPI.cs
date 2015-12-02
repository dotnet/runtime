// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class GlobalSSPI
    {
        internal static SSPIInterface SSPIAuth = new SSPIAuthType();
        internal static SSPIInterface SSPISecureChannel = new SSPISecureChannelType();
    }
}
