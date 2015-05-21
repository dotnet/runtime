// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class CngAsnFormatter : AsnFormatter
    {
        protected override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
        {
            // If OID is not present, then we can force CryptFormatObject 
            // to use hex formatting by providing an empty OID string.
            String oidValue = String.Empty;
            if (oid != null && oid.Value != null)
                oidValue = oid.Value;

            return Cng.CryptFormatObject(oidValue, rawData, multiLine);
        }
    }
}