// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.ServerSentEvents
{
    public delegate T SseItemParser<out T>(string eventType, System.ReadOnlySpan<byte> data);
    public readonly partial struct SseItem<T>
    {
        private readonly T _Data_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SseItem(T data, string eventType) { throw null; }
        public T Data { get { throw null; } }
        public string EventType { get { throw null; } }
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
