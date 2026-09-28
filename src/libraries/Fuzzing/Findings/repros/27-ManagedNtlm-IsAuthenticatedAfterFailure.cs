// Finding: the managed NTLM client (default on macOS, iOS, Android and OpenBSD; opt-in on Linux via the
// System.Net.Security.UseManagedNtlm switch) sets _isAuthenticated = true before it parses the server's CHALLENGE message and never
// resets it when the challenge is rejected. After GetOutgoingBlob returns InvalidToken, IsAuthenticated and IsSigned report true,
// RemoteIdentity returns the target name, and ComputeIntegrityCheck throws NullReferenceException.
// Run: dotnet run 27-ManagedNtlm-IsAuthenticatedAfterFailure.cs
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Text;

AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);

using var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
{
    Package = "NTLM",
    Credential = new NetworkCredential("user", "Pa$$w0rd", "DOMAIN"),
    TargetName = "HTTP/server.example.com",
    RequiredProtectionLevel = ProtectionLevel.Sign,
});

client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out _);
// A server CHALLENGE message whose flags lack what the client requires (e.g. NTLMv2 session security), so it's rejected.
client.GetOutgoingBlob(Challenge(flags: 0x00000205), out NegotiateAuthenticationStatusCode status);
Console.WriteLine($"GetOutgoingBlob(challenge) -> {status}");
Console.WriteLine($"IsAuthenticated = {client.IsAuthenticated}, IsSigned = {client.IsSigned}, RemoteIdentity = {client.RemoteIdentity.Name}");
string integrity;
try { client.ComputeIntegrityCheck("message"u8, new ArrayBufferWriter<byte>()); integrity = "ok"; }
catch (Exception ex) { integrity = ex.GetType().Name; }
Console.WriteLine($"ComputeIntegrityCheck -> {integrity}");

Console.WriteLine(status != NegotiateAuthenticationStatusCode.Completed && client.IsAuthenticated ? "REPRODUCED: a rejected challenge leaves the context 'authenticated'." : "NOT REPRODUCED");

static byte[] Challenge(uint flags)
{
    byte[] targetName = Encoding.Unicode.GetBytes("DOMAIN");
    var message = new List<byte>();
    message.AddRange("NTLMSSP\0"u8.ToArray());
    message.AddRange(BitConverter.GetBytes(2));                             // MessageType = CHALLENGE
    message.AddRange(BitConverter.GetBytes((ushort)targetName.Length));    // TargetNameFields
    message.AddRange(BitConverter.GetBytes((ushort)targetName.Length));
    message.AddRange(BitConverter.GetBytes(56));
    message.AddRange(BitConverter.GetBytes(flags));                         // NegotiateFlags
    message.AddRange(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });                // ServerChallenge
    message.AddRange(new byte[8]);                                          // Reserved
    message.AddRange(new byte[8]);                                          // TargetInfoFields (empty)
    message.AddRange(new byte[8]);                                          // Version
    message.AddRange(targetName);
    return message.ToArray();
}
