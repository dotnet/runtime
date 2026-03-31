// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class SslStreamClientHelloFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => ["System.Net.Security"];

        public string[] TargetCoreLibPrefixes => [];

        public static SslStreamCertificateContext s_certCtx = GenerateSelfSignedCertificate();

        public static SslStreamCertificateContext GenerateSelfSignedCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
            if (OperatingSystem.IsWindows())
            {
                // On Windows, the returned certificate doesn't have the private key marked as exportable, which causes issues when used in SslStream.
                // Re-importing it as exportable resolves the issue.
                cert = X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
            }

            return SslStreamCertificateContext.Create(cert, new X509Certificate2Collection());
        }

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                bytes.CopyTo(buffer);
                using MemoryStream ms = new MemoryStream(buffer, 0, bytes.Length);
                using SslStream sslStream = new SslStream(ms);
                sslStream.AuthenticateAsServerAsync((stream, clientHelloInfo, b, token) =>
                {
                    // after this point, the comms are encrypted anyway, so
                    // there is no point fuzzing it
                    throw new MyCustomException();
                }, null).GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is AuthenticationException or IOException or MyCustomException)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        internal class MyCustomException : Exception
        {
        }
    }
}
