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
    public class X509Certificate2Collection : X509CertificateCollection
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
                return (X509Certificate2)(List[index]);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                List[index] = value;
            }
        }

        public int Add(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            return List.Add(certificate);
        }

        public void AddRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException("certificates");

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
                throw new ArgumentNullException("certificates");

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
                for (int j = 0; j < i; j++)
                {
                    Remove(certificates[j]);
                }
                throw;
            }
        }

        public bool Contains(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            return List.Contains(certificate);
        }

        public byte[] Export(X509ContentType contentType)
        {
            return Export(contentType, password: null);
        }

        public byte[] Export(X509ContentType contentType, String password)
        {
            using (IStorePal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.Export(contentType, password);
            }
        }

        public X509Certificate2Collection Find(X509FindType findType, Object findValue, bool validOnly)
        {
            if (findValue == null)
                throw new ArgumentNullException("findValue");

            X509Certificate2Collection collection = new X509Certificate2Collection();
            using (IStorePal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                storePal.FindAndCopyTo(findType, findValue, validOnly, collection);
            }
            return collection;
        }

        public new X509Certificate2Enumerator GetEnumerator()
        {
            X509CertificateEnumerator baseEnumerator = base.GetEnumerator();
            return new X509Certificate2Enumerator(baseEnumerator);
        }

        public void Import(byte[] rawData)
        {
            Import(rawData, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(byte[] rawData, String password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null)
                throw new ArgumentNullException("rawData");

            using (IStorePal storePal = StorePal.FromBlob(rawData, password, keyStorageFlags))
            {
                storePal.CopyTo(this);
            }
        }

        public void Import(String fileName)
        {
            Import(fileName, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(String fileName, String password, X509KeyStorageFlags keyStorageFlags)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            using (IStorePal storePal = StorePal.FromFile(fileName, password, keyStorageFlags))
            {
                storePal.CopyTo(this);
            }
        }

        public void Insert(int index, X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            List.Insert(index, certificate);
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            List.Remove(certificate);
        }

        public void RemoveRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException("certificates");

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
                throw new ArgumentNullException("certificates");

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
                for (int j = 0; j < i; j++)
                {
                    Add(certificates[j]);
                }
                throw;
            }
        }
    }
}

