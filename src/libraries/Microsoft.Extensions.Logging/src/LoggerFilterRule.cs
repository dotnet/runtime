// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Defines a rule used to filter log messages
    /// </summary>
    public class LoggerFilterRule
    {
        public LoggerFilterRule(string providerName, string categoryName, LogLevel? logLevel, Func<string, string, LogLevel, bool> filter)
        {
            ProviderName = providerName;
            CategoryName = categoryName;
            LogLevel = logLevel;
            Filter = filter;
        }

        /// <summary>
        /// Gets the logger provider type or alias this rule applies to.
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// Gets the logger category this rule applies to.
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// Gets the minimum <see cref="LogLevel"/> of messages.
        /// </summary>
        public LogLevel? LogLevel { get; }

        /// <summary>
        /// Gets the filter delegate that would be applied to messages that passed the <see cref="LogLevel"/>.
        /// </summary>
        public Func<string, string, LogLevel, bool> Filter { get; }

        public override string ToString()
        {
            return $"{nameof(ProviderName)}: '{ProviderName}', {nameof(CategoryName)}: '{CategoryName}', {nameof(LogLevel)}: '{LogLevel}', {nameof(Filter)}: '{Filter}'";
        }
    }
}