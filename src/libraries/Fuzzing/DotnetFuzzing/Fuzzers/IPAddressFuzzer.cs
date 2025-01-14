// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class IPAddressFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => ["System.Net.Primitives", "System.Private.Uri"];
        public string[] TargetCoreLibPrefixes => [];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using var poisonedBytes = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);
            using var poisonedChars = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(bytes), PoisonPagePlacement.After);

            if (IPAddress.IsValidUtf8(poisonedBytes.Span))
            {
                TestValidInput(bytes: poisonedBytes.Span);
            }
            else
            {
                Assert.False(IPAddress.TryParse(poisonedBytes.Span, out _));
            }

            if (IPAddress.IsValid(poisonedChars.Span))
            {
                TestValidInput(chars: poisonedChars.Span.ToString());
            }
            else
            {
                Assert.False(IPAddress.TryParse(poisonedChars.Span, out _));
            }

            static void TestValidInput(ReadOnlySpan<byte> bytes = default, string? chars = null)
            {
                if (chars is null)
                {
                    // bytes past the '%' may not be valid UTF-8: https://github.com/dotnet/runtime/issues/111288
                    int percentIndex = bytes.IndexOf((byte)'%');
                    Assert.True(Utf8.IsValid(bytes.Slice(0, percentIndex < 0 ? bytes.Length : percentIndex)));

                    chars = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    bytes = Encoding.UTF8.GetBytes(chars);
                }

                Assert.True(IPAddress.IsValid(chars));
                Assert.True(IPAddress.TryParse(chars, out IPAddress? ipFromChars));
                Assert.True(IPAddress.TryParse(bytes, out IPAddress? ipFromBytes));

                Assert.True(ipFromChars.Equals(ipFromBytes));
                Assert.True(ipFromBytes.Equals(ipFromChars));

                Assert.True(IPAddress.IsValid(ipFromChars.ToString()));

                TestUri(chars);
            }

            static void TestUri(string chars)
            {
                bool isIpv6 = chars.Contains(':');
                UriHostNameType hostNameType = isIpv6 ? UriHostNameType.IPv6 : UriHostNameType.IPv4;

                if (isIpv6)
                {
                    // Remove the ScopeId
                    int percentIndex = chars.IndexOf('%');
                    if (percentIndex >= 0)
                    {
                        chars = chars.Substring(0, percentIndex);
                        if (chars.StartsWith('['))
                        {
                            chars = $"{chars}]";
                        }
                    }

                    if (!chars.StartsWith('['))
                    {
                        chars = $"[{chars}]";
                    }

                    // Remove the port
                    int bracketIndex = chars.IndexOf(']');
                    if (bracketIndex >= 0 &&
                        bracketIndex + 1 < chars.Length &&
                        chars[bracketIndex + 1] == ':')
                    {
                        chars = chars.Substring(0, bracketIndex + 1);
                    }
                }

                Assert.True(Uri.TryCreate($"http://{chars}/", UriKind.Absolute, out Uri? uri));
                Assert.Equal(hostNameType, uri.HostNameType);
                Assert.Equal(hostNameType, Uri.CheckHostName(chars));
            }
        }
    }
}
