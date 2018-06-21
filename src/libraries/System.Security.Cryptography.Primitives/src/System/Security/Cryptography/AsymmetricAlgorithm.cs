// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        protected KeySizes[] LegalKeySizesValue;

        protected AsymmetricAlgorithm() { }

        public static AsymmetricAlgorithm Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        public static AsymmetricAlgorithm Create(string algName) =>
            (AsymmetricAlgorithm)CryptoConfigForwarder.CreateFromName(algName);

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
                // Desktop compat: No null check is performed
                return (KeySizes[])LegalKeySizesValue.Clone();
            }
        }

        public virtual string SignatureAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string KeyExchangeAlgorithm
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
                Span<byte> writtenSpan = Span<byte>.Empty;
                byte[] buf = ArrayPool<byte>.Shared.Rent(bufSize);
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(password, pbeParameters, buf, out int bytesWritten))
                        {
                            writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        if (writtenSpan.Length > 0)
                        {
                            CryptographicOperations.ZeroMemory(writtenSpan);
                        }

                        ArrayPool<byte>.Shared.Return(buf);
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
                Span<byte> writtenSpan = Span<byte>.Empty;
                byte[] buf = ArrayPool<byte>.Shared.Rent(bufSize);
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(buf, out int bytesWritten))
                        {
                            writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        if (writtenSpan.Length > 0)
                        {
                            CryptographicOperations.ZeroMemory(writtenSpan);
                        }

                        ArrayPool<byte>.Shared.Return(buf);
                    }

                    bufSize = checked(bufSize * 2);
                }
            }
        }
    }
}
