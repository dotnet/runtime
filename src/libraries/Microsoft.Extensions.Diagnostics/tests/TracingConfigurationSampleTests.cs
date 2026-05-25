// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
                    .AddListener<SampleActivityListener>()
                    .AddConfiguration(configuration))
                .Services
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", activityName: null, listenerName: null, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: true)))
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
                    .AddListener<SampleActivityListener>()
                    .AddConfiguration(configuration))
                .Services
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", activityName: null, listenerName: null, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: false)))
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
                    .AddListener<SampleActivityListener>()
                    .AddConfiguration(configuration))
                .Services
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();
            IActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<IActivitySourceFactory>();

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
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions("Demo.ReloadableSource", enabled: false));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            using var source = new ActivitySource("Demo.ReloadableSource");

            AssertActivityCreation(source, "BeforeEnable", expectedCreated: false);

            optionsMonitor.Set(CreateOptions("Demo.ReloadableSource", enabled: true));
            AssertActivityCreation(source, "AfterEnable", expectedCreated: true);

            optionsMonitor.Set(CreateOptions("Demo.ReloadableSource", enabled: false));
            AssertActivityCreation(source, "AfterDisable", expectedCreated: false);
        }

        [Fact]
        public void UpdateRules_DoesNotBlockCreateWhileResettingSourceFilters()
        {
            var blockingListener = new BlockingNameActivityListener();
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions("Demo.ReloadableSource", enabled: false));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener(blockingListener))
                .Services
                .AddSingleton<IOptionsMonitor<TracingOptions>>(optionsMonitor)
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<IStartupValidator>().Validate();
            IActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<IActivitySourceFactory>();

            using var existingSource = activitySourceFactory.Create("Demo.ReloadableSource");

            Task updateTask = Task.Run(() =>
            {
                blockingListener.BlockCurrentThreadNameReads();
                optionsMonitor.Set(CreateOptions("Demo.ReloadableSource", enabled: true));
            });

            Assert.True(blockingListener.WaitForNameRead(TimeSpan.FromSeconds(10)));

            Task<ActivitySource> createTask = Task.Run(() => activitySourceFactory.Create("Demo.ConcurrentCreate"));
            try
            {
                Assert.True(createTask.Wait(TimeSpan.FromSeconds(5)));
                using ActivitySource createdSource = createTask.GetAwaiter().GetResult();
                Assert.NotNull(createdSource);
            }
            finally
            {
                blockingListener.ReleaseNameRead();
            }

            Assert.True(updateTask.Wait(TimeSpan.FromSeconds(10)));
            updateTask.GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void UnspecifiedListenerNameRuleMatchesNamedListener(string? listenerName)
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo.DefaultListenerBucket", activityName: null, listenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
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
                new TracingRule("Demo.ListenerSpecificDisable", activityName: null, listenerName: SampleListenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: false),
                new TracingRule("Demo.ListenerSpecificDisable", activityName: null, listenerName: unspecifiedListenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
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
                new TracingRule("Demo.ListenerSpecificEnable", activityName: null, listenerName: SampleListenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: true),
                new TracingRule("Demo.ListenerSpecificEnable", activityName: null, listenerName: unspecifiedListenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: false)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
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
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
                .Services
                .BuildServiceProvider();

            IActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<IActivitySourceFactory>();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                activitySourceFactory.Create(new ActivitySourceOptions("Demo.InvalidScopeSource")
                {
                    Scope = new object()
                }));

            Assert.Contains("activity source", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("meter", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ActivitySourceNamePatternWithMultipleWildcards_ThrowsTracingSpecificMessage()
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions(
                new TracingRule("Demo*Wildcard*Source", activityName: null, listenerName: null, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled: true)));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
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
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
                .Services
                .BuildServiceProvider();

            IActivitySourceFactory activitySourceFactory = serviceProvider.GetRequiredService<IActivitySourceFactory>();
            ActivitySourceOptions options = new ActivitySourceOptions("Demo.ThrowingScopeSource")
            {
                Tags = new ThrowingTagsEnumerable()
            };

            Assert.Null(options.Scope);
            Assert.Throws<InvalidOperationException>(() => activitySourceFactory.Create(options));
            Assert.Null(options.Scope);
        }

        private static TracingOptions CreateOptions(string activitySourceName, bool enabled)
        {
            return CreateOptions(
                new TracingRule(activitySourceName, activityName: null, listenerName: null, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled),
                new TracingRule(activitySourceName, activityName: null, listenerName: SampleListenerName, scopes: ActivitySourceScope.Global | ActivitySourceScope.Local, enabled));
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

        public sealed class SampleActivityListener : IActivityListener
        {
            public string Name => SampleListenerName;

            public SampleActivity<string>? SampleUsingParentId => static (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded;

            public SampleActivity<ActivityContext>? Sample => static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;

            public void ActivityStarted(Activity activity)
            {
            }

            public void ActivityStopped(Activity activity)
            {
            }

            public void ActivityExceptionRecorded(Activity activity, Exception exception, ref TagList tags)
            {
            }
        }

        private sealed class BlockingNameActivityListener : IActivityListener
        {
            private readonly ManualResetEventSlim _nameReadStarted = new();
            private readonly ManualResetEventSlim _allowNameRead = new();
            private int _blockNameReads;
            private int _blockedThreadId;

            public string Name
            {
                get
                {
                    if (Volatile.Read(ref _blockNameReads) != 0
                        && Volatile.Read(ref _blockedThreadId) == Environment.CurrentManagedThreadId)
                    {
                        _nameReadStarted.Set();
                        _allowNameRead.Wait();
                    }

                    return nameof(BlockingNameActivityListener);
                }
            }

            public SampleActivity<string>? SampleUsingParentId => static (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded;

            public SampleActivity<ActivityContext>? Sample => static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;

            public void BlockCurrentThreadNameReads()
            {
                Volatile.Write(ref _blockedThreadId, Environment.CurrentManagedThreadId);
                Volatile.Write(ref _blockNameReads, 1);
            }

            public bool WaitForNameRead(TimeSpan timeout) => _nameReadStarted.Wait(timeout);

            public void ReleaseNameRead() => _allowNameRead.Set();

            public void ActivityStarted(Activity activity)
            {
            }

            public void ActivityStopped(Activity activity)
            {
            }

            public void ActivityExceptionRecorded(Activity activity, Exception exception, ref TagList tags)
            {
            }
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
