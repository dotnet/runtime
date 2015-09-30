// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    // Need a global so we can pass the interfaces as variables.
    internal static class GlobalSSPI
    {
        internal static SSPIInterface SSPISecureChannel = new SSPISecureChannelType();
    }
}