// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    //
    // These structures define the layout of CNG key blobs passed to NCryptImportKey
    //
    internal partial class BCrypt
    {
        /// <summary>
        ///     Append "value" to the data already in blob.
        /// </summary>
        internal static void Emit(byte[] blob, ref int offset, byte[] value)
        {
            Buffer.BlockCopy(value, 0, blob, offset, value.Length);
            offset += value.Length;
        }

        /// <summary>
        ///     Peel off the next "count" bytes in blob and return them in a byte array.
        /// </summary>
        internal static byte[] Consume(byte[] blob, ref int offset, int count)
        {
            byte[] value = new byte[count];
            Buffer.BlockCopy(blob, offset, value, 0, count);
            offset += count;
            return value;
        }

        /// <summary>
        ///     Magic numbers identifying blob types
        /// </summary>
        internal enum KeyBlobMagicNumber : int
        {
            BCRYPT_ECDH_PUBLIC_P256_MAGIC = 0x314B4345,
            BCRYPT_ECDH_PRIVATE_P256_MAGIC = 0x324B4345,
            BCRYPT_ECDH_PUBLIC_P384_MAGIC = 0x334B4345,
            BCRYPT_ECDH_PRIVATE_P384_MAGIC = 0x344B4345,
            BCRYPT_ECDH_PUBLIC_P521_MAGIC = 0x354B4345,
            BCRYPT_ECDH_PRIVATE_P521_MAGIC = 0x364B4345,
            BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC = 0x504B4345,
            BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC = 0x564B4345,

            BCRYPT_ECDSA_PUBLIC_P256_MAGIC = 0x31534345,
            BCRYPT_ECDSA_PRIVATE_P256_MAGIC = 0x32534345,
            BCRYPT_ECDSA_PUBLIC_P384_MAGIC = 0x33534345,
            BCRYPT_ECDSA_PRIVATE_P384_MAGIC = 0x34534345,
            BCRYPT_ECDSA_PUBLIC_P521_MAGIC = 0x35534345,
            BCRYPT_ECDSA_PRIVATE_P521_MAGIC = 0x36534345,
            BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC = 0x50444345,
            BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC = 0x56444345,

            BCRYPT_RSAPUBLIC_MAGIC = 0x31415352,
            BCRYPT_RSAPRIVATE_MAGIC = 0x32415352,
            BCRYPT_RSAFULLPRIVATE_MAGIC = 0x33415352,
            BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b,
        }

        /// <summary>
        ///     Well known key blob types
        /// </summary>
        internal static class KeyBlobType
        {
            internal const string BCRYPT_RSAFULLPRIVATE_BLOB = "RSAFULLPRIVATEBLOB";
            internal const string BCRYPT_RSAPRIVATE_BLOB = "RSAPRIVATEBLOB";
            internal const string BCRYPT_PUBLIC_KEY_BLOB = "RSAPUBLICBLOB";

            internal const string BCRYPT_ECCPUBLIC_BLOB = "ECCPUBLICBLOB";
            internal const string BCRYPT_ECCPRIVATE_BLOB = "ECCPRIVATEBLOB";
            internal const string BCRYPT_ECCFULLPUBLIC_BLOB = "ECCFULLPUBLICBLOB";
            internal const string BCRYPT_ECCFULLPRIVATE_BLOB = "ECCFULLPRIVATEBLOB";
        }

        /// <summary>
        ///     The BCRYPT_RSAKEY_BLOB structure is used as a header for an RSA public key or private key BLOB in memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_RSAKEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int BitLength;
            internal int cbPublicExp;
            internal int cbModulus;
            internal int cbPrime1;
            internal int cbPrime2;
        }

        /// <summary>
        ///     The BCRYPT_ECCKEY_BLOB structure is used as a header for an ECC public key or private key BLOB in memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_ECCKEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int cbKey;
        }

        /// <summary>
        ///     Represents the type of curve.
        /// </summary>
        internal enum ECC_CURVE_TYPE_ENUM : int
        {
            BCRYPT_ECC_PRIME_SHORT_WEIERSTRASS_CURVE = 0x1,
            BCRYPT_ECC_PRIME_TWISTED_EDWARDS_CURVE = 0x2,
            BCRYPT_ECC_PRIME_MONTGOMERY_CURVE = 0x3,
        }

        /// <summary>
        ///     Represents the algorithm that was used with Seed to generate A and B.
        /// </summary>
        internal enum ECC_CURVE_ALG_ID_ENUM : int
        {
            BCRYPT_NO_CURVE_GENERATION_ALG_ID = 0x0,
        }

        /// <summary>
        ///     Used as a header to curve parameters including the public and potentially private key.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_ECCFULLKEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int Version;              //Version of the structure
            internal ECC_CURVE_TYPE_ENUM CurveType;            //Supported curve types.
            internal ECC_CURVE_ALG_ID_ENUM CurveGenerationAlgId; //For X.592 verification purposes, if we include Seed we will need to include the algorithm ID.
            internal int cbFieldLength;          //Byte length of the fields P, A, B, X, Y.
            internal int cbSubgroupOrder;        //Byte length of the subgroup.
            internal int cbCofactor;             //Byte length of cofactor of G in E.
            internal int cbSeed;                 //Byte length of the seed used to generate the curve.
            // The rest of the buffer contains the domain parameters
        }

        /// <summary>
        ///     NCrypt buffer descriptors
        /// </summary>
        internal enum NCryptBufferDescriptors : int
        {
            NCRYPTBUFFER_ECC_CURVE_NAME = 60,
        }

        /// <summary>
        ///     BCrypt buffer
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCryptBuffer
        {
            internal int cbBuffer;             // Length of buffer, in bytes
            internal NCryptBufferDescriptors BufferType; // Buffer type
            internal IntPtr pvBuffer;          // Pointer to buffer
        }

        /// <summary>
        ///     The version of BCryptBuffer
        /// </summary>
        internal const int BCRYPTBUFFER_VERSION = 0;

        /// <summary>
        ///     Contains a set of generic CNG buffers.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCryptBufferDesc
        {
            internal int ulVersion;            // Version number
            internal int cBuffers;             // Number of buffers
            internal IntPtr pBuffers;          // Pointer to array of BCryptBuffers
        }

        /// <summary>
        ///     The version of BCRYPT_ECC_PARAMETER_HEADER
        /// </summary>
        internal const int BCRYPT_ECC_PARAMETER_HEADER_V1 = 1;

        /// <summary>
        ///     Used as a header to curve parameters.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_ECC_PARAMETER_HEADER
        {
            internal int Version;              //Version of the structure
            internal ECC_CURVE_TYPE_ENUM CurveType;            //Supported curve types.
            internal ECC_CURVE_ALG_ID_ENUM CurveGenerationAlgId; //For X.592 verification purposes, if we include Seed we will need to include the algorithm ID.
            internal int cbFieldLength;          //Byte length of the fields P, A, B, X, Y.
            internal int cbSubgroupOrder;        //Byte length of the subgroup.
            internal int cbCofactor;             //Byte length of cofactor of G in E.
            internal int cbSeed;                 //Byte length of the seed used to generate the curve.
            // The rest of the buffer contains the domain parameters
        }
    }
}
