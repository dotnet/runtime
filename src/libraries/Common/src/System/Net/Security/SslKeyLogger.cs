// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;

internal static class SslKeyLogger
{
    private static readonly string? s_keyLogFile = Environment.GetEnvironmentVariable("SSLKEYLOGFILE");
    private static readonly FileStream? s_fileStream;

#pragma warning disable CA1810 // Initialize all static fields when declared and remove cctor
    static SslKeyLogger()
    {
        s_fileStream = null;

        try
        {
#if DEBUG
            bool isEnabled = true;
#else
            bool isEnabled = AppContext.TryGetSwitch("System.Net.EnableSslKeyLogging", out bool enabled) && enabled;
#endif

            if (isEnabled && s_keyLogFile != null)
            {
                s_fileStream = File.Open(s_keyLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            }
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Failed to open SSL key log file '{s_keyLogFile}': {ex}");
            }
        }
    }
#pragma warning restore CA1810

    public static bool IsEnabled => s_fileStream != null;

    public static void WriteLineRaw(ReadOnlySpan<byte> data)
    {
        Debug.Assert(s_fileStream != null);
        if (s_fileStream == null)
        {
            return;
        }

        if (data.Length > 0)
        {
            lock (s_fileStream)
            {
                s_fileStream.Write(data);
                s_fileStream.WriteByte((byte)'\n');
                s_fileStream.Flush();
            }
        }
    }

    public static void WriteSecrets(
        ReadOnlySpan<byte> clientRandom,
        ReadOnlySpan<byte> clientHandshakeTrafficSecret,
        ReadOnlySpan<byte> serverHandshakeTrafficSecret,
        ReadOnlySpan<byte> clientTrafficSecret0,
        ReadOnlySpan<byte> serverTrafficSecret0,
        ReadOnlySpan<byte> clientEarlyTrafficSecret)
    {
        Debug.Assert(s_fileStream != null);
        Debug.Assert(!clientRandom.IsEmpty);

        if (s_fileStream == null ||
            clientRandom.IsEmpty ||

            // return early if there is nothing to log
            (clientHandshakeTrafficSecret.IsEmpty &&
            serverHandshakeTrafficSecret.IsEmpty &&
            clientTrafficSecret0.IsEmpty &&
            serverTrafficSecret0.IsEmpty &&
            clientEarlyTrafficSecret.IsEmpty))
        {
            return;
        }

        Span<byte> clientRandomUtf8 = clientRandom.Length <= 1024 ? stackalloc byte[clientRandom.Length * 2] : new byte[clientRandom.Length * 2];
        HexEncode(clientRandom, clientRandomUtf8);

        lock (s_fileStream)
        {
            WriteSecretCore("CLIENT_HANDSHAKE_TRAFFIC_SECRET"u8, clientRandomUtf8, clientHandshakeTrafficSecret);
            WriteSecretCore("SERVER_HANDSHAKE_TRAFFIC_SECRET"u8, clientRandomUtf8, serverHandshakeTrafficSecret);
            WriteSecretCore("CLIENT_TRAFFIC_SECRET_0"u8, clientRandomUtf8, clientTrafficSecret0);
            WriteSecretCore("SERVER_TRAFFIC_SECRET_0"u8, clientRandomUtf8, serverTrafficSecret0);
            WriteSecretCore("CLIENT_EARLY_TRAFFIC_SECRET"u8, clientRandomUtf8, clientEarlyTrafficSecret);

            s_fileStream.Flush();
        }
    }

    private static void WriteSecretCore(ReadOnlySpan<byte> labelUtf8, ReadOnlySpan<byte> clientRandomUtf8, ReadOnlySpan<byte> secret)
    {
        if (secret.Length == 0)
        {
            return;
        }

        // write the secret line in the format {label} {client_random (hex)} {secret (hex)} e.g.
        // SERVER_HANDSHAKE_TRAFFIC_SECRET bae582227f0f46ca663cb8c3d62e68cec38c2b947e7c4a9ec6f4e262b5ed5354 48f6bd5b0c8447d97129c6dad080f34c7f9f11ade8eeabb011f33811543411d7ab1013b1374bcd81bfface6a2deef539
        int totalLength = labelUtf8.Length + 1 + clientRandomUtf8.Length + 1 + 2 * secret.Length + 1;
        Span<byte> line = totalLength <= 1024 ? stackalloc byte[totalLength] : new byte[totalLength];

        labelUtf8.CopyTo(line);
        line[labelUtf8.Length] = (byte)' ';

        clientRandomUtf8.CopyTo(line.Slice(labelUtf8.Length + 1));
        line[labelUtf8.Length + 1 + clientRandomUtf8.Length] = (byte)' ';

        HexEncode(secret, line.Slice(labelUtf8.Length + 1 + clientRandomUtf8.Length + 1));
        line[^1] = (byte)'\n';

        s_fileStream!.Write(line);
    }

    private static void HexEncode(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            HexConverter.ToBytesBuffer(source[i], destination.Slice(i * 2));
        }
    }

}
