// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implements <see cref="IOptions{TOptions}"/> and <see cref="IOptionsSnapshot{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public class OptionsManager<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>,
        IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly OptionsCache<TOptions> _cache = new OptionsCache<TOptions>(); // Note: this is a private cache
        private readonly IOptionsMonitorCache<TOptions>? _validatedCache;

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        public OptionsManager(IOptionsFactory<TOptions> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Initializes a new instance with the specified options factory and shared validated cache.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="validatedCache">The shared cache holding options instances validated during startup.</param>
        public OptionsManager(IOptionsFactory<TOptions> factory, IOptionsMonitorCache<TOptions> validatedCache)
            : this(factory)
        {
            // Only consult the shared validated cache for types that have an asynchronous validator.
            // Sync-only types keep the existing per-scope behavior of creating and validating synchronously.
            if (factory is OptionsFactory<TOptions> optionsFactory && optionsFactory.HasAsyncValidators)
            {
                _validatedCache = validatedCache;
            }
        }

        /// <summary>
        /// Gets the default configured <typeparamref name="TOptions"/> instance (equivalent to <c>Get(Options.DefaultName)</c>).
        /// </summary>
        public TOptions Value => Get(Options.DefaultName);

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance. If <see langword="null"/>, <see cref="Options.DefaultName"/>, which is the empty string, is used.</param>
        /// <returns>The <typeparamref name="TOptions"/> instance that matches the given <paramref name="name"/>.</returns>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating the <typeparamref name="TOptions"/> instance created.</exception>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public virtual TOptions Get(string? name)
        {
            name ??= Options.DefaultName;

            if (!_cache.TryGetValue(name, out TOptions? options))
            {
                // Store the options in our instance cache. Avoid closure on fast path by storing state into scoped locals.
                IOptionsFactory<TOptions> localFactory = _factory;
                string localName = name;
                IOptionsMonitorCache<TOptions>? localValidatedCache = _validatedCache;
                options = _cache.GetOrAdd(name, () => CreateValue(localFactory, localName, localValidatedCache));
            }

            return options;
        }

        private static TOptions CreateValue(IOptionsFactory<TOptions> factory, string name, IOptionsMonitorCache<TOptions>? validatedCache)
        {
            // For an async-validated type, prefer the value validated during startup (seeded into the shared cache) so a
            // synchronous snapshot returns the last validated value instead of re-running the throwing synchronous Validate.
            // If nothing has been validated yet, fall back to Create, which fails fast with an actionable message.
            if (validatedCache is OptionsCache<TOptions> optionsCache && optionsCache.TryGetValue(name, out TOptions? validated))
            {
                return validated;
            }

            return factory.Create(name);
        }
    }
}
