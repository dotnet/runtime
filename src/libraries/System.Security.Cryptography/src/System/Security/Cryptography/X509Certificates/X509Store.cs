// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Store : IDisposable
    {
        internal const string RootStoreName = "Root";
        internal const string IntermediateCAStoreName = "CA";
        internal const string DisallowedStoreName = "Disallowed";
        internal const string MyStoreName = "My";

        private IStorePal? _storePal;

        public X509Store()
            : this("MY", StoreLocation.CurrentUser)
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
            : this("MY", storeLocation)
        {
        }

        public X509Store(StoreName storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(storeLocation)));

            Name = storeName switch
            {
                StoreName.AddressBook => "AddressBook",
                StoreName.AuthRoot => "AuthRoot",
                StoreName.CertificateAuthority => IntermediateCAStoreName,
                StoreName.Disallowed => DisallowedStoreName,
                StoreName.My => MyStoreName,
                StoreName.Root => RootStoreName,
                StoreName.TrustedPeople => "TrustedPeople",
                StoreName.TrustedPublisher => "TrustedPublisher",
                _ => throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(storeName))),
            };
            Location = storeLocation;
        }

        public X509Store(StoreName storeName, StoreLocation storeLocation, OpenFlags flags)
            : this(storeName, storeLocation)
        {
            Open(flags);
        }

        public X509Store(string storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(storeLocation)));

            Location = storeLocation;
            Name = storeName;
        }

        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, System.Security.Cryptography.X509Certificates.OpenFlags flags)
            : this(storeName, storeLocation)
        {
            Open(flags);
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

        public string? Name { get; private set; }


        public void Open(OpenFlags flags)
        {
            Close();
            _storePal = StorePal.FromSystemStore(Name!, Location, flags);
        }

        public X509Certificate2Collection Certificates
        {
            get
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                _storePal?.CloneTo(certificates);
                return certificates;
            }
        }

        public bool IsOpen
        {
            get { return _storePal != null; }
        }

        public void Add(X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            if (certificate.Pal == null)
                throw new CryptographicException(SR.Cryptography_InvalidHandle, "pCertContext");

            _storePal.Add(certificate.Pal);
        }

        public void AddRange(X509Certificate2Collection certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

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
                // For .NET Framework compat, we keep the exception semantics even though they are not ideal
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
            ArgumentNullException.ThrowIfNull(certificate);

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            if (certificate.Pal == null)
                return;

            _storePal.Remove(certificate.Pal);
        }

        public void RemoveRange(X509Certificate2Collection certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

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
                // For .NET Framework compat, we keep the exception semantics even though they are not ideal
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
            IStorePal? storePal = _storePal;
            if (storePal != null)
            {
                _storePal = null;
                storePal.Dispose();
            }
        }
    }
}
