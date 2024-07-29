// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// The options for a LoggerFilter.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class LoggerFilterOptions
    {
        /// <summary>
        /// Creates a new <see cref="LoggerFilterOptions"/> instance.
        /// </summary>
        public LoggerFilterOptions() { }

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
        public IList<LoggerFilterRule> Rules => RulesInternal;

        // Concrete representation of the rule list
        internal List<LoggerFilterRule> RulesInternal { get; } = new List<LoggerFilterRule>();

        internal string DebuggerToString()
        {
            string debugText;
            if (MinLevel != LogLevel.None)
            {
                debugText = $"MinLevel = {MinLevel}";
            }
            else
            {
                // Display "Enabled = false". This makes it clear that the entire ILogger
                // is disabled and nothing is written.
                //
                // If "MinLevel = None" was displayed then someone could think that the
                // min level is disabled and everything is written.
                debugText = $"Enabled = false";
            }

            if (Rules.Count > 0)
            {
                debugText += $", Rules = {Rules.Count}";
            }

            return debugText;
        }
    }
}
