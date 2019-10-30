// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
