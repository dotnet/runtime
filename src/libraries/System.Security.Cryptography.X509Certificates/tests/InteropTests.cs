// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class InteropTests
    {
        [Fact]
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
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = (Interop.Crypt32.CERT_CONTEXT*)h;

                    // Does the blob data match?
                    int cbCertEncoded = pCertContext->cbCertEncoded;
                    Assert.Equal(TestData.MsCertificate.Length, cbCertEncoded);

                    byte[] pCertEncoded = new byte[cbCertEncoded];
                    Marshal.Copy((IntPtr)(pCertContext->pbCertEncoded), pCertEncoded, 0, cbCertEncoded);
                    Assert.Equal(TestData.MsCertificate, pCertEncoded);

                    // Does the serial number match?
                    Interop.Crypt32.CERT_INFO* pCertInfo = pCertContext->pCertInfo;
                    byte[] serialNumber = pCertInfo->SerialNumber.ToByteArray();
                    byte[] expectedSerial = "b00000000100dd9f3bd08b0aaf11b000000033".HexToByteArray();
                    Assert.Equal(expectedSerial, serialNumber);
                }
            }
        }

        [Fact]
        public static void TestHandleCtor()
        {
            IntPtr pCertContext = IntPtr.Zero;
            byte[] rawData = TestData.MsCertificate;
            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    Interop.Crypt32.DATA_BLOB certBlob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pRawData), (uint)rawData.Length);
                    bool success = Interop.Crypt32.CryptQueryObject(
                        Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                        &certBlob,
                        Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT,
                        Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_BINARY,
                        0,
                        IntPtr.Zero,
                        out Interop.Crypt32.ContentType contentType,
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
                Interop.Crypt32.CertFreeCertificateContext(pCertContext);

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
    }
}
