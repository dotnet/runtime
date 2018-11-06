// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of IOptions and IOptionsSnapshot.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class OptionsManager<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class, new()
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

        public TOptions Value
        {
            get
            {
                return Get(Options.DefaultName);
            }
        }

        public virtual TOptions Get(string name)
        {
            name = name ?? Options.DefaultName;

            // Store the options in our instance cache
            return _cache.GetOrAdd(name, () => _factory.Create(name));
        }
    }
}