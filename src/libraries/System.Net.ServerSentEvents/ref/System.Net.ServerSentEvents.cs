// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.ServerSentEvents
{
    public static partial class SseFormatter
    {
        public static System.Threading.Tasks.Task WriteAsync(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<string>> source, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task WriteAsync<T>(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> source, System.IO.Stream destination, System.Action<System.Net.ServerSentEvents.SseItem<T>, System.Buffers.IBufferWriter<byte>> itemFormatter, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public delegate T SseItemParser<out T>(string eventType, System.ReadOnlySpan<byte> data);
    public readonly partial struct SseItem<T>
    {
        private readonly T _Data_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SseItem(T data, string? eventType = null) { throw null; }
        public T Data { get { throw null; } }
        public string? EventId { get { throw null; } init { } }
        public string EventType { get { throw null; } }
        public System.TimeSpan? ReconnectionInterval { get { throw null; } init { } }
    }
    public static partial class SseParser
    {
        public const string EventTypeDefault = "message";
        public static System.Net.ServerSentEvents.SseParser<string> Create(System.IO.Stream sseStream) { throw null; }
        public static System.Net.ServerSentEvents.SseParser<T> Create<T>(System.IO.Stream sseStream, System.Net.ServerSentEvents.SseItemParser<T> itemParser) { throw null; }
    }
    public sealed partial class SseParser<T>
    {
        internal SseParser() { }
        public string LastEventId { get { throw null; } }
        public System.TimeSpan ReconnectionInterval { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Net.ServerSentEvents.SseItem<T>> Enumerate() { throw null; }
        public System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> EnumerateAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
