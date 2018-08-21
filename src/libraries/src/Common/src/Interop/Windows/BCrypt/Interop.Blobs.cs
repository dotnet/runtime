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
            Debug.Assert(blob != null);
            Debug.Assert(offset >= 0);

            Buffer.BlockCopy(value, 0, blob, offset, value.Length);
            offset += value.Length;
        }

        /// <summary>
        ///     Append "value" to the data already in blob.
        /// </summary>
        internal static void EmitByte(byte[] blob, ref int offset, byte value, int count = 1)
        {
            Debug.Assert(blob != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count > 0);

            int finalOffset = offset + count;
            for (int i = offset; i < finalOffset; i++)
            {
                blob[i] = value;
            }
            offset = finalOffset;
        }

        /// <summary>
        ///     Append "value" in big Endian format to the data already in blob.
        /// </summary>
        internal static void EmitBigEndian(byte[] blob, ref int offset, int value)
        {
            Debug.Assert(blob != null);
            Debug.Assert(offset >= 0);

            unchecked
            {
                blob[offset++] = ((byte)(value >> 24));
                blob[offset++] = ((byte)(value >> 16));
                blob[offset++] = ((byte)(value >> 8));
                blob[offset++] = ((byte)(value));
            }
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
            BCRYPT_DSA_PUBLIC_MAGIC = 0x42505344,
            BCRYPT_DSA_PRIVATE_MAGIC = 0x56505344,
            BCRYPT_DSA_PUBLIC_MAGIC_V2 = 0x32425044,
            BCRYPT_DSA_PRIVATE_MAGIC_V2 = 0x32565044,

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
            internal const string BCRYPT_PUBLIC_KEY_BLOB = "PUBLICBLOB";
            internal const string BCRYPT_PRIVATE_KEY_BLOB = "PRIVATEBLOB";

            internal const string BCRYPT_RSAFULLPRIVATE_BLOB = "RSAFULLPRIVATEBLOB";
            internal const string BCRYPT_RSAPRIVATE_BLOB = "RSAPRIVATEBLOB";
            internal const string BCRYPT_RSAPUBLIC_KEY_BLOB = "RSAPUBLICBLOB";

            internal const string BCRYPT_DSA_PUBLIC_BLOB = "DSAPUBLICBLOB";
            internal const string BCRYPT_DSA_PRIVATE_BLOB = "DSAPRIVATEBLOB";
            internal const string LEGACY_DSA_V2_PUBLIC_BLOB = "V2CAPIDSAPUBLICBLOB";
            internal const string LEGACY_DSA_V2_PRIVATE_BLOB = "V2CAPIDSAPRIVATEBLOB";

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
        ///     The BCRYPT_DSA_KEY_BLOB structure is used as a v1 header for a DSA public key or private key BLOB in memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct BCRYPT_DSA_KEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int cbKey;
            internal fixed byte Count[4];
            internal fixed byte Seed[20];
            internal fixed byte q[20];
        }

        /// <summary>
        ///     The BCRYPT_DSA_KEY_BLOB structure is used as a v2 header for a DSA public key or private key BLOB in memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct BCRYPT_DSA_KEY_BLOB_V2
        {
            internal KeyBlobMagicNumber Magic;
            internal int cbKey;
            internal HASHALGORITHM_ENUM hashAlgorithm;
            internal DSAFIPSVERSION_ENUM standardVersion;
            internal int cbSeedLength;
            internal int cbGroupSize;
            internal fixed byte Count[4];
        }

        public enum HASHALGORITHM_ENUM
        {
            DSA_HASH_ALGORITHM_SHA1 = 0,
            DSA_HASH_ALGORITHM_SHA256 = 1,
            DSA_HASH_ALGORITHM_SHA512 = 2,
        }

        public enum DSAFIPSVERSION_ENUM
        {
            DSA_FIPS186_2 = 0,
            DSA_FIPS186_3 = 1,
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

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            int cbSize;
            uint dwInfoVersion;
            internal byte* pbNonce;
            internal int cbNonce;
            internal byte* pbAuthData;
            internal int cbAuthData;
            internal byte* pbTag;
            internal int cbTag;
            internal byte* pbMacContext;
            internal int cbMacContext;
            internal int cbAAD;
            internal ulong cbData;
            internal uint dwFlags;

            public static BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO Create()
            {
                BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO ret = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO();

                ret.cbSize = sizeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO);

                const uint BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION = 1;
                ret.dwInfoVersion = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION;

                return ret;
            }
        }
    }
}
