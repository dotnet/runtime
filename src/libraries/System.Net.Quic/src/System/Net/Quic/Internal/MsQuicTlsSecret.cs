// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal sealed class MsQuicTlsSecret : IDisposable
{
    private static readonly string? s_keyLogFile = Environment.GetEnvironmentVariable("SSLKEYLOGFILE");
    private static readonly FileStream? s_fileStream = s_keyLogFile != null ? File.Open(s_keyLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite) : null;

    private unsafe QUIC_TLS_SECRETS* _tlsSecrets;

    public static unsafe MsQuicTlsSecret? Create(MsQuicContextSafeHandle handle)
    {
        if (s_fileStream is null)
        {
            return null;
        }

        QUIC_TLS_SECRETS* tlsSecrets = null;
        try
        {
            tlsSecrets = (QUIC_TLS_SECRETS*)NativeMemory.AllocZeroed((nuint)sizeof(QUIC_TLS_SECRETS));
            MsQuicHelpers.SetMsQuicParameter(handle, QUIC_PARAM_CONN_TLS_SECRETS, (uint)sizeof(QUIC_TLS_SECRETS), (byte*)tlsSecrets);
            MsQuicTlsSecret instance = new MsQuicTlsSecret(tlsSecrets);
            handle.Disposable = instance;
            return instance;
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(handle, $"Failed to set native memory for TLS secret: {ex}");
            }
            if (tlsSecrets is not null)
            {
                NativeMemory.Free(tlsSecrets);
            }
            return null;
        }
    }

    private unsafe MsQuicTlsSecret(QUIC_TLS_SECRETS* tlsSecrets)
    {
        _tlsSecrets = tlsSecrets;
    }

    public unsafe void WriteSecret()
    {
        Debug.Assert(_tlsSecrets is not null);
        Debug.Assert(s_fileStream is not null);

        lock (s_fileStream)
        {
            string clientRandom = string.Empty;
            if (_tlsSecrets->IsSet.ClientRandom != 0)
            {
                clientRandom = Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ClientRandom, 32));
            }
            if (_tlsSecrets->IsSet.ClientHandshakeTrafficSecret != 0)
            {
                s_fileStream.Write(Encoding.ASCII.GetBytes($"CLIENT_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ClientHandshakeTrafficSecret, _tlsSecrets->SecretLength))}\n"));
            }
            if (_tlsSecrets->IsSet.ServerHandshakeTrafficSecret != 0)
            {
                s_fileStream.Write(Encoding.ASCII.GetBytes($"SERVER_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ServerHandshakeTrafficSecret, _tlsSecrets->SecretLength))}\n"));
            }
            if (_tlsSecrets->IsSet.ClientTrafficSecret0 != 0)
            {
                s_fileStream.Write(Encoding.ASCII.GetBytes($"CLIENT_TRAFFIC_SECRET_0 {clientRandom} {Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ClientTrafficSecret0, _tlsSecrets->SecretLength))}\n"));
            }
            if (_tlsSecrets->IsSet.ServerTrafficSecret0 != 0)
            {
                s_fileStream.Write(Encoding.ASCII.GetBytes($"SERVER_TRAFFIC_SECRET_0 {clientRandom} {Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ServerTrafficSecret0, _tlsSecrets->SecretLength))}\n"));
            }
            if (_tlsSecrets->IsSet.ClientEarlyTrafficSecret != 0)
            {
                s_fileStream.Write(Encoding.ASCII.GetBytes($"CLIENT_EARLY_TRAFFIC_SECRET {clientRandom} {Convert.ToHexString(new ReadOnlySpan<byte>(_tlsSecrets->ClientEarlyTrafficSecret, _tlsSecrets->SecretLength))}\n"));
            }
            s_fileStream.Flush();
        }

        NativeMemory.Clear(_tlsSecrets, (nuint)sizeof(QUIC_TLS_SECRETS));
    }

    public unsafe void Dispose()
    {
        if (_tlsSecrets is null)
        {
            return;
        }
        lock (this)
        {
            if (_tlsSecrets is null)
            {
                return;
            }

            QUIC_TLS_SECRETS* tlsSecrets = _tlsSecrets;
            _tlsSecrets = null;
            NativeMemory.Free(_tlsSecrets);
        }
    }
}
#endif
