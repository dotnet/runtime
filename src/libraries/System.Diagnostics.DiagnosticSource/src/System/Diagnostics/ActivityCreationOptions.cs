// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityCreationOptions is encapsulating all needed information which will be sent to the ActivityListener to decide about creating the Activity object and with what state.
    /// The possible generic type parameters is <see cref="ActivityContext"/> or <see cref="string"/>
    /// </summary>
    public readonly struct ActivityCreationOptions<T>
    {
        /// <summary>
        /// Construct a new <see cref="ActivityCreationOptions{T}"/> object.
        /// </summary>
        /// <param name="source">The trace Activity source<see cref="ActivitySource"/> used to request creating the Activity object.</param>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="parent">The requested parent to create the Activity object with. The parent either be a parent Id represented as string or it can be a parent context <see cref="ActivityContext"/>.</param>
        /// <param name="kind"><see cref="ActivityKind"/> to create the Activity object with.</param>
        /// <param name="tags">Key-value pairs list for the tags to create the Activity object with.<see cref="ActivityContext"/></param>
        /// <param name="links"><see cref="ActivityLink"/> list to create the Activity object with.</param>
        internal ActivityCreationOptions(ActivitySource source, string name, T parent, ActivityKind kind, IEnumerable<KeyValuePair<string, string?>>? tags, IEnumerable<ActivityLink>? links)
        {
            Source = source;
            Name = name;
            Kind = kind;
            Parent = parent;
            Tags = tags;
            Links = links;
        }

        /// <summary>
        /// Retrieve the <see cref="ActivitySource"/> object.
        /// </summary>
        public ActivitySource Source { get; }

        /// <summary>
        /// Retrieve the name which requested to create the Activity object with.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Retrieve the <see cref="ActivityKind"/> which requested to create the Activity object with.
        /// </summary>
        public ActivityKind Kind { get; }

        /// <summary>
        /// Retrieve the parent which requested to create the Activity object with. Parent will be either in form of string or <see cref="ActivityContext"/>.
        /// </summary>
        public T Parent { get; }

        /// <summary>
        /// Retrieve the tags which requested to create the Activity object with.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string?>>? Tags { get; }

        /// <summary>
        /// Retrieve the list of <see cref="ActivityLink"/> which requested to create the Activity object with.
        /// </summary>
        public IEnumerable<ActivityLink>? Links { get; }
    }
}