// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates.Asn1;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Certificate2Collection : X509CertificateCollection, IEnumerable<X509Certificate2>
    {
        public X509Certificate2Collection()
        {
        }

        public X509Certificate2Collection(X509Certificate2 certificate)
        {
            Add(certificate);
        }

        public X509Certificate2Collection(X509Certificate2[] certificates)
        {
            AddRange(certificates);
        }

        public X509Certificate2Collection(X509Certificate2Collection certificates)
        {
            AddRange(certificates);
        }

        public new X509Certificate2 this[int index]
        {
            get
            {
                return (X509Certificate2)(base[index]);
            }
            set
            {
                base[index] = value;
            }
        }

        public int Add(X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            return base.Add(certificate);
        }

        public void AddRange(X509Certificate2[] certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            int i = 0;
            try
            {
                for (; i < certificates.Length; i++)
                {
                    Add(certificates[i]);
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    Remove(certificates[j]);
                }
                throw;
            }
        }

        public void AddRange(X509Certificate2Collection certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            int i = 0;
            try
            {
                for (; i < certificates.Count; i++)
                {
                    Add(certificates[i]);
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    Remove(certificates[j]);
                }
                throw;
            }
        }

        public bool Contains(X509Certificate2 certificate)
        {
            // This method used to throw ArgumentNullException, but it has been deliberately changed
            // to no longer throw to match the behavior of X509CertificateCollection.Contains and the
            // IList.Contains implementation, which do not throw.

            return base.Contains(certificate);
        }

        public byte[]? Export(X509ContentType contentType)
        {
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.Export(contentType, SafePasswordHandle.InvalidHandle);
            }
        }

        /// <summary>
        ///   Exports the certificate and private key in PKCS#12 / PFX format.
        /// </summary>
        /// <param name="exportParameters">The algorithm parameters to use for the export.</param>
        /// <param name="password">The password to use for the export.</param>
        /// <returns>A byte array containing the encoded PKCS#12.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="password"/> contains a Unicode 'NULL' character.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="exportParameters"/> is not a valid value.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The export operation failed.</para>
        /// </exception>
        public byte[] ExportPkcs12(Pkcs12ExportPbeParameters exportParameters, string? password)
        {
            Helpers.ThrowIfInvalidPkcs12ExportParameters(exportParameters);
            Helpers.ThrowIfPasswordContainsNullCharacter(password);

            using (SafePasswordHandle safePasswordHandle = new(password, passwordProvided: true))
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.ExportPkcs12(exportParameters, safePasswordHandle);
            }
        }

        /// <summary>
        ///   Exports the certificates and private keys in PKCS#12 / PFX format.
        /// </summary>
        /// <param name="exportParameters">The algorithm parameters to use for the export.</param>
        /// <param name="password">The password to use for the export.</param>
        /// <returns>A byte array containing the encoded PKCS#12.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="password"/> contains a Unicode 'NULL' character.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="exportParameters"/> is <see langword="null"/> .
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The export operation failed.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="exportParameters"/> specifies a <see cref="PbeParameters.HashAlgorithm"/> value that is
        ///     not supported for the <see cref="PbeParameters.EncryptionAlgorithm"/> value.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="exportParameters"/> contains an invalid encryption algorithm for
        ///     <see cref="PbeParameters.EncryptionAlgorithm"/>.
        ///   </para>
        /// </exception>
        public byte[] ExportPkcs12(PbeParameters exportParameters, string? password)
        {
            ArgumentNullException.ThrowIfNull(exportParameters);
            Helpers.ThrowIfInvalidPkcs12ExportParameters(exportParameters);
            Helpers.ThrowIfPasswordContainsNullCharacter(password);

            using (SafePasswordHandle safePasswordHandle = new(password, passwordProvided: true))
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.ExportPkcs12(exportParameters, safePasswordHandle);
            }
        }

        public byte[]? Export(X509ContentType contentType, string? password)
        {
            using (var safePasswordHandle = new SafePasswordHandle(password, passwordProvided: true))
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.Export(contentType, safePasswordHandle);
            }
        }

        public X509Certificate2Collection Find(X509FindType findType, object findValue, bool validOnly)
        {
            ArgumentNullException.ThrowIfNull(findValue);

            return FindPal.FindFromCollection(this, findType, findValue, validOnly);
        }

        public new X509Certificate2Enumerator GetEnumerator()
        {
            return new X509Certificate2Enumerator(this);
        }

        IEnumerator<X509Certificate2> IEnumerable<X509Certificate2>.GetEnumerator() => GetEnumerator();

        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(byte[] rawData)
        {
            ArgumentNullException.ThrowIfNull(rawData);

            Import(rawData.AsSpan());
        }

        /// <summary>
        ///   Imports the certificates from the provided data into this collection.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to read.
        /// </param>
        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(ReadOnlySpan<byte> rawData)
        {
            using (ILoaderPal storePal = StorePal.FromBlob(rawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.DefaultKeySet))
            {
                storePal.MoveTo(this);
            }
        }

        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            ArgumentNullException.ThrowIfNull(rawData);

            Import(rawData.AsSpan(), password.AsSpan(), keyStorageFlags);
        }

        /// <summary>
        ///   Imports the certificates from the provided data into this collection.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to read.
        /// </param>
        /// <param name="password">
        ///   The password required to access the certificate data.
        /// </param>
        /// <param name="keyStorageFlags">
        ///   A bitwise combination of the enumeration values that control where and how to import the certificate.
        /// </param>
        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(ReadOnlySpan<byte> rawData, string? password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            Import(rawData, password.AsSpan(), keyStorageFlags);
        }

        /// <summary>
        ///   Imports the certificates from the provided data into this collection.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to read.
        /// </param>
        /// <param name="password">
        ///   The password required to access the certificate data.
        /// </param>
        /// <param name="keyStorageFlags">
        ///   A bitwise combination of the enumeration values that control where and how to import the certificate.
        /// </param>
        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password, passwordProvided: true))
            using (ILoaderPal storePal = StorePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            using (ILoaderPal storePal = StorePal.FromFile(fileName, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.DefaultKeySet))
            {
                storePal.MoveTo(this);
            }
        }

        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password, passwordProvided: true))
            using (ILoaderPal storePal = StorePal.FromFile(fileName, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        /// <summary>
        ///   Imports the certificates from the specified file a into this collection.
        /// </summary>
        /// <param name="fileName">
        ///   The name of the file containing the certificate information.
        /// </param>
        /// <param name="password">
        ///   The password required to access the certificate data.
        /// </param>
        /// <param name="keyStorageFlags">
        ///   A bitwise combination of the enumeration values that control where and how to import the certificate.
        /// </param>
        [Obsolete(Obsoletions.X509CtorCertDataObsoleteMessage, DiagnosticId = Obsoletions.X509CtorCertDataObsoleteDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Import(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password, passwordProvided: true))
            using (ILoaderPal storePal = StorePal.FromFile(fileName, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        public void Insert(int index, X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            base.Insert(index, certificate);
        }

        public void Remove(X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            base.Remove(certificate);
        }

        public void RemoveRange(X509Certificate2[] certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            int i = 0;
            try
            {
                for (; i < certificates.Length; i++)
                {
                    Remove(certificates[i]);
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    Add(certificates[j]);
                }
                throw;
            }
        }

        public void RemoveRange(X509Certificate2Collection certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            int i = 0;
            try
            {
                for (; i < certificates.Count; i++)
                {
                    Remove(certificates[i]);
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    Add(certificates[j]);
                }
                throw;
            }
        }

        /// <summary>
        /// Imports a collection of RFC 7468 PEM-encoded certificates.
        /// </summary>
        /// <param name="certPemFilePath">The path for the PEM-encoded X509 certificate collection.</param>
        /// <remarks>
        /// <para>
        /// See <see cref="System.IO.File.ReadAllText(string)" /> for additional documentation about
        /// exceptions that can be thrown.
        /// </para>
        /// <para>
        /// PEM-encoded items with a CERTIFICATE PEM label will be imported. PEM items
        /// with other labels will be ignored.
        /// </para>
        /// <para>
        /// More advanced scenarios for loading certificates and
        /// can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        /// PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="CryptographicException">
        /// The decoded contents of a PEM are invalid or corrupt and could not be imported.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="certPemFilePath" /> is <see langword="null" />.
        /// </exception>
        public void ImportFromPemFile(string certPemFilePath)
        {
            ArgumentNullException.ThrowIfNull(certPemFilePath);

            ReadOnlySpan<char> contents = System.IO.File.ReadAllText(certPemFilePath);
            ImportFromPem(contents);
        }

        /// <summary>
        /// Imports a collection of RFC 7468 PEM-encoded certificates.
        /// </summary>
        /// <param name="certPem">The text of the PEM-encoded X509 certificate collection.</param>
        /// <remarks>
        /// <para>
        /// PEM-encoded items with a CERTIFICATE PEM label will be imported. PEM items
        /// with other labels will be ignored.
        /// </para>
        /// <para>
        /// More advanced scenarios for loading certificates and
        /// can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        /// PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="CryptographicException">
        /// The decoded contents of a PEM are invalid or corrupt and could not be imported.
        /// </exception>
        public void ImportFromPem(ReadOnlySpan<char> certPem)
        {
            int added = 0;

            try
            {
                foreach ((ReadOnlySpan<char> contents, PemFields fields) in PemEnumerator.Utf16(certPem))
                {
                    ReadOnlySpan<char> label = contents[fields.Label];

                    if (label.SequenceEqual(PemLabels.X509Certificate))
                    {
                        // We verify below that every byte is written to.
                        byte[] certBytes = GC.AllocateUninitializedArray<byte>(fields.DecodedDataLength);

                        if (!Convert.TryFromBase64Chars(contents[fields.Base64Data], certBytes, out int bytesWritten)
                            || bytesWritten != fields.DecodedDataLength)
                        {
                            Debug.Fail("The contents should have already been validated by the PEM reader.");
                            throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
                        }

                        try
                        {
                            // Check that the contents are actually an X509 DER encoded
                            // certificate, not something else that the constructor will
                            // will otherwise be able to figure out.
                            CertificateAsn.Decode(certBytes, AsnEncodingRules.DER);
                        }
                        catch (CryptographicException)
                        {
                            throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
                        }
#pragma warning disable CA1416 // X509CertificateLoader is not available on browser.
                        Add(X509CertificateLoader.LoadCertificate(certBytes));
#pragma warning restore CA1416
                        added++;
                    }
                }
            }
            catch
            {
                for (int i = 0; i < added; i++)
                {
                    RemoveAt(Count - 1);
                }
                throw;
            }
        }

        /// <summary>
        /// Exports the X.509 public certificates as a PKCS7 certificate collection, encoded as PEM.
        /// </summary>
        /// <returns>The PEM encoded PKCS7 collection.</returns>
        /// <exception cref="CryptographicException">
        /// A certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public string ExportPkcs7Pem()
        {
            byte[]? pkcs7 = Export(X509ContentType.Pkcs7);

            if (pkcs7 is null)
            {
                throw new CryptographicException(SR.Cryptography_X509_ExportFailed);
            }

            return PemEncoding.WriteString(PemLabels.Pkcs7Certificate, pkcs7);
        }

        /// <summary>
        /// Attempts to export the X.509 public certificates as a PKCS7 certificate collection, encoded as PEM.
        /// </summary>
        /// <param name="destination">The buffer to receive the PEM encoded PKCS7 collection.</param>
        /// <param name="charsWritten">When this method returns, the total number of characters written to <paramref name="destination" />.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> was large enough to receive PEM encoded PKCS7
        ///   certificate collection; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// A certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public bool TryExportPkcs7Pem(Span<char> destination, out int charsWritten)
        {
            byte[]? pkcs7 = Export(X509ContentType.Pkcs7);

            if (pkcs7 is null)
            {
                throw new CryptographicException(SR.Cryptography_X509_ExportFailed);
            }

            return PemEncoding.TryWrite(PemLabels.Pkcs7Certificate, pkcs7, destination, out charsWritten);
        }

        /// <summary>
        /// Exports the public X.509 certificates, encoded as PEM.
        /// </summary>
        /// <returns>
        /// The PEM encoding of the certificates.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// A certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        /// <exception cref="OverflowException">
        /// The combined size of encoding all certificates exceeds <see cref="int.MaxValue" />.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 certificate collection will contain certificates
        ///   where each certificate begins with <c>-----BEGIN CERTIFICATE-----</c>
        ///   and ends with <c>-----END CERTIFICATE-----</c>, with the base64 encoded DER
        ///   contents of the certificate between the PEM boundaries. Each certificate is
        ///   separated by a single line-feed character.
        /// </p>
        /// <p>
        ///   Certificates are encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public string ExportCertificatePems()
        {
            int size = GetCertificatePemsSize();

            return string.Create(size, this, static (destination, col) => {
                if (!col.TryExportCertificatePems(destination, out int charsWritten) ||
                    charsWritten != destination.Length)
                {
                    Debug.Fail("Pre-allocated buffer was not the correct size.");
                    throw new CryptographicException();
                }
            });
        }

        /// <summary>
        /// Attempts to export the public X.509 certificates, encoded as PEM.
        /// </summary>
        /// <param name="destination">The buffer to receive the PEM encoded certificates.</param>
        /// <param name="charsWritten">When this method returns, the total number of characters written to <paramref name="destination" />.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> was large enough to receive the encoded PEMs;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// A certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 certificate collection will contain certificates
        ///   where each certificate begins with <c>-----BEGIN CERTIFICATE-----</c>
        ///   and ends with <c>-----END CERTIFICATE-----</c>, with the base64 encoded DER
        ///   contents of the certificate between the PEM boundaries. Each certificate is
        ///   separated by a single line-feed character.
        /// </p>
        /// <p>
        ///   Certificates are encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportCertificatePems(Span<char> destination, out int charsWritten)
        {
            Span<char> buffer = destination;
            int written = 0;

            for (int i = 0; i < Count; i++)
            {
                ReadOnlyMemory<byte> certData = this[i].RawDataMemory;
                int certSize = PemEncoding.GetEncodedSize(PemLabels.X509Certificate.Length, certData.Length);

                // If we ran out of space in the destination, return false. It's okay
                // that we may have successfully written data to the destination
                // already. Since certificates only contain "public" information,
                // we don't need to clear what has been written already.
                if (buffer.Length < certSize)
                {
                    charsWritten = 0;
                    return false;
                }

                if (!PemEncoding.TryWrite(PemLabels.X509Certificate, certData.Span, buffer, out int certWritten) ||
                    certWritten != certSize)
                {
                    Debug.Fail("Presized buffer is too small or did not write the correct amount.");
                    throw new CryptographicException();
                }

                buffer = buffer.Slice(certWritten);
                written += certWritten;

                // write a new line if not the last certificate.
                if (i < Count - 1)
                {
                    if (buffer.IsEmpty)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    // Always use Unix line endings between certificates to match the
                    // behavior of PemEncoding.TryWrite, which is following RFC 7468.
                    buffer[0] = '\n';
                    buffer = buffer.Slice(1);
                    written++;
                }
            }

            charsWritten = written;
            return true;
        }

        /// <summary>
        ///   Searches the collection for certificates with a matching thumbprint.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the hash algorithm to compute the thumbprint.</param>
        /// <param name="thumbprintHex">The thumbprint to match, hex-encoded.</param>
        /// <returns>A collection of certificates with a matching thumbprint.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="thumbprintHex"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <see langword="null"/> or empty.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="thumbprintHex"/> contains invalid hexadecimal characters.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="thumbprintHex"/> does not decode evenly and contains an odd number of characters.
        /// </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <para>
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   An error occured while finding the certificates with a matching thumbprint.
        /// </para>
        /// </exception>
        public X509Certificate2Collection FindByThumbprint(HashAlgorithmName hashAlgorithm, string thumbprintHex)
        {
            ArgumentNullException.ThrowIfNull(thumbprintHex);
            return FindByThumbprint(hashAlgorithm, thumbprintHex.AsSpan());
        }

        /// <summary>
        ///   Searches the collection for certificates with a matching thumbprint.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the hash algorithm to compute the thumbprint.</param>
        /// <param name="thumbprintHex">The thumbprint to match, hex-encoded.</param>
        /// <returns>A collection of certificates with a matching thumbprint.</returns>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <see langword="null"/> or empty.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="thumbprintHex"/> contains invalid hexadecimal characters.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="thumbprintHex"/> does not decode evenly and contains an odd number of characters.
        /// </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <para>
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   An error occured while finding the certificates with a matching thumbprint.
        /// </para>
        /// </exception>
        public X509Certificate2Collection FindByThumbprint(HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> thumbprintHex)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            const int MaxThumbprintStackAlloc = 64; // SHA-2/3-512 is the largest thumbprint currently known.

            // Make sure the buffer is big enough even if the input string is the incorrect length so we get the proper
            // error out of the decoder.
            int maxDecodedLength = checked(thumbprintHex.Length + 1) / 2;
            Span<byte> thumbprint = maxDecodedLength > MaxThumbprintStackAlloc ?
                new byte[maxDecodedLength] :
                stackalloc byte[MaxThumbprintStackAlloc];

            OperationStatus status = Convert.FromHexString(thumbprintHex, thumbprint, out _, out int bytesWritten);

            switch (status)
            {
                case OperationStatus.InvalidData:
                case OperationStatus.NeedMoreData:
                    throw new ArgumentException(SR.Argument_Thumbprint_Invalid, nameof(thumbprintHex));
                case OperationStatus.DestinationTooSmall:
                    Debug.Fail("Precomputed buffer was not large enough");
                    throw new CryptographicException();
                case OperationStatus.Done:
                    break;
            }

            return FindByThumbprintCore(hashAlgorithm, thumbprint.Slice(0, bytesWritten));
        }

        /// <summary>
        ///   Searches the collection for certificates with a matching thumbprint.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the hash algorithm to compute the thumbprint.</param>
        /// <param name="thumbprintBytes">The thumbprint to match.</param>
        /// <returns>A collection of certificates with a matching thumbprint.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <para>
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   An error occured while finding the certificates with a matching thumbprint.
        /// </para>
        /// </exception>
        public X509Certificate2Collection FindByThumbprint(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> thumbprintBytes)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            return FindByThumbprintCore(hashAlgorithm, thumbprintBytes);
        }

        private X509Certificate2Collection FindByThumbprintCore(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> thumbprintBytes)
        {
            const int MaxThumbprintStackAlloc = 64; // SHA-2/3-512 is the largest thumbprint currently known.
            Span<byte> thumbprintBuffer = stackalloc byte[MaxThumbprintStackAlloc];

            X509Certificate2Collection results = [];

            foreach (X509Certificate2 cert in this)
            {
                int bytesWritten = CryptographicOperations.HashData(hashAlgorithm, cert.RawDataMemory.Span, thumbprintBuffer);

                if (thumbprintBuffer.Slice(0, bytesWritten).SequenceEqual(thumbprintBytes))
                {
                    results.Add(cert);
                }
            }

            return results;
        }

        private int GetCertificatePemsSize()
        {
            checked
            {
                int size = 0;

                for (int i = 0; i < Count; i++)
                {
                    size += PemEncoding.GetEncodedSize(PemLabels.X509Certificate.Length, this[i].RawDataMemory.Length);

                    // Add a \n character between each certificate, except the last one.
                    if (i < Count - 1)
                    {
                        size += 1;
                    }
                }

                return size;
            }
        }
    }
}
