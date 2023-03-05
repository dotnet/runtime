// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal sealed class MsQuicTlsSecret : IDisposable
{
    private static readonly int ClientRandomOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientRandom");
    private static readonly int ClientEarlyTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientEarlyTrafficSecret");
    private static readonly int ClientHandshakeTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientHandshakeTrafficSecret");
    private static readonly int ServerHandshakeTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ServerHandshakeTrafficSecret");
    private static readonly int ClientTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientTrafficSecret0");
    private static readonly int ServerTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ServerTrafficSecret0");

    public QUIC_TLS_SECRETS tlsSecret;
    private GCHandle gcHandle;

    public unsafe MsQuicTlsSecret(MsQuicContextSafeHandle handle)
    {
        tlsSecret = default;
        gcHandle = GCHandle.Alloc(this, GCHandleType.Pinned);

        fixed (void* ptr = &tlsSecret)
        {
            // Best effort. Ignores failures.
            MsQuicApi.Api.SetParam(handle, QUIC_PARAM_CONN_TLS_SECRETS, (uint)sizeof(QUIC_TLS_SECRETS), ptr);
        }
    }

    public void WriteSecret(string fileName)
    {
        using (FileStream stream = File.Open(fileName, FileMode.Append, FileAccess.Write))
        {
            WriteSecret(stream);
        }
    }

    public unsafe void WriteSecret(FileStream stream)
    {
        lock (stream)
        {
            fixed (void* tls = &tlsSecret)
            {
                string clientRandom = string.Empty;
                ReadOnlySpan<byte> secrets = new ReadOnlySpan<byte>(tls, sizeof(QUIC_TLS_SECRETS));

                if (tlsSecret.IsSet.ClientRandom != 0)
                {
                    clientRandom =  HexConverter.ToString(secrets.Slice(ClientRandomOffset, 32));
                }

                if (tlsSecret.IsSet.ClientHandshakeTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(secrets.Slice(ClientHandshakeTrafficSecretOffset, tlsSecret.SecretLength))}\n"));
                }

                if (tlsSecret.IsSet.ServerHandshakeTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"SERVER_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(secrets.Slice(ServerHandshakeTrafficSecretOffset, tlsSecret.SecretLength))}\n"));
                }

                if (tlsSecret.IsSet.ClientTrafficSecret0 != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(secrets.Slice(ClientTrafficSecretOffset, tlsSecret.SecretLength))}\n"));
                }

                if (tlsSecret.IsSet.ServerTrafficSecret0 != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"SERVER_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(secrets.Slice(ServerTrafficSecretOffset, tlsSecret.SecretLength))}\n"));
                }

                if (tlsSecret.IsSet.ClientEarlyTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_EARLY_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(secrets.Slice(ClientEarlyTrafficSecretOffset, tlsSecret.SecretLength))}\n"));
                }
            }
        }
    }

    public void Dispose()
    {
        if (gcHandle.IsAllocated)
        {
            gcHandle.Free();
        }
    }
}
