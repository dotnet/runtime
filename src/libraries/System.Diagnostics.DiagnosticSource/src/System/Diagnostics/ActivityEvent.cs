// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// A text annotation associated with a collection of tags.
    /// </summary>
    public readonly struct ActivityEvent
    {
        private static readonly ActivityTagsCollection s_emptyTags = new ActivityTagsCollection();

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        public ActivityEvent(string name) : this(name, DateTimeOffset.UtcNow, s_emptyTags)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEvent"/> class.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="timestamp">Event timestamp. Timestamp MUST only be used for the events that happened in the past, not at the moment of this call.</param>
        /// <param name="tags">Event Tags.</param>
        public ActivityEvent(string name, DateTimeOffset timestamp = default, ActivityTagsCollection? tags = null)
        {
            Name = name ?? string.Empty;
            Tags = tags ?? s_emptyTags;
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
        /// Gets the collection of tags associated with the event.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>> Tags { get; }
    }
}
