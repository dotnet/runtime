// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class PfxIterationCountTests_X509Certificate2Collection : PfxIterationCountTests
    {
        internal override X509Certificate Import(byte[] blob)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(blob);
            return collection[0];
        }

        internal override X509Certificate Import(byte[] blob, string password)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(blob, password, X509KeyStorageFlags.DefaultKeySet);
            return collection[0];
        }

        // X509Certificate2Collection.Import does not support SecureString so we just make this work.
        internal override X509Certificate Import(byte[] blob, SecureString password)
            => new X509Certificate2(blob, password);

        internal override X509Certificate Import(string fileName)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(fileName);
            return collection[0];
        }

        internal override X509Certificate Import(string fileName, string password)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(fileName, password, X509KeyStorageFlags.DefaultKeySet);
            return collection[0];
        }

        // X509Certificate2Collection.Import does not support SecureString so we just make this work.
        internal override X509Certificate Import(string fileName, SecureString password)
            => new X509Certificate2(fileName, password);
    }
}
