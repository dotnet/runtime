// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal sealed class MsQuicTlsSecret : IDisposable
{
    private static readonly string? s_keyLogFile = Environment.GetEnvironmentVariable("SSLKEYLOGFILE");

    private static readonly int s_clientRandomOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientRandom");
    private static readonly int s_clientEarlyTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientEarlyTrafficSecret");
    private static readonly int s_clientHandshakeTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientHandshakeTrafficSecret");
    private static readonly int s_serverHandshakeTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ServerHandshakeTrafficSecret");
    private static readonly int s_clientTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ClientTrafficSecret0");
    private static readonly int s_serverTrafficSecretOffset = (int)Marshal.OffsetOf(typeof(QUIC_TLS_SECRETS), "ServerTrafficSecret0");

    private IntPtr tlsSecretsPtr = IntPtr.Zero;

    public static MsQuicTlsSecret? Create(MsQuicContextSafeHandle handle)
    {
        MsQuicTlsSecret? secret = null;
        if (s_keyLogFile != null)
        {
            try {
                secret = new MsQuicTlsSecret(handle);
            }
            catch { };
        }

        return secret;
    }

    private unsafe MsQuicTlsSecret(MsQuicContextSafeHandle handle)
    {
        void * ptr = NativeMemory.Alloc((nuint)sizeof(QUIC_TLS_SECRETS));
        if (ptr != null)
        {
            MsQuicApi.Api.SetParam(handle, QUIC_PARAM_CONN_TLS_SECRETS, (uint)sizeof(QUIC_TLS_SECRETS), ptr);
            tlsSecretsPtr = (IntPtr)ptr;
        }
    }

    public void WriteSecret()
    {
        if (s_keyLogFile != null)
        {
            WriteSecret(s_keyLogFile);
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
            string clientRandom = string.Empty;
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>((void*)tlsSecretsPtr, sizeof(QUIC_TLS_SECRETS));
            ReadOnlySpan<QUIC_TLS_SECRETS> secrets = MemoryMarshal.Cast<byte, QUIC_TLS_SECRETS>(buffer);

            if (secrets[0].IsSet.ClientRandom != 0)
            {
                clientRandom =  HexConverter.ToString(buffer.Slice(s_clientRandomOffset, 32));
            }

            if (secrets[0].IsSet.ClientHandshakeTrafficSecret != 0)
            {
                stream.Write(Encoding.ASCII.GetBytes($"CLIENT_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(buffer.Slice(s_clientHandshakeTrafficSecretOffset, secrets[0].SecretLength))}\n"));
            }

            if (secrets[0].IsSet.ServerHandshakeTrafficSecret != 0)
            {
                stream.Write(Encoding.ASCII.GetBytes($"SERVER_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(buffer.Slice(s_serverHandshakeTrafficSecretOffset, secrets[0].SecretLength))}\n"));
            }

            if (secrets[0].IsSet.ClientTrafficSecret0 != 0)
            {
                stream.Write(Encoding.ASCII.GetBytes($"CLIENT_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(buffer.Slice(s_clientTrafficSecretOffset, secrets[0].SecretLength))}\n"));
            }

            if (secrets[0].IsSet.ServerTrafficSecret0 != 0)
            {
                stream.Write(Encoding.ASCII.GetBytes($"SERVER_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(buffer.Slice(s_serverTrafficSecretOffset, secrets[0].SecretLength))}\n"));
            }

            if (secrets[0].IsSet.ClientEarlyTrafficSecret != 0)
            {
                stream.Write(Encoding.ASCII.GetBytes($"CLIENT_EARLY_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(buffer.Slice(s_clientEarlyTrafficSecretOffset, secrets[0].SecretLength))}\n"));
            }
        }
    }

    public unsafe void Dispose()
    {
        IntPtr ptr = Interlocked.Exchange(ref tlsSecretsPtr, IntPtr.Zero);
        if (ptr != IntPtr.Zero)
        {
            Span<byte> secret = new Span<byte>((void*)ptr, sizeof(QUIC_TLS_SECRETS));
            secret.Clear();
            NativeMemory.Free((void*)ptr);
        }
    }
}
#endif
