// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Specifies the current state of an IP address.
    /// </summary>
    public enum DuplicateAddressDetectionState
    {
        Invalid = 0,
        Tentative,
        Duplicate,
        Deprecated,
        Preferred,
    }
}
