// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class PfxIterationCountTests_X509Certificate2 : PfxIterationCountTests
    {
        internal override X509Certificate Import(byte[] blob)
            => new X509Certificate2(blob);

        internal override X509Certificate Import(byte[] blob, string password)
            => new X509Certificate2(blob, password);

        internal override X509Certificate Import(byte[] blob, SecureString password)
            => new X509Certificate2(blob, password);

        internal override X509Certificate Import(string fileName)
            => new X509Certificate2(fileName);

        internal override X509Certificate Import(string fileName, string password)
            => new X509Certificate2(fileName, password);

        internal override X509Certificate Import(string fileName, SecureString password)
            => new X509Certificate2(fileName, password);
    }
}
