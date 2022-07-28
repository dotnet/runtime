// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Defines a rule used to filter log messages
    /// </summary>
    public class LoggerFilterRule
    {
        /// <summary>
        /// Creates a new <see cref="LoggerFilterRule"/> instance.
        /// </summary>
        /// <param name="providerName">The provider name to use in this filter rule.</param>
        /// <param name="categoryName">The category name to use in this filter rule.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
        /// <param name="filter">The filter to apply.</param>
        public LoggerFilterRule(string? providerName, string? categoryName, LogLevel? logLevel, Func<string?, string?, LogLevel, bool>? filter)
        {
            ProviderName = providerName;
            CategoryName = categoryName;
            LogLevel = logLevel;
            Filter = filter;
        }

        /// <summary>
        /// Gets the logger provider type or alias this rule applies to.
        /// </summary>
        public string? ProviderName { get; }

        /// <summary>
        /// Gets the logger category this rule applies to.
        /// </summary>
        public string? CategoryName { get; }

        /// <summary>
        /// Gets the minimum <see cref="LogLevel"/> of messages.
        /// </summary>
        public LogLevel? LogLevel { get; }

        /// <summary>
        /// Gets the filter delegate that would be applied to messages that passed the <see cref="LogLevel"/>.
        /// </summary>
        public Func<string?, string?, LogLevel, bool>? Filter { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(ProviderName)}: '{ProviderName}', {nameof(CategoryName)}: '{CategoryName}', {nameof(LogLevel)}: '{LogLevel}', {nameof(Filter)}: '{Filter}'";
        }
    }
}
