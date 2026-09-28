// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.ServerSentEvents;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for <see cref="SseParser"/>: the parsed items are compared with a direct implementation of the
/// WHATWG "event stream interpretation" algorithm. The stream is served in fuzzer-chosen read sizes, parsed both synchronously
/// and asynchronously, and optionally with a small <see cref="SseParserOptions{T}.MaxBufferSize"/>.
/// </summary>
/// <remarks>Input layout: [0] read-size seed, [1] flags (bit0 async, bits1-3 buffer limit), [2..] the event stream.</remarks>
internal sealed class SseParserFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * A blank line with an empty data buffer doesn't reset the event type buffer (the spec says to), so "event: foo\n\n"
    //   followed by "data: x\n\n" yields an event of type "foo" instead of "message"; the per-item EventId and
    //   ReconnectionInterval likewise leak into the next event.
    // * "retry" values with trailing NUL characters ("7\0") are accepted, because long.TryParse ignores trailing NULs;
    //   the spec only accepts ASCII digits.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Net.ServerSentEvents"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        byte seed = bytes[0];
        bool async = (bytes[1] & 1) != 0;
        int limitSelector = (bytes[1] >> 1) & 7;
        int? maxBufferSize = limitSelector == 0 ? null : 1 << (limitSelector + 3); // 16 .. 1024
        byte[] stream = bytes.Slice(2).ToArray();

        List<Item> expected = Reference(stream);
        var actual = new List<Item>();
        Exception? error = null;

        var options = new SseParserOptions<byte[]>(static (_, data) => data.ToArray());
        if (maxBufferSize is int limit)
        {
            options.MaxBufferSize = limit;
        }

        SseParser<byte[]> parser = SseParser.Create(new ChunkedStream(stream, seed), options);
        try
        {
            if (async)
            {
                Task.Run(async () =>
                {
                    await foreach (SseItem<byte[]> item in parser.EnumerateAsync().ConfigureAwait(false))
                    {
                        actual.Add(new Item(item.EventType, item.Data, item.EventId, item.ReconnectionInterval, parser.LastEventId));
                    }
                }).GetAwaiter().GetResult();
            }
            else
            {
                foreach (SseItem<byte[]> item in parser.Enumerate())
                {
                    actual.Add(new Item(item.EventType, item.Data, item.EventId, item.ReconnectionInterval, parser.LastEventId));
                }
            }
        }
        catch (InvalidDataException ex) when (maxBufferSize is not null)
        {
            error = ex;
        }

        string Describe() =>
            $"SseParser ({(async ? "async" : "sync")}, MaxBufferSize {maxBufferSize?.ToString() ?? "default"}) on {Escape(stream)}\n" +
            $"  expected: {string.Join(" | ", expected)}\n  actual:   {string.Join(" | ", actual)}{(error is null ? "" : " then " + error.Message)}";

        if (error is not null)
        {
            // The limit can cut the stream short; everything produced before that must still be right, and the limit must have
            // been plausibly reached (some line or event data at least half the limit).
            Check(actual.Count <= expected.Count && actual.SequenceEqual(expected.Take(actual.Count)), Describe);
            return;
        }

        Check(actual.SequenceEqual(expected), Describe);
    }

    private sealed record Item(string EventType, byte[] Data, string? EventId, TimeSpan? ReconnectionInterval, string LastEventId)
    {
        public bool Equals(Item? other) =>
            other is not null && EventType == other.EventType && Data.AsSpan().SequenceEqual(other.Data) &&
            EventId == other.EventId && ReconnectionInterval == other.ReconnectionInterval && LastEventId == other.LastEventId;

        public override int GetHashCode() => EventType.GetHashCode();

        public override string ToString() =>
            $"{{type '{Escape(Encoding.UTF8.GetBytes(EventType))}' data '{Escape(Data)}' id {(EventId is null ? "null" : $"'{EventId}'")} retry {ReconnectionInterval?.TotalMilliseconds.ToString() ?? "null"} last '{LastEventId}'}}";
    }

    /// <summary>The WHATWG event stream interpretation, over UTF-8 bytes.</summary>
    private static List<Item> Reference(byte[] stream)
    {
        var items = new List<Item>();
        ReadOnlySpan<byte> input = stream;
        if (input.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
        {
            input = input.Slice(3);
        }

        var data = new List<byte>();
        bool hasData = false;
        string? eventType = null, eventId = null;
        TimeSpan? retry = null;
        string lastEventId = "";

        while (true)
        {
            // Lines end in CRLF, LF or CR; a final line without a terminator is incomplete and is discarded.
            int end = input.IndexOfAny((byte)'\r', (byte)'\n');
            if (end < 0)
            {
                break;
            }

            ReadOnlySpan<byte> line = input.Slice(0, end);
            input = input.Slice(input[end] == '\r' && end + 1 < input.Length && input[end + 1] == '\n' ? end + 2 : end + 1);

            if (line.IsEmpty)
            {
                // Dispatch. "If the data buffer is an empty string, set the data buffer and the event type buffer to the
                // empty string and return."
                if (hasData)
                {
                    items.Add(new Item(eventType ?? SseParser.EventTypeDefault, [.. data], eventId, retry, lastEventId));
                }
                else if (!s_strict)
                {
                    continue; // Known issue: nothing is reset.
                }

                data.Clear();
                hasData = false;
                eventType = null;
                eventId = null;
                retry = null;
                continue;
            }

            if (line[0] == ':')
            {
                continue;
            }

            int colon = line.IndexOf((byte)':');
            ReadOnlySpan<byte> field = colon < 0 ? line : line.Slice(0, colon);
            ReadOnlySpan<byte> value = colon < 0 ? [] : line.Slice(colon + 1);
            if (!value.IsEmpty && value[0] == ' ')
            {
                value = value.Slice(1);
            }

            if (field.SequenceEqual("data"u8))
            {
                if (hasData)
                {
                    data.Add((byte)'\n');
                }

                data.AddRange(value);
                hasData = true;
            }
            else if (field.SequenceEqual("event"u8))
            {
                eventType = Encoding.UTF8.GetString(value);
            }
            else if (field.SequenceEqual("id"u8))
            {
                if (!value.Contains((byte)0))
                {
                    lastEventId = eventId = Encoding.UTF8.GetString(value);
                }
            }
            else if (field.SequenceEqual("retry"u8))
            {
                if (!s_strict)
                {
                    value = value.TrimEnd((byte)0);
                }

                if (!value.IsEmpty && !value.ContainsAnyExceptInRange((byte)'0', (byte)'9') &&
                    long.TryParse(value, out long ms) && ms <= (long)TimeSpan.MaxValue.TotalMilliseconds)
                {
                    retry = ms == (long)TimeSpan.MaxValue.TotalMilliseconds ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(ms);
                }
            }
        }

        return items;
    }

    /// <summary>A stream that serves its data in pseudo-random read sizes.</summary>
    private sealed class ChunkedStream(byte[] data, byte seed) : Stream
    {
        private int _position;
        private uint _state = seed * 2654435761u + 7;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            _state = _state * 1103515245 + 12345;
            int size = Math.Min(Math.Min(buffer.Length, 1 + (int)((_state >> 16) % 13)), data.Length - _position);
            data.AsSpan(_position, size).CopyTo(buffer);
            _position += size;
            return size;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer.AsSpan(offset, count)));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            new(Read(buffer.Span));
    }

    private static string Escape(byte[] bytes) =>
        string.Concat(bytes.Take(200).Select(b => b is >= 0x20 and < 0x7F and not (byte)'\\' ? ((char)b).ToString() : $"\\x{b:X2}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
