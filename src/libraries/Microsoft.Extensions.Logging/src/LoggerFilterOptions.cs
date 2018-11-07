// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    public class LoggerFilterOptions
    {
        /// <summary>
        /// Gets or sets value indicating whether logging scopes are being captured. Defaults to <c>true</c>
        /// </summary>
        public bool CaptureScopes { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum level of log messages if none of the rules match.
        /// </summary>
        public LogLevel MinLevel { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="LoggerFilterRule"/> used for filtering log messages.
        /// </summary>
        public IList<LoggerFilterRule> Rules { get; } = new List<LoggerFilterRule>();
    }
}