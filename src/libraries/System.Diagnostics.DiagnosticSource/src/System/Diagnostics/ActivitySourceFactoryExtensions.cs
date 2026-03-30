// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Extension methods for <see cref="ActivitySource"/> and <see cref="IActivitySourceFactory"/>.
    /// </summary>
    public static class ActivitySourceFactoryExtensions
    {
        /// <summary>
        /// Creates an <see cref="ActivitySource"/> with the specified <paramref name="name"/>, <paramref name="version"/>, and <paramref name="tags"/>.
        /// </summary>
        /// <param name="activitySourceFactory">The <see cref="IActivitySourceFactory"/> to use to create the <see cref="ActivitySource"/>.</param>
        /// <param name="name">The name of the <see cref="ActivitySource"/>.</param>
        /// <param name="version">The version of the <see cref="ActivitySource"/>.</param>
        /// <param name="tags">The tags to associate with the <see cref="ActivitySource"/>.</param>
        /// <returns>An <see cref="ActivitySource"/> with the specified <paramref name="name"/>, <paramref name="version"/>, and <paramref name="tags"/>.</returns>
        public static ActivitySource Create(this IActivitySourceFactory activitySourceFactory, string name, string? version = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)
        {
            ArgumentNullException.ThrowIfNull(activitySourceFactory);

            return activitySourceFactory.Create(new ActivitySourceOptions(name)
            {
                Version = version,
                Tags = tags,
                Scope = activitySourceFactory,
            });
        }
    }
}
