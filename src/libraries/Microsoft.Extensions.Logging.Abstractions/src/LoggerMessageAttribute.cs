// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides information to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <remarks>
    /// <para>The method this attribute is applied to:</para>
    /// <para>   - Must be a partial method.</para>
    /// <para>   - Must return <c>void</c>.</para>
    /// <para>   - Must not be generic.</para>
    /// <para>   - Must have an <see cref="ILogger"/> as one of its parameters.</para>
    /// <para>   - Must have a <see cref="Microsoft.Extensions.Logging.LogLevel"/> as one of its parameters.</para>
    /// <para>   - None of the parameters can be generic.</para>
    /// </remarks>
    /// <example>
    /// <format type="text/markdown"><![CDATA[
    /// ```csharp
    /// static partial class Log
    /// {
    ///     [LoggerMessage(EventId = 0, Message = "Could not open socket for {hostName}")]
    ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
    /// }
    /// ```
    /// ]]></format>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LoggerMessageAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessageAttribute"/> class
        /// which is used to guide the production of a strongly-typed logging method.
        /// </summary>
        public LoggerMessageAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessageAttribute"/> class
        /// which is used to guide the production of a strongly-typed logging method.
        /// </summary>
        /// <param name="eventId">The log event Id.</param>
        /// <param name="level">The log level.</param>
        /// <param name="message">Format string of the log message.</param>
        public LoggerMessageAttribute(int eventId, LogLevel level, string message)
        {
            EventId = eventId;
            Level = level;
            Message = message;
        }

        /// <summary>
        /// Gets the logging event id for the logging method.
        /// </summary>
        public int EventId { get; set; } = -1;

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
        public LogLevel Level { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets the message text for the logging method.
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Gets the flag to skip IsEnabled check for the logging method.
        /// </summary>
        public bool SkipEnabledCheck { get; set; }
    }
}
