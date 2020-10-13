// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.Managed.Internal.Tls.OpenSsl;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal sealed class MockTls : ITls
    {
        // magic bytes to distinguish this implementation from the other TLS implementations
        private static readonly byte[] _magicBytes = Encoding.UTF8.GetBytes(".NET QUIC mock TLS");

        private static readonly Random _random = new Random();

        private static byte[] GenerateRandomSecret()
        {
            var secret = new byte[32];

            lock (_random)
            {
                _random.NextBytes(secret);
            }

            return secret;
        }

        private readonly byte[] _handshakeWriteSecret = GenerateRandomSecret();
        private readonly byte[] _applicationWriteSecret = GenerateRandomSecret();

        private readonly ManagedQuicConnection _connection;
        private readonly TransportParameters _localTransportParams;
        private TransportParameters? _remoteTransportParams;
        private readonly List<SslApplicationProtocol> _alpn;

        private ArrayBuffer _recvBufferInitial = new ArrayBuffer(1200, true);
        private ArrayBuffer _recvBufferHandshake = new ArrayBuffer(1200, true);

        private readonly bool _isServer;

        public MockTls(ManagedQuicConnection connection, QuicClientConnectionOptions options, TransportParameters localTransportParams)
            : this(connection, localTransportParams, options.ClientAuthenticationOptions?.ApplicationProtocols)
        {
            _isServer = false;
            WriteLevel = EncryptionLevel.Initial;
        }

        public MockTls(ManagedQuicConnection connection, QuicListenerOptions options, TransportParameters localTransportParams)
            : this(connection, localTransportParams, options.ServerAuthenticationOptions?.ApplicationProtocols)
        {
            WriteLevel = EncryptionLevel.Initial;
            _isServer = true;
        }

        private MockTls(ManagedQuicConnection connection, TransportParameters localTransportParams,
            List<SslApplicationProtocol>? alpn)
        {
            _connection = connection;
            _localTransportParams = localTransportParams;
            _alpn = alpn ?? throw new ArgumentNullException(nameof(SslServerAuthenticationOptions.ApplicationProtocols));
        }

        public void Dispose()
        {
            _recvBufferInitial.Dispose();
            _recvBufferHandshake.Dispose();
        }

        public bool IsHandshakeComplete { get; private set; }
        public EncryptionLevel WriteLevel { get; private set; }
        public void OnHandshakeDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            ref ArrayBuffer buffer = ref level == EncryptionLevel.Initial
                ? ref _recvBufferInitial
                : ref _recvBufferHandshake;

            buffer.EnsureAvailableSpace(data.Length);
            data.CopyTo(buffer.AvailableSpan);
            buffer.Commit(data.Length);
        }

        public bool TryAdvanceHandshake()
        {
           // The handshake flow we want to imitate looks like this:
           //
           // Initial[0]: CRYPTO[CH] ->
           //
           //                                  Initial[0]: CRYPTO[SH] ACK[0]
           //                        Handshake[0]: CRYPTO[EE, CERT, CV, FIN]
           //
           // Initial[1]: ACK[0]
           // Handshake[0]: CRYPTO[FIN], ACK[0]
           //
           //                                           Handshake[1]: ACK[0]

           if (!_isServer)
           {
               switch (WriteLevel)
               {
                   case EncryptionLevel.Initial:
                   {
                       WriteInitial();

                       // wait for server reply
                       WriteLevel = EncryptionLevel.Handshake;
                       break;
                   }

                   case EncryptionLevel.Handshake:
                   {
                       if (_recvBufferInitial.ActiveLength > 0)
                       {
                           ReadInitial();
                       }

                       if (_recvBufferHandshake.ActiveLength > 0)
                       {
                           ReadHandshake();
                           WriteHandshake();

                           WriteLevel = EncryptionLevel.Application;
                           IsHandshakeComplete = true;
                       }

                       break;
                   }
                   case EncryptionLevel.Application:
                       return true; // done
                   default:
                       // should be unreachable
                       throw new ArgumentOutOfRangeException();
               }
           }
           else // server
           {
               switch (WriteLevel)
               {
                   case EncryptionLevel.Initial:
                       if (_recvBufferInitial.ActiveLength > 0)
                       {
                           ReadInitial();

                           WriteInitial();
                           WriteHandshake();

                           WriteLevel = EncryptionLevel.Handshake;
                       }

                       break;
                   case EncryptionLevel.Handshake:
                       if (_recvBufferHandshake.ActiveLength > 0)
                       {
                           IsHandshakeComplete = true;
                           WriteLevel = EncryptionLevel.Application;

                           // send an improvised fin message
                           AddHandshakeData(EncryptionLevel.Application, _magicBytes);
                       }
                       break;
                   case EncryptionLevel.Application:
                       break;
                   default:
                       // should be unreachable
                       throw new ArgumentOutOfRangeException();
               }
           }

           Flush();
           return true;
        }

        private void WriteInitial()
        {
            Span<byte> buffer = stackalloc byte[1024];
            AddHandshakeData(EncryptionLevel.Initial, _magicBytes);

            int written = TransportParameters.Write(buffer, _isServer, _localTransportParams);
            // prepend the transport parameters by length
            AddHandshakeData(EncryptionLevel.Initial, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref written, 1)));
            AddHandshakeData(EncryptionLevel.Initial, buffer.Slice(0, written));

            // send our protection secrets
            AddHandshakeData(EncryptionLevel.Initial, _handshakeWriteSecret);
            AddHandshakeData(EncryptionLevel.Initial, _applicationWriteSecret);
        }

        private void ReadInitial()
        {
            // check if we mock tls is used on the other side as well
            if (!_recvBufferInitial.ActiveSpan.StartsWith(_magicBytes))
            {
                throw new InvalidOperationException("Cannot mock TLS implementation to contact server running true TLS.");
            }

            _recvBufferInitial.Discard(_magicBytes.Length);
            int length = MemoryMarshal.Read<int>(_recvBufferInitial.ActiveSpan);
            _recvBufferInitial.Discard(sizeof(int));
            if (!TransportParameters.Read(_recvBufferInitial.ActiveSpan.Slice(0, length), _isServer,
                out _remoteTransportParams))
            {
                throw new Exception("Failed to read transport parameters");
            }
            _recvBufferInitial.Discard(length);
            if (_recvBufferInitial.ActiveLength != _handshakeWriteSecret.Length * 2)
            {
                throw new Exception("Failed to read protection secrets");
            }

            SetEncryptionSecrets(EncryptionLevel.Handshake,
                _recvBufferInitial.ActiveSpan.Slice(0, _handshakeWriteSecret.Length),
                _handshakeWriteSecret);
            _recvBufferInitial.Discard(_handshakeWriteSecret.Length);

            if (_isServer)
            {
                // server can derive the application secrets from the first initial
                SetEncryptionSecrets(EncryptionLevel.Application, _recvBufferInitial.ActiveSpan, _applicationWriteSecret);
            }
            _recvBufferInitial.Discard(_applicationWriteSecret.Length);
        }

        private void WriteHandshake()
        {
            // all we need now is application secrets
            AddHandshakeData(EncryptionLevel.Handshake, _applicationWriteSecret);
        }

        private void ReadHandshake()
        {
            if (_recvBufferHandshake.ActiveLength != _applicationWriteSecret.Length)
            {
                throw new Exception("Failed to read application secret");
            }

            if (!_isServer)
            {
                // client derives application secrets upon receiving handshake packet
                SetEncryptionSecrets(EncryptionLevel.Application, _recvBufferHandshake.ActiveSpan, _applicationWriteSecret);
            }
        }

        public TlsCipherSuite GetNegotiatedCipher() => QuicConstants.InitialCipherSuite;

        public TransportParameters? GetPeerTransportParameters(bool isServer) => _remoteTransportParams;

        public SslApplicationProtocol GetNegotiatedProtocol() => throw new NotImplementedException();

        private void AddHandshakeData(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            _connection.AddHandshakeData(level, data);
        }

        private void SendTlsAlert(EncryptionLevel level, int alert)
        {
            _connection.SendTlsAlert(level, alert);
        }

        private void SetEncryptionSecrets(EncryptionLevel level, ReadOnlySpan<byte> readSecret,
            ReadOnlySpan<byte> writeSecret)
        {
            _connection.SetEncryptionSecrets(level, readSecret, writeSecret);
        }

        private void Flush()
        {
            _connection.FlushHandshakeData();
        }
    }
}
