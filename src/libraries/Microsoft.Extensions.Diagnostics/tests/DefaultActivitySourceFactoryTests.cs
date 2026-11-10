// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tracing.Tests
{
    public class DefaultActivitySourceFactoryTests
    {
        [Fact]
        public void FactoryReturnsCachedSourceForSameNameVersionAndTags()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();

            var tags = new[] { new KeyValuePair<string, object?>("k", "v") };
            ActivitySource a = factory.Create("MySource", "1.0", tags);
            ActivitySource b = factory.Create("MySource", "1.0", tags);

            Assert.Same(a, b);
            Assert.Same(factory, a.Scope);
        }

        [Fact]
        public void FactoryReturnsDifferentSourcesForDifferentVersionOrTags()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();

            ActivitySource v1 = factory.Create("MySource", "1.0");
            ActivitySource v2 = factory.Create("MySource", "2.0");
            ActivitySource t1 = factory.Create("MySource", "1.0", new[] { new KeyValuePair<string, object?>("k", "v") });

            Assert.NotSame(v1, v2);
            Assert.NotSame(v1, t1);
        }

        [Fact]
        public void FactoryTagOrderDoesNotAffectCacheLookup()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();

            ActivitySource a = factory.Create("MySource", "1.0", new[]
            {
                new KeyValuePair<string, object?>("a", "1"),
                new KeyValuePair<string, object?>("b", "2"),
            });
            ActivitySource b = factory.Create("MySource", "1.0", new[]
            {
                new KeyValuePair<string, object?>("b", "2"),
                new KeyValuePair<string, object?>("a", "1"),
            });

            Assert.Same(a, b);
        }

        [Fact]
        public void CreateAfterDisposeThrowsObjectDisposedException()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
            factory.Dispose();

            Assert.Throws<ObjectDisposedException>(() => factory.Create("MySource"));
        }

        [Fact]
        public void CreateWithExplicitScopeMatchingFactoryIsAllowed()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();

            var options = new ActivitySourceOptions("MySource") { Scope = factory };
            ActivitySource source = factory.Create(options);

            Assert.Same(factory, source.Scope);
        }

        [Fact]
        public void CreateWithDifferentScopeThrows()
        {
            using var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
            using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();

            var foreignScope = new object();
            var options = new ActivitySourceOptions("MySource") { Scope = foreignScope };

            Assert.Throws<InvalidOperationException>(() => factory.Create(options));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EnabledTracingRuleAllowsActivityStart()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");
                using Activity? activity = source.StartActivity("Op1");

                Assert.NotNull(activity);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DisabledByDefaultProducesNoActivity()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("Other");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");
                using Activity? activity = source.StartActivity("Op1");

                Assert.Null(activity);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MostSpecificRuleWinsAcrossSourcePrefix()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MyCompany");
                        builder.DisableTracing("MyCompany.Service");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource broad = factory.Create("MyCompany.Other");
                using ActivitySource narrow = factory.Create("MyCompany.Service");

                using Activity? broadActivity = broad.StartActivity("Op");
                using Activity? narrowActivity = narrow.StartActivity("Op");

                Assert.NotNull(broadActivity);
                Assert.Null(narrowActivity);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WildcardSourcePatternMatchesPrefixAndSuffix()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MyCompany.*.Public");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource matching = factory.Create("MyCompany.Service.Public");
                using ActivitySource nonMatching = factory.Create("MyCompany.Service.Internal");

                using Activity? matched = matching.StartActivity("Op");
                using Activity? unmatched = nonMatching.StartActivity("Op");

                Assert.NotNull(matched);
                Assert.Null(unmatched);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void OperationNameRuleDisablesOneOperationOfEnabledSource()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource");
                        builder.DisableTracing("MySource", "Quiet");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");

                using Activity? loud = source.StartActivity("Loud");
                using Activity? quiet = source.StartActivity("Quiet");

                Assert.NotNull(loud);
                Assert.Null(quiet);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void OperationNameRuleEnablesOneOperationOfDisabledSource()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", "Loud");
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");

                using Activity? loud = source.StartActivity("Loud");
                using Activity? quiet = source.StartActivity("Quiet");

                Assert.NotNull(loud);
                Assert.Null(quiet);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LocalScopeRuleDoesNotMatchStandaloneSource()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", scopes: ActivitySourceScopes.Local);
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource factorySource = factory.Create("MySource");
                using ActivitySource standalone = new ActivitySource("MySource");

                using Activity? fromFactory = factorySource.StartActivity("Op");
                using Activity? fromStandalone = standalone.StartActivity("Op");

                Assert.NotNull(fromFactory);
                Assert.Null(fromStandalone);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GlobalScopeRuleMatchesStandaloneAndFactorySources()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", scopes: ActivitySourceScopes.Global | ActivitySourceScopes.Local);
                        AddSamplingListener(builder, "L1", out _);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource factorySource = factory.Create("MySource");
                using ActivitySource standalone = new ActivitySource("MySource");

                using Activity? fromFactory = factorySource.StartActivity("Op");
                using Activity? fromStandalone = standalone.StartActivity("Op");

                Assert.NotNull(fromFactory);
                Assert.NotNull(fromStandalone);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GlobalRuleOnOneFactoryDoesNotMatchAnotherFactorysLocalSource()
        {
            RemoteExecutor.Invoke(() =>
            {
                List<Activity>? aStarted = null;
                using ServiceProvider spA = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", scopes: ActivitySourceScopes.Global);
                        AddSamplingListener(builder, "LA", out aStarted);
                    });
                using ServiceProvider spB = BuildServices(
                    configure: builder =>
                    {
                        // factory B has no rules: any activity that fires is the result of A's
                        // listener attaching to B's source, which would be the cross-factory leak.
                        AddSamplingListener(builder, "LB", out _);
                    });

                using ActivitySourceFactory factoryA = spA.GetRequiredService<ActivitySourceFactory>();
                using ActivitySourceFactory factoryB = spB.GetRequiredService<ActivitySourceFactory>();

                using ActivitySource sourceFromB = factoryB.Create("MySource");
                using Activity? activity = sourceFromB.StartActivity("Op");

                Assert.Null(activity);
                Assert.Empty(aStarted!);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LocalRuleOnOneFactoryDoesNotMatchAnotherFactorysLocalSource()
        {
            RemoteExecutor.Invoke(() =>
            {
                List<Activity>? aStarted = null;
                using ServiceProvider spA = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", scopes: ActivitySourceScopes.Local);
                        AddSamplingListener(builder, "LA", out aStarted);
                    });
                using ServiceProvider spB = BuildServices(
                    configure: builder =>
                    {
                        AddSamplingListener(builder, "LB", out _);
                    });

                using ActivitySourceFactory factoryA = spA.GetRequiredService<ActivitySourceFactory>();
                using ActivitySourceFactory factoryB = spB.GetRequiredService<ActivitySourceFactory>();

                using ActivitySource sourceFromB = factoryB.Create("MySource");
                using Activity? activity = sourceFromB.StartActivity("Op");

                Assert.Null(activity);
                Assert.Empty(aStarted!);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerNameTargetsSpecificListenerOnly()
        {
            RemoteExecutor.Invoke(() =>
            {
                List<Activity>? l1Started = null;
                List<Activity>? l2Started = null;
                using var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource", listenerName: "L1");
                        AddSamplingListener(builder, "L1", out l1Started);
                        AddSamplingListener(builder, "L2", out l2Started);
                    });

                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");
                using Activity? activity = source.StartActivity("Op");

                Assert.NotNull(activity);
                Assert.NotEmpty(l1Started!);
                Assert.Empty(l2Started!);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DisposingFactoryUnsubscribesListeners()
        {
            RemoteExecutor.Invoke(() =>
            {
                var sp = BuildServices(
                    configure: builder =>
                    {
                        builder.EnableTracing("MySource");
                        AddSamplingListener(builder, "L1", out var started);
                    });

                ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using (ActivitySource warmup = factory.Create("MySource"))
                {
                    using Activity? a = warmup.StartActivity("Op");
                    Assert.NotNull(a);
                }

                factory.Dispose();

                using ActivitySource standalone = new ActivitySource("MySource");
                using Activity? afterDispose = standalone.StartActivity("Op");
                Assert.Null(afterDispose);

                sp.Dispose();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ConfigurationReloadAppliesNewRules()
        {
            RemoteExecutor.Invoke(() =>
            {
                var initial = new Dictionary<string, string?>
                {
                    ["EnabledTracing:MySource"] = "false",
                };
                var memorySource = new MemoryConfigurationSource { InitialData = initial };
                IConfigurationRoot config = new ConfigurationBuilder().Add(memorySource).Build();

                var services = new ServiceCollection();
                services.AddTracing(builder =>
                {
                    builder.AddConfiguration(config);
                    AddSamplingListener(builder, "L1", out _);
                });
                using ServiceProvider sp = services.BuildServiceProvider();
                using ActivitySourceFactory factory = sp.GetRequiredService<ActivitySourceFactory>();
                using ActivitySource source = factory.Create("MySource");

                using (Activity? before = source.StartActivity("Op"))
                {
                    Assert.Null(before);
                }

                config["EnabledTracing:MySource"] = "true";
                config.Reload();

                using (Activity? after = source.StartActivity("Op"))
                {
                    Assert.NotNull(after);
                }
            }).Dispose();
        }

        private static ServiceProvider BuildServices(Action<ITracingBuilder> configure)
        {
            var services = new ServiceCollection();
            services.AddTracing(configure);
            return services.BuildServiceProvider();
        }

        private static void AddSamplingListener(ITracingBuilder builder, string name, out List<Activity> started)
        {
            var local = new List<Activity>();
            started = local;
            builder.AddListener(name, b =>
            {
                b.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
                b.SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData;
                b.ActivityStarted = a =>
                {
                    lock (local) { local.Add(a); }
                };
            });
        }
    }
}
