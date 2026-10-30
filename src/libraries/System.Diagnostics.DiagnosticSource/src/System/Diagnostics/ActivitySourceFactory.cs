// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// A factory for creating <see cref="ActivitySource"/> instances.
    /// </summary>
    /// <remarks>
    /// Activity source factories are responsible for creating and caching activity sources. Derived classes implement
    /// <see cref="CreateCore(ActivitySourceOptions)"/> to provide the actual creation logic; the framework invariants
    /// (null and scope validation, scope assignment) are enforced by the base class.
    /// </remarks>
    public abstract class ActivitySourceFactory : IDisposable
    {
        /// <summary>
        /// Creates an <see cref="ActivitySource"/> using the supplied <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The <see cref="ActivitySourceOptions"/> describing the activity source to create.</param>
        /// <returns>An <see cref="ActivitySource"/> configured with the supplied <paramref name="options"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ActivitySourceOptions.Scope"/> is set to a value other than this factory.</exception>
        /// <remarks>
        /// The base implementation validates <paramref name="options"/>, then constructs a fresh
        /// <see cref="ActivitySourceOptions"/> copy with <see cref="ActivitySourceOptions.Scope"/> bound to this factory
        /// and delegates construction to <see cref="CreateCore(ActivitySourceOptions)"/>. The caller-supplied
        /// <paramref name="options"/> instance is never mutated, so concurrent calls that share an options instance are
        /// safe.
        /// </remarks>
        public ActivitySource Create(ActivitySourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.Scope is not null && !ReferenceEquals(options.Scope, this))
            {
                throw new InvalidOperationException(SR.InvalidActivitySourceScope);
            }

            ActivitySourceOptions scoped = new(options.Name)
            {
                Version = options.Version,
                Tags = options.Tags,
                TelemetrySchemaUrl = options.TelemetrySchemaUrl,
                Scope = this,
            };

            return CreateCore(scoped);
        }

        /// <summary>
        /// Creates an <see cref="ActivitySource"/> with the specified <paramref name="name"/>, <paramref name="version"/>, and <paramref name="tags"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="ActivitySource"/>.</param>
        /// <param name="version">The version of the <see cref="ActivitySource"/>.</param>
        /// <param name="tags">The tags to associate with the <see cref="ActivitySource"/>.</param>
        /// <returns>An <see cref="ActivitySource"/> with the specified <paramref name="name"/>, <paramref name="version"/>, and <paramref name="tags"/>.</returns>
        public ActivitySource Create(string name, string? version = "", IEnumerable<KeyValuePair<string, object?>>? tags = null)
        {
            ActivitySourceOptions options = new(name)
            {
                Version = version,
                Tags = tags,
            };

            return Create(options);
        }

        /// <summary>
        /// When overridden in a derived class, creates the <see cref="ActivitySource"/> for the supplied <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The <see cref="ActivitySourceOptions"/> describing the activity source to create.
        /// <see cref="ActivitySourceOptions.Scope"/> is guaranteed to be set to this factory.</param>
        /// <returns>An <see cref="ActivitySource"/> configured with the supplied <paramref name="options"/>.</returns>
        /// <remarks>
        /// Derived classes implement this method to perform the actual creation (and optional caching) of the
        /// <see cref="ActivitySource"/>. The supplied <paramref name="options"/> have already been validated and the
        /// <see cref="ActivitySourceOptions.Scope"/> property has been set to this factory instance; derived classes
        /// should forward the options to the <see cref="ActivitySource"/> constructor unchanged.
        /// </remarks>
        protected abstract ActivitySource CreateCore(ActivitySourceOptions options);

        /// <summary>
        /// Releases all resources used by the <see cref="ActivitySourceFactory"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ActivitySourceFactory"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
