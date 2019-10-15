// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs.Tests
{
    //
    // Wraps an X509Certificate2Collection in an IDisposable for easier cleanup.
    //
    internal sealed class ImportedCollection : IDisposable
    {
        private X509Certificate2[] _certs;
        public X509Certificate2Collection Collection { get; }

        public ImportedCollection(X509Certificate2Collection collection)
        {
            // Make an independent copy of the certs to dispose
            // (in case the test mutates the collection after we return.)
            _certs = new X509Certificate2[collection.Count];
            collection.CopyTo(_certs, 0);
            Collection = collection;
        }

        public void Dispose()
        {
            if (_certs != null)
            {
                foreach (X509Certificate2 cert in _certs)
                {
                    cert.Dispose();
                }
                _certs = null;
            }
        }

        public static ImportedCollection Import(byte[] data)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();
            coll.Import(data);
            return new ImportedCollection(coll);
        }

        public static ImportedCollection Import(
            byte[] data,
            string password,
            X509KeyStorageFlags keyStorageFlags)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();
            coll.Import(data, password, keyStorageFlags);
            return new ImportedCollection(coll);
        }
    }
}
