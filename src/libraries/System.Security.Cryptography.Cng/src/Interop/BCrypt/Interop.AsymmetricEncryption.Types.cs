// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    /// <summary>
    /// BCrypt types related to asymmetric encryption algorithms
    /// </summary>
    internal partial class BCrypt
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_OAEP_PADDING_INFO
        {
            /// <summary>
            ///     Null-terminated Unicode string that identifies the hashing algorithm used to create the padding.
            /// </summary>
            internal IntPtr pszAlgId;

            /// <summary>
            ///     Address of a buffer that contains the data used to create the padding.
            /// </summary>
            internal IntPtr pbLabel;

            /// <summary>
            ///     Number of bytes in the pbLabel buffer.
            /// </summary>
            internal int cbLabel;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_PKCS1_PADDING_INFO
        {
            /// <summary>
            ///     Null-terminated Unicode string that identifies the hashing algorithm used to create the padding.
            /// </summary>
            internal IntPtr pszAlgId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_PSS_PADDING_INFO
        {
            /// <summary>
            ///     Null-terminated Unicode string that identifies the hashing algorithm used to create the padding.
            /// </summary>
            internal IntPtr pszAlgId;

            /// <summary>
            ///     The size, in bytes, of the random salt to use for the padding.
            /// </summary>
            internal int cbSalt;
        }
    }
}
