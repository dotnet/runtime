// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// A text annotation associated with a collection of attributes.
    /// </summary>
    public readonly struct ActivityEvent
    {
        private static readonly IEnumerable<KeyValuePair<string, object>> s_emptyAttributes = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        public ActivityEvent(string name) : this(name, DateTimeOffset.UtcNow, s_emptyAttributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="timestamp">Event timestamp. Timestamp MUST only be used for the events that happened in the past, not at the moment of this call.</param>
        public ActivityEvent(string name, DateTimeOffset timestamp) : this(name, timestamp, s_emptyAttributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="attributes">Event attributes.</param>
        public ActivityEvent(string name, IEnumerable<KeyValuePair<string, object>>? attributes) : this(name, default, attributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="timestamp">Event timestamp. Timestamp MUST only be used for the events that happened in the past, not at the moment of this call.</param>
        /// <param name="attributes">Event attributes.</param>
        public ActivityEvent(string name, DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>>? attributes)
        {
            Name = name ?? string.Empty;
            Attributes = attributes ?? s_emptyAttributes;
            Timestamp = timestamp != default ? timestamp : DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the <see cref="ActivityEvent"/> name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="ActivityEvent"/> timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the collection of attributes associated with the event.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Attributes { get; }
    }
}