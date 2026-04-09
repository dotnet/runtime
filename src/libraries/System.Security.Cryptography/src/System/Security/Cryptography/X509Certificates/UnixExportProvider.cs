// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal abstract class UnixExportProvider : IExportPal
    {
        private static readonly Oid s_Pkcs12X509CertBagTypeOid = new Oid(Oids.Pkcs12X509CertBagType, null);

        protected ICertificatePalCore? _singleCertPal;
        protected X509Certificate2Collection? _certs;

        internal UnixExportProvider(ICertificatePalCore singleCertPal)
        {
            _singleCertPal = singleCertPal;
        }

        internal UnixExportProvider(X509Certificate2Collection certs)
        {
            _certs = certs;
        }

        public void Dispose()
        {
            // Don't dispose any of the resources, they're still owned by the caller.
            _singleCertPal = null;
            _certs = null;
        }

        protected abstract byte[] ExportPkcs7();

        protected abstract byte[] ExportPkcs8(
            ICertificatePalCore certificatePal,
            PbeParameters pbeParameters,
            ReadOnlySpan<char> password);

        public byte[]? Export(X509ContentType contentType, SafePasswordHandle password)
        {
            Debug.Assert(password != null);
            switch (contentType)
            {
                case X509ContentType.Cert:
                    return ExportX509Der();
                case X509ContentType.Pfx:
                    return ExportPkcs12(Helpers.Windows3desPbe, password);
                case X509ContentType.Pkcs7:
                    return ExportPkcs7();
                case X509ContentType.SerializedCert:
                case X509ContentType.SerializedStore:
                    throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_SerializedExport);
                default:
                    throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
            }
        }

        public byte[] ExportPkcs12(Pkcs12ExportPbeParameters exportParameters, SafePasswordHandle password)
        {
            PbeParameters pbeParameters = Helpers.MapExportParametersToPbeParameters(exportParameters);
            return ExportPkcs12(pbeParameters, password);
        }

        private byte[]? ExportX509Der()
        {
            if (_singleCertPal != null)
            {
                return _singleCertPal.RawData;
            }

            // Windows/Desktop compatibility: Exporting a collection (or store) as
            // X509ContentType.Cert returns the equivalent of FirstOrDefault(),
            // so anything past _certs[0] is ignored, and an empty collection is
            // null (not an Exception)
            if (_certs!.Count == 0)
            {
                return null;
            }

            return _certs[0].RawData;
        }

        public byte[] ExportPkcs12(PbeParameters exportParameters, SafePasswordHandle password)
        {
            bool gotRef = false;

            try
            {
                password.DangerousAddRef(ref gotRef);
                ReadOnlySpan<char> passwordSpan = password.DangerousGetSpan();

                int localKeyIdCounter = 1;
                Pkcs12Builder builder = new();
                Pkcs12SafeContents certContainer = new();
                Pkcs12SafeContents keyContainer = new();

                if (_singleCertPal is not null)
                {
                    AddCertPalToSafeContents(
                        certContainer,
                        keyContainer,
                        _singleCertPal,
                        passwordSpan,
                        ref localKeyIdCounter);
                }
                else
                {
                    Debug.Assert(_certs is not null);

                    // Add the certificates in reverse order for compat.
                    for (int i = _certs.Count - 1; i >= 0; i--)
                    {
                        X509Certificate2 cert = _certs[i];
                        AddCertPalToSafeContents(
                            certContainer,
                            keyContainer,
                            cert.Pal,
                            passwordSpan,
                            ref localKeyIdCounter);
                    }
                }

                builder.AddSafeContentsEncrypted(certContainer, passwordSpan, exportParameters);
                builder.AddSafeContentsUnencrypted(keyContainer);
                builder.SealWithMac(passwordSpan, exportParameters.HashAlgorithm, exportParameters.IterationCount);
                return builder.Encode();
            }
            finally
            {
                if (gotRef)
                {
                    password.DangerousRelease();
                }
            }

            void AddCertPalToSafeContents(
                Pkcs12SafeContents certContainer,
                Pkcs12SafeContents keyContainer,
                ICertificatePalCore certificatePal,
                ReadOnlySpan<char> password,
                ref int localKeyIdCounter)
            {
                Pkcs12CertBag certBag = new(s_Pkcs12X509CertBagTypeOid, PkcsHelpers.EncodeOctetString(certificatePal.RawData));

                if (certificatePal.HasPrivateKey)
                {
                    Span<byte> localKeyIdAttributeValue = stackalloc byte[sizeof(int)];
                    BinaryPrimitives.WriteInt32LittleEndian(localKeyIdAttributeValue, localKeyIdCounter);
                    Pkcs9LocalKeyId keyId = new(localKeyIdAttributeValue);
                    Pkcs12ShroudedKeyBag keyBag = new(ExportPkcs8(certificatePal, exportParameters, password), skipCopy: true);

                    certBag.Attributes.Add(keyId);
                    keyBag.Attributes.Add(keyId);
                    keyContainer.AddSafeBag(keyBag);
                    localKeyIdCounter++;
                }

                certContainer.AddSafeBag(certBag);
            }
        }
    }
}
