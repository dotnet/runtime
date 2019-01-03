// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CMSG_KEY_AGREE_RECIPIENT_INFO
        {
            internal int dwVersion;
            internal CMsgKeyAgreeOriginatorChoice dwOriginatorChoice;

            // union based on dwOriginatorChoice. use OriginatorCertId or OriginatorPublicKeyInfo property instead.
            private CMSG_KEY_AGREE_RECIPIENT_INFO_ORIGINATOR_UNION Originator; // Do NOT add an underscore - this name still maps to a C++ Win32 header definition.

            internal DATA_BLOB UserKeyingMaterial;
            internal CRYPT_ALGORITHM_IDENTIFIER KeyEncryptionAlgorithm;
            internal int cRecipientEncryptedKeys;
            internal CMSG_RECIPIENT_ENCRYPTED_KEY_INFO** rgpRecipientEncryptedKeys;

            internal CERT_ID OriginatorCertId
            {
                get
                {
                    Debug.Assert(dwOriginatorChoice == CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_CERT);
                    return Originator.OriginatorCertId;
                }
            }

            internal CERT_PUBLIC_KEY_INFO OriginatorPublicKeyInfo
            {
                get
                {
                    Debug.Assert(dwOriginatorChoice == CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_PUBLIC_KEY);
                    return Originator.OriginatorPublicKeyInfo;
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct CMSG_KEY_AGREE_RECIPIENT_INFO_ORIGINATOR_UNION
            {
                [FieldOffset(0)]
                internal CERT_ID OriginatorCertId;

                [FieldOffset(0)]
                internal CERT_PUBLIC_KEY_INFO OriginatorPublicKeyInfo;
            }
        }
    }
}
