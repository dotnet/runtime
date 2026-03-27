// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class TracingConfigurationSampleTests
    {
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
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", listenerName: null, enabled: true)))
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
                .Configure<TracingOptions>(options =>
                    options.Rules.Add(new TracingRule("Demo.Source", listenerName: null, enabled: false)))
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
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<IActivityListenerConfigurationFactory>();
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
                .BuildServiceProvider();

            var factory = serviceProvider.GetRequiredService<IActivityListenerConfigurationFactory>();
            var mergedConfiguration = factory.GetConfiguration(listenerName);

            Assert.Equal("true", mergedConfiguration["EnabledTracing:SourceA"]);
            Assert.Equal("true", mergedConfiguration["EnabledTracing:SourceB"]);
            Assert.Equal("false", mergedConfiguration["EnabledTracing:SourceC"]);
        }

        [Fact]
        public void ScopeConfigurationMatchesSampleBehavior()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnabledTracing:Default"] = "false",
                    ["EnabledGlobalTracing:Demo.ScopeSource:Default"] = "true",
                    ["EnabledLocalTracing:Demo.ScopeSource:Default"] = "false",
                    ["EnabledLocalTracing:Demo.LocalOnlySource:Default"] = "true",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder
                    .AddListener<SampleActivityListener>()
                    .AddConfiguration(configuration))
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
        public void LegacySamplingValuesMapToEnabledRules()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnabledTracing:SourceA"] = "AllData",
                    ["EnabledTracing:SourceB"] = "None",
                })
                .Build();

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddConfiguration(configuration))
                .BuildServiceProvider();

            var options = serviceProvider.GetRequiredService<IOptions<TracingOptions>>().Value;
            Assert.Contains(options.Rules, r => r.ActivitySourceName == "SourceA" && r.Enabled);
            Assert.Contains(options.Rules, r => r.ActivitySourceName == "SourceB" && !r.Enabled);
        }

        [Fact]
        public void ExistingSourceRespondsToRuleChanges()
        {
            var optionsMonitor = new TestActivityOptionsMonitor(CreateOptions("Demo.ReloadableSource", enabled: false));

            using var serviceProvider = new ServiceCollection()
                .AddTracing(builder => builder.AddListener<SampleActivityListener>())
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

        private static TracingOptions CreateOptions(string activitySourceName, bool enabled)
        {
            var options = new TracingOptions();
            options.Rules.Add(new TracingRule(activitySourceName, listenerName: null, enabled));
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
            public string Name => "SampleActivityListener";

            public bool Enabled => true;

            public bool ShouldListenTo(ActivitySource activitySource) => true;

            public ActivitySamplingResult SampleUsingParentId(ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded;

            public ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;

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
                    callback(options, Options.DefaultName);
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