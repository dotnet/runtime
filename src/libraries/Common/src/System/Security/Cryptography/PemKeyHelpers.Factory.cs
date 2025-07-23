// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class PemKeyHelpers
    {
        internal delegate TAlg ImportFactoryKeyAction<TAlg>(ReadOnlySpan<byte> source);
        internal delegate ImportFactoryKeyAction<TAlg>? FindImportFactoryActionFunc<TAlg>(ReadOnlySpan<char> label);
        internal delegate TAlg ImportFactoryEncryptedKeyAction<TAlg, TPass>(ReadOnlySpan<TPass> password, ReadOnlySpan<byte> source);

        internal static TAlg ImportFactoryPem<TAlg>(ReadOnlySpan<char> source, FindImportFactoryActionFunc<TAlg> callback) where TAlg : class
        {
            ImportFactoryKeyAction<TAlg>? importAction = null;
            PemFields foundFields = default;
            ReadOnlySpan<char> foundSlice = default;
            bool containsEncryptedPem = false;

            ReadOnlySpan<char> pem = source;
            while (PemEncoding.TryFind(pem, out PemFields fields))
            {
                ReadOnlySpan<char> label = pem[fields.Label];
                ImportFactoryKeyAction<TAlg>? action = callback(label);

                if (action is not null)
                {
                    if (importAction is not null || containsEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(source));
                    }

                    importAction = action;
                    foundFields = fields;
                    foundSlice = pem;
                }
                else if (label.SequenceEqual(PemLabels.EncryptedPkcs8PrivateKey))
                {
                    if (importAction != null || containsEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(source));
                    }

                    containsEncryptedPem = true;
                }

                Index offset = fields.Location.End;
                pem = pem[offset..];
            }

            // The only PEM found that could potentially be used is encrypted PKCS8,
            // but we won't try to import it with a null or blank password, so
            // throw.
            if (containsEncryptedPem)
            {
                throw new ArgumentException(SR.Argument_PemImport_EncryptedPem, nameof(source));
            }

            // We went through the PEM and found nothing that could be handled.
            if (importAction is null)
            {
                throw new ArgumentException(SR.Argument_PemImport_NoPemFound, nameof(source));
            }

            ReadOnlySpan<char> base64Contents = foundSlice[foundFields.Base64Data];
#if NET
            int base64size = foundFields.DecodedDataLength;
            byte[] decodeBuffer = CryptoPool.Rent(base64size);
            int bytesWritten = 0;

            try
            {
                if (!Convert.TryFromBase64Chars(base64Contents, decodeBuffer, out bytesWritten))
                {
                    // Couldn't decode base64. We shouldn't get here since the
                    // contents are pre-validated.
                    Debug.Fail("Base64 decoding failed on already validated contents.");
                    throw new ArgumentException();
                }

                Debug.Assert(bytesWritten == base64size);
                Span<byte> decodedBase64 = decodeBuffer.AsSpan(0, bytesWritten);

                return importAction(decodedBase64);
            }
            finally
            {
                CryptoPool.Return(decodeBuffer, clearSize: bytesWritten);
            }
#else
            return importAction(Convert.FromBase64String(base64Contents.ToString()));
#endif
        }

        internal static TAlg ImportEncryptedFactoryPem<TAlg, TPass>(
            ReadOnlySpan<char> source,
            ReadOnlySpan<TPass> password,
            ImportFactoryEncryptedKeyAction<TAlg, TPass> importAction) where TAlg : class
        {
            bool foundEncryptedPem = false;
            PemFields foundFields = default;
            ReadOnlySpan<char> foundSlice = default;

            ReadOnlySpan<char> pem = source;
            while (PemEncoding.TryFind(pem, out PemFields fields))
            {
                ReadOnlySpan<char> label = pem[fields.Label];

                if (label.SequenceEqual(PemLabels.EncryptedPkcs8PrivateKey))
                {
                    if (foundEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(source));
                    }

                    foundEncryptedPem = true;
                    foundFields = fields;
                    foundSlice = pem;
                }

                Index offset = fields.Location.End;
                pem = pem[offset..];
            }

            if (!foundEncryptedPem)
            {
                throw new ArgumentException(SR.Argument_PemImport_NoPemFound, nameof(source));
            }

            ReadOnlySpan<char> base64Contents = foundSlice[foundFields.Base64Data];
#if NET
            int base64size = foundFields.DecodedDataLength;
            byte[] decodeBuffer = CryptoPool.Rent(base64size);
            int bytesWritten = 0;

            try
            {
                if (!Convert.TryFromBase64Chars(base64Contents, decodeBuffer, out bytesWritten))
                {
                    // Couldn't decode base64. We shouldn't get here since the
                    // contents are pre-validated.
                    Debug.Fail("Base64 decoding failed on already validated contents.");
                    throw new ArgumentException();
                }

                Debug.Assert(bytesWritten == base64size);
                Span<byte> decodedBase64 = decodeBuffer.AsSpan(0, bytesWritten);

                return importAction(password, decodedBase64);
            }
            finally
            {
                CryptoPool.Return(decodeBuffer, clearSize: bytesWritten);
            }
#else
            return importAction(password, Convert.FromBase64String(base64Contents.ToString()));
#endif
        }
    }
}
