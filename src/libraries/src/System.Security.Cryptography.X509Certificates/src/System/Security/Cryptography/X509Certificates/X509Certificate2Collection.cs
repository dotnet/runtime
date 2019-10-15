// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography.Pal;
using Microsoft.Win32.SafeHandles;

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
                return (X509Certificate2)(base[index]);
            }
            set
            {
                base[index] = value;
            }
        }

        public int Add(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            return base.Add(certificate);
        }

        public void AddRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
                throw new ArgumentNullException(nameof(certificates));

            int i = 0;
            try
            {
                for (; i < certificates.Count; i++)
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

        public bool Contains(X509Certificate2 certificate)
        {
            // This method used to throw ArgumentNullException, but it has been deliberately changed
            // to no longer throw to match the behavior of X509CertificateCollection.Contains and the
            // IList.Contains implementation, which do not throw.

            return base.Contains(certificate);
        }

        public byte[] Export(X509ContentType contentType)
        {
            return Export(contentType, password: null);
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (IExportPal storePal = StorePal.LinkFromCertificateCollection(this))
            {
                return storePal.Export(contentType, safePasswordHandle);
            }
        }

        public X509Certificate2Collection Find(X509FindType findType, object findValue, bool validOnly)
        {
            if (findValue == null)
                throw new ArgumentNullException(nameof(findValue));

            return FindPal.FindFromCollection(this, findType, findValue, validOnly);
        }

        public new X509Certificate2Enumerator GetEnumerator()
        {
            return new X509Certificate2Enumerator(this);
        }

        public void Import(byte[] rawData)
        {
            Import(rawData, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (ILoaderPal storePal = StorePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        public void Import(string fileName)
        {
            Import(fileName, password: null, keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
        }

        public void Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            using (ILoaderPal storePal = StorePal.FromFile(fileName, safePasswordHandle, keyStorageFlags))
            {
                storePal.MoveTo(this);
            }
        }

        public void Insert(int index, X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            base.Insert(index, certificate);
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            base.Remove(certificate);
        }

        public void RemoveRange(X509Certificate2[] certificates)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));

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
                throw new ArgumentNullException(nameof(certificates));

            int i = 0;
            try
            {
                for (; i < certificates.Count; i++)
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
    }
}
