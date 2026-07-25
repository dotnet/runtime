// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class FilterLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="filter">The filter to be added. The filter function receives the provider type full name, the logger category name, and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives three parameters:
        /// <list type="bullet">
        /// <item><description>The full type name of the <see cref="ILoggerProvider"/> (e.g., "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider").</description></item>
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string?, string?, LogLevel, bool> filter) =>
            builder.ConfigureFilter(options => options.AddFilter(filter));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="categoryLevelFilter">The filter to be added. The filter function receives the logger category name and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives two parameters:
        /// <list type="bullet">
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string?, LogLevel, bool> categoryLevelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(categoryLevelFilter));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="categoryLevelFilter">The filter to be added. The filter function receives the logger category name and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and receives two parameters:
        /// <list type="bullet">
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<string?, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(categoryLevelFilter));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="levelFilter">The filter to be added. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(levelFilter));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="levelFilter">The filter to be added. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(levelFilter));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="level">The level to filter.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string? category, LogLevel level) =>
            builder.ConfigureFilter(options => options.AddFilter(category, level));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="level">The level to filter.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string? category, LogLevel level) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(category, level));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified category and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string? category, Func<LogLevel, bool> levelFilter) =>
            builder.ConfigureFilter(options => options.AddFilter(category, levelFilter));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and category and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string? category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            builder.ConfigureFilter(options => options.AddFilter<T>(category, levelFilter));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="filter">The filter function to apply. The filter function receives the provider type full name, the logger category name, and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives three parameters:
        /// <list type="bullet">
        /// <item><description>The full type name of the <see cref="ILoggerProvider"/> (e.g., "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider").</description></item>
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string?, string?, LogLevel, bool> filter) =>
            AddRule(builder, filter: filter);

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="categoryLevelFilter">The filter function to apply. The filter function receives the logger category name and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives two parameters:
        /// <list type="bullet">
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string?, LogLevel, bool> categoryLevelFilter) =>
            AddRule(builder, filter: (type, name, level) => categoryLevelFilter(name, level));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="categoryLevelFilter">The filter function to apply. The filter function receives the logger category name and the log level, and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and receives two parameters:
        /// <list type="bullet">
        /// <item><description>The logger category name (e.g., "MyNamespace.MyClass").</description></item>
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<string?, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => categoryLevelFilter(name, level));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter) =>
            AddRule(builder, filter: (type, name, level) => levelFilter(level));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => levelFilter(level));

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="level">The level to filter.</param>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string? category, LogLevel level) =>
            AddRule(builder, category: category, level: level);

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="level">The level to filter.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string? category, LogLevel level) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, category: category, level: level);

        /// <summary>
        /// Adds a log filter to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified category and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string? category, Func<LogLevel, bool> levelFilter) =>
            AddRule(builder, category: category, filter: (type, name, level) => levelFilter(level));

        /// <summary>
        /// Adds a log filter for the given <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFilterOptions"/> to add the filter to.</param>
        /// <param name="category">The category to filter.</param>
        /// <param name="levelFilter">The filter function to apply. The filter function receives the log level and returns <see langword="true"/> to log the message or <see langword="false"/> to filter it out.</param>
        /// <typeparam name="T">The <see cref="ILoggerProvider"/> which this filter will be added for.</typeparam>
        /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// The filter function is called for each log message from the specified provider and category and receives one parameter:
        /// <list type="bullet">
        /// <item><description>The <see cref="LogLevel"/> of the log message.</description></item>
        /// </list>
        /// Return <see langword="true"/> to allow the message to be logged, or <see langword="false"/> to filter it out.
        /// </remarks>
        public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string? category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider =>
            AddRule(builder, type: typeof(T).FullName, category: category, filter: (type, name, level) => levelFilter(level));

        private static ILoggingBuilder ConfigureFilter(this ILoggingBuilder builder, Action<LoggerFilterOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static LoggerFilterOptions AddRule(LoggerFilterOptions options,
            string? type = null,
            string? category = null,
            LogLevel? level = null,
            Func<string?, string?, LogLevel, bool>? filter = null)
        {
            options.Rules.Add(new LoggerFilterRule(type, category, level, filter));
            return options;
        }
    }
}
