// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    public enum OperationalStatus
    {
        Up = 1,
        Down,
        Testing,
        Unknown,
        Dormant,
        NotPresent,
        LowerLayerDown
    }
}
