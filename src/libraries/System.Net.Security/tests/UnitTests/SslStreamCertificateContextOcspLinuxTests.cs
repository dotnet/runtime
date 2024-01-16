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

// [OuterLoop("Tests tests run long time")]
public class SslStreamCertificateContextOcspLinuxTests
{
    [Fact]
    public async Task Runs()
    {
        var pkiOptions = PkiOptions.None;
        await SimpleTest(pkiOptions, async (root, intermediate, endEntity, ctxFactory, responder) =>
        {
            var ctx = ctxFactory(false);
            var ocsp = await ctx.GetOcspResponseAsync();
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

            await RetryHelper.ExecuteAsync(() =>
            {
                return callback(root, intermediate, endEntity, factory, responder);
            });
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
