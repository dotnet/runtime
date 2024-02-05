// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public static class SslStreamCertificateContextTests
    {
        [Fact]
        [OuterLoop("Subject to resource contention and load.")]
        [PlatformSpecific(TestPlatforms.Linux)]
        public static async Task Create_OcspDoesNotReturnOrCacheInvalidStapleData()
        {
            string serverName = $"{nameof(Create_OcspDoesNotReturnOrCacheInvalidStapleData)}.example";

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp | PkiOptions.CrlEverywhere,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out X509Certificate2 serverCert,
                intermediateAuthorityCount: 1,
                subjectName: serverName,
                keySize: 2048,
                extensions: Configuration.Certificates.BuildTlsServerCertExtensions(serverName));

            using (responder)
            using (rootAuthority)
            using (CertificateAuthority intermediateAuthority = intermediateAuthorities[0])
            using (serverCert)
            using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
            using (X509Certificate2 issuerCert = intermediateAuthority.CloneIssuerCert())
            {
                responder.RespondKind = RespondKind.Invalid;

                SslStreamCertificateContext context = SslStreamCertificateContext.Create(
                    serverCert,
                    additionalCertificates: new X509Certificate2Collection { issuerCert },
                    offline: false);

                MethodInfo fetchOcspAsyncMethod = typeof(SslStreamCertificateContext).GetMethod(
                    "DownloadOcspAsync",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo ocspResponseField = typeof(SslStreamCertificateContext).GetField(
                    "_ocspResponse",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(fetchOcspAsyncMethod);
                Assert.NotNull(ocspResponseField);

                byte[] ocspFetch = await (ValueTask<byte[]>)fetchOcspAsyncMethod.Invoke(context, Array.Empty<object>());
                Assert.Null(ocspFetch);

                byte[] ocspResponseValue = (byte[])ocspResponseField.GetValue(context);
                Assert.Null(ocspResponseValue);
            }
        }
    }
}
