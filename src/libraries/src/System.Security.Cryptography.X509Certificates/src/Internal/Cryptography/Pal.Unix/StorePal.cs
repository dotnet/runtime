// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            
            return null;
        }

        public static IStorePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            using (SafeBioHandle bio = Interop.libcrypto.BIO_new_file(fileName, "rb"))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(bio);

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
            Exception openSslException = Interop.libcrypto.CreateOpenSslCryptographicException();

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
                // TODO (#2206): Support CurrentUser persisted stores
                // (they'd not be very useful without the ability to add/remove content)
                throw new NotImplementedException();
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

            // TODO (#2207): Support the rest of the stores, or throw PlatformNotSupportedException.
            throw new NotImplementedException();
        }

        private static IStorePal SingleCertToStorePal(ICertificatePal singleCert)
        {
            return new OpenSslX509StoreProvider(
                new X509Certificate2Collection(
                    new X509Certificate2(singleCert)));
        }

        private static IStorePal ListToStorePal(List<ICertificatePal> certPals)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            for (int i = 0; i < certPals.Count; i++)
            {
                coll.Add(new X509Certificate2(certPals[i]));
            }

            return new OpenSslX509StoreProvider(coll);
        }

        private static IStorePal PfxToCollection(OpenSslPkcs12Reader pfx, string password)
        {
            pfx.Decrypt(password);

            X509Certificate2Collection coll = new X509Certificate2Collection();

            foreach (OpenSslX509CertificateReader certPal in pfx.ReadCertificates())
            {
                coll.Add(new X509Certificate2(certPal));
            }

            return new OpenSslX509StoreProvider(coll);
        }

        private static IStorePal CloneStore(X509Certificate2Collection seed)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            foreach (X509Certificate2 cert in seed)
            {
                // Duplicate the certificate context into a new handle.
                coll.Add(new X509Certificate2(cert.Handle));
            }

            return new OpenSslX509StoreProvider(coll);
        }

        private static void LoadMachineStores()
        {
            Debug.Assert(
                Monitor.IsEntered(s_machineIntermediateStore),
                "LoadMachineStores assumes a lock(s_machineIntermediateStore)");

            X509Certificate2Collection rootStore = new X509Certificate2Collection();

            DirectoryInfo directoryInfo;

            try
            {
                directoryInfo = new DirectoryInfo(Interop.Crypto.GetX509RootStorePath());
            }
            catch (ArgumentException)
            {
                // If SSL_CERT_DIR is set to the empty string, or anything else which gives
                // "The path is not of a legal form", then just call it a day.
                s_machineRootStore = rootStore;
                return;
            }

            if (!directoryInfo.Exists)
            {
                s_machineRootStore = rootStore;
                return;
            }

            HashSet<X509Certificate2> uniqueRootCerts = new HashSet<X509Certificate2>();
            HashSet<X509Certificate2> uniqueIntermediateCerts = new HashSet<X509Certificate2>();

            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                byte[] bytes;

                try
                {
                    bytes = File.ReadAllBytes(file.FullName);
                }
                catch (IOException)
                {
                    // Broken symlink, symlink to a network file share that's timing out,
                    // file was deleted since being enumerated, etc.
                    //
                    // Skip anything that we can't read, we'll just be a bit restrictive
                    // on our trust model, that's all.
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    // If, for some reason, one of the files is not world-readable,
                    // and this user doesn't have access to read it, just pretend it
                    // isn't there.
                    continue;
                }

                X509Certificate2 cert;

                try
                {
                    cert = new X509Certificate2(bytes);
                }
                catch (CryptographicException)
                {
                    // The data was in a format we didn't understand. Maybe it was a text file,
                    // or just a certificate type we don't know how to read. Either way, let's load
                    // what we can.
                    continue;
                }

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

            s_machineRootStore = rootStore;
        }
    }
}
