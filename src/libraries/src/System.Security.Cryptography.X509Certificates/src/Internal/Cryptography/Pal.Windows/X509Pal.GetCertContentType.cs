// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public X509ContentType GetCertContentType(byte[] rawData)
        {
            ContentType contentType;

            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    CRYPTOAPI_BLOB certBlob = new CRYPTOAPI_BLOB(rawData.Length, pRawData);
                    if (!Interop.crypt32.CryptQueryObject(
                        CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                        &certBlob,
                        ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL,
                        ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
                        0,
                        IntPtr.Zero,
                        out contentType,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero))
                    {
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                    }
                }
            }

            return MapContentType(contentType);
        }

        public X509ContentType GetCertContentType(string fileName)
        {
            ContentType contentType;

            unsafe
            {
                fixed (char* pFileName = fileName)
                {
                    if (!Interop.crypt32.CryptQueryObject(
                        CertQueryObjectType.CERT_QUERY_OBJECT_FILE,
                        pFileName,
                        ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL,
                        ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
                        0,
                        IntPtr.Zero,
                        out contentType,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero))
                    {
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                    }
                }
            }

            return MapContentType(contentType);
        }

        //
        // this method maps a cert content type returned from CryptQueryObject
        // to a value in the managed X509ContentType enum
        //
        private static X509ContentType MapContentType(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.CERT_QUERY_CONTENT_CERT:
                    return X509ContentType.Cert;
                case ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE:
                    return X509ContentType.SerializedStore;
                case ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT:
                    return X509ContentType.SerializedCert;
                case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED:
                case ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
                    return X509ContentType.Pkcs7;
                case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
                    return X509ContentType.Authenticode;
                case ContentType.CERT_QUERY_CONTENT_PFX:
                    return X509ContentType.Pkcs12;
                default:
                    return X509ContentType.Unknown;
            }
        }
    }
}

