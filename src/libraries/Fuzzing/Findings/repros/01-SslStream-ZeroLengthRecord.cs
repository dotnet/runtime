// Finding (regression on main, not in 11.0 RC1): an SslStream server receiving the 5-byte zero-length TLS handshake record
// 16 03 01 00 00 throws IndexOutOfRangeException from ReceiveHandshakeFrameAsync. Since "Fix SslStream detection of
// exactly-5-byte TLS frames" (dotnet/runtime#132694) a 5-byte record counts as a complete frame, but the ClientHello checks still
// read the handshake type at offset 5.
// Needs a runtime built from main: see ../FINDINGS.md ("Findings on main only"). On 11.0 RC1 it prints NOT REPRODUCED.
// Run: dotnet run 01-SslStream-ZeroLengthRecord.cs
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using var rsa = RSA.Create(2048);
using X509Certificate2 certificate = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
    .CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));

using var server = new SslStream(new MemoryStream([0x16, 0x03, 0x01, 0x00, 0x00]));
string outcome;
try
{
    await server.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate });
    outcome = "no exception";
}
catch (Exception ex) { outcome = ex.GetType().Name + ": " + ex.Message; }

Console.WriteLine($"{Environment.Version}: AuthenticateAsServerAsync(16 03 01 00 00) -> {outcome}");
Console.WriteLine(outcome.StartsWith(nameof(IndexOutOfRangeException), StringComparison.Ordinal) ? "REPRODUCED: IndexOutOfRangeException from untrusted input." : "NOT REPRODUCED (expected IOException/AuthenticationException)");
