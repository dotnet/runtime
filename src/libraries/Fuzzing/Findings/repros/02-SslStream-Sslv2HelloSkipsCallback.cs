// Finding (regression on main, not in 11.0 RC1): an SslStream server using the ServerOptionsSelectionCallback overload that
// receives the 5-byte SSLv2-framed hello 00 01 01 03 03 skips the callback and throws NotSupportedException ("The server mode SSL
// must use a certificate with the associated private key") instead of IOException/AuthenticationException.
// TlsFrameHelper.TryGetFrameInfo reads the handshake type at the TLS offset (5) even for SSLv2 frames (offset 2), so the frame is
// not recognized as a ClientHello, while SslStream's first-frame check (dotnet/runtime#126352) uses the right offset and lets it
// through; dotnet/runtime#132694 made this exactly-5-byte frame reachable.
// Needs a runtime built from main: see ../FINDINGS.md ("Findings on main only"). On 11.0 RC1 it prints NOT REPRODUCED.
// Run: dotnet run 02-SslStream-Sslv2HelloSkipsCallback.cs
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using var rsa = RSA.Create(2048);
using X509Certificate2 certificate = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
    .CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));

bool callbackInvoked = false;
using var server = new SslStream(new MemoryStream([0x00, 0x01, 0x01, 0x03, 0x03]));
string outcome;
try
{
    await server.AuthenticateAsServerAsync((_, _, _, _) =>
    {
        callbackInvoked = true;
        return ValueTask.FromResult(new SslServerAuthenticationOptions { ServerCertificate = certificate });
    }, null);
    outcome = "no exception";
}
catch (Exception ex) { outcome = ex.GetType().Name + ": " + ex.Message; }

Console.WriteLine($"{Environment.Version}: AuthenticateAsServerAsync(callback, 00 01 01 03 03) -> callback invoked {callbackInvoked}, {outcome}");
Console.WriteLine(outcome.StartsWith(nameof(NotSupportedException), StringComparison.Ordinal) ? "REPRODUCED: the options callback is skipped." : "NOT REPRODUCED (expected IOException/AuthenticationException)");
