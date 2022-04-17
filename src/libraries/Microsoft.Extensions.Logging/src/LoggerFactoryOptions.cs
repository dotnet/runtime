// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// The options for a LoggerFactory.
    /// </summary>
    public class LoggerFactoryOptions
    {
        /// <summary>
        /// Creates a new <see cref="LoggerFactoryOptions"/> instance.
        /// </summary>
        public LoggerFactoryOptions() { }

        /// <summary>
        /// Gets or sets <see cref="LoggerFactoryOptions"/> value to indicate which parts of the tracing context information should be included with the logging scopes.
        /// </summary>
        public ActivityTrackingOptions ActivityTrackingOptions { get; set; }
    }
}
