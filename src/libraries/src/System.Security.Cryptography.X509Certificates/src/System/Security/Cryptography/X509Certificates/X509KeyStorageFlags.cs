// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    // DefaultKeySet, UserKeySet and MachineKeySet are mutually exclusive
    [Flags]
    public enum X509KeyStorageFlags
    {
        DefaultKeySet = 0x00,
        UserKeySet = 0x01,
        MachineKeySet = 0x02,
        Exportable = 0x04,
        UserProtected = 0x08,
        PersistKeySet = 0x10,
    }
}

