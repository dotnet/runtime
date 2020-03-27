// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The provider for the <see cref="EventSourceLogger"/>.
    /// </summary>
    [ProviderAlias("EventSource")]
    public class EventSourceLoggerProvider : ILoggerProvider
    {
        private static int _globalFactoryID;

        // A small integer that uniquely identifies the LoggerFactory associated with this LoggingProvider.
        private readonly int _factoryID;

        private EventSourceLogger _loggers; // Linked list of loggers that I have created
        private readonly LoggingEventSource _eventSource;

        /// <inheritdoc />
        public EventSourceLoggerProvider(LoggingEventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }
            _eventSource = eventSource;
            _factoryID = Interlocked.Increment(ref _globalFactoryID);
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers = new EventSourceLogger(categoryName, _factoryID, _eventSource, _loggers);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Turn off any logging
            for (var logger = _loggers; logger != null; logger = logger.Next)
            {
                logger.Level = LogLevel.None;
            }
        }
    }
}
