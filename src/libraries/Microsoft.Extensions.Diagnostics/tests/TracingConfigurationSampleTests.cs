// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Tracing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class TracingConfigurationSampleTests
    {
        private const string SampleListenerName = nameof(SampleActivityListener);

        [Fact]
        public void EnabledRuleAllowsActivityCreation()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnabledTracing:Default"] = "true",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder
                    .AddListener(_ => SampleActivityListener.Create())
                    .AddConfiguration(configuration))
                .Services
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true)))
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.Source");

            AssertActivityCreation(source, "AllowedOperation", expectedCreated: true);
            AssertActivityCreation(source, "BlockedOperation", expectedCreated: true);
        }

        [Fact]
        public void DisabledRuleSkipsActivityCreation()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnabledTracing:Default"] = "true",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder
                    .AddListener(_ => SampleActivityListener.Create())
                    .AddConfiguration(configuration))
                .Services
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: false)))
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.Source");
            AssertActivityCreation(source, "BlockedOperation", expectedCreated: false);
        }

        [Fact]
        public void AddTracing_RegistersActivityListenerConfigurationFactory()
        {
            using var serviceProvider = new ServiceCollection()
                .AddTracing()
                .Services
                .BuildServiceProvider();

            Type factoryType = GetActivityListenerConfigurationFactoryType();
            var factory = serviceProvider.GetService(factoryType);
            Assert.NotNull(factory);
        }

        [Fact]
        public void AddConfiguration_MergesListenerConfigurationAcrossCalls()
        {
            const string listenerName = "SampleActivityListener";

            var configuration1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{listenerName}:EnabledTracing:SourceA"] = "true",
                    [$"{listenerName}:EnabledTracing:SourceB"] = "false",
                })
                .Build();

            var configuration2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{listenerName}:EnabledTracing:SourceB"] = "true",
                    [$"{listenerName}:EnabledTracing:SourceC"] = "false",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder
                    .AddConfiguration(configuration1)
                    .AddConfiguration(configuration2))
                .Services
                .BuildServiceProvider();

            Type factoryType = GetActivityListenerConfigurationFactoryType();
            object factory = serviceProvider.GetRequiredService(factoryType);
            object? mergedConfigurationObject = factoryType.GetMethod("GetConfiguration")!.Invoke(factory, [listenerName]);
            Assert.NotNull(mergedConfigurationObject);
            IConfiguration mergedConfiguration = (IConfiguration)mergedConfigurationObject;

            Assert.Equal("true", mergedConfiguration["EnabledTracing:SourceA"]);
            Assert.Equal("true", mergedConfiguration["EnabledTracing:SourceB"]);
            Assert.Equal("false", mergedConfiguration["EnabledTracing:SourceC"]);
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        private static Type GetActivityListenerConfigurationFactoryType()
            => typeof(TracingServiceExtensions).Assembly.GetType("Microsoft.Extensions.Diagnostics.Tracing.ActivityListenerConfigurationFactory", throwOnError: true)!;

        [Fact]
        public void ScopeConfigurationMatchesSampleBehavior()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnabledTracing:Default"] = "false",
                    ["EnabledGlobalTracing:Demo.ScopeSource"] = "true",
                    ["EnabledLocalTracing:Demo.ScopeSource"] = "false",
                    ["EnabledLocalTracing:Demo.LocalOnlySource"] = "true",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder
                    .AddListener(_ => SampleActivityListener.Create())
                    .AddConfiguration(configuration))
                .Services
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();
            ActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<ActivitySourceFactory>();

            using var globalSource = new ActivitySource("Demo.ScopeSource");
            using var localScopeSource = activitySourceFactory.Create(new ActivitySourceOptions("Demo.ScopeSource"));
            using var localOnlySource = activitySourceFactory.Create("Demo.LocalOnlySource");
            using var blockedSource = new ActivitySource("Demo.BlockedSource");

            AssertActivityCreation(globalSource, "AllowedOperation", expectedCreated: true);
            AssertActivityCreation(localScopeSource, "AllowedOperation", expectedCreated: false);
            AssertActivityCreation(localOnlySource, "LocalOnlyOperation", expectedCreated: true);
            AssertActivityCreation(blockedSource, "AllowedOperation", expectedCreated: false);
        }

        [Fact]
        public void ExistingSourceRespondsToRuleChanges()
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions("Demo.ReloadableSource", enable: false));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.ReloadableSource");

            AssertActivityCreation(source, "BeforeEnable", expectedCreated: false);

            optionsMonitor.Set(CreateOptions("Demo.ReloadableSource", enable: true));
            AssertActivityCreation(source, "AfterEnable", expectedCreated: true);

            optionsMonitor.Set(CreateOptions("Demo.ReloadableSource", enable: false));
            AssertActivityCreation(source, "AfterDisable", expectedCreated: false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void UnspecifiedListenerNameRuleMatchesNamedListener(string? listenerName)
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo.DefaultListenerBucket", operationName: null, listenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.DefaultListenerBucket");
            AssertActivityCreation(source, "DefaultListenerBucketOperation", expectedCreated: true);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ExplicitListenerNameDisableWinsOverUnspecifiedEnable(string? unspecifiedListenerName)
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo.ListenerSpecificDisable", operationName: null, listenerName: SampleListenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: false),
                new TracingRule("Demo.ListenerSpecificDisable", operationName: null, listenerName: unspecifiedListenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.ListenerSpecificDisable");
            AssertActivityCreation(source, "ListenerSpecificDisableOperation", expectedCreated: false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ExplicitListenerNameEnableWinsOverUnspecifiedDisable(string? unspecifiedListenerName)
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo.ListenerSpecificEnable", operationName: null, listenerName: SampleListenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true),
                new TracingRule("Demo.ListenerSpecificEnable", operationName: null, listenerName: unspecifiedListenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: false)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.ListenerSpecificEnable");
            AssertActivityCreation(source, "ListenerSpecificEnableOperation", expectedCreated: true);
        }

        [Fact]
        public void ActivitySourceFactoryCreate_WithInvalidScope_ThrowsTracingSpecificMessage()
        {
            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .BuildServiceProvider();

            ActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<ActivitySourceFactory>();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                activitySourceFactory.Create(new ActivitySourceOptions("Demo.InvalidScopeSource")
                {
                    Scope = new object()
                }));

            Assert.Contains("activity source", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("meter", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SourceNamePatternWithMultipleWildcards_ThrowsTracingSpecificMessage()
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo*Wildcard*Source", operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => serviceProvider.GetRequiredService<IStartupValidator>().Validate());
            Assert.Contains("activity source", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("category", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ActivitySourceFactoryCreate_RestoresScope_WhenActivitySourceCreationThrows()
        {
            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => SampleActivityListener.Create()))
                .Services
                .BuildServiceProvider();

            ActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<ActivitySourceFactory>();
            ActivitySourceOptions options = new ActivitySourceOptions("Demo.ThrowingScopeSource")
            {
                Tags = new ThrowingTagsEnumerable()
            };

            Assert.Null(options.Scope);
            Assert.Throws<InvalidOperationException>(() => activitySourceFactory.Create(options));
            Assert.Null(options.Scope);
        }

        private static TracingOptions CreateOptions(string sourceName, bool enable)
        {
            return CreateOptions(
                new TracingRule(sourceName, operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable),
                new TracingRule(sourceName, operationName: null, listenerName: SampleListenerName, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable));
        }

        private static TracingOptions CreateOptions(params TracingRule[] rules)
        {
            var options = new TracingOptions();
            foreach (TracingRule rule in rules)
            {
                options.Rules.Add(rule);
            }

            return options;
        }

        private static void AssertActivityCreation(ActivitySource source, string operationName, bool expectedCreated)
        {
            using Activity? activity = source.StartActivity(operationName);
            if (expectedCreated)
            {
                Assert.NotNull(activity);
            }
            else
            {
                Assert.Null(activity);
            }
        }

        [Fact]
        public void DisabledOperationName_FiltersNotifications_EvenWhenAnotherListenerCreatesActivity()
        {
            using var externalListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Demo.NotificationFilter",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(externalListener);

            var recording = new RecordingActivityListener();
            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => recording.Listener))
                .Services
                .Configure<TracingOptions>(options =>
                {
                    options.Rules.Add(new TracingRule("Demo.NotificationFilter", operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true));
                    options.Rules.Add(new TracingRule("Demo.NotificationFilter", operationName: "BlockedOperation", listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: false));
                })
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.NotificationFilter");

            using (var allowed = source.StartActivity("AllowedOperation"))
            {
                Assert.NotNull(allowed);
            }
            using (var blocked = source.StartActivity("BlockedOperation"))
            {
                Assert.NotNull(blocked);
            }

            Assert.Equal(new[] { "AllowedOperation" }, recording.StartedNames);
            Assert.Equal(new[] { "AllowedOperation" }, recording.StoppedNames);
        }

        [Fact]
        public void EnabledOperationName_NotifiesListener_EvenWhenSourceDefaultDisabled()
        {
            var recording = new RecordingActivityListener();
            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(_ => recording.Listener))
                .Services
                .Configure<TracingOptions>(options =>
                {
                    options.Rules.Add(new TracingRule("Demo.OptIn", operationName: null, listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: false));
                    options.Rules.Add(new TracingRule("Demo.OptIn", operationName: "OptedInOperation", listenerName: null, scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local, enable: true));
                })
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.OptIn");

            using (var optedIn = source.StartActivity("OptedInOperation"))
            {
                Assert.NotNull(optedIn);
            }

            Assert.Equal(new[] { "OptedInOperation" }, recording.StartedNames);
            Assert.Equal(new[] { "OptedInOperation" }, recording.StoppedNames);
        }

        private sealed class RecordingActivityListener
        {
            private readonly object _lock = new();
            private readonly List<string> _started = new();
            private readonly List<string> _stopped = new();

            public RecordingActivityListener()
            {
                Listener = new ActivityListener(nameof(RecordingActivityListener))
                {
                    Sample = static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                    SampleUsingParentId = static (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded,
                    ActivityStarted = activity =>
                    {
                        lock (_lock) { _started.Add(activity.OperationName); }
                    },
                    ActivityStopped = activity =>
                    {
                        lock (_lock) { _stopped.Add(activity.OperationName); }
                    },
                };
            }

            public ActivityListener Listener { get; }

            public IReadOnlyList<string> StartedNames
            {
                get { lock (_lock) { return _started.ToArray(); } }
            }

            public IReadOnlyList<string> StoppedNames
            {
                get { lock (_lock) { return _stopped.ToArray(); } }
            }
        }

        private static class SampleActivityListener
        {
            public static ActivityListener Create() => new ActivityListener(SampleListenerName)
            {
                Sample = static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = static (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded,
            };
        }

        private sealed class ThrowingTagsEnumerable : IEnumerable<KeyValuePair<string, object?>>
        {
            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => new ThrowingTagsEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ThrowingTagsEnumerator : IEnumerator<KeyValuePair<string, object?>>
            {
                public KeyValuePair<string, object?> Current => default;

                object IEnumerator.Current => Current;

                public bool MoveNext() => throw new InvalidOperationException("Test exception.");

                public void Dispose()
                {
                }

                public void Reset() => throw new NotSupportedException();
            }
        }

        private sealed class TestActivityOptionsMonitor : IOptionsMonitor<TracingOptions>
        {
            private readonly List<Action<TracingOptions, string?>> _callbacks = new();

            public TestActivityOptionsMonitor(TracingOptions currentValue)
            {
                CurrentValue = currentValue;
            }

            public TracingOptions CurrentValue { get; private set; }

            public TracingOptions Get(string? name) => CurrentValue;

            public IDisposable OnChange(Action<TracingOptions, string?> listener)
            {
                _callbacks.Add(listener);
                return new CallbackRegistration(_callbacks, listener);
            }

            public void Set(TracingOptions options)
            {
                CurrentValue = options;
                foreach (Action<TracingOptions, string?> callback in _callbacks.ToArray())
                {
                    callback(options, Options.Options.DefaultName);
                }
            }

            private sealed class CallbackRegistration : IDisposable
            {
                private readonly List<Action<TracingOptions, string?>> _callbacks;
                private readonly Action<TracingOptions, string?> _callback;

                public CallbackRegistration(List<Action<TracingOptions, string?>> callbacks, Action<TracingOptions, string?> callback)
                {
                    _callbacks = callbacks;
                    _callback = callback;
                }

                public void Dispose()
                {
                    _callbacks.Remove(_callback);
                }
            }
        }
    }
}
