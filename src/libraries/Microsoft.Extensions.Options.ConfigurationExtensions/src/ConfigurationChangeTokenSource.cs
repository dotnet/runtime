// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        {
        }

        /// <summary>
        /// Constructor taking the <see cref="IConfiguration"/> instance to watch.
        /// </summary>
        /// <param name="name">The name of the options instance being watched.</param>
        /// <param name="config">The configuration instance.</param>
        public ConfigurationChangeTokenSource(string? name, IConfiguration config)
        {
            ThrowHelper.ThrowIfNull(config);

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
