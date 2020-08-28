// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class LoggingServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddLogging(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddLogging(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Logging.ILoggingBuilder> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging
{
    [System.FlagsAttribute]
    public enum ActivityTrackingOptions
    {
        None = 0,
        SpanId = 1,
        TraceId = 2,
        ParentId = 4,
        TraceState = 8,
        TraceFlags = 16,
    }
    public static partial class FilterLoggingBuilderExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> categoryLevelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Func<string, string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string category, Microsoft.Extensions.Logging.LogLevel level) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string category, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> categoryLevelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, System.Func<string, string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, string category, Microsoft.Extensions.Logging.LogLevel level) { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, string category, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter<T>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter<T>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> categoryLevelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter<T>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string category, Microsoft.Extensions.Logging.LogLevel level) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddFilter<T>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string category, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter<T>(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter<T>(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> categoryLevelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter<T>(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, string category, Microsoft.Extensions.Logging.LogLevel level) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
        public static Microsoft.Extensions.Logging.LoggerFilterOptions AddFilter<T>(this Microsoft.Extensions.Logging.LoggerFilterOptions builder, string category, System.Func<Microsoft.Extensions.Logging.LogLevel, bool> levelFilter) where T : Microsoft.Extensions.Logging.ILoggerProvider { throw null; }
    }
    public partial interface ILoggingBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public partial class LoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory, System.IDisposable
    {
        public LoggerFactory() { }
        public LoggerFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> providers) { }
        public LoggerFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> providers, Microsoft.Extensions.Logging.LoggerFilterOptions filterOptions) { }
        public LoggerFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> providers, Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.LoggerFilterOptions> filterOption) { }
        public LoggerFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> providers, Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.LoggerFilterOptions> filterOption, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Logging.LoggerFactoryOptions> options = null) { }
        public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) { }
        protected virtual bool CheckDisposed() { throw null; }
        public static Microsoft.Extensions.Logging.ILoggerFactory Create(System.Action<Microsoft.Extensions.Logging.ILoggingBuilder> configure) { throw null; }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) { throw null; }
        public void Dispose() { }
    }
    public partial class LoggerFactoryOptions
    {
        public LoggerFactoryOptions() { }
        public Microsoft.Extensions.Logging.ActivityTrackingOptions ActivityTrackingOptions { get { throw null; } set { } }
    }
    public partial class LoggerFilterOptions
    {
        public LoggerFilterOptions() { }
        public bool CaptureScopes { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.LogLevel MinLevel { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Extensions.Logging.LoggerFilterRule> Rules { get { throw null; } }
    }
    public partial class LoggerFilterRule
    {
        public LoggerFilterRule(string providerName, string categoryName, Microsoft.Extensions.Logging.LogLevel? logLevel, System.Func<string, string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { }
        public string CategoryName { get { throw null; } }
        public System.Func<string, string, Microsoft.Extensions.Logging.LogLevel, bool> Filter { get { throw null; } }
        public Microsoft.Extensions.Logging.LogLevel? LogLevel { get { throw null; } }
        public string ProviderName { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public static partial class LoggingBuilderExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddProvider(this Microsoft.Extensions.Logging.ILoggingBuilder builder, Microsoft.Extensions.Logging.ILoggerProvider provider) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder ClearProviders(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder Configure(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.LoggerFactoryOptions> action) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder SetMinimumLevel(this Microsoft.Extensions.Logging.ILoggingBuilder builder, Microsoft.Extensions.Logging.LogLevel level) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public partial class ProviderAliasAttribute : System.Attribute
    {
        public ProviderAliasAttribute(string alias) { }
        public string Alias { get { throw null; } }
    }
}
