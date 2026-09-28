// Finding: the managed HttpListener (Linux/macOS) accepts request targets that are none of the forms RFC 9112 allows
// (origin-form "/path", absolute-form, authority-form, "*"), such as "#frag" or "?x", and delivers them to the application with a
// synthesized Url that disagrees with RawUrl. In Debug/Checked builds of System.Net.HttpListener these requests trip Debug.Assert in
// HttpListenerRequestUriBuilder ("Request Uri string is not an absolute Uri, absolute path, or '*'"), and so does a valid
// absolute-form target without a path ("http://host:port", "'rawPath' must have at least one character"), so any client can abort
// such a server.
// Run: dotnet run 26-HttpListener-InvalidRequestTarget.cs   (on Linux or macOS)
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new HttpListener();
int port = Random.Shared.Next(20000, 60000);
listener.Prefixes.Add($"http://127.0.0.1:{port}/");
listener.Start();
var delivered = new List<string>();
_ = Task.Run(async () =>
{
    while (true)
    {
        HttpListenerContext context = await listener.GetContextAsync();
        lock (delivered) { delivered.Add(context.Request.RawUrl!); }
        Console.WriteLine($"    delivered to the application: RawUrl '{context.Request.RawUrl}', Url '{context.Request.Url}'");
        context.Response.Close();
    }
});

foreach (string target in new[] { "#frag", "?x=1", $"http://127.0.0.1:{port}", "/valid" })
{
    using var client = new TcpClient();
    client.Connect(IPAddress.Loopback, port);
    NetworkStream stream = client.GetStream();
    stream.Write(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));
    client.Client.Shutdown(SocketShutdown.Send);
    string statusLine = new StreamReader(stream).ReadLine() ?? "(connection closed)";
    await Task.Delay(100);
    Console.WriteLine($"GET {target} -> {statusLine}");
}

bool reproduced;
lock (delivered) { reproduced = delivered.Contains("#frag") || delivered.Contains("?x=1"); }
Console.WriteLine(reproduced ? "REPRODUCED: invalid request targets reach the application." : "NOT REPRODUCED");
