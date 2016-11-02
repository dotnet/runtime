// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private static CollectionBackedStoreProvider s_machineRootStore;
        private static CollectionBackedStoreProvider s_machineIntermediateStore;
        private static readonly object s_machineLoadLock = new object();

        public static ILoaderPal FromBlob(byte[] rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal singleCert;

            if (CertificatePal.TryReadX509Der(rawData, out singleCert) ||
                CertificatePal.TryReadX509Pem(rawData, out singleCert))
            {
                // The single X509 structure methods shouldn't return true and out null, only empty
                // collections have that behavior.
                Debug.Assert(singleCert != null);

                return SingleCertToLoaderPal(singleCert);
            }

            List<ICertificatePal> certPals;

            if (PkcsFormatReader.TryReadPkcs7Der(rawData, out certPals) ||
                PkcsFormatReader.TryReadPkcs7Pem(rawData, out certPals) ||
                PkcsFormatReader.TryReadPkcs12(rawData, password, out certPals))
            {
                Debug.Assert(certPals != null);

                return ListToLoaderPal(certPals);
            }

            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        public static ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                return FromBio(bio, password);
            }
        }

        private static ILoaderPal FromBio(SafeBioHandle bio, SafePasswordHandle password)
        {
            int bioPosition = Interop.Crypto.BioTell(bio);
            Debug.Assert(bioPosition >= 0);

            ICertificatePal singleCert;

            if (CertificatePal.TryReadX509Pem(bio, out singleCert))
            {
                return SingleCertToLoaderPal(singleCert);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (CertificatePal.TryReadX509Der(bio, out singleCert))
            {
                return SingleCertToLoaderPal(singleCert);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            List<ICertificatePal> certPals;

            if (PkcsFormatReader.TryReadPkcs7Pem(bio, out certPals))
            {
                return ListToLoaderPal(certPals);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Der(bio, out certPals))
            {
                return ListToLoaderPal(certPals);
            }

            // Rewind, try again.
            CertificatePal.RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs12(bio, password, out certPals))
            {
                return ListToLoaderPal(certPals);
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

        public static IExportPal FromCertificate(ICertificatePal cert)
        {
            return new ExportProvider(cert);
        }

        public static IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new ExportProvider(certificates);
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            if (storeLocation != StoreLocation.LocalMachine)
            {
                return new DirectoryBasedStoreProvider(storeName, openFlags);
            }

            if ((openFlags & OpenFlags.ReadWrite) == OpenFlags.ReadWrite)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresReadOnly);
            }

            // The static store approach here is making an optimization based on not
            // having write support.  Once writing is permitted the stores would need
            // to fresh-read whenever being requested.
            if (s_machineRootStore == null)
            {
                lock (s_machineLoadLock)
                {
                    if (s_machineRootStore == null)
                    {
                        LoadMachineStores();
                    }
                }
            }

            if (StringComparer.Ordinal.Equals("Root", storeName))
            {
                return s_machineRootStore;
            }

            if (StringComparer.Ordinal.Equals("CA", storeName))
            {
                return s_machineIntermediateStore;
            }

            throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresRootOnly);
        }

        private static ILoaderPal SingleCertToLoaderPal(ICertificatePal singleCert)
        {
            return new SingleCertLoader(singleCert);
        }

        private static ILoaderPal ListToLoaderPal(List<ICertificatePal> certPals)
        {
            return new CertCollectionLoader(certPals);
        }

        private static void LoadMachineStores()
        {
            Debug.Assert(
                Monitor.IsEntered(s_machineLoadLock),
                "LoadMachineStores assumes a lock(s_machineLoadLock)");

            var rootStore = new List<X509Certificate2>();
            var intermedStore = new List<X509Certificate2>();

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
                                continue;
                            }
                        }
                        else
                        {
                            if (uniqueIntermediateCerts.Add(cert))
                            {
                                intermedStore.Add(cert);
                                continue;
                            }
                        }

                        // There's a good chance we'll encounter duplicates on systems that have both one-cert-per-file
                        // and one-big-file trusted certificate stores. Anything that wasn't unique will end up here.
                        cert.Dispose();
                    }
                }
            }

            var rootStorePal = new CollectionBackedStoreProvider(rootStore);
            s_machineIntermediateStore = new CollectionBackedStoreProvider(intermedStore);

            // s_machineRootStore's nullarity is the loaded-state sentinel, so write it with Volatile.
            Debug.Assert(Monitor.IsEntered(s_machineLoadLock), "LoadMachineStores assumes a lock(s_machineLoadLock)");
            Volatile.Write(ref s_machineRootStore, rootStorePal);
        }

        private static IEnumerable<T> Append<T>(IEnumerable<T> current, T addition)
        {
            foreach (T element in current)
                yield return element;

            yield return addition;
        }
    }
}
