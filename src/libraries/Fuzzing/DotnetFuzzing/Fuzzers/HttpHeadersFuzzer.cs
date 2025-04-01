// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

internal sealed class HttpHeadersFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Net.Http"];
    public string[] TargetCoreLibPrefixes => [];

    // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/Headers/KnownHeaders.cs
    private static readonly string[] s_knownHeaderNames = [
        ":status", "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Accept-Patch",
        "Accept-Ranges", "Access-Control-Allow-Credentials", "Access-Control-Allow-Headers",
        "Access-Control-Allow-Methods", "Access-Control-Allow-Origin", "Access-Control-Expose-Headers",
        "Access-Control-Max-Age", "Age", "Allow", "Alt-Svc", "Alt-Used", "Authorization", "Cache-Control",
        "Connection", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length",
        "Content-Location", "Content-MD5", "Content-Range", "Content-Security-Policy", "Content-Type",
        "Cookie", "Cookie2", "Date", "ETag", "Expect", "Expect-CT", "Expires", "From", "grpc-encoding",
        "grpc-message", "grpc-status", "Host", "If-Match", "If-Modified-Since", "If-None-Match",
        "If-Range", "If-Unmodified-Since", "Keep-Alive", "Last-Modified", "Link", "Location",
        "Max-Forwards", "Origin", "P3P", "Pragma", "Proxy-Authenticate", "Proxy-Authorization",
        "Proxy-Connection", "Proxy-Support", "Public-Key-Pins", "Range", "Referer", "Referrer-Policy",
        "Refresh", "Retry-After", "Sec-WebSocket-Accept", "Sec-WebSocket-Extensions", "Sec-WebSocket-Key",
        "Sec-WebSocket-Protocol", "Sec-WebSocket-Version", "Server", "Server-Timing", "Set-Cookie",
        "Set-Cookie2", "Strict-Transport-Security", "TE", "TSV", "Trailer", "Transfer-Encoding",
        "Upgrade", "Upgrade-Insecure-Requests", "User-Agent", "Vary", "Via", "WWW-Authenticate",
        "Warning", "X-AspNet-Version", "X-Cache", "X-Content-Duration", "X-Content-Type-Options",
        "X-Frame-Options", "X-MSEdge-Ref", "X-Powered-By", "X-Request-ID", "X-UA-Compatible", "X-XSS-Protection"
    ];

    private static readonly HttpRequestHeaders s_requestHeaders = new HttpRequestMessage().Headers;
    private static readonly HttpContentHeaders s_contentHeaders = new ByteArrayContent([]).Headers;
    private static readonly HttpResponseHeaders s_responseHeaders = new HttpResponseMessage().Headers;

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        // We use the first byte to select a known header name.
        // The rest of the input is used as the UTF-16 header value.
        // The second byte is skipped to keep the value chars 2-byte aligned.
        string name = s_knownHeaderNames[bytes[0] % s_knownHeaderNames.Length];
        string value = MemoryMarshal.Cast<byte, char>(bytes.Slice(2)).ToString();

        Test(s_requestHeaders, name, value);
        Test(s_contentHeaders, name, value);
        Test(s_responseHeaders, name, value);

        static void Test(HttpHeaders headers, string name, string value)
        {
            headers.Clear();

            if (headers.TryAddWithoutValidation(name, value))
            {
                // Enumerating the header collection should never throw,
                // even if invalid values were added without validation.
                foreach (var _ in headers) { }

                headers.Clear();

                // If the value is invalid, we should only throw FormatException.
                try
                {
                    headers.Add(name, value);
                    headers.Add(name, value);
                }
                catch (FormatException) { }

                foreach (var _ in headers) { }
            }
        }
    }
}
