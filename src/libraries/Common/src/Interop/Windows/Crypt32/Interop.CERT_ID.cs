// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_ID
        {
            internal CertIdChoice dwIdChoice;
            internal CERT_ID_UNION u;

            [StructLayout(LayoutKind.Explicit)]
            internal struct CERT_ID_UNION
            {
                [FieldOffset(0)]
                internal CERT_ISSUER_SERIAL_NUMBER IssuerSerialNumber;

                [FieldOffset(0)]
                internal DATA_BLOB KeyId;

                [FieldOffset(0)]
                internal DATA_BLOB HashId;
            }
        }
    }
}
