// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpClientBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddDefaultLogger(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.DelegatingHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.DelegatingHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler<THandler>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where THandler : System.Net.Http.DelegatingHandler { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddLogger(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, Microsoft.Extensions.Http.Logging.IHttpClientLogger> httpClientLoggerFactory, bool wrapHandlersPipeline = false) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddLogger<TLogger>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, bool wrapHandlersPipeline = false) where TLogger : Microsoft.Extensions.Http.Logging.IHttpClientLogger { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpClient, System.IServiceProvider, TClient> factory) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpClient, TClient> factory) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureAdditionalHttpMessageHandlers(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.Collections.Generic.IList<System.Net.Http.DelegatingHandler>, System.IServiceProvider> configureAdditionalHandlers) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpClient(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpClient(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.Net.Http.HttpClient> configureClient) { throw null; }
        [System.Obsolete("This method has been deprecated. Use ConfigurePrimaryHttpMessageHandler or ConfigureAdditionalHttpMessageHandlers instead.")]
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpMessageHandlerBuilder(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> configureBuilder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.HttpMessageHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpMessageHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where THandler : System.Net.Http.HttpMessageHandler { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.Net.Http.HttpMessageHandler, System.IServiceProvider> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder RedactLoggedHeaders(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Collections.Generic.IEnumerable<string> redactedLoggedHeaderNames) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder RedactLoggedHeaders(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<string, bool> shouldRedactHeaderValue) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder RemoveAllLoggers(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder SetHandlerLifetime(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.TimeSpan handlerLifetime) { throw null; }
#if NET
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder UseSocketsHttpHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.Net.Http.SocketsHttpHandler, System.IServiceProvider>? configureHandler = null) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder UseSocketsHttpHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<Microsoft.Extensions.DependencyInjection.ISocketsHttpHandlerBuilder> configureBuilder) { throw null; }
#endif
    }
    public static partial class HttpClientFactoryServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.Net.Http.HttpClient, System.IServiceProvider, TImplementation> factory) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.Net.Http.HttpClient, TImplementation> factory) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Func<System.Net.Http.HttpClient, System.IServiceProvider, TImplementation> factory) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Func<System.Net.Http.HttpClient, TImplementation> factory) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureHttpClientDefaults(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.DependencyInjection.IHttpClientBuilder> configure) { throw null; }
    }
    public partial interface IHttpClientBuilder
    {
        string Name { get; }
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
#if NET
    public partial interface ISocketsHttpHandlerBuilder
    {
        string Name { get; }
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
#endif
#if NET
    public static partial class SocketsHttpHandlerBuilderExtensions
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static Microsoft.Extensions.DependencyInjection.ISocketsHttpHandlerBuilder Configure(this Microsoft.Extensions.DependencyInjection.ISocketsHttpHandlerBuilder builder, System.Action<System.Net.Http.SocketsHttpHandler, System.IServiceProvider> configure) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static Microsoft.Extensions.DependencyInjection.ISocketsHttpHandlerBuilder Configure(this Microsoft.Extensions.DependencyInjection.ISocketsHttpHandlerBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
    }
#endif
}
namespace Microsoft.Extensions.Http
{
    public partial class HttpClientFactoryOptions
    {
        public HttpClientFactoryOptions() { }
        public System.TimeSpan HandlerLifetime { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Action<System.Net.Http.HttpClient>> HttpClientActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Func<string, bool> ShouldRedactHeaderValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressHandlerScope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class HttpMessageHandlerBuilder
    {
        protected HttpMessageHandlerBuilder() { }
        public abstract System.Collections.Generic.IList<System.Net.Http.DelegatingHandler> AdditionalHandlers { get; }
        [System.Diagnostics.CodeAnalysis.DisallowNull]
        public abstract string? Name { get; set; }
        public abstract System.Net.Http.HttpMessageHandler PrimaryHandler { get; set; }
        public virtual System.IServiceProvider Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract System.Net.Http.HttpMessageHandler Build();
        protected internal static System.Net.Http.HttpMessageHandler CreateHandlerPipeline(System.Net.Http.HttpMessageHandler primaryHandler, System.Collections.Generic.IEnumerable<System.Net.Http.DelegatingHandler> additionalHandlers) { throw null; }
    }
    public partial interface IHttpMessageHandlerBuilderFilter
    {
        System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> Configure(System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> next);
    }
    public partial interface ITypedHttpClientFactory<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>
    {
        TClient CreateClient(System.Net.Http.HttpClient httpClient);
    }
}
namespace Microsoft.Extensions.Http.Logging
{
    public partial class LoggingHttpMessageHandler : System.Net.Http.DelegatingHandler
    {
        public LoggingHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger) { }
        public LoggingHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Http.HttpClientFactoryOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class LoggingScopeHttpMessageHandler : System.Net.Http.DelegatingHandler
    {
        public LoggingScopeHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger) { }
        public LoggingScopeHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Http.HttpClientFactoryOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial interface IHttpClientAsyncLogger : Microsoft.Extensions.Http.Logging.IHttpClientLogger
    {
        System.Threading.Tasks.ValueTask<object?> LogRequestStartAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.ValueTask LogRequestStopAsync(object? context, System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpResponseMessage response, System.TimeSpan elapsed, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.ValueTask LogRequestFailedAsync(object? context, System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpResponseMessage? response, System.Exception exception, System.TimeSpan elapsed, System.Threading.CancellationToken cancellationToken = default);
    }
    public partial interface IHttpClientLogger
    {
        object? LogRequestStart(System.Net.Http.HttpRequestMessage request);
        void LogRequestStop(object? context, System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpResponseMessage response, System.TimeSpan elapsed);
        void LogRequestFailed(object? context, System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpResponseMessage? response, System.Exception exception, System.TimeSpan elapsed);
    }
}
namespace System.Net.Http
{
    public static partial class HttpClientFactoryExtensions
    {
        public static System.Net.Http.HttpClient CreateClient(this System.Net.Http.IHttpClientFactory factory) { throw null; }
    }
    public static partial class HttpMessageHandlerFactoryExtensions
    {
        public static System.Net.Http.HttpMessageHandler CreateHandler(this System.Net.Http.IHttpMessageHandlerFactory factory) { throw null; }
    }
    public partial interface IHttpClientFactory
    {
        System.Net.Http.HttpClient CreateClient(string name);
    }
    public partial interface IHttpMessageHandlerFactory
    {
        System.Net.Http.HttpMessageHandler CreateHandler(string name);
    }
}
