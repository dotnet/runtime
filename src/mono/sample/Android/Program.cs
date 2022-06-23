#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

TestAuthenticateAsClient(hostname: "microsoft.com");
TestAuthenticateAsClient(hostname: "self-signed.badssl.com");
TestAuthenticateAsClient(hostname: "expired.badssl.com");

void TestAuthenticateAsClient(string hostname, int port = 443)
{
    using var tcpClient = new TcpClient();
    IAsyncResult result = tcpClient.BeginConnect(hostname, port, null, null);
    var connected = result.AsyncWaitHandle.WaitOne(2000, true);
    if (connected)
    {
        tcpClient.EndConnect(result);
        var stream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null);
        try
        {
            stream.AuthenticateAsClient(hostname);

            Console.WriteLine($"OK - {hostname}:{port}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!! - {hostname}:{port} ({ex.Message})");
            Console.WriteLine(ex);
        }
    }
}

bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
{
    return true;
}
