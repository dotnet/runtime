// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    /// <summary>
    /// Activity may be linked to zero or more other <see cref="ActivityContext"/> that are causally related.
    /// Links can point to ActivityContexts inside a single Trace or across different Traces.
    /// Links can be used to represent batched operations where a Activity was initiated by multiple initiating Activities,
    /// each representing a single incoming item being processed in the batch.
    /// </summary>
    public readonly partial struct ActivityLink : IEquatable<ActivityLink>
    {
        private readonly Activity.TagsLinkedList? _tags;

        /// <summary>
        /// Construct a new <see cref="ActivityLink"/> object which can be linked to an Activity object.
        /// </summary>
        /// <param name="context">The trace Activity context<see cref="ActivityContext"/></param>
        /// <param name="tags">The key-value pair list of tags which associated to the <see cref="ActivityContext"/></param>
        public ActivityLink(ActivityContext context, ActivityTagsCollection? tags = null)
        {
            Context = context;

            _tags = tags?.Count > 0 ? new Activity.TagsLinkedList(tags) : null;
        }

        /// <summary>
        /// Retrieve the <see cref="ActivityContext"/> object inside this <see cref="ActivityLink"/> object.
        /// </summary>
        public ActivityContext Context { get; }

        /// <summary>
        /// Retrieve the key-value pair list of tags attached with the <see cref="ActivityContext"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags => _tags;

        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is ActivityLink link) && this.Equals(link);

        public bool Equals(ActivityLink value) => Context == value.Context && value.Tags == Tags;
        public static bool operator ==(ActivityLink left, ActivityLink right) => left.Equals(right);
        public static bool operator !=(ActivityLink left, ActivityLink right) => !left.Equals(right);

        /// <summary>
        /// Enumerate the tags attached to this <see cref="ActivityLink"/> object.
        /// </summary>
        /// <returns><see cref="Activity.Enumerator{T}"/>.</returns>
        public Activity.Enumerator<KeyValuePair<string, object?>> EnumerateTagObjects() => new Activity.Enumerator<KeyValuePair<string, object?>>(_tags?.First);
    }
}
