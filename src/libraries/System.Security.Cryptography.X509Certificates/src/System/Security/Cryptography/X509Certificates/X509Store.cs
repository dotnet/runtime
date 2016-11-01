// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Store : IDisposable
    {
        public X509Store()
            : this(StoreName.My, StoreLocation.CurrentUser)
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

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            _storePal.Remove(certificate.Pal);
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            IStorePal storePal = _storePal;
            _storePal = null;
            if (storePal != null)
            {
                storePal.Dispose();
            }
        }

        private IStorePal _storePal;
    }
}

