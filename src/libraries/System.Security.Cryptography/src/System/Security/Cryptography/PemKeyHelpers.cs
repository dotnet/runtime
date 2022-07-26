// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static class PemKeyHelpers
    {
        public delegate bool TryExportKeyAction<T>(T arg, Span<byte> destination, out int bytesWritten);
        public delegate bool TryExportEncryptedKeyAction<T>(
            T arg,
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten);

        public static unsafe bool TryExportToEncryptedPem<T>(
            T arg,
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            TryExportEncryptedKeyAction<T> exporter,
            Span<char> destination,
            out int charsWritten)
        {
            int bufferSize = 4096;

            while (true)
            {
                byte[] buffer = CryptoPool.Rent(bufferSize);
                int bytesWritten = 0;
                bufferSize = buffer.Length;

                // Fixed to prevent GC moves.
                fixed (byte* bufferPtr = buffer)
                {
                    try
                    {
                        if (exporter(arg, password, pbeParameters, buffer, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buffer, 0, bytesWritten);
                            return PemEncoding.TryWrite(PemLabels.EncryptedPkcs8PrivateKey, writtenSpan, destination, out charsWritten);
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buffer, bytesWritten);
                    }

                    bufferSize = checked(bufferSize * 2);
                }
            }
        }

        public static unsafe bool TryExportToPem<T>(
            T arg,
            string label,
            TryExportKeyAction<T> exporter,
            Span<char> destination,
            out int charsWritten)
        {
            int bufferSize = 4096;

            while (true)
            {
                byte[] buffer = CryptoPool.Rent(bufferSize);
                int bytesWritten = 0;
                bufferSize = buffer.Length;

                // Fixed to prevent GC moves.
                fixed (byte* bufferPtr = buffer)
                {
                    try
                    {
                        if (exporter(arg, buffer, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buffer, 0, bytesWritten);
                            return PemEncoding.TryWrite(label, writtenSpan, destination, out charsWritten);
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buffer, bytesWritten);
                    }

                    bufferSize = checked(bufferSize * 2);
                }
            }
        }

        public delegate void ImportKeyAction(ReadOnlySpan<byte> source, out int bytesRead);
        public delegate ImportKeyAction? FindImportActionFunc(ReadOnlySpan<char> label);
        public delegate void ImportEncryptedKeyAction<TPass>(
            ReadOnlySpan<TPass> password,
            ReadOnlySpan<byte> source,
            out int bytesRead);

        public static void ImportEncryptedPem<TPass>(
            ReadOnlySpan<char> input,
            ReadOnlySpan<TPass> password,
            ImportEncryptedKeyAction<TPass> importAction)
        {
            bool foundEncryptedPem = false;
            PemFields foundFields = default;
            ReadOnlySpan<char> foundSlice = default;

            ReadOnlySpan<char> pem = input;
            while (PemEncoding.TryFind(pem, out PemFields fields))
            {
                ReadOnlySpan<char> label = pem[fields.Label];

                if (label.SequenceEqual(PemLabels.EncryptedPkcs8PrivateKey))
                {
                    if (foundEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(input));
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
                throw new ArgumentException(SR.Argument_PemImport_NoPemFound, nameof(input));
            }

            ReadOnlySpan<char> base64Contents = foundSlice[foundFields.Base64Data];
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

                // Don't need to check the bytesRead here. We're already operating
                // on an input which is already a parsed subset of the input.
                importAction(password, decodedBase64, out _);
            }
            finally
            {
                CryptoPool.Return(decodeBuffer, clearSize: bytesWritten);
            }
        }

        public static void ImportPem(ReadOnlySpan<char> input, FindImportActionFunc callback)
        {
            ImportKeyAction? importAction = null;
            PemFields foundFields = default;
            ReadOnlySpan<char> foundSlice = default;
            bool containsEncryptedPem = false;

            ReadOnlySpan<char> pem = input;
            while (PemEncoding.TryFind(pem, out PemFields fields))
            {
                ReadOnlySpan<char> label = pem[fields.Label];
                ImportKeyAction? action = callback(label);

                // Caller knows how to handle this PEM by label.
                if (action != null)
                {
                    // There was a previous PEM that could have been handled,
                    // which means this is ambiguous and contains multiple
                    // importable keys. Or, this contained an encrypted PEM.
                    // For purposes of encrypted PKCS8 with another actionable
                    // PEM, we will throw a duplicate exception.
                    if (importAction != null || containsEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(input));
                    }

                    importAction = action;
                    foundFields = fields;
                    foundSlice = pem;
                }
                else if (label.SequenceEqual(PemLabels.EncryptedPkcs8PrivateKey))
                {
                    if (importAction != null || containsEncryptedPem)
                    {
                        throw new ArgumentException(SR.Argument_PemImport_AmbiguousPem, nameof(input));
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
                throw new ArgumentException(SR.Argument_PemImport_EncryptedPem, nameof(input));
            }

            // We went through the PEM and found nothing that could be handled.
            if (importAction is null)
            {
                throw new ArgumentException(SR.Argument_PemImport_NoPemFound, nameof(input));
            }

            ReadOnlySpan<char> base64Contents = foundSlice[foundFields.Base64Data];
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

                // Don't need to check the bytesRead here. We're already operating
                // on an input which is already a parsed subset of the input.
                importAction(decodedBase64, out _);
            }
            finally
            {
                CryptoPool.Return(decodeBuffer, clearSize: bytesWritten);
            }
        }
    }
}
