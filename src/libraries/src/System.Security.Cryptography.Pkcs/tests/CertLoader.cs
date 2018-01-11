// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Test.Cryptography
{
    internal abstract partial class CertLoader
    {
        private static readonly X509KeyStorageFlags s_defaultKeyStorageFlags = GetBestKeyStorageFlags();

        // Prefer ephemeral when available
        private static X509KeyStorageFlags GetBestKeyStorageFlags()
        {
#if netcoreapp
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows 7 ephemeral keys with a key usage embedded in the PFX
                // are treated differently than Windows 8.  So just use the default
                // storage flags for Win7.
                Version win8 = new Version(6, 2, 9200);

                if (Environment.OSVersion.Version >= win8)
                {
                    return X509KeyStorageFlags.EphemeralKeySet;
                }
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // OSX doesn't allow ephemeral, but every other Unix does.
                return X509KeyStorageFlags.EphemeralKeySet;
            }
#endif

            return X509KeyStorageFlags.DefaultKeySet;
        }

        protected X509KeyStorageFlags KeyStorageFlags = s_defaultKeyStorageFlags;

        /// <summary>
        /// Returns a freshly allocated X509Certificate2 instance that has a public key only. 
        /// 
        /// This method never returns null.
        /// </summary>
        public X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(CerData);
        }

        /// <summary>
        /// Attempts to return a freshly allocated X509Certificate2 instance that has a public and private key. Only use this method if your test
        /// needs the private key to work. Otherwise, use GetCertificate() which places far fewer conditions on you.
        /// 
        /// The test must check the return value for null. If it is null, exit the test without reporting a failure. A null means
        /// the test host chose to disable loading private keys.
        /// 
        /// If this method does return a certificate, the test must Dispose() it manually and as soon it no longer needs the certificate. 
        /// Due to the way PFX loading works on Windows, failure to dispose may leave an artifact on the test machine's disk. 
        /// </summary>
        public X509Certificate2 TryGetCertificateWithPrivateKey()
        {
            if (PfxData == null)
                throw new Exception("Cannot call TryGetCertificateWithPrivateKey() on this CertLoader: No PfxData provided.");

            if (!_alreadySearchedMyStore)
            {
                // Machine config check: Make sure that a matching certificate isn't stored in the MY store. Apis such as EnvelopedCms.Decrypt() look in the MY store for matching certs (with no opt-out)
                // and having our test certificates there can cause false test errors or false test successes.
                //
                // This may be an odd place to do this check but it's better than expecting every individual test to remember to do this.
                CheckIfCertWasLeftInCertStore();
                _alreadySearchedMyStore = true;
            }

            CertLoadMode testMode = CertLoader.TestMode;
            switch (testMode)
            {
                case CertLoadMode.Disable:
                    return null;

                case CertLoadMode.LoadFromPfx:
                    return new X509Certificate2(PfxData, Password, KeyStorageFlags);

                case CertLoadMode.LoadFromStore:
                    {
                        X509Certificate2Collection matches = FindMatches(CertLoader.StoreName, StoreLocation.CurrentUser);
                        if (matches.Count == 1)
                            return matches[0];

                        if (matches.Count == 0)
                            throw new Exception($"No matching certificate found in store {CertLoader.StoreName}");
                        else
                            throw new Exception($"Multiple matching certificates found in store {CertLoader.StoreName}");
                    }

                default:
                    throw new Exception($"Unexpected CertLoader.TestMode value: {testMode}");
            }
        }

        public abstract byte[] CerData { get; }
        public abstract byte[] PfxData { get; }
        public abstract string Password { get; }

        private void CheckIfCertWasLeftInCertStore()
        {
            X509Certificate2Collection matches = FindMatches("MY", StoreLocation.CurrentUser);
            if (matches.Count == 0)
            {
                matches = FindMatches("MY", StoreLocation.LocalMachine);
            }

            if (matches.Count != 0)
            {
                X509Certificate2 cer = new X509Certificate2(CerData);
                string issuer = cer.Issuer;
                string serial = cer.SerialNumber;

                throw new Exception($"A certificate issued to {issuer} with serial number {serial} was found in your Personal certificate store. This will cause some tests to fail due to machine configuration. Please remove these.");
            }
        }

        private X509Certificate2Collection FindMatches(string storeName, StoreLocation storeLocation)
        {
            using (X509Certificate2 cer = new X509Certificate2(CerData))
            {
                X509Certificate2Collection matches = new X509Certificate2Collection();

                using (X509Store store = new X509Store(storeName, storeLocation))
                {
                    try
                    {
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    }
                    catch (CryptographicException)
                    {
                        return matches;
                    }

                    foreach (X509Certificate2 candidate in store.Certificates)
                    {
                        // X509Certificate2.Equals() compares issuer and serial.
                        if (cer.Equals(candidate))
                        {
                            matches.Add(candidate);
                        }
                    } 
                    return matches;
                }
            }
        }

        internal abstract CertLoader CloneAsEphemeralLoader();
        internal abstract CertLoader CloneAsPerphemeralLoader();

        private bool _alreadySearchedMyStore = false;
    }

    internal sealed class CertLoaderFromRawData : CertLoader
    {
        public CertLoaderFromRawData(byte[] cerData, byte[] pfxData = null, string password = null)
        {
            CerData = cerData;
            PfxData = pfxData;
            Password = password;
        }

        public sealed override byte[] CerData { get; }
        public sealed override byte[] PfxData { get; }
        public sealed override string Password { get; }

        internal override CertLoader CloneAsEphemeralLoader()
        {
#if netcoreapp
            return new CertLoaderFromRawData(CerData, PfxData, Password)
            {
                KeyStorageFlags = X509KeyStorageFlags.EphemeralKeySet,
            };
#else
            throw new PlatformNotSupportedException();
#endif
        }

        internal override CertLoader CloneAsPerphemeralLoader()
        {
            return new CertLoaderFromRawData(CerData, PfxData, Password)
            {
                KeyStorageFlags = X509KeyStorageFlags.DefaultKeySet,
            };
        }
    }

    internal enum CertLoadMode
    {
        // Disable all tests that rely on private keys. Unfortunately, this has to be the default for checked in tests to avoid cluttering
        // people's machines with leaked keys on disk every time they build.
        Disable = 1,

        // Load certs from PFX data. This is convenient as it requires no preparatory steps. The downside is that every time you open a CNG .PFX,
        // a temporarily key is permanently leaked to your disk. (And every time you open a CAPI PFX, a key is leaked if the test aborts before
        // Disposing the certificate.) 
        //
        // Only use if you're testing on a VM or if you just don't care about your machine accumulating leaked keys.
        LoadFromPfx = 2,

        // Load certs from the certificate store (set StoreName to the name you want to use.) This requires that you preinstall the certificates
        // into the cert store (say by File.WriteAllByte()'ing the PFX blob into a "foo.pfx" file, then launching it and following the wizard.)
        // but then you don't need to worry about key leaks. 
        LoadFromStore = 3,
    }
}

