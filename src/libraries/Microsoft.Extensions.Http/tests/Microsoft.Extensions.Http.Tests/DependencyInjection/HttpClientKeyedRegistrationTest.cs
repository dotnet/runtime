// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class HttpClientKeyedRegistrationTest
    {
        [Fact]
        public void HttpClient_AddAsKeyed_Resolved()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test).AddAsKeyed();
            AddConfiguredNamedClient(serviceCollection, Other).AddAsKeyed();

            serviceCollection.AddTransient<ServiceWithTestClient>(); // [FromKeyedServices(Test)]
            serviceCollection.AddTransient<ServiceWithOtherClient>(); // [FromKeyedServices(Other)]

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            AssertNamedConfig(Test, services.GetRequiredKeyedService<HttpClient>(Test));
            AssertNamedConfig(Other, services.GetRequiredKeyedService<HttpClient>(Other));

            AssertNamedConfig(Test, services.GetRequiredService<ServiceWithTestClient>().HttpClient);
            AssertNamedConfig(Other, services.GetRequiredService<ServiceWithOtherClient>().HttpClient);
        }

        [Fact]
        public void HttpClient_RemoveAsKeyedOrDefault_NotResolved()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Disabled).RemoveAsKeyed();
            AddConfiguredNamedClient(serviceCollection, KeyedDefaults); // no Keyed APIs called

            serviceCollection.AddTransient<ServiceWithDisabledClient>(); // [FromKeyedServices(Disabled)]
            serviceCollection.AddTransient<ServiceWithKeyedDefaultsClient>(); // [FromKeyedServices(KeyedDefaults)]
            serviceCollection.AddTransient<ServiceWithAbsentClient>(); // [FromKeyedServices(Absent)] -- no such name

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Disabled));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(KeyedDefaults));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Absent)); // no such name

 //!           //Assert.Throws<InvalidOperationException>(services.GetRequiredService<ServiceWithDisabledClient>);
            //Assert.Throws<InvalidOperationException>(services.GetRequiredService<ServiceWithKeyedDefaultsClient>);
            //Assert.Throws<InvalidOperationException>(services.GetRequiredService<ServiceWithAbsentClient>);

            var factory = services.GetRequiredService<IHttpClientFactory>();

            AssertNamedConfig(Disabled, factory.CreateClient(Disabled)); // clients can still be created with the factory
            AssertNamedConfig(KeyedDefaults, factory.CreateClient(KeyedDefaults));
            AssertNotConfigured(factory.CreateClient(Absent)); // no such name -- factory still creates a (not configured) client
        }

        [Fact]
        public void HttpMessageHandler_AddAsKeyed_Resolved()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test).AddAsKeyed();
            AddConfiguredNamedClient(serviceCollection, Other).AddAsKeyed();

            serviceCollection.AddTransient<ServiceWithTestHandler>(); // [FromKeyedServices(Test)]
            serviceCollection.AddTransient<ServiceWithOtherHandler>(); // [FromKeyedServices(Other)]

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            AssertNamedConfig(Test, services.GetRequiredKeyedService<HttpMessageHandler>(Test));
            AssertNamedConfig(Other, services.GetRequiredKeyedService<HttpMessageHandler>(Other));

            AssertNamedConfig(Test, services.GetRequiredService<ServiceWithTestHandler>().Handler);
            AssertNamedConfig(Other, services.GetRequiredService<ServiceWithOtherHandler>().Handler);
        }

        [Fact]
        public void HttpMessageHandler_RemoveAsKeyedOrDefault_NotResolved()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Disabled).RemoveAsKeyed();
            AddConfiguredNamedClient(serviceCollection, KeyedDefaults); // no Keyed APIs called

            serviceCollection.AddTransient<ServiceWithDisabledHandler>(); // [FromKeyedServices(Disabled)]
            serviceCollection.AddTransient<ServiceWithKeyedDefaultsHandler>(); // [FromKeyedServices(KeyedDefaults)]
            serviceCollection.AddTransient<ServiceWithAbsentHandler>(); // [FromKeyedServices(Absent)] -- no such name

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpMessageHandler>(Disabled));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpMessageHandler>(KeyedDefaults));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpMessageHandler>(Absent)); // no such name

 //!           //Assert.Throws<InvalidOperationException>(services.GetRequiredService<KeyedClientDisabledService>);
            //Assert.Throws<InvalidOperationException>(services.GetRequiredService<KeyedClientKeyedDefaultsService>);
            //Assert.Throws<InvalidOperationException>(services.GetRequiredService<KeyedClientAbsentService>);

            var factory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            AssertNamedConfig(Disabled, factory.CreateHandler(Disabled));  // handlers can still be created with the factory
            AssertNamedConfig(KeyedDefaults, factory.CreateHandler(KeyedDefaults));
            AssertNotConfigured(factory.CreateHandler(Absent)); // no such name -- factory still creates a (not configured) handler
        }

        [Fact]
        public void HttpClient_LastRegistrationWins()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient(Test)
                .AddAsKeyed(ServiceLifetime.Transient)
                .RemoveAsKeyed()
                .AddAsKeyed(); // scoped

            serviceCollection.AddHttpClient(Other)
                .RemoveAsKeyed()
                .AddAsKeyed(ServiceLifetime.Scoped)
                .AddAsKeyed(ServiceLifetime.Transient);

            serviceCollection.AddHttpClient(Disabled)
                .AddAsKeyed(ServiceLifetime.Scoped)
                .AddAsKeyed(ServiceLifetime.Singleton)
                .RemoveAsKeyed();

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.Same( // scoped was last
                services.GetRequiredKeyedService<HttpClient>(Test),
                services.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame( // transient was last
                services.GetRequiredKeyedService<HttpClient>(Other),
                services.GetRequiredKeyedService<HttpClient>(Other));

            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Disabled)); // RemoveAsKeyed was last
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientDefaults_KeyedByDefault(bool defaultsFirst)
        {
            var serviceCollection = new ServiceCollection();

            void SetupDefaults() => serviceCollection.ConfigureHttpClientDefaults(b => b.AddAsKeyed()); // scoped

            void SetupNamedClients()
            {
                serviceCollection.AddHttpClient(Test).AddAsKeyed(ServiceLifetime.Transient);
                serviceCollection.AddHttpClient(Disabled).RemoveAsKeyed();
                serviceCollection.AddHttpClient(KeyedDefaults); // no Keyed APIs called
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

            Assert.NotSame( // transient -- per-name config wins
                services.GetRequiredKeyedService<HttpClient>(Test),
                services.GetRequiredKeyedService<HttpClient>(Test));

            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Disabled)); // removed -- per-name config wins

            Assert.Same( // scoped -- defaults applied
                services.GetRequiredKeyedService<HttpClient>(KeyedDefaults),
                services.GetRequiredKeyedService<HttpClient>(KeyedDefaults));

            Assert.Same( // scoped -- defaults applied for absent as well
                services.GetRequiredKeyedService<HttpClient>(Absent),
                services.GetRequiredKeyedService<HttpClient>(Absent));

            Assert.NotSame( // absent clients are still different per name
                services.GetRequiredKeyedService<HttpClient>(Absent),
                services.GetRequiredKeyedService<HttpClient>(OtherAbsent));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientDefaults_RemoveAsKeyedByDefault(bool defaultsFirst)
        {
            var serviceCollection = new ServiceCollection();

            void SetupDefaults() => serviceCollection.ConfigureHttpClientDefaults(b => b.RemoveAsKeyed());

            void SetupNamedClients()
            {
                serviceCollection.AddHttpClient(Test).AddAsKeyed(); // scoped
                serviceCollection.AddHttpClient(Disabled).RemoveAsKeyed();
                serviceCollection.AddHttpClient(KeyedDefaults); // no Keyed APIs called
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

            Assert.Same( // scoped -- per-name config wins
                services.GetRequiredKeyedService<HttpClient>(Test),
                services.GetRequiredKeyedService<HttpClient>(Test));

            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Disabled));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(KeyedDefaults));
            Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<HttpClient>(Absent));
        }

        [Theory]
        [InlineData(typeof(HttpClient), true)]
        [InlineData(typeof(HttpClient), false)]
        [InlineData(typeof(HttpMessageHandler), true)]
        [InlineData(typeof(HttpMessageHandler), false)]
        public void HttpClientDefaults_LastRegistrationWins(Type clientOrHandler, bool removeAsKeyedByDefault)
        {
            var serviceCollection = new ServiceCollection();

            Action<IHttpClientBuilder> lastConfigureHttpClientDefaults =
                removeAsKeyedByDefault
                    ? b => b.RemoveAsKeyed()
                    : b => b.AddAsKeyed();

            // ---

            var testClientBuilder = serviceCollection.AddHttpClient(Test)
                .RemoveAsKeyed(); // #4

            var otherClientBuilder = serviceCollection.AddHttpClient(Other)
                .AddAsKeyed(ServiceLifetime.Transient); // #5

            var disabledClientBuilder = serviceCollection.AddHttpClient(Disabled)
                .AddAsKeyed(ServiceLifetime.Scoped); // #6

            otherClientBuilder.AddAsKeyed(ServiceLifetime.Scoped); // #7 -- last 'other' -> scoped

            serviceCollection.ConfigureHttpClientDefaults(
                b => b.AddAsKeyed(ServiceLifetime.Transient)); // #1

            serviceCollection.AddHttpClient(KeyedDefaults); // no keyed APIs called -> (removeAsKeyedByDefault ? disabled : scoped)

            serviceCollection.ConfigureHttpClientDefaults(
                b => b.RemoveAsKeyed()); // #2

            testClientBuilder.AddAsKeyed(ServiceLifetime.Transient); // #8 -- last 'test' -> transient

            serviceCollection.ConfigureHttpClientDefaults(
                lastConfigureHttpClientDefaults); // #3

            disabledClientBuilder.RemoveAsKeyed(); // #9 -- last 'disabled' -> disabled

            // ---

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);
            var services = rootServices.CreateScope().ServiceProvider;

            Assert.NotSame( // #8 -> transient
                services.GetRequiredKeyedService(clientOrHandler, Test),
                services.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.Same( // #7 -> scoped
                services.GetRequiredKeyedService(clientOrHandler, Other),
                services.GetRequiredKeyedService(clientOrHandler, Other));

            Assert.Throws<InvalidOperationException>(() => // #9 -> disabled
                services.GetRequiredKeyedService(clientOrHandler, Disabled));

            if (removeAsKeyedByDefault)
            {
                Assert.Throws<InvalidOperationException>(() => // defaults -> disabled
                    services.GetRequiredKeyedService(clientOrHandler, KeyedDefaults));
                Assert.Throws<InvalidOperationException>(() =>
                    services.GetRequiredKeyedService(clientOrHandler, Absent));
            }
            else
            {
                Assert.Same( // defaults -> scoped
                    services.GetRequiredKeyedService(clientOrHandler, KeyedDefaults),
                    services.GetRequiredKeyedService(clientOrHandler, KeyedDefaults));

                Assert.Same(
                    services.GetRequiredKeyedService(clientOrHandler, Absent),
                    services.GetRequiredKeyedService(clientOrHandler, Absent));

                Assert.NotSame(
                    services.GetRequiredKeyedService(clientOrHandler, KeyedDefaults),
                    services.GetRequiredKeyedService(clientOrHandler, Absent));
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

        private static IHttpClientBuilder AddConfiguredNamedClient(ServiceCollection services, string name)
        {
            services.AddKeyedTransient(name, (_, _) => new KeyedPrimaryHandler(name));

            return services
                .AddHttpClient(name, c => c.BaseAddress = GetUri(name))
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredKeyedService<KeyedPrimaryHandler>(name));
        }

        private static void AssertNamedConfig(string name, HttpClient client)
            => Assert.Equal(GetUri(name), client.BaseAddress);

        private static void AssertNotConfigured(HttpClient client)
            => Assert.Null(client.BaseAddress);

        private static void AssertNamedConfig(string name, HttpMessageHandler handler)
        {
            var primaryHandler = GetPrimaryHandler(handler);
            var keyedPrimaryHandler = Assert.IsType<KeyedPrimaryHandler>(primaryHandler);
            Assert.Equal(name, keyedPrimaryHandler.Name);
        }

        private static void AssertNotConfigured(HttpMessageHandler handler)
        {
            Type defaultPrimaryHandlerType =
#if NET
                SocketsHttpHandler.IsSupported ? typeof(SocketsHttpHandler) :
#endif
                typeof(HttpClientHandler);

            Assert.Equal(defaultPrimaryHandlerType, GetPrimaryHandler(handler).GetType());
        }

        private static Uri GetUri(string name) => new Uri($"http://{name}.example.com");
    }
}
