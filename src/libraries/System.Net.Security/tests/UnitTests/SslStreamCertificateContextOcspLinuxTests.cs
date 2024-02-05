// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Net.Security;

using Xunit;

namespace System.Net.Security.Tests;

public class SslStreamCertificateContextOcspLinuxTests
{
    [Fact]
    public async Task OfflineContext_NoFetchOcspResponse()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            intermediate.RevocationExpiration = null;

            SslStreamCertificateContext ctx = ctxFactory(true);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.Null(ocsp);
        });
    }

    [Fact]
    public async Task FetchOcspResponse_NoExpiration_Success()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            intermediate.RevocationExpiration = null;

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp);
        });
    }

    [Theory]
    [InlineData(PkiOptions.OcspEverywhere)]
    [InlineData(PkiOptions.OcspEverywhere | PkiOptions.IssuerAuthorityHasDesignatedOcspResponder)]
    public async Task FetchOcspResponse_WithExpiration_Success(PkiOptions pkiOptions)
    {
        await SimpleTest(pkiOptions, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.AddDays(1);

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp);

            // should cache and return the same
            byte[] ocsp2 = await ctx.GetOcspResponseAsync();
            Assert.Equal(ocsp, ocsp2);
        });
    }

    [Fact]
    public async Task FetchOcspResponse_Expired_ReturnsNull()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.AddMinutes(-5);

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.Null(ocsp);
        });
    }

    [Fact]
    public async Task FetchOcspResponse_FirstInvalidThenValid()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            responder.RespondKind = RespondKind.Invalid;

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.Null(ocsp);

            responder.RespondKind = RespondKind.Normal;
            ocsp = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp);
        });
    }

    [Fact]
    public async Task RefreshOcspResponse_BeforeExpiration()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            // Set the expiration to be in the future, but close enough that a refresh gets triggered
            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.Add(SslStreamCertificateContext.MinRefreshBeforeExpirationInterval);

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp);

            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.AddDays(1);

            // first call will dispatch a download and return the cached response, the first call after
            // the pending download finishes will return the updated response
            byte[] ocsp2 = ctx.GetOcspResponseNoWaiting();
            Assert.Equal(ocsp, ocsp2);

            await RetryHelper.ExecuteAsync(async () =>
            {
                byte[] ocsp3 = await ctx.GetOcspResponseAsync();
                Assert.NotNull(ocsp3);
                Assert.NotEqual(ocsp, ocsp3);
            }, maxAttempts: 5, backoffFunc: i => (i + 1) * 200 /* ms */);
        });
    }

    [Fact]
    public async Task RefreshOcspResponse_AfterExpiration()
    {
        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.AddSeconds(1);

            SslStreamCertificateContext ctx = ctxFactory(false);

            await Task.Delay(2000);

            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.AddDays(1);

            // The cached OCSP is expired, so the first call will dispatch a download and return the cached response,
            byte[] ocsp = ctx.GetOcspResponseNoWaiting();
            Assert.Null(ocsp);

            // subsequent call will return the new response
            byte[] ocsp2 = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp2);
        });
    }

    [Fact]
    [OuterLoop("Takes about 15 seconds")]
    public async Task RefreshOcspResponse_FirstInvalidThenValid()
    {
        Assert.True(SslStreamCertificateContext.MinRefreshBeforeExpirationInterval > SslStreamCertificateContext.RefreshAfterFailureBackOffInterval * 4, "Backoff interval is too long");

        await SimpleTest(PkiOptions.OcspEverywhere, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            // Set the expiration to be in the future, but close enough that a refresh gets triggered
            intermediate.RevocationExpiration = DateTimeOffset.UtcNow.Add(SslStreamCertificateContext.MinRefreshBeforeExpirationInterval);

            SslStreamCertificateContext ctx = ctxFactory(false);
            byte[] ocsp = await ctx.GetOcspResponseAsync();
            Assert.NotNull(ocsp);

            responder.RespondKind = RespondKind.Invalid;
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(SslStreamCertificateContext.RefreshAfterFailureBackOffInterval);
                byte[] ocsp2 = await ctx.GetOcspResponseAsync();
                Assert.Equal(ocsp, ocsp2);
            }

            // after responder comes back online, the staple is eventually refreshed
            responder.RespondKind = RespondKind.Normal;
            await RetryHelper.ExecuteAsync(async () =>
            {
                byte[] ocsp3 = await ctx.GetOcspResponseAsync();
                Assert.NotNull(ocsp3);
                Assert.NotEqual(ocsp, ocsp3);
            }, maxAttempts: 5, backoffFunc: i => (i + 1) * 200 /* ms */);
        });
    }

    private delegate Task RunSimpleTest(
        CertificateAuthority root,
        CertificateAuthority intermediate,
        X509Certificate2 endEntity,
        Func<bool, SslStreamCertificateContext> ctxFactory,
        RevocationResponder responder);

    private static async Task SimpleTest(
        PkiOptions pkiOptions,
        RunSimpleTest callback,
        [CallerMemberName] string callerName = null,
        bool pkiOptionsInTestName = true)
    {
        BuildPrivatePki(
            pkiOptions,
            out RevocationResponder responder,
            out CertificateAuthority root,
            out CertificateAuthority intermediate,
            out X509Certificate2 endEntity,
            callerName,
            pkiOptionsInSubject: pkiOptionsInTestName);

        using (responder)
        using (root)
        using (intermediate)
        using (endEntity)
        using (X509Certificate2 rootCert = root.CloneIssuerCert())
        using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
        {
            if (pkiOptions.HasFlag(PkiOptions.RootAuthorityHasDesignatedOcspResponder))
            {
                using (RSA tmpKey = RSA.Create())
                using (X509Certificate2 tmp = root.CreateOcspSigner(
                    BuildSubject("A Root Designated OCSP Responder", callerName, pkiOptions, true),
                    tmpKey))
                {
                    root.DesignateOcspResponder(tmp.CopyWithPrivateKey(tmpKey));
                }
            }

            if (pkiOptions.HasFlag(PkiOptions.IssuerAuthorityHasDesignatedOcspResponder))
            {
                using (RSA tmpKey = RSA.Create())
                using (X509Certificate2 tmp = intermediate.CreateOcspSigner(
                    BuildSubject("An Intermediate Designated OCSP Responder", callerName, pkiOptions, true),
                    tmpKey))
                {
                    intermediate.DesignateOcspResponder(tmp.CopyWithPrivateKey(tmpKey));
                }
            }

            X509Certificate2Collection additionalCerts = new();
            additionalCerts.Add(intermediateCert);
            additionalCerts.Add(rootCert);

            Func<bool, SslStreamCertificateContext> factory = offline => SslStreamCertificateContext.Create(
                endEntity,
                additionalCerts,
                offline,
                trust: null);

            await callback(root, intermediate, endEntity, factory, responder);
        }
    }

    internal static void BuildPrivatePki(
        PkiOptions pkiOptions,
        out RevocationResponder responder,
        out CertificateAuthority rootAuthority,
        out CertificateAuthority intermediateAuthority,
        out X509Certificate2 endEntityCert,
        [CallerMemberName] string testName = null,
        bool registerAuthorities = true,
        bool pkiOptionsInSubject = false)
    {
        bool issuerRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaCrl);
        bool issuerRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaOcsp);
        bool endEntityRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaCrl);
        bool endEntityRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaOcsp);

        Assert.True(
            issuerRevocationViaCrl || issuerRevocationViaOcsp ||
                endEntityRevocationViaCrl || endEntityRevocationViaOcsp,
            "At least one revocation mode is enabled");

        CertificateAuthority.BuildPrivatePki(pkiOptions, out responder, out rootAuthority, out intermediateAuthority, out endEntityCert, testName, registerAuthorities, pkiOptionsInSubject);
    }

    private static string BuildSubject(
        string cn,
        string testName,
        PkiOptions pkiOptions,
        bool includePkiOptions)
    {
        if (includePkiOptions)
        {
            return $"CN=\"{cn}\", O=\"{testName}\", OU=\"{pkiOptions}\"";
        }

        return $"CN=\"{cn}\", O=\"{testName}\"";
    }
}
