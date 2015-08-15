// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            : this("MY", StoreLocation.CurrentUser)
        {
        }

        public X509Store(StoreName storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, "storeLocation"));

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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, "storeName"));
            }

            Location = storeLocation;
            return;
        }

        public X509Store(String storeName, StoreLocation storeLocation)
        {
            if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, "storeLocation"));

            Location = storeLocation;
            Name = storeName;
            return;
        }

        public StoreLocation Location { get; private set; }

        public String Name { get; private set; }


        public void Open(OpenFlags flags)
        {
            Close();
            _storePal = StorePal.FromSystemStore(Name, Location, flags);
            return;
        }

        public X509Certificate2Collection Certificates
        {
            get
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                if (_storePal != null)
                {
                    _storePal.CopyTo(certificates);
                }
                return certificates;
            }
        }

        public void Add(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            _storePal.Add(certificate.Pal);
            return;
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            if (_storePal == null)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);

            _storePal.Remove(certificate.Pal);
            return;
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

        private IStorePal _storePal = null;
    }
}

