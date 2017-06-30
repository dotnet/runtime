// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates
{
    // DefaultKeySet, UserKeySet and MachineKeySet are mutually exclusive
    // PersistKeySet and EphemeralKeySet are mutually exclusive
    [Flags]
    public enum X509KeyStorageFlags
    {
        DefaultKeySet = 0x00,
        UserKeySet = 0x01,
        MachineKeySet = 0x02,
        Exportable = 0x04,
        UserProtected = 0x08,
        PersistKeySet = 0x10,
        EphemeralKeySet = 0x20,
    }
}

