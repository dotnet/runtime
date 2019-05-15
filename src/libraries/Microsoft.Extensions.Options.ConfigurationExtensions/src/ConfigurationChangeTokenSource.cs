// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Creates <see cref="IChangeToken"/>s so that <see cref="IOptionsMonitor{TOptions}"/> gets
    /// notified when <see cref="IConfiguration"/> changes.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class ConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
    {
        private IConfiguration _config;

        /// <summary>
        /// Constructor taking the <see cref="IConfiguration"/> instance to watch.
        /// </summary>
        /// <param name="config">The configuration instance.</param>
        public ConfigurationChangeTokenSource(IConfiguration config) : this(Options.DefaultName, config)
        { }

        /// <summary>
        /// Constructor taking the <see cref="IConfiguration"/> instance to watch.
        /// </summary>
        /// <param name="name">The name of the options instance being watche.</param>
        /// <param name="config">The configuration instance.</param>
        public ConfigurationChangeTokenSource(string name, IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _config = config;
            Name = name ?? Options.DefaultName;
        }

        /// <summary>
        /// The name of the option instance being changed.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the reloadToken from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetChangeToken()
        {
            return _config.GetReloadToken();
        }
    }
}
