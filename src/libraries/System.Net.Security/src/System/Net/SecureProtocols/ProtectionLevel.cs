// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Security
{
    public enum ProtectionLevel
    {
        None = 0,
        // Data integrity only
        Sign = 1,
        // Both data confidentiality and integrity
        EncryptAndSign = 2
    }
}
