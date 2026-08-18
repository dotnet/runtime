// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The provider for the <see cref="EventSourceLogger"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider creates a new <see cref="EventSourceLogger"/> instance for each call to <see cref="CreateLogger(string)"/>
    /// and does not cache logger instances. Callers are responsible for caching logger instances if needed to avoid creating
    /// multiple loggers for the same category name.
    /// </para>
    /// <para>
    /// The provider maintains a linked list of all created loggers to support dynamic configuration changes through EventSource/ETW infrastructure.
    /// </para>
    /// </remarks>
    [ProviderAlias("EventSource")]
    public class EventSourceLoggerProvider : ILoggerProvider
    {
        private static int _globalFactoryID;

        // A small integer that uniquely identifies the LoggerFactory associated with this LoggingProvider.
        private readonly int _factoryID;

        private EventSourceLogger? _loggers; // Linked list of loggers that I have created
        private readonly LoggingEventSource _eventSource;

        /// <summary>
        /// Creates an instance of <see cref="EventSourceLoggerProvider"/>.
        /// </summary>
        /// <param name="eventSource">The logging event source.</param>
        public EventSourceLoggerProvider(LoggingEventSource eventSource)
        {
            ArgumentNullException.ThrowIfNull(eventSource);

            _eventSource = eventSource;
            _factoryID = Interlocked.Increment(ref _globalFactoryID);
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method creates a new <see cref="EventSourceLogger"/> instance for each call and does not cache logger instances.
        /// Callers should cache the returned logger if the same category name will be used multiple times to avoid creating
        /// unnecessary logger instances.
        /// </remarks>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers = new EventSourceLogger(categoryName, _factoryID, _eventSource, _loggers);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Turn off any logging
            for (EventSourceLogger? logger = _loggers; logger != null; logger = logger.Next)
            {
                logger.Level = LogLevel.None;
            }
        }
    }
}
