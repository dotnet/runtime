// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        public enum X509KeyUsageFlags
        {
            None = 0x0000,
            EncipherOnly = 0x0001,
            CrlSign = 0x0002,
            KeyCertSign = 0x0004,
            KeyAgreement = 0x0008,
            DataEncipherment = 0x0010,
            KeyEncipherment = 0x0020,
            NonRepudiation = 0x0040,
            DigitalSignature = 0x0080,
            DecipherOnly = 0x8000,
        }
    }
}
