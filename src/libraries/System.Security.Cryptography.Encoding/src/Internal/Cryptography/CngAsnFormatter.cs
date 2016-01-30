// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
