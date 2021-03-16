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
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public sealed partial class LoggerMessageAttribute : System.Attribute
    {
        public LoggerMessageAttribute(int eventId, Microsoft.Extensions.Logging.LogLevel level, string? message = null) { }
        public LoggerMessageAttribute(int eventId, string? message = null) { }
        public int EventId { get { throw null; } }
        public string? EventName { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.LogLevel? Level { get { throw null; } }
        public string? Message { get { throw null; } }
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
namespace Microsoft.Extensions.Logging.Internal
{
    public sealed partial class LogValues : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues, System.Exception?, string> formatFunc) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValuesN : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValuesN(System.Func<Microsoft.Extensions.Logging.Internal.LogValuesN, System.Exception?, string> formatFunc, System.Collections.Generic.KeyValuePair<string, object>[] kvp) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T>, System.Exception?, string> formatFunc, string name, T value) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T Value { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T1, T2> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T1, T2>, System.Exception?, string> formatFunc, string[] names, T1 value1, T2 value2) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T1 Value1 { get { throw null; } }
        public T2 Value2 { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T1, T2, T3> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T1, T2, T3>, System.Exception?, string> formatFunc, string[] names, T1 value1, T2 value2, T3 value3) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T1 Value1 { get { throw null; } }
        public T2 Value2 { get { throw null; } }
        public T3 Value3 { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T1, T2, T3, T4> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T1, T2, T3, T4>, System.Exception?, string> formatFunc, string[] names, T1 value1, T2 value2, T3 value3, T4 value4) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T1 Value1 { get { throw null; } }
        public T2 Value2 { get { throw null; } }
        public T3 Value3 { get { throw null; } }
        public T4 Value4 { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T1, T2, T3, T4, T5> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T1, T2, T3, T4, T5>, System.Exception?, string> formatFunc, string[] names, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T1 Value1 { get { throw null; } }
        public T2 Value2 { get { throw null; } }
        public T3 Value3 { get { throw null; } }
        public T4 Value4 { get { throw null; } }
        public T5 Value5 { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class LogValues<T1, T2, T3, T4, T5, T6> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable
    {
        public LogValues(System.Func<Microsoft.Extensions.Logging.Internal.LogValues<T1, T2, T3, T4, T5, T6>, System.Exception?, string> formatFunc, string[] names, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index] { get { throw null; } }
        public T1 Value1 { get { throw null; } }
        public T2 Value2 { get { throw null; } }
        public T3 Value3 { get { throw null; } }
        public T4 Value4 { get { throw null; } }
        public T5 Value5 { get { throw null; } }
        public T6 Value6 { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
}
