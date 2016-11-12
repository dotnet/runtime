// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography.Pal;
using System.Diagnostics;
using System.Globalization;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Store : IDisposable
    {
        private IStorePal _storePal;

        public X509Store()
            : this(StoreName.My, StoreLocation.CurrentUser)
        {
        }

        public X509Store(string storeName)
            : this(storeName, StoreLocation.CurrentUser)
        {
        }

        public X509Store(StoreName storeName)
            : this(storeName, StoreLocation.CurrentUser)
        {
        }

        public X509Store(StoreLocation storeLocation)
            : this(StoreName.My, storeLocation)
        {
        }

        public X509Store(StoreName storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(storeLocation)));

            switch (storeName)
            {
                case StoreName.AddressBook:
                    Name = "AddressBook";
                    break;
                case StoreName.AuthRoot:
                    Name = "AuthRoot";
                    break;
                case StoreName.CertificateAuthority:
                    Name = "CA";
                    break;
                case StoreName.Disallowed:
                    Name = "Disallowed";
                    break;
                case StoreName.My:
                    Name = "My";
                    break;
                case StoreName.Root:
                    Name = "Root";
                    break;
                case StoreName.TrustedPeople:
                    Name = "TrustedPeople";
                    break;
                case StoreName.TrustedPublisher:
                    Name = "TrustedPublisher";
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(storeName)));
            }

            Location = storeLocation;
        }

        public X509Store(string storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, nameof(storeLocation)));

            Location = storeLocation;
            Name = storeName;
        }

        public X509Store(IntPtr storeHandle)
        {
            _storePal = StorePal.FromHandle(storeHandle);
            Debug.Assert(_storePal != null);
        }

        public IntPtr StoreHandle
        {
            get
            {
                if (_storePal == null)
                    throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

                // The Pal layer may return null (Unix) or throw exception (Windows)
                if (_storePal.SafeHandle == null)
                    return IntPtr.Zero;

                return _storePal.SafeHandle.DangerousGetHandle();
            }
        }

        public StoreLocation Location { get; private set; }

        public string Name { get; private set; }


        public void Open(OpenFlags flags)
        {
            Close();
            _storePal = StorePal.FromSystemStore(Name, Location, flags);
        }

        public X509Certificate2Collection Certificates
        {
            get
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                if (_storePal != null)
                {
                    _storePal.CloneTo(certificates);
                }
                return certificates;
            }
        }

        public void Add(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            _storePal.Add(certificate.Pal);
        }

        public void AddRange(X509Certificate2Collection certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

            int i = 0;
            try
            {
                foreach (X509Certificate2 certificate in certificates)
                {
                    Add(certificate);
                    i++;
                }
            }
            catch
            {
                // For desktop compat, we keep the exception semantics even though they are not ideal
                // because an exception may cause certs to be removed even if they weren't there before.
                for (int j = 0; j < i; j++)
                {
                    Remove(certificates[j]);
                }
                throw;
            }
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            _storePal.Remove(certificate.Pal);
        }

        public void RemoveRange(X509Certificate2Collection certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

            int i = 0;
            try
            {
                foreach (X509Certificate2 certificate in certificates)
                {
                    Remove(certificate);
                    i++;
                }
            }
            catch
            {
                // For desktop compat, we keep the exception semantics even though they are not ideal
                // because an exception above may cause certs to be added even if they weren't there before
                // and an exception here may cause certs not to be re-added.
                for (int j = 0; j < i; j++)
                {
                    Add(certificates[j]);
                }
                throw;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            IStorePal storePal = _storePal;
            _storePal = null;
            if (storePal != null)
            {
                storePal.Dispose();
            }
        }
    }
}

