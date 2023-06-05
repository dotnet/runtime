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
    private static readonly FileStream? s_fileStream = s_keyLogFile != null ? File.Open(s_keyLogFile, FileMode.Append, FileAccess.Write) : null;

    private unsafe QUIC_TLS_SECRETS* _tlsSecrets;

    public static unsafe MsQuicTlsSecret? Create(MsQuicContextSafeHandle handle)
    {
        if (s_fileStream != null)
        {
            try
            {
                QUIC_TLS_SECRETS* ptr = handle.GetSecretsBuffer();
                if (ptr != null)
                {
                    int status = MsQuicApi.Api.SetParam(handle, QUIC_PARAM_CONN_TLS_SECRETS, (uint)sizeof(QUIC_TLS_SECRETS), ptr);

                    if (StatusSucceeded(status))
                    {
                        return new MsQuicTlsSecret(ptr);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(handle, "Failed to set native memory for TLS secret.");
                        }
                    }
                }
            }
            catch { };
        }

        return null;
    }

    private unsafe MsQuicTlsSecret(QUIC_TLS_SECRETS* memory)
    {
        _tlsSecrets = memory;
    }

    public void WriteSecret() => WriteSecret(s_fileStream);
    public unsafe void WriteSecret(FileStream? stream)
    {
        if (stream != null && _tlsSecrets != null)
        {
            lock (stream)
            {
                string clientRandom = string.Empty;

                if (_tlsSecrets->IsSet.ClientRandom != 0)
                {
                    clientRandom = HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ClientRandom, 32));
                }

                if (_tlsSecrets->IsSet.ClientHandshakeTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ClientHandshakeTrafficSecret, _tlsSecrets->SecretLength))}\n"));
                }

                if (_tlsSecrets->IsSet.ServerHandshakeTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"SERVER_HANDSHAKE_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ServerHandshakeTrafficSecret, _tlsSecrets->SecretLength))}\n"));
                }

                if (_tlsSecrets->IsSet.ClientTrafficSecret0 != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ClientTrafficSecret0, _tlsSecrets->SecretLength))}\n"));
                }

                if (_tlsSecrets->IsSet.ServerTrafficSecret0 != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"SERVER_TRAFFIC_SECRET_0 {clientRandom} {HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ServerTrafficSecret0, _tlsSecrets->SecretLength))}\n"));
                }

                if (_tlsSecrets->IsSet.ClientEarlyTrafficSecret != 0)
                {
                    stream.Write(Encoding.ASCII.GetBytes($"CLIENT_EARLY_TRAFFIC_SECRET {clientRandom} {HexConverter.ToString(new ReadOnlySpan<byte>(_tlsSecrets->ClientEarlyTrafficSecret, _tlsSecrets->SecretLength))}\n"));
                }

                stream.Flush();
            }
        }
    }

    public unsafe void Dispose()
    {
        if (_tlsSecrets != null)
        {
            NativeMemory.Clear(_tlsSecrets, (nuint)sizeof(QUIC_TLS_SECRETS));
        }
    }
}
#endif
