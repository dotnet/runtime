// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
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


        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Import_IterationCountLimitExceeded_ThrowsInAllottedTime()
        {
            const int AllottedTime = 5000;

            if (!PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException("Pkcs12NoPassword100MRounds uses PBES2, which is not supported on this version.");
            }

            RemoteInvokeOptions options = new()
            {
                TimeOut = AllottedTime
            };

            RemoteExecutor.Invoke(static () =>
            {
                byte[] blob = TestData.Pkcs12NoPassword100MRounds;
                CryptographicException ce = Assert.Throws<CryptographicException>(() => new X509Certificate2(blob));
                Assert.Contains(FwlinkId, ce.Message);
            }, options).Dispose();
        }
    }
}
