// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IOptions{TOptions}"/> and <see cref="IOptionsSnapshot{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    public class OptionsManager<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>,
        IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly OptionsCache<TOptions> _cache = new OptionsCache<TOptions>(); // Note: this is a private cache

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        public OptionsManager(IOptionsFactory<TOptions> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// The default configured <typeparamref name="TOptions"/> instance, equivalent to Get(Options.DefaultName).
        /// </summary>
        public TOptions Value
        {
            get
            {
                return Get(Options.DefaultName);
            }
        }

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        public virtual TOptions Get(string name)
        {
            name = name ?? Options.DefaultName;

            // Store the options in our instance cache
            return _cache.GetOrAdd(name, () => _factory.Create(name));
        }
    }
}
