// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides information to guide the production of a strongly-typed logging method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LoggerMessageAttribute : Attribute
    {
#pragma warning disable SA1629 // Documentation text should end with a period
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessageAttribute"/> class
        /// which is used to guide the production of a strongly-typed logging method.
        /// </summary>
        /// <param name="eventId">The stable event id for this log message.</param>
        /// <param name="level">The logging level produced when invoking the strongly-typed logging method.</param>
        /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters.</param>
        /// <remarks>
        /// The method this attribute is applied to:
        ///    - Must be a partial method.
        ///    - Must be a static method.
        ///    - Must return <c>void</c>.
        ///    - Must not be generic.
        ///    - Must have an <see cref="ILogger" /> as first parameter.
        ///    - None of the parameters can be generic.
        /// </remarks>
        /// <example>
        /// static partial class Log
        /// {
        ///     [LoggerMessage(0, LogLevel.Critical, "Could not open socket for {hostName}")]
        ///     static partial void CouldNotOpenSocket(ILogger logger, string hostName);
        /// }
        /// </example>
        public LoggerMessageAttribute(int eventId, LogLevel level, string? message = null)
        {
            (EventId, Level, Message) = (eventId, level, message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessageAttribute"/> class
        /// which is used to guide the production of a strongly-typed logging method.
        /// </summary>
        /// <param name="eventId">The stable event id for this log message.</param>
        /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters.
        /// If this is null, then the message will be an auto-generated JSON fragment containing the logging method arguments.</param>
        /// <remarks>
        /// This overload is not commonly used. In general, the overload that accepts a <see cref="Microsoft.Extensions.Logging.LogLevel" />
        /// value is preferred.
        ///
        /// The method this attribute is applied to:
        ///    - Must be a partial method.
        ///    - Must be a static method.
        ///    - Must return <c>void</c>.
        ///    - Must not be generic.
        ///    - Must have an <see cref="ILogger"/> as first parameter.
        ///    - Must have a <see cref="LogLevel"/> as second parameter.
        ///    - None of the parameters can be generic.
        /// </remarks>
        /// <example>
        /// static partial class Log
        /// {
        ///     [LoggerMessage(0, "Could not open socket for {hostName}")]
        ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
        /// }
        /// </example>
        public LoggerMessageAttribute(int eventId, string? message = null)
        {
            (EventId, Message) = (eventId, message);
        }
#pragma warning restore SA1629 // Documentation text should end with a period

        /// <summary>
        /// Gets the logging event id for the logging method.
        /// </summary>
        public int EventId { get; }

        /// <summary>
        /// Gets or sets the logging event name for the logging method.
        /// </summary>
        /// <remarks>
        /// This will equal the method name if not specified.
        /// </remarks>
        public string? EventName { get; set; }

        /// <summary>
        /// Gets the logging level for the logging method.
        /// </summary>
        public LogLevel? Level { get; }

        /// <summary>
        /// Gets the message text for the logging method.
        /// </summary>
        public string? Message { get; }
    }
}
