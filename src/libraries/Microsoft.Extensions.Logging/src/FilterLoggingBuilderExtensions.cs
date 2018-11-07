// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class FilterLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, string, LogLevel, bool> filter) =>
            builder.ConfigureFilter(options => options.AddFilter(filter));

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(categoryLevelFilter));

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(categoryLevelFilter));

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(levelFilter));

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(levelFilter));

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, LogLevel level) =>
            builder.ConfigureFilter(options => options.AddFilter(category, level));

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, LogLevel level) where T: ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(category, level));

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(category, levelFilter));

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(category, levelFilter));


        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string, string, LogLevel, bool> filter) =>
            AddRule(builder, filter: filter);

        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string, LogLevel, bool> categoryLevelFilter) =>
            AddRule(builder, filter: (type, name, level) => categoryLevelFilter(name, level));

        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<string, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => categoryLevelFilter(name, level));

        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter) =>
            AddRule(builder, filter: (type, name, level) => levelFilter(level));

        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => levelFilter(level));

        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string category, LogLevel level) =>
            AddRule(builder, category: category, level: level);

        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string category, LogLevel level) where T: ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, category: category, level: level);

        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string category, Func<LogLevel, bool> levelFilter) =>
            AddRule(builder, category: category, filter: (type, name, level) => levelFilter(level));

        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, category: category, filter: (type, name, level) => levelFilter(level));

        private static ILoggingBuilder ConfigureFilter(this ILoggingBuilder builder, Action<LoggerFilterOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static LoggerFilterOptions AddRule(LoggerFilterOptions options,
            string type = null,
            string category = null,
            LogLevel? level = null,
            Func<string, string, LogLevel, bool> filter = null)
        {
            options.Rules.Add(new LoggerFilterRule(type, category, level, filter));
            return options;
        }
    }
}