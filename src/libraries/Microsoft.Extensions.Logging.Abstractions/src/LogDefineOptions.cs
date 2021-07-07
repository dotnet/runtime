// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Options for <see cref="LoggerMessage.Define(LogLevel, EventId, string)"/> and its overloads
    /// </summary>
    public class LogDefineOptions
    {
        /// <summary>
        /// Gets or sets the flag to skip IsEnabled check for the logging method.
        /// </summary>
        public bool SkipEnabledCheck { get; set; }
    }
}
