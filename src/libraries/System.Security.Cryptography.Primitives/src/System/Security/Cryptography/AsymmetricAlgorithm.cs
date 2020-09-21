// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        [MaybeNull] protected KeySizes[] LegalKeySizesValue = null!;

        protected AsymmetricAlgorithm() { }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static AsymmetricAlgorithm Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        public static AsymmetricAlgorithm? Create(string algName) =>
            (AsymmetricAlgorithm?)CryptoConfigForwarder.CreateFromName(algName);

        public virtual int KeySize
        {
            get
            {
                return KeySizeValue;
            }

            set
            {
                if (!value.IsLegalSize(this.LegalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                KeySizeValue = value;
                return;
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // .NET Framework compat: No null check is performed
                return (KeySizes[])LegalKeySizesValue!.Clone();
            }
        }

        public virtual string? SignatureAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string? KeyExchangeAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual void FromXmlString(string xmlString)
        {
            throw new NotImplementedException();
        }

        public virtual string ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        public virtual void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            return ExportArray(
                passwordBytes,
                pbeParameters,
                (ReadOnlySpan<byte> span, PbeParameters parameters, Span<byte> destination, out int i) =>
                    TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i));
        }

        public virtual byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            return ExportArray(
                password,
                pbeParameters,
                (ReadOnlySpan<char> span, PbeParameters parameters, Span<byte> destination, out int i) =>
                    TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i));
        }

        public virtual byte[] ExportPkcs8PrivateKey() =>
            ExportArray(
                (Span<byte> destination, out int i) => TryExportPkcs8PrivateKey(destination, out i));

        public virtual byte[] ExportSubjectPublicKeyInfo() =>
            ExportArray(
                (Span<byte> destination, out int i) => TryExportSubjectPublicKeyInfo(destination, out i));


        public virtual bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        /// <summary>
        /// When overridden in a derived class, imports an encrypted RFC 7468
        /// PEM-encoded key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The PEM text of the encrypted key to import.</param>
        /// <param name="password">
        /// The password to use for decrypting the key material.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// A derived type has not overridden this member.
        /// </exception>
        /// <remarks>
        /// Because each algorithm may have algorithm-specific PEM labels, the
        /// default behavior will throw <see cref="NotImplementedException" />.
        /// </remarks>
        public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        /// <summary>
        /// When overridden in a derived class, imports an encrypted RFC 7468
        /// PEM-encoded key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The PEM text of the encrypted key to import.</param>
        /// <param name="passwordBytes">
        /// The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// A derived type has not overridden this member.
        /// </exception>
        /// <remarks>
        /// Because each algorithm may have algorithm-specific PEM labels, the
        /// default behavior will throw <see cref="NotImplementedException" />.
        /// </remarks>
        public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        /// <summary>
        /// When overridden in a derived class, imports an RFC 7468 textually
        /// encoded key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The text of the PEM key to import.</param>
        /// <exception cref="NotImplementedException">
        /// A derived type has not overridden this member.
        /// </exception>
        /// <remarks>
        /// Because each algorithm may have algorithm-specific PEM labels, the
        /// default behavior will throw <see cref="NotImplementedException" />.
        /// </remarks>
        public virtual void ImportFromPem(ReadOnlySpan<char> input) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        private delegate bool TryExportPbe<T>(
            ReadOnlySpan<T> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten);

        private delegate bool TryExport(Span<byte> destination, out int bytesWritten);

        private static unsafe byte[] ExportArray<T>(
            ReadOnlySpan<T> password,
            PbeParameters pbeParameters,
            TryExportPbe<T> exporter)
        {
            int bufSize = 4096;

            while (true)
            {
                byte[] buf = CryptoPool.Rent(bufSize);
                int bytesWritten = 0;
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(password, pbeParameters, buf, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buf, bytesWritten);
                    }

                    bufSize = checked(bufSize * 2);
                }
            }
        }

        private static unsafe byte[] ExportArray(TryExport exporter)
        {
            int bufSize = 4096;

            while (true)
            {
                byte[] buf = CryptoPool.Rent(bufSize);
                int bytesWritten = 0;
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(buf, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buf, bytesWritten);
                    }

                    bufSize = checked(bufSize * 2);
                }
            }
        }
    }
}
