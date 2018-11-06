// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// The provider for the <see cref="EventLogLogger"/>.
    /// </summary>
    [ProviderAlias("EventLog")]
    public class EventLogLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly EventLogSettings _settings;

        private IExternalScopeProvider _scopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLoggerProvider"/> class.
        /// </summary>
        public EventLogLoggerProvider()
            : this(settings: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLoggerProvider"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        public EventLogLoggerProvider(EventLogSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new EventLogLogger(name, _settings ?? new EventLogSettings());
        }

        public void Dispose()
        {
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
