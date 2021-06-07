// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Internal.Cryptography.Pal;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Collections.Generic;

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
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            return base.Add(certificate);
        }

        public void AddRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
            return Export(contentType, password: null);
        }

        public byte[]? Export(X509ContentType contentType, string? password)
        {
            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.Export(contentType, safePasswordHandle);
            }
        }

        public X509Certificate2Collection Find(X509FindType findType, object findValue, bool validOnly)
        {
            if (findValue == null)
                throw new ArgumentNullException(nameof(findValue));

            return FindPal.FindFromCollection(this, findType, findValue, validOnly);
        }

        public new X509Certificate2Enumerator GetEnumerator()
        {
            return new X509Certificate2Enumerator(this);
        }

        IEnumerator<X509Certificate2> IEnumerable<X509Certificate2>.GetEnumerator() => GetEnumerator();

        public void Import(byte[] rawData)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));

            Import(rawData.AsSpan());
        }

        /// <summary>
        ///   Imports the certificates from the provided data into this collection.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to read.
        /// </param>
        public void Import(ReadOnlySpan<byte> rawData)
        {
            Import(rawData, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));

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
        public void Import(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (ILoaderPal storePal = StorePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        public void Import(string fileName)
        {
            Import(fileName, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
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
        public void Import(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (ILoaderPal storePal = StorePal.FromFile(fileName, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        public void Insert(int index, X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            base.Insert(index, certificate);
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            base.Remove(certificate);
        }

        public void RemoveRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
            if (certPemFilePath is null)
                throw new ArgumentNullException(nameof(certPemFilePath));

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
                foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(certPem))
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

                        Import(certBytes);
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
    }
}
