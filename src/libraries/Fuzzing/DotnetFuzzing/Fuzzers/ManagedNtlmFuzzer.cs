// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Security;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the managed NTLM and SPNEGO client implementations behind <see cref="NegotiateAuthentication"/> (the default on
/// macOS, iOS, OpenBSD and Android; opt-in on Linux with the System.Net.Security.UseManagedNtlm switch). The fuzzer plays the
/// server: after the client's first message it answers with up to three fuzzed tokens, and once the client is authenticated it
/// feeds fuzzed data to Unwrap, UnwrapInPlace and VerifyIntegrityCheck. The client may only answer with status codes.
/// </summary>
/// <remarks>
/// Input layout: [0] flags (bit0 SPNEGO instead of NTLM, bits1-2 protection level), then length-prefixed server tokens
/// (2-byte little-endian length each, up to three), then the rest as data for the wrap/unwrap/MIC checks.
/// </remarks>
internal sealed class ManagedNtlmFuzzer : IFuzzer
{
    private static bool s_switchSet;

    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * The managed NTLM client sets IsAuthenticated before parsing the CHALLENGE message and never resets it when the
    //   challenge is rejected, so a failed exchange reports IsAuthenticated/IsSigned = true.
    // * An out-of-range MsvAvTimestamp in the challenge makes GetOutgoingBlob throw ArgumentOutOfRangeException
    //   (DateTime.FromFileTimeUtc) instead of returning InvalidToken.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Net.Security"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (!s_switchSet)
        {
            AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);
            s_switchSet = true;
        }

        if (bytes.Length < 1)
        {
            return;
        }

        byte flags = bytes[0];
        ReadOnlySpan<byte> rest = bytes.Slice(1);
        var tokens = new List<byte[]>();
        while (tokens.Count < 3 && rest.Length >= 2)
        {
            int tokenLength = Math.Min(BinaryPrimitives.ReadUInt16LittleEndian(rest), rest.Length - 2);
            if (tokenLength == 0 && !s_strict)
            {
                break; // Known issue: an empty server token trips Debug.Assert(!incomingBlob.IsEmpty) in Debug builds.
            }

            tokens.Add(rest.Slice(2, tokenLength).ToArray());
            rest = rest.Slice(2 + tokenLength);
        }

        byte[] data = rest.ToArray();
        var options = new NegotiateAuthenticationClientOptions
        {
            Package = (flags & 1) != 0 ? "Negotiate" : "NTLM",
            Credential = new NetworkCredential("user", "Pa$$w0rd", "DOMAIN"),
            TargetName = "HTTP/localhost",
            RequiredProtectionLevel = ((flags >> 1) & 3) switch
            {
                0 => ProtectionLevel.None,
                1 => ProtectionLevel.Sign,
                _ => ProtectionLevel.EncryptAndSign,
            },
        };

        using var client = new NegotiateAuthentication(options);
        byte[]? outgoing = client.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out NegotiateAuthenticationStatusCode status);
        if (status is not NegotiateAuthenticationStatusCode.ContinueNeeded)
        {
            return; // E.g. the package isn't available.
        }

        foreach (byte[] token in tokens)
        {
            try
            {
                outgoing = client.GetOutgoingBlob(token, out status);
            }
            catch (ArgumentOutOfRangeException ex) when (!s_strict && ex.ParamName == "fileTime")
            {
                return;
            }

            if (status != NegotiateAuthenticationStatusCode.ContinueNeeded)
            {
                break;
            }
        }

        if (s_strict && status is not (NegotiateAuthenticationStatusCode.Completed or NegotiateAuthenticationStatusCode.ContinueNeeded))
        {
            Check(!client.IsAuthenticated, $"GetOutgoingBlob returned {status} but IsAuthenticated is true");
        }

        _ = client.IsAuthenticated;
        _ = client.IsSigned;
        _ = client.IsEncrypted;
        _ = client.IsMutuallyAuthenticated;
        _ = client.ImpersonationLevel;
        if (!client.IsAuthenticated || status != NegotiateAuthenticationStatusCode.Completed)
        {
            return;
        }

        _ = client.RemoteIdentity;

        // Fuzzed data as wrapped messages and signatures from the server.
        var writer = new ArrayBufferWriter<byte>();
        _ = client.Unwrap(data, writer, out _);
        byte[] copy = data.ToArray();
        NegotiateAuthenticationStatusCode unwrapStatus = client.UnwrapInPlace(copy, out int offset, out int length, out _);
        if (unwrapStatus == NegotiateAuthenticationStatusCode.Completed)
        {
            Check(offset >= 0 && length >= 0 && offset + length <= copy.Length, $"UnwrapInPlace returned offset {offset}, length {length} for a {copy.Length}-byte input");
        }

        int split = data.Length == 0 ? 0 : data[0] % (data.Length + 1);
        _ = client.VerifyIntegrityCheck(data.AsSpan(0, split), data.AsSpan(split));

        // And the client's own output must be well-formed.
        var wrapped = new ArrayBufferWriter<byte>();
        if (client.Wrap(data, wrapped, requestEncryption: (flags & 4) != 0, out _) == NegotiateAuthenticationStatusCode.Completed)
        {
            Check(wrapped.WrittenCount >= data.Length, $"Wrap produced {wrapped.WrittenCount} bytes for a {data.Length}-byte message");
        }

        var signature = new ArrayBufferWriter<byte>();
        client.ComputeIntegrityCheck(data, signature);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
