// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// The provider for the <see cref="EventLogLogger"/>.
    /// </summary>
    [ProviderAlias("EventLog")]
    public class EventLogLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        internal readonly EventLogSettings _settings;

        private IExternalScopeProvider? _scopeProvider;

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
        public EventLogLoggerProvider(EventLogSettings? settings)
        {
            _settings = settings ?? new EventLogSettings();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLoggerProvider"/> class.
        /// </summary>
        /// <param name="options">The <see cref="IOptions{EventLogSettings}"/>.</param>
        public EventLogLoggerProvider(IOptions<EventLogSettings> options)
            : this(options.Value)
        {
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new EventLogLogger(name, _settings, _scopeProvider);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_settings.EventLog is WindowsEventLog windowsEventLog)
            {
                windowsEventLog.DiagnosticsEventLog.Dispose();
            }
        }

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
