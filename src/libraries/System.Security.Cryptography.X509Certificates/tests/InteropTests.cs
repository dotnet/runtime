// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class InteropTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestHandle()
        {
            //
            // Ensure that the Handle property returns a valid CER_CONTEXT pointer.
            //
            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                IntPtr h = c.Handle;
                unsafe
                {
                    CERT_CONTEXT* pCertContext = (CERT_CONTEXT*)h;

                    // Does the blob data match?
                    int cbCertEncoded = pCertContext->cbCertEncoded;
                    Assert.Equal(TestData.MsCertificate.Length, cbCertEncoded);

                    byte[] pCertEncoded = new byte[cbCertEncoded];
                    Marshal.Copy((IntPtr)(pCertContext->pbCertEncoded), pCertEncoded, 0, cbCertEncoded);
                    Assert.Equal(TestData.MsCertificate, pCertEncoded);

                    // Does the serial number match?
                    CERT_INFO* pCertInfo = pCertContext->pCertInfo;
                    byte[] serialNumber = pCertInfo->SerialNumber.ToByteArray();
                    byte[] expectedSerial = "b00000000100dd9f3bd08b0aaf11b000000033".HexToByteArray();
                    Assert.Equal(expectedSerial, serialNumber);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestHandleCtor()
        {
            IntPtr pCertContext = IntPtr.Zero;
            byte[] rawData = TestData.MsCertificate;
            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    CRYPTOAPI_BLOB certBlob = new CRYPTOAPI_BLOB() { cbData = rawData.Length, pbData = pRawData };
                    bool success = CryptQueryObject(
                        CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                        ref certBlob,
                        ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT,
                        ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_BINARY,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        out pCertContext
                            );

                    if (!success)
                    {
                        int hr = Marshal.GetHRForLastWin32Error();
                        throw new CryptographicException(hr);
                    }
                }
            }

            // Now, create an X509Certificate around our handle.
            using (X509Certificate2 c = new X509Certificate2(pCertContext))
            {
                // And release our ref-count on the handle. X509Certificate better be maintaining its own.
                CertFreeCertificateContext(pCertContext);

                // Now, test various properties to make sure the X509Certificate actually wraps our CERT_CONTEXT.
                IntPtr h = c.Handle;
                Assert.Equal(pCertContext, h);
                pCertContext = IntPtr.Zero;

                Assert.Equal(rawData, c.GetRawCertData());
                Assert.Equal(rawData, c.GetRawCertDataString().HexToByteArray());

                string issuer = c.Issuer;
                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    issuer);

                byte[] expectedPublicKey = (
                    "3082010a0282010100e8af5ca2200df8287cbc057b7fadeeeb76ac28533f3adb" +
                    "407db38e33e6573fa551153454a5cfb48ba93fa837e12d50ed35164eef4d7adb" +
                    "137688b02cf0595ca9ebe1d72975e41b85279bf3f82d9e41362b0b40fbbe3bba" +
                    "b95c759316524bca33c537b0f3eb7ea8f541155c08651d2137f02cba220b10b1" +
                    "109d772285847c4fb91b90b0f5a3fe8bf40c9a4ea0f5c90a21e2aae3013647fd" +
                    "2f826a8103f5a935dc94579dfb4bd40e82db388f12fee3d67a748864e162c425" +
                    "2e2aae9d181f0e1eb6c2af24b40e50bcde1c935c49a679b5b6dbcef9707b2801" +
                    "84b82a29cfbfa90505e1e00f714dfdad5c238329ebc7c54ac8e82784d37ec643" +
                    "0b950005b14f6571c50203010001").HexToByteArray();

                byte[] publicKey = c.GetPublicKey();
                Assert.Equal(expectedPublicKey, publicKey);

                byte[] expectedThumbPrint = "108e2ba23632620c427c570b6d9db51ac31387fe".HexToByteArray();
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, thumbPrint);
            }
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            [In] ref CRYPTOAPI_BLOB pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            IntPtr pdwMsgAndCertEncodingType,
            IntPtr pdwContentType,
            IntPtr pdwFormatType,
            IntPtr phCertStore,
            IntPtr phMsg,
            out IntPtr ppvContext
            );

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        private enum CertQueryObjectType : int
        {
            CERT_QUERY_OBJECT_FILE = 0x00000001,
            CERT_QUERY_OBJECT_BLOB = 0x00000002,
        }

        [Flags]
        private enum ExpectedContentTypeFlags : int
        {
            //encoded single certificate
            CERT_QUERY_CONTENT_FLAG_CERT = 1 << ContentType.CERT_QUERY_CONTENT_CERT,

            //encoded single CTL
            CERT_QUERY_CONTENT_FLAG_CTL = 1 << ContentType.CERT_QUERY_CONTENT_CTL,

            //encoded single CRL
            CERT_QUERY_CONTENT_FLAG_CRL = 1 << ContentType.CERT_QUERY_CONTENT_CRL,

            //serialized store
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE,

            //serialized single certificate
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT,

            //serialized single CTL
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CTL,

            //serialized single CRL
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CRL,

            //an encoded PKCS#7 signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED,

            //an encoded PKCS#7 message.  But it is not a signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED,

            //the content includes an embedded PKCS7 signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED,

            //an encoded PKCS#10
            CERT_QUERY_CONTENT_FLAG_PKCS10 = 1 << ContentType.CERT_QUERY_CONTENT_PKCS10,

            //an encoded PFX BLOB
            CERT_QUERY_CONTENT_FLAG_PFX = 1 << ContentType.CERT_QUERY_CONTENT_PFX,

            //an encoded CertificatePair (contains forward and/or reverse cross certs)
            CERT_QUERY_CONTENT_FLAG_CERT_PAIR = 1 << ContentType.CERT_QUERY_CONTENT_CERT_PAIR,

            //an encoded PFX BLOB, and we do want to load it (not included in
            //CERT_QUERY_CONTENT_FLAG_ALL)
            CERT_QUERY_CONTENT_FLAG_PFX_AND_LOAD = 1 << ContentType.CERT_QUERY_CONTENT_PFX_AND_LOAD,
        }

        [Flags]
        private enum ExpectedFormatTypeFlags : int
        {
            CERT_QUERY_FORMAT_FLAG_BINARY = 1 << FormatType.CERT_QUERY_FORMAT_BINARY,
            CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_BASE64_ENCODED,
            CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED,

            CERT_QUERY_FORMAT_FLAG_ALL = CERT_QUERY_FORMAT_FLAG_BINARY | CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED | CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED,
        }

        private enum MsgAndCertEncodingType : int
        {
            PKCS_7_ASN_ENCODING = 0x10000,
            X509_ASN_ENCODING = 0x1,
        }

        private enum ContentType : int
        {
            //encoded single certificate
            CERT_QUERY_CONTENT_CERT = 1,
            //encoded single CTL                   
            CERT_QUERY_CONTENT_CTL = 2,
            //encoded single CRL
            CERT_QUERY_CONTENT_CRL = 3,
            //serialized store
            CERT_QUERY_CONTENT_SERIALIZED_STORE = 4,
            //serialized single certificate
            CERT_QUERY_CONTENT_SERIALIZED_CERT = 5,
            //serialized single CTL
            CERT_QUERY_CONTENT_SERIALIZED_CTL = 6,
            //serialized single CRL
            CERT_QUERY_CONTENT_SERIALIZED_CRL = 7,
            //a PKCS#7 signed message
            CERT_QUERY_CONTENT_PKCS7_SIGNED = 8,
            //a PKCS#7 message, such as enveloped message.  But it is not a signed message,
            CERT_QUERY_CONTENT_PKCS7_UNSIGNED = 9,
            //a PKCS7 signed message embedded in a file
            CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10,
            //an encoded PKCS#10
            CERT_QUERY_CONTENT_PKCS10 = 11,
            //an encoded PFX BLOB
            CERT_QUERY_CONTENT_PFX = 12,
            //an encoded CertificatePair (contains forward and/or reverse cross certs)
            CERT_QUERY_CONTENT_CERT_PAIR = 13,
            //an encoded PFX BLOB, which was loaded to phCertStore
            CERT_QUERY_CONTENT_PFX_AND_LOAD = 14,
        }

        private enum FormatType : int
        {
            CERT_QUERY_FORMAT_BINARY = 1,
            CERT_QUERY_FORMAT_BASE64_ENCODED = 2,
            CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED = 3,
        }

        // CRYPTOAPI_BLOB has many typedef aliases in the C++ world (CERT_BLOB, DATA_BLOB, etc.) We'll just stick to one name here.
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CRYPTOAPI_BLOB
        {
            public int cbData;
            public byte* pbData;

            public byte[] ToByteArray()
            {
                if (cbData == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] array = new byte[cbData];
                Marshal.Copy((IntPtr)pbData, array, 0, cbData);
                return array;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CERT_CONTEXT
        {
            public readonly MsgAndCertEncodingType dwCertEncodingType;
            public readonly byte* pbCertEncoded;
            public readonly int cbCertEncoded;
            public readonly CERT_INFO* pCertInfo;
            public readonly IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CERT_INFO
        {
            public readonly int dwVersion;
            public CRYPTOAPI_BLOB SerialNumber;
            public readonly CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            public readonly CRYPTOAPI_BLOB Issuer;
            public readonly FILETIME NotBefore;
            public readonly FILETIME NotAfter;
            public readonly CRYPTOAPI_BLOB Subject;
            public readonly CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            public readonly CRYPT_BIT_BLOB IssuerUniqueId;
            public readonly CRYPT_BIT_BLOB SubjectUniqueId;
            public readonly int cExtension;
            public readonly CERT_EXTENSION* rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_ALGORITHM_IDENTIFIER
        {
            public readonly IntPtr pszObjId;
            public readonly CRYPTOAPI_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CERT_PUBLIC_KEY_INFO
        {
            public readonly CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            public readonly CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CRYPT_BIT_BLOB
        {
            public readonly int cbData;
            public readonly byte* pbData;
            public readonly int cUnusedBits;

            public byte[] ToByteArray()
            {
                if (cbData == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] array = new byte[cbData];
                Marshal.Copy((IntPtr)pbData, array, 0, cbData);
                return array;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CERT_EXTENSION
        {
            public readonly IntPtr pszObjId;
            public readonly int fCritical;
            public readonly CRYPTOAPI_BLOB Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            private readonly uint _ftTimeLow;
            private readonly uint _ftTimeHigh;

            public DateTime ToDateTime()
            {
                long fileTime = (((long)_ftTimeHigh) << 32) + _ftTimeLow;
                return DateTime.FromFileTime(fileTime);
            }
        }
    }
}
