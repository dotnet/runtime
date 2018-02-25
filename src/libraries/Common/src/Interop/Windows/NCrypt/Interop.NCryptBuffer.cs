// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        /// <summary>
        ///     Types of NCryptBuffers
        /// </summary>
        internal enum BufferType
        {
            KdfHashAlgorithm = 0x00000000,              // KDF_HASH_ALGORITHM
            KdfSecretPrepend = 0x00000001,              // KDF_SECRET_PREPEND
            KdfSecretAppend = 0x00000002,               // KDF_SECRET_APPEND
            KdfHmacKey = 0x00000003,                    // KDF_HMAC_KEY
            KdfTlsLabel = 0x00000004,                   // KDF_TLS_PRF_LABEL
            KdfTlsSeed = 0x00000005                     // KDF_TLS_PRF_SEED
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCryptBuffer
        {
            public int cbBuffer;
            public BufferType BufferType;
            public IntPtr pvBuffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCryptBufferDesc
        {
            public int ulVersion;
            public int cBuffers;
            public IntPtr pBuffers;         // NCryptBuffer[cBuffers]
        }
    }
}

