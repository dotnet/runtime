// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private static X509Certificate2Collection s_machineRootStore;
        private static readonly X509Certificate2Collection s_machineIntermediateStore = new X509Certificate2Collection();

        public static IStorePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            ICertificatePal singleCert;

            if (CertificatePal.TryReadX509Der(rawData, out singleCert) ||
                CertificatePal.TryReadX509Pem(rawData, out singleCert))
            {
                // The single X509 structure methods shouldn't return true and out null, only empty
                // collections have that behavior.
                Debug.Assert(singleCert != null);

                return SingleCertToStorePal(singleCert);
            }

            List<ICertificatePal> certPals;

            if (PkcsFormatReader.TryReadPkcs7Der(rawData, out certPals) ||
                PkcsFormatReader.TryReadPkcs7Pem(rawData, out certPals) ||
                PkcsFormatReader.TryReadPkcs12(rawData, password, out certPals))
            {
                Debug.Assert(certPals != null);

                return ListToStorePal(certPals);
            }

            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        public static IStorePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                return FromBio(bio, password);
            }
        }

        private static IStorePal FromBio(SafeBioHandle bio, string password)
        {
            int bioPosition = Interop.Crypto.BioTell(bio);
            Debug.Assert(bioPosition >= 0);

            ICertificatePal singleCert;

            if (CertificatePal.TryReadX509Pem(bio, out singleCert))
            {
                return SingleCertToStorePal(singleCert);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (CertificatePal.TryReadX509Der(bio, out singleCert))
            {
                return SingleCertToStorePal(singleCert);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            List<ICertificatePal> certPals;

            if (PkcsFormatReader.TryReadPkcs7Pem(bio, out certPals))
            {
                return ListToStorePal(certPals);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Der(bio, out certPals))
            {
                return ListToStorePal(certPals);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs12(bio, password, out certPals))
            {
                return ListToStorePal(certPals);
            }

            // Since we aren't going to finish reading, leaving the buffer where it was when we got
            // it seems better than leaving it in some arbitrary other position.
            // 
            // But, before seeking back to start, save the Exception representing the last reported
            // OpenSSL error in case the last BioSeek would change it.
            Exception openSslException = Interop.Crypto.CreateOpenSslCryptographicException();

            // Use BioSeek directly for the last seek attempt, because any failure here should instead
            // report the already created (but not yet thrown) exception.
            Interop.Crypto.BioSeek(bio, bioPosition);

            throw openSslException;
        }

        public static IStorePal FromCertificate(ICertificatePal cert)
        {
            ICertificatePal duplicatedHandles = ((OpenSslX509CertificateReader)cert).DuplicateHandles();

            return new CollectionBackedStoreProvider(new X509Certificate2(duplicatedHandles));
        }

        public static IStorePal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new CollectionBackedStoreProvider(certificates);
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            if (storeLocation != StoreLocation.LocalMachine)
            {
                return new DirectoryBasedStoreProvider(storeName, openFlags);
            }

            if (openFlags.HasFlag(OpenFlags.ReadWrite))
            {
                throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresReadOnly);
            }

            // The static store approach here is making an optimization based on not
            // having write support.  Once writing is permitted the stores would need
            // to fresh-read whenever being requested (or use FileWatcher/etc).
            if (s_machineRootStore == null)
            {
                lock (s_machineIntermediateStore)
                {
                    if (s_machineRootStore == null)
                    {
                        LoadMachineStores();
                    }
                }
            }

            if (StringComparer.Ordinal.Equals("Root", storeName))
            {
                return CloneStore(s_machineRootStore);
            }

            if (StringComparer.Ordinal.Equals("CA", storeName))
            {
                return CloneStore(s_machineIntermediateStore);
            }

            throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresRootOnly);
        }

        private static IStorePal SingleCertToStorePal(ICertificatePal singleCert)
        {
            return new CollectionBackedStoreProvider(new X509Certificate2(singleCert));
        }

        private static IStorePal ListToStorePal(List<ICertificatePal> certPals)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            for (int i = 0; i < certPals.Count; i++)
            {
                coll.Add(new X509Certificate2(certPals[i]));
            }

            return new CollectionBackedStoreProvider(coll);
        }

        private static IStorePal CloneStore(X509Certificate2Collection seed)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            foreach (X509Certificate2 cert in seed)
            {
                // Duplicate the certificate context into a new handle.
                coll.Add(new X509Certificate2(cert.Handle));
            }

            return new CollectionBackedStoreProvider(coll);
        }

        private static void LoadMachineStores()
        {
            Debug.Assert(
                Monitor.IsEntered(s_machineIntermediateStore),
                "LoadMachineStores assumes a lock(s_machineIntermediateStore)");

            X509Certificate2Collection rootStore = new X509Certificate2Collection();

            DirectoryInfo rootStorePath = null;
            IEnumerable<FileInfo> trustedCertFiles;

            try
            {
                rootStorePath = new DirectoryInfo(Interop.Crypto.GetX509RootStorePath());
            }
            catch (ArgumentException)
            {
                // If SSL_CERT_DIR is set to the empty string, or anything else which gives
                // "The path is not of a legal form", then the GetX509RootStorePath value is ignored.
            }

            if (rootStorePath != null && rootStorePath.Exists)
            {
                trustedCertFiles = rootStorePath.EnumerateFiles();
            }
            else
            {
                trustedCertFiles = Array.Empty<FileInfo>();
            }

            FileInfo rootStoreFile = null;

            try
            {
                rootStoreFile = new FileInfo(Interop.Crypto.GetX509RootStoreFile());
            }
            catch (ArgumentException)
            {
                // If SSL_CERT_FILE is set to the empty string, or anything else which gives
                // "The path is not of a legal form", then the GetX509RootStoreFile value is ignored.
            }

            if (rootStoreFile != null && rootStoreFile.Exists)
            {
                trustedCertFiles = Append(trustedCertFiles, rootStoreFile);
            }

            HashSet<X509Certificate2> uniqueRootCerts = new HashSet<X509Certificate2>();
            HashSet<X509Certificate2> uniqueIntermediateCerts = new HashSet<X509Certificate2>();

            foreach (FileInfo file in trustedCertFiles)
            {
                using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(file.FullName, "rb"))
                {
                    ICertificatePal pal;

                    while (CertificatePal.TryReadX509Pem(fileBio, out pal) ||
                        CertificatePal.TryReadX509Der(fileBio, out pal))
                    {
                        X509Certificate2 cert = new X509Certificate2(pal);

                        // The HashSets are just used for uniqueness filters, they do not survive this method.
                        if (StringComparer.Ordinal.Equals(cert.Subject, cert.Issuer))
                        {
                            if (uniqueRootCerts.Add(cert))
                            {
                                rootStore.Add(cert);
                            }
                        }
                        else
                        {
                            if (uniqueIntermediateCerts.Add(cert))
                            {
                                s_machineIntermediateStore.Add(cert);
                            }
                        }
                    }
                }
            }

            s_machineRootStore = rootStore;
        }

        private static IEnumerable<T> Append<T>(IEnumerable<T> current, T addition)
        {
            foreach (T element in current)
                yield return element;

            yield return addition;
        }
    }
}
