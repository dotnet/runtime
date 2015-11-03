// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Specifies how an IP address host suffix was located.
    /// </summary>
    public enum SuffixOrigin
    {
        Other = 0,
        Manual,
        WellKnown,
        OriginDhcp,
        LinkLayerAddress,
        Random,
    }
}
