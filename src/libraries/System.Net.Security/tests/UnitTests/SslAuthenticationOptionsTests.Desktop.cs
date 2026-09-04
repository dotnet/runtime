// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Net.Security.Tests
{
    public partial class SslAuthenticationOptionsTests
    {
        // This test exercises SslStreamCertificateContext.Create with intermediate certificates, which
        // triggers a generic Dictionary instantiation whose IL body is stripped from the Apple-mobile
        // CoreCLR ReadyToRun composite image, crashing the test app at startup. Compile it on desktop
        // platforms only.
        [Fact]
        public void UpdateOptions_ServerCertificateContextProvided_DoesNotDisposeCallerContext()
        {
            // Build a certificate chain: root → intermediate → leaf.
            // Use ECDSA P-256 keys: fast and accepted on FIPS-mode platforms.
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rootReq = new CertificateRequest("CN=TestRoot", rootKey, HashAlgorithmName.SHA256);
            rootReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using X509Certificate2 rootCert = rootReq.CreateSelfSigned(now.AddDays(-1), now.AddDays(365));

            using ECDsa intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var intermediateReq = new CertificateRequest("CN=TestIntermediate", intermediateKey, HashAlgorithmName.SHA256);
            intermediateReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using X509Certificate2 intermediatePub = intermediateReq.Create(rootCert, now.AddDays(-1), now.AddDays(365), new byte[] { 1 });
            using X509Certificate2 intermediateWithKey = intermediatePub.CopyWithPrivateKey(intermediateKey);

            using ECDsa leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var leafReq = new CertificateRequest("CN=TestLeaf", leafKey, HashAlgorithmName.SHA256);
            using X509Certificate2 leafPub = leafReq.Create(intermediateWithKey, now.AddDays(-1), now.AddDays(365), new byte[] { 2 });
            using X509Certificate2 leafWithKey = leafPub.CopyWithPrivateKey(leafKey);

            // Create a caller-owned context with an intermediate certificate
            SslStreamCertificateContext callerContext = SslStreamCertificateContext.Create(
                leafWithKey,
                new X509Certificate2Collection { intermediateWithKey },
                offline: true);

            // Simulate first UpdateOptions call: bare ServerCertificate creates and owns a context
            var options = new SslAuthenticationOptions();
            SslStreamCertificateContext ownedContext = SslStreamCertificateContext.Create(
                leafWithKey,
                new X509Certificate2Collection { intermediateWithKey },
                offline: true);
            // Capture the internal intermediate cert object before it is released
            Assert.NotEmpty(ownedContext.IntermediateCertificates);
            X509Certificate2 ownedIntermediate = ownedContext.IntermediateCertificates[0];
            options.CertificateContext = ownedContext;
            options.OwnsCertificateContext = true;

            // Simulate second UpdateOptions call: caller provides a ServerCertificateContext.
            // Before the fix, OwnsCertificateContext was not reset to false here, causing Dispose()
            // to incorrectly call ReleaseResources() on the caller-owned context.
            options.UpdateOptions(new SslServerAuthenticationOptions
            {
                ServerCertificateContext = callerContext,
            });

            Assert.False(options.OwnsCertificateContext);
            Assert.Same(callerContext, options.CertificateContext);

            // UpdateOptions should have released the previously-owned context's resources.
            // After X509Certificate2.Dispose(), Handle returns IntPtr.Zero.
            Assert.NotNull(ownedIntermediate);
            Assert.Equal(IntPtr.Zero, ownedIntermediate.Handle);

            // Dispose should NOT release the caller's context
            options.Dispose();

            // Verify that the caller's intermediate certificates were not disposed
            Assert.Equal(1, callerContext.IntermediateCertificates.Count);
            // Export() requires the native certificate handle; a CryptographicException would indicate the certificate was incorrectly disposed.
            foreach (X509Certificate2 cert in callerContext.IntermediateCertificates)
            {
                Assert.Null(Record.Exception(() => cert.Export(X509ContentType.Cert)));
            }
        }
    }
}
