// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the <see cref="SocketsHttpHandler"/> response side: the fuzz input is served as the bytes the server sends
/// (HTTP/1.1, or HTTP/2 with prior knowledge), through a ConnectCallback stream that hands them out in fuzzer-chosen read
/// sizes and swallows whatever the client writes. Covers status line/header/chunked parsing, HTTP/2 framing, HPACK, and the
/// handler's automatic gzip/deflate/brotli response decompression. Only the documented failure types are allowed.
/// </summary>
/// <remarks>Input layout: [0] flags (bit0 HTTP/2, bit1 decompression, bit2 HEAD, bit3 small header limit), [1] read-size seed, [2..] server bytes.</remarks>
internal sealed class HttpClientResponseFuzzer : IFuzzer
{
    // Known issues on main, avoided unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * A response with a duplicated single-valued header (e.g. two Content-Type or Cache-Control lines) trips
    //   Debug.Assert("Only a single parsed value should be stored for this parser") in HttpHeaders when the typed
    //   property is read; release builds return the first value.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Net.Http"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        byte flags = bytes[0];
        byte seed = bytes[1];
        byte[] response = bytes.Slice(2).ToArray();
        bool http2 = (flags & 1) != 0;

        using var handler = new SocketsHttpHandler
        {
            ConnectCallback = (_, _) => ValueTask.FromResult<Stream>(new ServerStream(response, seed)),
            AutomaticDecompression = (flags & 2) != 0 ? DecompressionMethods.All : DecompressionMethods.None,
            MaxResponseHeadersLength = (flags & 8) != 0 ? 1 : 64,
            UseProxy = false,
            UseCookies = true,
            AllowAutoRedirect = false,
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        using var request = new HttpRequestMessage((flags & 4) != 0 ? HttpMethod.Head : HttpMethod.Get, "http://fuzz.test/")
        {
            Version = http2 ? HttpVersion.Version20 : HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        if (!http2)
        {
            request.Headers.ConnectionClose = true;
        }

        try
        {
            Task.Run(async () =>
            {
                using HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                // Touch everything a caller typically looks at.
                _ = message.StatusCode;
                _ = message.ReasonPhrase;
                foreach (KeyValuePair<string, IEnumerable<string>> header in message.Headers.NonValidated.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)))
                {
                    _ = header.Value.Count();
                }

                bool Single(System.Net.Http.Headers.HttpHeaders headers, string name) =>
                    s_strict || !headers.NonValidated.TryGetValues(name, out System.Net.Http.Headers.HeaderStringValues values) || values.Count <= 1;

                if (Single(message.Content.Headers, "Content-Length"))
                {
                    _ = message.Content.Headers.ContentLength;
                }

                if (Single(message.Content.Headers, "Content-Type"))
                {
                    _ = message.Content.Headers.ContentType;
                }

                if (Single(message.Headers, "Location"))
                {
                    _ = message.Headers.Location;
                }

                if (Single(message.Headers, "Cache-Control"))
                {
                    _ = message.Headers.CacheControl;
                }

                using Stream content = await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
                byte[] buffer = new byte[4096];
                long total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > 64 * 1024 * 1024)
                    {
                        break;
                    }
                }

                _ = message.TrailingHeaders.Count();
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
        }
    }

    private static bool IsExpected(Exception ex) => ex switch
    {
        HttpRequestException => true,
        IOException => true, // Includes HttpIOException.
        InvalidDataException => true, // Corrupt compressed content.
        TaskCanceledException { InnerException: TimeoutException } => true,
        _ => false,
    };

    /// <summary>The server side of the connection: serves the response bytes in pseudo-random read sizes, swallows writes.</summary>
    private sealed class ServerStream(byte[] response, byte seed) : Stream
    {
        private int _position;
        private uint _state = seed * 2654435761u + 3;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(ReadOnlySpan<byte> buffer) { }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.CompletedTask;
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => default;

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            _state = _state * 1103515245 + 12345;
            int size = Math.Min(Math.Min(buffer.Length, 1 + (int)((_state >> 16) % 61)), response.Length - _position);
            response.AsSpan(_position, size).CopyTo(buffer);
            _position += size;
            return size;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer.AsSpan(offset, count)));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            new(Read(buffer.Span));
    }
}
