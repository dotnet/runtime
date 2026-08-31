// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        internal static void TryDecodePem(ReadOnlySpan<byte> rawData, Func<ReadOnlySpan<byte>, X509ContentType, bool> derCallback)
        {
            // If the character is a control character that isn't whitespace, then we're probably using a DER encoding
            // and not using a PEM encoding in UTF8.
            if (char.IsControl((char)rawData[0]) && !char.IsWhiteSpace((char)rawData[0]))
            {
                return;
            }

            byte[]? certBytes = null;

            try
            {
                foreach ((ReadOnlySpan<byte> contents, PemFields fields) in PemEnumerator.Utf8(rawData))
                {
                    ReadOnlySpan<byte> label = contents[fields.Label];
                    bool isCertificate = label.SequenceEqual(PemLabels.X509CertificateUtf8);

                    if (isCertificate || label.SequenceEqual(PemLabels.Pkcs7CertificateUtf8))
                    {
                        certBytes = CryptoPool.Rent(fields.DecodedDataLength);

                        OperationStatus decodeResult = Base64.DecodeFromUtf8(
                            contents[fields.Base64Data],
                            certBytes,
                            out _,
                            out int bytesWritten);

                        if (decodeResult != OperationStatus.Done || bytesWritten != fields.DecodedDataLength)
                        {
                            Debug.Fail("The contents should have already been validated by the PEM reader.");
                            throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
                        }

                        X509ContentType contentType = isCertificate ? X509ContentType.Cert : X509ContentType.Pkcs7;
                        bool cont = derCallback(certBytes.AsSpan(0, bytesWritten), contentType);

                        byte[] toReturn = certBytes;
                        certBytes = null;
                        CryptoPool.Return(toReturn, clearSize: 0);

                        if (!cont)
                        {
                            return;
                        }
                    }
                }
            }
            finally
            {
                if (certBytes != null)
                {
                    CryptoPool.Return(certBytes, clearSize: 0);
                }
            }
        }
    }
}
