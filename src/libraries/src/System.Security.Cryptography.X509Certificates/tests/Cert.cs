// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    //
    // Helper class centralizes all loading of PFX's. Loading PFX's is a problem because of the key on disk that it creates and gets left behind
    // if the certificate isn't properly disposed. Properly disposing PFX's imported into a X509Certificate2Collection is a pain because X509Certificate2Collection 
    // doesn't implement IDisposable. To make this easier, we wrap these in an ImportedCollection class that does implement IDisposable.
    //
    internal static class Cert
    {
        // netstandard: DefaultKeySet
        // netcoreapp-OSX: DefaultKeySet
        // netcoreapp-other: EphemeralKeySet
        internal static readonly X509KeyStorageFlags EphemeralIfPossible =
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? X509KeyStorageFlags.EphemeralKeySet :
            X509KeyStorageFlags.DefaultKeySet;
        //
        // The Import() methods have an overload for each X509Certificate2Collection.Import() overload.
        //

        // Do not refactor this into a call to Import(byte[], string, X509KeyStorageFlags). The test meant to exercise
        // the api that takes only one argument.
        public static ImportedCollection Import(byte[] rawData)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(rawData);
            return new ImportedCollection(collection);
        }

        public static ImportedCollection Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(rawData, password, keyStorageFlags);
            return new ImportedCollection(collection);
        }

        // Do not refactor this into a call to Import(string, string, X509KeyStorageFlags). The test meant to exercise
        // the api that takes only one argument.
        public static ImportedCollection Import(string fileName)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(fileName);
            return new ImportedCollection(collection);
        }

        public static ImportedCollection Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(fileName, password, keyStorageFlags);
            return new ImportedCollection(collection);
        }
    }

    //
    // Wraps an X509Certificate2Collection in an IDisposable for easier cleanup.
    //
    internal sealed class ImportedCollection : IDisposable
    {
        public ImportedCollection(X509Certificate2Collection collection)
        {
            // Make an independent copy of the certs to dispose (in case the test mutates the collection after we return.)
            _certs = new X509Certificate2[collection.Count];
            collection.CopyTo(_certs, 0);
            Collection = collection;
        }

        public X509Certificate2Collection Collection { get; }

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

        private X509Certificate2[] _certs;
    }
}

