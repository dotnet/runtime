// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class HttpClientKeyedRegistrationTest
    {
        public const string Test = "test";
        public const string Other = "other";
        public const string Disabled = "disabled";
        public const string KeyedDefaults = "keyed-defaults";
        public const string Absent = "absent";

        [Fact]
        public void HttpClient_RespectsSingletonLifetime()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test)
                .AsKeyed(ServiceLifetime.Singleton);

            serviceCollection.AddTransient<KeyedClientTestService>(); // [FromKeyedServices(Test)] HttpClient

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var factory = rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Same(GetKeyedClient(rootServices), GetKeyedClient(rootServices));
            Assert.Same(
                rootServices.GetRequiredService<KeyedClientTestService>().HttpClient, // same singleton instance injected into a transient service
                GetKeyedClient(rootServices));
            Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(rootServices)); // factory creates a new instance each time

            var scopeA = rootServices.CreateScope();
            Assert.Same(GetKeyedClient(scopeA), GetKeyedClient(scopeA));
            Assert.Same(
                scopeA.ServiceProvider.GetRequiredService<KeyedClientTestService>().HttpClient,
                GetKeyedClient(scopeA));
            Assert.Same(GetKeyedClient(rootServices), GetKeyedClient(scopeA));
            Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(scopeA)); // factory creates a new instance each time

            var scopeB = rootServices.CreateScope();
            Assert.Same(GetKeyedClient(scopeB), GetKeyedClient(scopeB));
            Assert.Same(GetKeyedClient(scopeA), GetKeyedClient(scopeB));

            var clientA = GetKeyedClient(scopeA);
            var factoryClient = factory.CreateClient(Test);
            AssertAlive(clientA);
            AssertAlive(factoryClient);

            scopeA.Dispose();
            AssertAlive(clientA);
            AssertAlive(factoryClient);
        }

        [Fact]
        public void HttpClient_RespectsTransientLifetime()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test)
                .AsKeyed(ServiceLifetime.Transient);

            serviceCollection.AddTransient<KeyedClientTestService>(); // [FromKeyedServices(Test)] HttpClient

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var factory = rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.NotSame(GetKeyedClient(rootServices), GetKeyedClient(rootServices));
            Assert.NotSame(
                rootServices.GetRequiredService<KeyedClientTestService>().HttpClient,
                GetKeyedClient(rootServices));
            Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(rootServices));

            var scopeA = rootServices.CreateScope();
            Assert.NotSame(GetKeyedClient(scopeA), GetKeyedClient(scopeA));
            Assert.NotSame(
                scopeA.ServiceProvider.GetRequiredService<KeyedClientTestService>().HttpClient,
                GetKeyedClient(scopeA));
            Assert.NotSame(GetKeyedClient(rootServices), GetKeyedClient(scopeA));
            Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(scopeA));

            var scopeB = rootServices.CreateScope();
            Assert.NotSame(GetKeyedClient(scopeB), GetKeyedClient(scopeB));
            Assert.NotSame(GetKeyedClient(scopeA), GetKeyedClient(scopeB));

            var clientA = GetKeyedClient(scopeA);
            var clientB = GetKeyedClient(scopeB);
            var rootClient = GetKeyedClient(rootServices);
            var factoryClient = factory.CreateClient(Test);
            AssertAlive(clientA);
            AssertAlive(rootClient);
            AssertAlive(factoryClient);

            scopeA.Dispose();
            AssertDisposed(clientA); // transient instance disposed with the scope
            AssertAlive(clientB);
            AssertAlive(rootClient);
            AssertAlive(factoryClient);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClient_RespectsScopedLifetime(bool validateScopes)
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test)
                .AsKeyed(ServiceLifetime.Scoped);

            serviceCollection.AddTransient<KeyedClientTestService>(); // [FromKeyedServices(Test)] HttpClient

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes);
            var factory = rootServices.GetRequiredService<IHttpClientFactory>();

            if (validateScopes)
            {
                Assert.Throws<InvalidOperationException>(() => GetKeyedClient(rootServices)); // root scope is invalid
            }
            else
            {
                Assert.Same(GetKeyedClient(rootServices), GetKeyedClient(rootServices)); // root-capturing scoped instance
                Assert.Same(
                    rootServices.GetRequiredService<KeyedClientTestService>().HttpClient, // same root-captured instance injected into a transient service
                    GetKeyedClient(rootServices));
                Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(rootServices));
            }

            var scopeA = rootServices.CreateScope();
            Assert.Same(GetKeyedClient(scopeA), GetKeyedClient(scopeA));
            Assert.Same(
                scopeA.ServiceProvider.GetRequiredService<KeyedClientTestService>().HttpClient,  // same scoped instance injected into a transient service
                GetKeyedClient(scopeA));
            Assert.NotSame(factory.CreateClient(Test), GetKeyedClient(scopeA));
            if (!validateScopes)
            {
                Assert.NotSame(GetKeyedClient(rootServices), GetKeyedClient(scopeA));
            }

            var scopeB = rootServices.CreateScope();
            Assert.Same(GetKeyedClient(scopeB), GetKeyedClient(scopeB));
            Assert.NotSame(GetKeyedClient(scopeA), GetKeyedClient(scopeB));

            var clientA = GetKeyedClient(scopeA);
            var clientB = GetKeyedClient(scopeB);
            var rootClient = validateScopes ? null! : GetKeyedClient(rootServices);
            var factoryClient = factory.CreateClient(Test);
            AssertAlive(clientA);
            AssertAlive(clientB);
            AssertAlive(rootClient, skipIfNull: validateScopes);
            AssertAlive(factoryClient);

            scopeA.Dispose();
            AssertDisposed(clientA); // scoped instance disposed with the scope
            AssertAlive(clientB);
            AssertAlive(rootClient, skipIfNull: validateScopes);
            AssertAlive(factoryClient);
        }

        [Fact]
        public void HttpClient_ResolvedAsKeyedService()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Scoped);
            AddConfiguredClient(serviceCollection, Other).AsKeyed(ServiceLifetime.Scoped);
            AddConfiguredClient(serviceCollection, Disabled).DropKeyed();
            AddConfiguredClient(serviceCollection, KeyedDefaults); // no Keyed APIs called

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            AssertConfigured(GetKeyedClient(services, Test), Test);
            AssertConfigured(GetKeyedClient(services, Other), Other);
            Assert.Null(GetKeyedClientOrNull(services, Disabled));
            Assert.Null(GetKeyedClientOrNull(services, KeyedDefaults));
            Assert.Null(GetKeyedClientOrNull(services, Absent));

            var factory = services.GetRequiredService<IHttpClientFactory>();

            AssertConfigured(factory.CreateClient(Test), Test);
            AssertConfigured(factory.CreateClient(Other), Other);
            AssertConfigured(factory.CreateClient(Disabled), Disabled);
            AssertConfigured(factory.CreateClient(KeyedDefaults), KeyedDefaults);

            var absentClient = factory.CreateClient(Absent); // it's possible to create a (default) client for a name that wasn't explicitly registered
            Assert.Null(absentClient.BaseAddress); // not configured
        }

        [Fact]
        public void HttpClient_InjectedAsKeyedService()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Scoped);
            serviceCollection.AddTransient<KeyedClientTestService>(); // [FromKeyedServices(Test)] HttpClient

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            var service = services.GetRequiredService<KeyedClientTestService>();
            AssertConfigured(service.HttpClient, Test);
        }

        [Fact]
        public void HttpMessageHandler_ResolvedAsKeyedService()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Scoped);
            AddConfiguredClient(serviceCollection, Other).AsKeyed(ServiceLifetime.Scoped);
            AddConfiguredClient(serviceCollection, Disabled).DropKeyed();
            AddConfiguredClient(serviceCollection, KeyedDefaults); // no Keyed APIs called

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            AssertConfigured(GetKeyedHandler(services, Test), Test);
            AssertConfigured(GetKeyedHandler(services, Other), Other);
            Assert.Null(GetKeyedHandlerOrNull(services, Disabled));
            Assert.Null(GetKeyedHandlerOrNull(services, KeyedDefaults));
            Assert.Null(GetKeyedHandlerOrNull(services, Absent));

            var factory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            AssertConfigured(factory.CreateHandler(Test), Test);
            AssertConfigured(factory.CreateHandler(Other), Other);
            AssertConfigured(factory.CreateHandler(Disabled), Disabled);
            AssertConfigured(factory.CreateHandler(KeyedDefaults), KeyedDefaults);

            var absentHandler = factory.CreateHandler(Absent); // it's possible to create a (default) handler for a name that wasn't explicitly registered

            Type defaultPrimaryHandlerType =
#if NET
                SocketsHttpHandler.IsSupported ? typeof(SocketsHttpHandler) :
#endif
                typeof(HttpClientHandler);

            Assert.Equal(defaultPrimaryHandlerType, GetPrimaryHandler(absentHandler).GetType()); // not configured
        }

        [Fact]
        public void HttpMessageHandler_InjectedAsKeyedService()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Scoped);
            serviceCollection.AddTransient<KeyedHandlerTestService>(); // [FromKeyedServices(Test)] HttpMessageHandler

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            var service = services.GetRequiredService<KeyedHandlerTestService>();
            AssertConfigured(service.Handler, Test);
        }

        [Fact]
        public void HttpClient_LastRegistrationWins()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredClient(serviceCollection, Test)
                .AsKeyed(ServiceLifetime.Transient)
                .DropKeyed()
                .AsKeyed(ServiceLifetime.Scoped);

            AddConfiguredClient(serviceCollection, Other)
                .DropKeyed()
                .AsKeyed(ServiceLifetime.Scoped)
                .AsKeyed(ServiceLifetime.Transient);

            AddConfiguredClient(serviceCollection, Disabled)
                .AsKeyed(ServiceLifetime.Transient)
                .AsKeyed(ServiceLifetime.Singleton)
                .DropKeyed();

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.Same(GetKeyedClient(services, Test), GetKeyedClient(services, Test)); // scoped was last
            Assert.NotSame(GetKeyedClient(services, Other), GetKeyedClient(services, Other)); // transient was last
            Assert.Null(GetKeyedClientOrNull(services, Disabled)); // DropKeyed was last
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientDefaults_KeyedByDefault(bool defaultsFirst)
        {
            var serviceCollection = new ServiceCollection();

            void SetupDefaults() => serviceCollection.ConfigureHttpClientDefaults(b => b.AsKeyed(ServiceLifetime.Scoped));

            void SetupNamedClients()
            {
                AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Transient);
                AddConfiguredClient(serviceCollection, Disabled).DropKeyed();
                AddConfiguredClient(serviceCollection, KeyedDefaults); // no Keyed APIs called
            }

            if (defaultsFirst)
            {
                SetupDefaults();
                SetupNamedClients();
            }
            else
            {
                SetupNamedClients();
                SetupDefaults();
            }

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.NotSame(GetKeyedClient(services, Test), GetKeyedClient(services, Test)); // per-name config should win
            Assert.Null(GetKeyedClientOrNull(services, Disabled)); // per-name config should win
            Assert.Same(GetKeyedClient(services, KeyedDefaults), GetKeyedClient(services, KeyedDefaults)); // defaults only
            Assert.Same(GetKeyedClient(services, Absent), GetKeyedClient(services, Absent)); // defaults applied for absent as well
            Assert.NotSame(GetKeyedClient(services, Absent), GetKeyedClient(services, "other-absent")); // absent clients are still different per name
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientDefaults_DropKeyedByDefault(bool defaultsFirst)
        {
            var serviceCollection = new ServiceCollection();

            void SetupDefaults() => serviceCollection.ConfigureHttpClientDefaults(b => b.DropKeyed());

            void SetupNamedClients()
            {
                AddConfiguredClient(serviceCollection, Test).AsKeyed(ServiceLifetime.Scoped);
                AddConfiguredClient(serviceCollection, Disabled).DropKeyed();
                AddConfiguredClient(serviceCollection, KeyedDefaults); // no Keyed APIs called
            }

            if (defaultsFirst)
            {
                SetupDefaults();
                SetupNamedClients();
            }
            else
            {
                SetupNamedClients();
                SetupDefaults();
            }

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.Same(GetKeyedClient(services, Test), GetKeyedClient(services, Test)); // per-name config should win
            Assert.Null(GetKeyedClientOrNull(services, Disabled));
            Assert.Null(GetKeyedClientOrNull(services, KeyedDefaults));
            Assert.Null(GetKeyedClientOrNull(services, Absent));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientDefaults_LastRegistrationWins(bool dropKeyedByDefault)
        {
            var serviceCollection = new ServiceCollection();

            Action<IHttpClientBuilder> finalConfigureDefaults = dropKeyedByDefault
                ? b => b.DropKeyed()
                : b => b.AsKeyed(ServiceLifetime.Scoped);

            AddConfiguredClient(serviceCollection, Test)
                .DropKeyed();                          // #4 [####.....] tst-1

            AddConfiguredClient(serviceCollection, Other)
                .AsKeyed(ServiceLifetime.Transient);   // #5 [#####....] oth-1

            AddConfiguredClient(serviceCollection, Disabled)
                .AsKeyed(ServiceLifetime.Scoped);      // #6 [######...] dis-1

            // this adds to existing configuration
            serviceCollection.AddHttpClient(Other)
                .AsKeyed(ServiceLifetime.Scoped);      // #7 [#######..] oth-2 [fin] -> scoped

            serviceCollection.ConfigureHttpClientDefaults(b =>
                b.AsKeyed(ServiceLifetime.Transient)); // #1 [#........]

            // no keyed APIs called
            serviceCollection.AddHttpClient(KeyedDefaults); //           key-0 [fin] -> (#3)

            serviceCollection.ConfigureHttpClientDefaults(b =>
                b.DropKeyed());                        // #2 [##.......]

            // this adds to existing configuration
            serviceCollection.AddHttpClient(Test)
                .AsKeyed(ServiceLifetime.Transient);   // #8 [########.] tst-2 [fin] -> transient


            serviceCollection.ConfigureHttpClientDefaults(
                finalConfigureDefaults);               // #3 [###......]

            // this adds to existing configuration
            serviceCollection.AddHttpClient(Disabled)
                .DropKeyed();                          // #9 [#########] dis-2 [fin] -> disabled


            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.NotSame(GetKeyedClient(services, Test), GetKeyedClient(services, Test)); // tst-2 [fin] -> transient
            Assert.Same(GetKeyedClient(services, Other), GetKeyedClient(services, Other)); // oth-2 [fin] -> scoped
            Assert.Null(GetKeyedClientOrNull(services, Disabled)); //  dis-2 [fin] -> disabled

            if (dropKeyedByDefault)
            {
                Assert.Null(GetKeyedClientOrNull(services, KeyedDefaults)); // key-0 [fin] -> (#3) -> disabled
                Assert.Null(GetKeyedClientOrNull(services, Absent));
            }
            else
            {
                Assert.Same(GetKeyedClient(services, KeyedDefaults), GetKeyedClient(services, KeyedDefaults)); // key-0 [fin] -> (#3) -> scoped
                Assert.Same(GetKeyedClient(services, Absent), GetKeyedClient(services, Absent));
            }
        }

        private static HttpMessageHandler GetPrimaryHandler(HttpMessageHandler handler)
        {
            while (handler is DelegatingHandler dh)
            {
                handler = dh.InnerHandler;
            }
            return handler;
        }

        private static Uri GetUri(string name) => new Uri($"http://{name}.example.com");

        private static IHttpClientBuilder AddConfiguredClient(ServiceCollection services, string name, Action<KeyedPrimaryHandler>? configurePrimaryHandler = null)
        {
            services.AddKeyedTransient(name, (_, _) =>
            {
                var handler = new KeyedPrimaryHandler(name);
                configurePrimaryHandler?.Invoke(handler);
                return handler;
            });

            return services
                .AddHttpClient(name, c => c.BaseAddress = GetUri(name))
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredKeyedService<KeyedPrimaryHandler>(name));
        }

        private static void AssertConfigured(HttpClient client, string name = Test)
        {
            Assert.Equal(GetUri(name), client.BaseAddress);
            AssertAlive(client, name);
        }

        private static void AssertConfigured(HttpMessageHandler candlerChain, string name = Test)
        {
            var primaryHandler = GetPrimaryHandler(candlerChain);
            var keyedPrimaryHandler = Assert.IsType<KeyedPrimaryHandler>(primaryHandler);
            Assert.Equal(name, keyedPrimaryHandler.Name);
        }

        private static void AssertAlive(HttpClient client, string name = Test, bool skipIfNull = false)
        {
            if (skipIfNull && client is null)
            {
                return;
            }
            Assert.Equal(name, client.GetStringAsync("/").GetAwaiter().GetResult());
        }

        private static void AssertDisposed(HttpClient client, bool skipIfNull = false)
        {
            if (skipIfNull && client is null)
            {
                return;
            }
            var exception = Assert.Throws<ObjectDisposedException>(() => client.GetStringAsync("/").GetAwaiter().GetResult());
            Assert.Contains(typeof(HttpClient).FullName, exception.Message);
        }

        private static HttpClient GetKeyedClient(IServiceScope scope, string name = Test) => GetKeyedClient(scope.ServiceProvider, name);
        private static HttpClient GetKeyedClient(IServiceProvider sp, string name = Test) => sp.GetRequiredKeyedService<HttpClient>(name);
        private static HttpMessageHandler GetKeyedHandler(IServiceProvider sp, string name = Test) => sp.GetRequiredKeyedService<HttpMessageHandler>(name);
        private static HttpClient? GetKeyedClientOrNull(IServiceProvider sp, string name) => sp.GetKeyedService<HttpClient>(name);
        private static HttpMessageHandler? GetKeyedHandlerOrNull(IServiceProvider sp, string name) => sp.GetKeyedService<HttpMessageHandler>(name);

        internal class KeyedClientTestService
        {
            public HttpClient HttpClient { get; }
            public KeyedClientTestService([FromKeyedServices(Test)] HttpClient httpClient)
            {
                HttpClient = httpClient;
            }
        }

        internal class KeyedHandlerTestService
        {
            public HttpMessageHandler Handler { get; }
            public KeyedHandlerTestService([FromKeyedServices(Test)] HttpMessageHandler handler)
            {
                Handler = handler;
            }
        }

        internal class KeyedPrimaryHandler : TestMessageHandler
        {
            private bool _disposed;

            public string Name { get; }

            public KeyedPrimaryHandler([ServiceKey] string name) : base()
            {
                Name = name;
                _responseFactory = _ => CreateResponse();
            }

            protected override void Dispose(bool disposing)
            {
                _disposed = true;
                base.Dispose(disposing);
            }

            private HttpResponseMessage CreateResponse()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return new HttpResponseMessage() { Content = new StringContent(Name) };
            }
        }
    }
}
