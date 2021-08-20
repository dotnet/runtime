// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    [CollectionDefinition(nameof(EventSourceTests), DisableParallelization = true)]
    public class EventSourceTests : ICollectionFixture<EventSourceTests>
    {
    }

    [Collection(nameof(EventSourceTests))]
    public class DependencyInjectionEventSourceTests : IDisposable
    {
        private readonly TestEventListener _listener = new TestEventListener();

        public DependencyInjectionEventSourceTests()
        {
            // clear the provider list in between tests
            typeof(DependencyInjectionEventSource).GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(DependencyInjectionEventSource.Log, new List<WeakReference<ServiceProvider>>());

            _listener.EnableEvents(DependencyInjectionEventSource.Log, EventLevel.Verbose);
        }

        [Fact]
        public void ExistsWithCorrectId()
        {
            var esType = typeof(DependencyInjectionEventSource);

            Assert.NotNull(esType);

            Assert.Equal("Microsoft-Extensions-DependencyInjection", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("27837f46-1a43-573d-d30c-276de7d02192"), EventSource.GetGuid(esType));
            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }

        [Fact]
        public void EmitsCallSiteBuiltEvent()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var fakeDisposeCallback = new FakeDisposeCallback();
            serviceCollection.AddSingleton(fakeDisposeCallback);
            serviceCollection.AddTransient<IFakeOuterService, FakeDisposableCallbackOuterService>();
            serviceCollection.AddSingleton<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddSingleton<IFakeMultipleService>(provider => new FakeDisposableCallbackInnerService(fakeDisposeCallback));
            serviceCollection.AddScoped<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddTransient<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddSingleton<IFakeService, FakeDisposableCallbackInnerService>();

            serviceCollection.BuildServiceProvider().GetService<IEnumerable<IFakeOuterService>>();

            var callsiteBuiltEvent = _listener.EventData.Single(e => e.EventName == "CallSiteBuilt");


            Assert.Equal(
                string.Join(Environment.NewLine,
                "{",
                "  \"serviceType\": \"System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService]\",",
                "  \"kind\": \"IEnumerable\",",
                "  \"cache\": \"None\",",
                "  \"itemType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService\",",
                "  \"size\": \"1\",",
                "  \"items\": [",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService\",",
                "      \"kind\": \"Constructor\",",
                "      \"cache\": \"Dispose\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackOuterService\",",
                "      \"arguments\": [",
                "        {",
                "          \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService\",",
                "          \"kind\": \"Constructor\",",
                "          \"cache\": \"Root\",",
                "          \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\",",
                "          \"arguments\": [",
                "            {",
                "              \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\",",
                "              \"kind\": \"Constant\",",
                "              \"cache\": \"None\",",
                "              \"value\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\"",
                "            }",
                "          ]",
                "        },",
                "        {",
                "          \"serviceType\": \"System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService]\",",
                "          \"kind\": \"IEnumerable\",",
                "          \"cache\": \"None\",",
                "          \"itemType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "          \"size\": \"4\",",
                "          \"items\": [",
                "            {",
                "              \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "              \"kind\": \"Constructor\",",
                "              \"cache\": \"Root\",",
                "              \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\",",
                "              \"arguments\": [",
                "                {",
                "                  \"ref\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\"",
                "                }",
                "              ]",
                "            },",
                "            {",
                "              \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "              \"kind\": \"Factory\",",
                "              \"cache\": \"Root\",",
                "              \"method\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService <EmitsCallSiteBuiltEvent>b__0(System.IServiceProvider)\"",
                "            },",
                "            {",
                "              \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "              \"kind\": \"Constructor\",",
                "              \"cache\": \"Scope\",",
                "              \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\",",
                "              \"arguments\": [",
                "                {",
                "                  \"ref\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\"",
                "                }",
                "              ]",
                "            },",
                "            {",
                "              \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "              \"kind\": \"Constructor\",",
                "              \"cache\": \"Dispose\",",
                "              \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\",",
                "              \"arguments\": [",
                "                {",
                "                  \"ref\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\"",
                "                }",
                "              ]",
                "            }",
                "          ]",
                "        },",
                "        {",
                "          \"ref\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\"",
                "        }",
                "      ]",
                "    }",
                "  ]",
                "}"),
                JObject.Parse(GetProperty<string>(callsiteBuiltEvent, "callSite")).ToString());

            Assert.Equal("System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService]", GetProperty<string>(callsiteBuiltEvent, "serviceType"));
            Assert.Equal(0, GetProperty<int>(callsiteBuiltEvent, "chunkIndex"));
            Assert.Equal(1, GetProperty<int>(callsiteBuiltEvent, "chunkCount"));
            Assert.Equal(1, callsiteBuiltEvent.EventId);
        }

        [Fact]
        public void EmitsServiceResolvedEvent()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetService<IFakeService>();
            serviceProvider.GetService<IFakeService>();
            serviceProvider.GetService<IFakeService>();

            var serviceResolvedEvents = _listener.EventData.Where(e => e.EventName == "ServiceResolved").ToArray();

            Assert.Equal(3, serviceResolvedEvents.Length);
            foreach (var serviceResolvedEvent in serviceResolvedEvents)
            {
                Assert.Equal("Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService", GetProperty<string>(serviceResolvedEvent, "serviceType"));
                Assert.Equal(2, serviceResolvedEvent.EventId);
            }
        }

        [Fact]
        public void EmitsExpressionTreeBuiltEvent()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider(ServiceProviderMode.Expressions);

            serviceProvider.GetService<IFakeService>();

            var expressionTreeGeneratedEvent = _listener.EventData.Single(e => e.EventName == "ExpressionTreeGenerated");

            Assert.Equal("Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService", GetProperty<string>(expressionTreeGeneratedEvent, "serviceType"));
            Assert.Equal(9, GetProperty<int>(expressionTreeGeneratedEvent, "nodeCount"));
            Assert.Equal(3, expressionTreeGeneratedEvent.EventId);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/35753", TestPlatforms.Windows)]
        public void EmitsDynamicMethodBuiltEvent()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider(ServiceProviderMode.ILEmit);

            serviceProvider.GetService<IFakeService>();

            var expressionTreeGeneratedEvent = _listener.EventData.Single(e => e.EventName == "DynamicMethodBuilt");

            Assert.Equal("Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService", GetProperty<string>(expressionTreeGeneratedEvent, "serviceType"));
            Assert.Equal(12, GetProperty<int>(expressionTreeGeneratedEvent, "methodSize"));
            Assert.Equal(4, expressionTreeGeneratedEvent.EventId);
        }

        [Fact]
        public void EmitsScopeDisposedEvent()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService<IFakeService>();
            }

            var scopeDisposedEvent = _listener.EventData.Single(e => e.EventName == "ScopeDisposed");

            Assert.Equal(1, GetProperty<int>(scopeDisposedEvent, "scopedServicesResolved"));
            Assert.Equal(1, GetProperty<int>(scopeDisposedEvent, "disposableServices"));
            Assert.Equal(5, scopeDisposedEvent.EventId);
        }

        [Fact]
        public void EmitsServiceRealizationFailedEvent()
        {
            var exception = new Exception("Test error.");
            DependencyInjectionEventSource.Log.ServiceRealizationFailed(exception, 1234);

            var eventName = nameof(DependencyInjectionEventSource.Log.ServiceRealizationFailed);
            var serviceRealizationFailedEvent = _listener.EventData.Single(e => e.EventName == eventName);

            Assert.Equal("System.Exception: Test error.", GetProperty<string>(serviceRealizationFailedEvent, "exceptionMessage"));
            Assert.Equal(1234, GetProperty<int>(serviceRealizationFailedEvent, "serviceProviderHashCode"));
            Assert.Equal(6, serviceRealizationFailedEvent.EventId);
        }

        [Fact]
        public void EmitsServiceProviderBuilt()
        {
            ServiceCollection serviceCollection = new();
            FakeDisposeCallback fakeDisposeCallback = new();
            serviceCollection.AddSingleton(fakeDisposeCallback);
            serviceCollection.AddTransient<IFakeOuterService, FakeDisposableCallbackOuterService>();
            serviceCollection.AddSingleton<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddSingleton<IFakeMultipleService>(provider => new FakeDisposableCallbackInnerService(fakeDisposeCallback));
            serviceCollection.AddScoped<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddTransient<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddSingleton<IFakeService, FakeDisposableCallbackInnerService>();

            using ServiceProvider provider = serviceCollection.BuildServiceProvider();

            EventWrittenEventArgs serviceProviderBuiltEvent = _listener.EventData.Single(e => e.EventName == "ServiceProviderBuilt");
            GetProperty<int>(serviceProviderBuiltEvent, "serviceProviderHashCode"); // assert hashcode exists as an int
            Assert.Equal(4, GetProperty<int>(serviceProviderBuiltEvent, "singletonServices"));
            Assert.Equal(1, GetProperty<int>(serviceProviderBuiltEvent, "scopedServices"));
            Assert.Equal(2, GetProperty<int>(serviceProviderBuiltEvent, "transientServices"));
            Assert.Equal(7, serviceProviderBuiltEvent.EventId);

            EventWrittenEventArgs serviceProviderDescriptorsEvent = _listener.EventData.Single(e => e.EventName == "ServiceProviderDescriptors");
            Assert.Equal(
                string.Join(Environment.NewLine,
                "{",
                "  \"descriptors\": [",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback\",",
                "      \"lifetime\": \"Singleton\",",
                "      \"implementationInstance\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback (instance)\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService\",",
                "      \"lifetime\": \"Transient\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackOuterService\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "      \"lifetime\": \"Singleton\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "      \"lifetime\": \"Singleton\",",
                "      \"implementationFactory\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService <EmitsServiceProviderBuilt>b__0(System.IServiceProvider)\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "      \"lifetime\": \"Scoped\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService\",",
                "      \"lifetime\": \"Transient\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\"",
                "    },",
                "    {",
                "      \"serviceType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService\",",
                "      \"lifetime\": \"Singleton\",",
                "      \"implementationType\": \"Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService\"",
                "    }",
                "  ]",
                "}"),
                JObject.Parse(GetProperty<string>(serviceProviderDescriptorsEvent, "descriptors")).ToString());

            GetProperty<int>(serviceProviderDescriptorsEvent, "serviceProviderHashCode"); // assert hashcode exists as an int
            Assert.Equal(0, GetProperty<int>(serviceProviderDescriptorsEvent, "chunkIndex"));
            Assert.Equal(1, GetProperty<int>(serviceProviderDescriptorsEvent, "chunkCount"));
            Assert.Equal(8, serviceProviderDescriptorsEvent.EventId);
        }

        /// <summary>
        /// Verifies that when an EventListener is enabled after the ServiceProvider has been built,
        /// the ServiceProviderBuilt events fire. This way users can get ServiceProvider info when
        /// attaching while the app is running.
        /// </summary>
        [Fact]
        public void EmitsServiceProviderBuiltOnAttach()
        {
            _listener.DisableEvents(DependencyInjectionEventSource.Log);

            ServiceCollection serviceCollection = new();
            serviceCollection.AddSingleton(new FakeDisposeCallback());

            using ServiceProvider provider = serviceCollection.BuildServiceProvider();

            Assert.Empty(_listener.EventData);

            _listener.EnableEvents(DependencyInjectionEventSource.Log, EventLevel.Verbose);

            EventWrittenEventArgs serviceProviderBuiltEvent = _listener.EventData.Single(e => e.EventName == "ServiceProviderBuilt");
            Assert.Equal(1, GetProperty<int>(serviceProviderBuiltEvent, "singletonServices"));

            EventWrittenEventArgs serviceProviderDescriptorsEvent = _listener.EventData.Single(e => e.EventName == "ServiceProviderDescriptors");
            Assert.NotNull(JObject.Parse(GetProperty<string>(serviceProviderDescriptorsEvent, "descriptors")));
        }

        private T GetProperty<T>(EventWrittenEventArgs data, string propName)
            => (T)data.Payload[data.PayloadNames.IndexOf(propName)];

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;
            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (!_disposed)
                {
                    _events.Enqueue(eventData);
                }
            }

            public override void Dispose()
            {
                _disposed = true;
                base.Dispose();
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
