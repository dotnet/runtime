// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private static X509Certificate2Collection s_machineRootStore;
        private static readonly X509Certificate2Collection s_machineIntermediateStore = new X509Certificate2Collection();

        public static IStorePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new NotImplementedException();
        }

        public static IStorePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new NotImplementedException();
        }

        public static IStorePal FromCertificate(ICertificatePal cert)
        {
            throw new NotImplementedException();
        }

        public static IStorePal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new OpenSslX509StoreProvider(certificates);
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            if (storeLocation != StoreLocation.LocalMachine)
            {
                throw new NotImplementedException();
            }

            if (openFlags.HasFlag(OpenFlags.ReadWrite))
            {
                throw new NotImplementedException();
            }

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

            throw new NotImplementedException();
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
            X509Certificate2Collection rootStore = new X509Certificate2Collection();

            DirectoryInfo directoryInfo;

            try
            {
                directoryInfo = new DirectoryInfo(Interop.NativeCrypto.GetX509RootStorePath());
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

            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                byte[] bytes;

                try
                {
                    bytes = File.ReadAllBytes(file.FullName);
                }
                catch (IOException)
                {
                    continue;
                }

                X509Certificate2 cert;

                try
                {
                    cert = new X509Certificate2(bytes);
                }
                catch (CryptographicException)
                {
                    continue;
                }

                if (StringComparer.Ordinal.Equals(cert.Subject, cert.Issuer))
                {
                    if (!rootStore.Contains(cert))
                    {
                        rootStore.Add(cert);
                    }
                }
                else
                {
                    if (!s_machineIntermediateStore.Contains(cert))
                    {
                        s_machineIntermediateStore.Add(cert);
                    }
                }
            }

            s_machineRootStore = rootStore;
        }
    }
}
