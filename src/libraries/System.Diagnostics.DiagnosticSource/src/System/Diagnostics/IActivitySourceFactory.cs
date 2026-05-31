// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// A factory for creating <see cref="ActivitySource"/> instances.
    /// </summary>
    /// <remarks>
    /// Activity source factories are responsible for creating and caching activity sources.
    /// </remarks>
    public interface IActivitySourceFactory : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ActivitySource"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="ActivitySourceOptions"/> to use when creating the activity source.</param>
        /// <returns>A new <see cref="ActivitySource"/> instance.</returns>
        ActivitySource Create(ActivitySourceOptions options);
    }
}
