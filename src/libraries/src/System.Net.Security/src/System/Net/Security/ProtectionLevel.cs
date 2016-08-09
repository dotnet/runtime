// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
