// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
