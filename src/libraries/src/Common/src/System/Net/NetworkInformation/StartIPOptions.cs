// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    [Flags]
    internal enum StartIPOptions
    {
        None = 0,
        StartIPv4 = 1,
        StartIPv6 = 2,
        Both = 3
    }
}
