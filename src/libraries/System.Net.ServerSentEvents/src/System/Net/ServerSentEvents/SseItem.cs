// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.ServerSentEvents
{
    /// <summary>Represents a server-sent event.</summary>
    /// <typeparam name="T">Specifies the type of data payload in the event.</typeparam>
    public readonly struct SseItem<T>
    {
        /// <summary>Initializes the server-sent event.</summary>
        /// <param name="data">The event's payload.</param>
        /// <param name="eventType">The event's type.</param>
        public SseItem(T data, string eventType)
        {
            Data = data;
            EventType = eventType;
        }

        /// <summary>Gets the event's payload.</summary>
        public T Data { get; }

        /// <summary>Gets the event's type.</summary>
        public string EventType { get; }
    }
}
