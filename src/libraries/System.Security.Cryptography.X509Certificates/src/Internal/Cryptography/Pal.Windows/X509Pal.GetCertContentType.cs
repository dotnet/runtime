// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
        {
            Interop.Crypt32.ContentType contentType;

            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    Interop.Crypt32.DATA_BLOB certBlob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pRawData), (uint)rawData.Length);
                    if (!Interop.Crypt32.CryptQueryObject(
                        Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                        &certBlob,
                        Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL,
                        Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
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
            Interop.Crypt32.ContentType contentType;

            unsafe
            {
                fixed (char* pFileName = fileName)
                {
                    if (!Interop.Crypt32.CryptQueryObject(
                        Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE,
                        pFileName,
                        Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL,
                        Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
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
        private static X509ContentType MapContentType(Interop.Crypt32.ContentType contentType)
        {
            switch (contentType)
            {
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_CERT:
                    return X509ContentType.Cert;
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE:
                    return X509ContentType.SerializedStore;
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT:
                    return X509ContentType.SerializedCert;
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED:
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
                    return X509ContentType.Pkcs7;
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
                    return X509ContentType.Authenticode;
                case Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX:
                    return X509ContentType.Pkcs12;
                default:
                    return X509ContentType.Unknown;
            }
        }
    }
}
