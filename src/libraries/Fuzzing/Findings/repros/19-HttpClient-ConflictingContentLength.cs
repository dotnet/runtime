// Finding: SocketsHttpHandler accepts an HTTP/1.1 response with two conflicting Content-Length headers and frames the body with
// the first one. RFC 9112 section 6.3 requires treating that as an unrecoverable error. The handler doesn't reuse the
// connection (leftover bytes), so no response desync was observed, but in Debug/Checked builds of System.Net.Http the internal
// ContentLength read trips Debug.Assert("Only a single parsed value should be stored for this parser").
// Run: dotnet run 19-HttpClient-ConflictingContentLength.cs
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
_ = Task.Run(async () =>
{
    while (true)
    {
        TcpClient connection = await listener.AcceptTcpClientAsync();
        _ = Task.Run(async () =>
        {
            NetworkStream stream = connection.GetStream();
            await stream.ReadAsync(new byte[4096]);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 3\r\nContent-Length: 10\r\n\r\nabcdefghij"));
            await Task.Delay(-1);
        });
    }
});

using var client = new HttpClient();
string outcome;
try
{
    using HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:{port}/");
    outcome = $"status {(int)response.StatusCode}, Content-Length headers [{string.Join(", ", response.Content.Headers.NonValidated["Content-Length"])}], body '{await response.Content.ReadAsStringAsync()}'";
}
catch (HttpRequestException ex) { outcome = "HttpRequestException: " + ex.Message; }

Console.WriteLine($"response with 'Content-Length: 3' and 'Content-Length: 10' -> {outcome}");
Console.WriteLine(outcome.StartsWith("status", StringComparison.Ordinal) ? "REPRODUCED: conflicting Content-Length accepted." : "NOT REPRODUCED");
