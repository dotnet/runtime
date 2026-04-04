// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public class LoopbackSmtpServer : IDisposable
    {
        private static readonly ReadOnlyMemory<byte> s_messageTerminator = "\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> s_bodyTerminator = "\r\n.\r\n"u8.ToArray();

        public bool ReceiveMultipleConnections = false;
        public bool SupportSmtpUTF8 = false;
        public bool AdvertiseNtlmAuthSupport = false;
        public bool AdvertiseGssapiAuthSupport = false;
        public SslServerAuthenticationOptions? SslOptions { get; set; }
        public NetworkCredential ExpectedGssapiCredential { get; set; }

        private ITestOutputHelper? _output;
        private bool _disposed = false;
        private readonly Socket _listenSocket;
        private readonly ConcurrentBag<Socket> _socketsToDispose;
        private long _messageCounter = Random.Shared.Next(1000, 2000);

        public readonly int Port;
        public SmtpClient CreateClient() => new SmtpClient("localhost", Port);

        public Action<Socket> OnConnected;
        public Action<string> OnHelloReceived;
        public Func<string, string, string?> OnCommandReceived;
        public Action<string> OnUnknownCommand;
        public Action<Socket> OnQuitReceived;

        public string ClientDomain { get; private set; }
        public string MailFrom { get; private set; }
        public List<string> MailTo { get; private set; } = new List<string>();
        public string UsernamePassword { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string AuthMethodUsed { get; private set; }
        public ParsedMailMessage Message { get; private set; }
        public bool IsEncrypted { get; private set; }
        public string TlsHostName { get; private set; }

        public int ConnectionCount { get; private set; }
        public int MessagesReceived { get; private set; }

        public LoopbackSmtpServer(ITestOutputHelper? output = null)
        {
            _output = output;
            _socketsToDispose = new ConcurrentBag<Socket>();
            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socketsToDispose.Add(_listenSocket);

            // if dual socket supported, bind to Ipv6Any, otherwise Any
            _listenSocket.Bind(new IPEndPoint(_listenSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));
            Port = ((IPEndPoint)_listenSocket.LocalEndPoint).Port;
            _listenSocket.Listen(1);

            _ = Task.Run(async () =>
            {
                do
                {
                    var socket = await _listenSocket.AcceptAsync();
                    _socketsToDispose.Add(socket);
                    ConnectionCount++;
                    _ = Task.Run(async () => await HandleConnectionAsync(socket));
                }
                while (ReceiveMultipleConnections);
            });
        }

        private async Task HandleConnectionAsync(Socket socket)
        {
            var buffer = new byte[1024].AsMemory();
            Stream stream = new NetworkStream(socket);

            string lastTag = string.Empty;
            void LogMessage(string tag, string message)
            {
                StringReader reader = new(message);
                while (reader.ReadLine() is string line)
                {
                    tag = tag == lastTag ? "      " : tag;
                    _output?.WriteLine($"{tag}> {line}");
                    lastTag = tag;
                }
            }

            async ValueTask<string> ReceiveMessageAsync(bool isBody = false)
            {
                var terminator = isBody ? s_bodyTerminator : s_messageTerminator;
                int suffix = terminator.Length;

                int received = 0;
                do
                {
                    int read = await stream.ReadAsync(buffer.Slice(received));

                    if (read == 0) return null;
                    received += read;
                }
                while (received < suffix || !buffer.Slice(received - suffix, suffix).Span.SequenceEqual(terminator.Span));

                MessagesReceived++;
                string message = Encoding.UTF8.GetString(buffer.Span.Slice(0, received - suffix));
                LogMessage("Client", Encoding.UTF8.GetString(buffer.Span.Slice(0, received)));
                return message;
            }

            async ValueTask SendMessageAsync(string text)
            {
                var bytes = buffer.Slice(0, Encoding.UTF8.GetBytes(text, buffer.Span) + 2);
                bytes.Span[^2] = (byte)'\r';
                bytes.Span[^1] = (byte)'\n';

                LogMessage("Server", text + "\r\n");
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }

            try
            {
                OnConnected?.Invoke(socket);
                await SendMessageAsync("220 localhost");
                bool isFirstMessage = true;

                while (await ReceiveMessageAsync() is string message && message != null)
                {
                    Debug.Assert(!isFirstMessage || (message.ToLower().StartsWith("helo ") || message.ToLower().StartsWith("ehlo ")), "Expected the first message to be HELO/EHLO");
                    isFirstMessage = false;

                    if (message.ToLower().StartsWith("helo ") || message.ToLower().StartsWith("ehlo "))
                    {
                        ClientDomain = message.Substring(5).ToLower();

                        if (OnCommandReceived?.Invoke(message.Substring(0, 4), ClientDomain) is string reply)
                        {
                            await SendMessageAsync(reply);
                            continue;
                        }

                        OnHelloReceived?.Invoke(ClientDomain);

                        await SendMessageAsync("250-localhost, mock server here");
                        if (SupportSmtpUTF8) await SendMessageAsync("250-SMTPUTF8");
                        if (SslOptions != null && stream is not SslStream) await SendMessageAsync("250-STARTTLS");
                        await SendMessageAsync(
                                                "250 AUTH PLAIN LOGIN" +
                                                (AdvertiseNtlmAuthSupport ? " NTLM" : "") +
                                                (AdvertiseGssapiAuthSupport ? " GSSAPI" : ""));

                        continue;
                    }

                    int colonIndex = message.IndexOf(':');
                    string command = colonIndex == -1 ? message : message.Substring(0, colonIndex);
                    string argument = command.Length == message.Length ? string.Empty : message.Substring(colonIndex + 1).Trim();

                    if (OnCommandReceived?.Invoke(command, argument) is string response)
                    {
                        await SendMessageAsync(response);
                        continue;
                    }

                    if (command.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = command.Split(' ');
                        Debug.Assert(parts.Length > 1, "Expected an actual auth request");

                        AuthMethodUsed = parts[1];

                        if (parts[1].Equals("LOGIN", StringComparison.OrdinalIgnoreCase))
                        {
                            if (parts.Length == 2)
                            {
                                await SendMessageAsync("334 VXNlcm5hbWU6");
                                Username = Encoding.UTF8.GetString(Convert.FromBase64String(await ReceiveMessageAsync()));
                            }
                            else
                            {
                                Username = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                            }
                            await SendMessageAsync("334 UGFzc3dvcmQ6");
                            Password = Encoding.UTF8.GetString(Convert.FromBase64String(await ReceiveMessageAsync()));
                            UsernamePassword = Username + Password;
                            await SendMessageAsync("235 Authentication successful");
                        }
                        else if (parts[1].Equals("GSSAPI", StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Assert(ExpectedGssapiCredential != null);
                            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(ExpectedGssapiCredential) { ForceNegotiateVersion = true };
                            FakeNegotiateServer fakeNegotiateServer = new FakeNegotiateServer(fakeNtlmServer);

                            try
                            {
                                // Do the authentication loop
                                byte[]? incomingBlob = Convert.FromBase64String(parts[2]);
                                byte[]? outgoingBlob;
                                do
                                {
                                    outgoingBlob = fakeNegotiateServer.GetOutgoingBlob(incomingBlob);
                                    if (outgoingBlob != null)
                                    {
                                        await SendMessageAsync("334 " + Convert.ToBase64String(outgoingBlob));
                                        incomingBlob = Convert.FromBase64String(await ReceiveMessageAsync());
                                    }
                                }
                                while (!fakeNegotiateServer.IsAuthenticated);

                                // Negotiate the SASL protection (no encryption and no signing)
                                byte[] saslToken = new byte[] { 1, 0, 0, 0 };
                                outgoingBlob = new byte[20]; // 16 bytes of NTLM signature, 4 bytes of content
                                fakeNtlmServer.Wrap(saslToken, outgoingBlob);
                                await SendMessageAsync("334 " + Convert.ToBase64String(outgoingBlob));
                                incomingBlob = Convert.FromBase64String(await ReceiveMessageAsync());
                                fakeNtlmServer.Unwrap(incomingBlob, saslToken);
                                // TODO: Verify the token we got back

                                await SendMessageAsync("235 Authentication successful");
                            }
                            catch (Exception e)
                            {
                                await SendMessageAsync("500 Unsuccessful authentication: " + e.ToString());
                            }
                        }
                        else if (parts[1].Equals("NTLM", StringComparison.OrdinalIgnoreCase))
                        {
                            await SendMessageAsync("12345 I lied, I can't speak NTLM - here's an invalid response");
                        }
                        else await SendMessageAsync("504 scheme not supported");
                        continue;
                    }

                    switch (command.ToUpper())
                    {
                        case "STARTTLS":
                            if (SslOptions == null || stream is SslStream)
                            {
                                await SendMessageAsync("454 TLS not available");
                                break;
                            }
                            await SendMessageAsync("220 Ready to start TLS");

                            // Upgrade connection to TLS
                            var sslStream = new SslStream(stream);
                            await sslStream.AuthenticateAsServerAsync(SslOptions);
                            IsEncrypted = true;
                            TlsHostName = sslStream.TargetHostName;

                            stream = sslStream;
                            break;

                        case "MAIL FROM":
                            MailFrom = argument;
                            MailTo.Clear();
                            Message = null;
                            await SendMessageAsync("250 Ok");
                            break;

                        case "RCPT TO":
                            MailTo.Add(argument);
                            await SendMessageAsync("250 Ok");
                            break;

                        case "DATA":
                            await SendMessageAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                            string data = await ReceiveMessageAsync(true);
                            Message = ParsedMailMessage.Parse(data);
                            await SendMessageAsync("250 Ok: queued as " + Interlocked.Increment(ref _messageCounter));
                            break;

                        case "QUIT":
                            OnQuitReceived?.Invoke(socket);
                            await SendMessageAsync("221 Bye");
                            return;

                        default:
                            OnUnknownCommand?.Invoke(message);
                            await SendMessageAsync("500 Idk that command");
                            break;
                    }
                }
            }
            catch { }
            finally
            {
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var socket in _socketsToDispose)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch { }
                }
                _socketsToDispose.Clear();
            }
        }

        public class ParsedMailMessage
        {
            public readonly IReadOnlyDictionary<string, string> Headers;
            public readonly string Body;
            public readonly string RawBody;
            public readonly List<ParsedAttachment> Attachments;

            private string GetHeader(string name) => Headers.TryGetValue(name, out string value) ? value : "NOT-PRESENT";
            public string From => GetHeader("From");
            public string To => GetHeader("To");
            public string Cc => GetHeader("Cc");
            public string Subject => GetHeader("Subject");

            public ContentType ContentType => field ??= new ContentType(GetHeader("Content-Type"));

            private ParsedMailMessage(Dictionary<string, string> headers, string body, string rawBody, List<ParsedAttachment> attachments)
            {
                Headers = headers;
                Body = body;
                RawBody = rawBody;
                Attachments = attachments;
            }

            private static (Dictionary<string, string> headers, string content) ParseContent(ReadOnlySpan<char> data)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                List<ParsedAttachment> attachments = new List<ParsedAttachment>();

                string body = null;

                // Parse headers with support for folded lines
                string currentHeaderName = null;
                StringBuilder currentHeaderValue = null;

                while (!data.IsEmpty)
                {
                    int endOfLine = data.IndexOf('\n');
                    Debug.Assert(endOfLine != -1, "Expected valid \r\n terminated lines");
                    var line = data.Slice(0, endOfLine).TrimEnd('\r');

                    if (line.IsEmpty)
                    {
                        // End of headers section - add the last header if there is one
                        if (currentHeaderName != null && currentHeaderValue != null)
                        {
                            headers.Add(currentHeaderName, currentHeaderValue.ToString().Trim());
                        }

                        body = data.Slice(endOfLine + 1).TrimEnd(stackalloc char[] { '\r', '\n' }).ToString();
                        break;
                    }
                    else if ((line[0] == ' ' || line[0] == '\t') && currentHeaderName != null)
                    {
                        // This is a folded line, append it to the current header value
                        currentHeaderValue.Append(' ').Append(line.ToString().TrimStart());
                    }
                    else // new header
                    {
                        // If we have a header being built, add it now
                        if (currentHeaderName != null && currentHeaderValue != null)
                        {
                            headers.Add(currentHeaderName, currentHeaderValue.ToString().Trim());
                        }

                        // Start a new header
                        int colon = line.IndexOf(':');
                        Debug.Assert(colon != -1, "Expected a valid header");
                        currentHeaderName = line.Slice(0, colon).Trim().ToString();
                        currentHeaderValue = new StringBuilder(line.Slice(colon + 1).ToString());
                    }

                    data = data.Slice(endOfLine + 1);
                }

                return (headers, body);
            }

            public static ParsedMailMessage Parse(string data)
            {
                List<ParsedAttachment> attachments = new List<ParsedAttachment>();
                string rawBody;
                (Dictionary<string, string> headers, string body) = ParseContent(data);
                rawBody = body;

                // Check if this is a multipart message
                string contentType = headers.TryGetValue("Content-Type", out string ct) ? ct : string.Empty;
                if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the boundary
                    string boundary = ExtractBoundary(contentType);
                    if (!string.IsNullOrEmpty(boundary))
                    {
                        // Parse multipart body
                        (attachments, body) = ParseMultipartBody(body, boundary);
                    }
                }

                return new ParsedMailMessage(headers, body, rawBody, attachments);
            }

            private static string ExtractBoundary(string contentType)
            {
                int boundaryIndex = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
                if (boundaryIndex < 0)
                    return null;

                string boundaryPart = contentType.Substring(boundaryIndex + 9);
                if (boundaryPart.StartsWith("\""))
                {
                    int endQuote = boundaryPart.IndexOf("\"", 1);
                    if (endQuote > 0)
                        return boundaryPart.Substring(1, endQuote - 1);
                }
                else
                {
                    int endBoundary = boundaryPart.IndexOfAny(new[] { ';', ' ' });
                    return endBoundary > 0 ? boundaryPart.Substring(0, endBoundary) : boundaryPart;
                }

                return null;
            }

            private static (List<ParsedAttachment> attachments, string textBody) ParseMultipartBody(string body, string boundary)
            {
                string textBody = null;
                List<ParsedAttachment> attachments = new List<ParsedAttachment>();

                string[] parts = body.Split(new[] { "--" + boundary }, StringSplitOptions.None);

                Debug.Assert(string.IsNullOrWhiteSpace(parts[0]), "Expected empty first part");
                Debug.Assert(parts[^1] == "--", "Expected empty last part");
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    string part = parts[i];

                    Debug.Assert(part.StartsWith("\r\n"));

                    (Dictionary<string, string> headers, string content) = ParseContent(part[2..]);

                    ContentType contentType = new ContentType(headers["Content-Type"]);

                    // Check if this part is an attachment
                    if (headers.TryGetValue("Content-Disposition", out string disposition) &&
                        disposition.StartsWith("attachment", StringComparison.OrdinalIgnoreCase))
                    {
                        attachments.Add(new ParsedAttachment
                        {
                            ContentType = contentType,
                            RawContent = content,
                            Headers = headers
                        });
                    }

                    // Check if this is a text part
                    else if (contentType.MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
                             textBody == null)
                    {
                        textBody = content;
                    }
                }

                return (attachments, textBody ?? "");
            }
        }

        public class ParsedAttachment
        {
            public ContentType ContentType { get; set; }
            public string RawContent { get; set; }
            public IDictionary<string, string> Headers { get; set; }

            private string GetHeader(string name) => Headers.TryGetValue(name, out string value) ? value : "NOT-PRESENT";
            public string ContentTransferEncoding => GetHeader("Content-Transfer-Encoding");
        }
    }
}
