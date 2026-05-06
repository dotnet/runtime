// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.ServerSentEvents
{
    /// <summary>Represents a server-sent event.</summary>
    /// <typeparam name="T">Specifies the type of data payload in the event.</typeparam>
    public readonly struct SseItem<T>
    {
        /// <summary>The event's type.</summary>
        internal readonly string? _eventType;
        /// <summary>The event's id.</summary>
        private readonly string? _eventId;
        /// <summary>The event's reconnection interval.</summary>
        private readonly TimeSpan? _reconnectionInterval;

        /// <summary>Initializes the server-sent event.</summary>
        /// <param name="data">The event's payload.</param>
        /// <param name="eventType">The event's type.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> contains a line break.</exception>
        public SseItem(T data, string? eventType = null)
        {
            if (eventType?.ContainsLineBreaks() is true)
            {
                ThrowHelper.ThrowArgumentException_CannotContainLineBreaks(nameof(eventType));
            }

            Data = data;
            _eventType = eventType;
        }

        /// <summary>Gets the event's payload.</summary>
        public T Data { get; }

        /// <summary>Gets the event's type.</summary>
        public string EventType => _eventType ?? SseParser.EventTypeDefault;

        /// <summary>Gets the event's id.</summary>
        /// <exception cref="ArgumentException">Thrown when the value contains a line break.</exception>
        public string? EventId
        {
            get => _eventId;
            init
            {
                if (value?.ContainsLineBreaks() is true)
                {
                    ThrowHelper.ThrowArgumentException_CannotContainLineBreaks(nameof(EventId));
                }

                _eventId = value;
            }
        }

        /// <summary>Gets the event's retry interval.</summary>
        /// <remarks>
        /// When specified on an event, instructs the client to update its reconnection time to the specified value.
        /// </remarks>
        public TimeSpan? ReconnectionInterval
        {
            get => _reconnectionInterval;
            init
            {
                if (value < TimeSpan.Zero)
                {
                    ThrowHelper.ThrowArgumentException_CannotBeNegative(nameof(ReconnectionInterval));
                }

                _reconnectionInterval = value;
            }
        }
    }
}
