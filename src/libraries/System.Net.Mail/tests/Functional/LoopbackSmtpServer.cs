// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systen.Net.Mail.Tests
{
    public class LoopbackSmtpServer : IDisposable
    {
        private static readonly ReadOnlyMemory<byte> s_messageTerminator = "\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> s_bodyTerminator = "\r\n.\r\n"u8.ToArray();

        public bool ReceiveMultipleConnections = false;
        public bool SupportSmtpUTF8 = false;
        public bool AdvertiseNtlmAuthSupport = false;

        private bool _disposed = false;
        private readonly Socket _listenSocket;
        private readonly ConcurrentBag<Socket> _socketsToDispose;
        private long _messageCounter = Random.Shared.Next(1000, 2000);

        public readonly int Port;
        public SmtpClient CreateClient() => new SmtpClient("localhost", Port);

        public Action<Socket> OnConnected;
        public Action<string> OnHelloReceived;
        public Action<string, string> OnCommandReceived;
        public Action<string> OnUnknownCommand;
        public Action<Socket> OnQuitReceived;

        public string ClientDomain { get; private set; }
        public string MailFrom { get; private set; }
        public string MailTo { get; private set; }
        public string UsernamePassword { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string AuthMethodUsed { get; private set; }
        public ParsedMailMessage Message { get; private set; }

        public int ConnectionCount { get; private set; }
        public int MessagesReceived { get; private set; }

        public LoopbackSmtpServer()
        {
            _socketsToDispose = new ConcurrentBag<Socket>();
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socketsToDispose.Add(_listenSocket);

            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
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

            async ValueTask<string> ReceiveMessageAsync(bool isBody = false)
            {
                var terminator = isBody ? s_bodyTerminator : s_messageTerminator;
                int suffix = terminator.Length;

                int received = 0;
                do
                {
                    int read = await socket.ReceiveAsync(buffer.Slice(received), SocketFlags.None);
                    if (read == 0) return null;
                    received += read;
                }
                while (received < suffix || !buffer.Slice(received - suffix, suffix).Span.SequenceEqual(terminator.Span));

                MessagesReceived++;
                return Encoding.UTF8.GetString(buffer.Span.Slice(0, received - suffix));
            }
            async ValueTask SendMessageAsync(string text)
            {
                var bytes = buffer.Slice(0, Encoding.UTF8.GetBytes(text, buffer.Span) + 2);
                bytes.Span[^2] = (byte)'\r';
                bytes.Span[^1] = (byte)'\n';
                await socket.SendAsync(bytes, SocketFlags.None);
            }

            try
            {
                OnConnected?.Invoke(socket);
                await SendMessageAsync("220 localhost");

                string message = await ReceiveMessageAsync();
                Debug.Assert(message.ToLower().StartsWith("helo ") || message.ToLower().StartsWith("ehlo "));
                ClientDomain = message.Substring(5).ToLower();
                OnCommandReceived?.Invoke(message.Substring(0, 4), ClientDomain);
                OnHelloReceived?.Invoke(ClientDomain);

                await SendMessageAsync("250-localhost, mock server here");
                if (SupportSmtpUTF8) await SendMessageAsync("250-SMTPUTF8");
                await SendMessageAsync("250 AUTH PLAIN LOGIN" + (AdvertiseNtlmAuthSupport ? " NTLM" : ""));

                while ((message = await ReceiveMessageAsync()) != null)
                {
                    int colonIndex = message.IndexOf(':');
                    string command = colonIndex == -1 ? message : message.Substring(0, colonIndex);
                    string argument = command.Length == message.Length ? string.Empty : message.Substring(colonIndex + 1).Trim();

                    OnCommandReceived?.Invoke(command, argument);

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
                        else if (parts[1].Equals("NTLM", StringComparison.OrdinalIgnoreCase))
                        {
                            await SendMessageAsync("12345 I lied, I can't speak NTLM - here's an invalid response");
                        }
                        else await SendMessageAsync("504 scheme not supported");
                        continue;
                    }

                    switch (command.ToUpper())
                    {
                        case "MAIL FROM":
                            MailFrom = argument;
                            await SendMessageAsync("250 Ok");
                            break;

                        case "RCPT TO":
                            MailTo = argument;
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
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    socket?.Close();
                }
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

            private string GetHeader(string name) => Headers.TryGetValue(name, out string value) ? value : "NOT-PRESENT";
            public string From => GetHeader("From");
            public string To => GetHeader("To");
            public string Subject => GetHeader("Subject");

            private ParsedMailMessage(Dictionary<string, string> headers, string body)
            {
                Headers = headers;
                Body = body;
            }

            public static ParsedMailMessage Parse(string data)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                ReadOnlySpan<char> dataSpan = data;
                string body = null;

                while (!dataSpan.IsEmpty)
                {
                    int endOfLine = dataSpan.IndexOf('\n');
                    Debug.Assert(endOfLine != -1, "Expected valid \r\n terminated lines");
                    var line = dataSpan.Slice(0, endOfLine).TrimEnd('\r');

                    if (line.IsEmpty)
                    {
                        body = dataSpan.Slice(endOfLine + 1).TrimEnd(stackalloc char[] { '\r', '\n' }).ToString();
                        break;
                    }
                    else
                    {
                        int colon = line.IndexOf(':');
                        Debug.Assert(colon != -1, "Expected a valid header");
                        headers.Add(line.Slice(0, colon).Trim().ToString(), line.Slice(colon + 1).Trim().ToString());
                        dataSpan = dataSpan.Slice(endOfLine + 1);
                    }
                }

                return new ParsedMailMessage(headers, body);
            }
        }
    }
}
