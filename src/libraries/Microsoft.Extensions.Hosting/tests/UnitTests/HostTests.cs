// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public partial class HostTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_IncludesContentRootByDefault()
        {
            var expected = Directory.GetCurrentDirectory();
            var builder = Host.CreateDefaultBuilder();
            var host = builder.Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            Assert.Equal(expected, config["ContentRoot"]);
            var env = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal(expected, env.ContentRootPath);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_IncludesCommandLineArguments()
        {
            var expected = Directory.GetParent(Directory.GetCurrentDirectory()).FullName; // It must exist
            var builder = Host.CreateDefaultBuilder(new string[] { "--contentroot", expected });
            var host = builder.Build();
            var env = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal(expected, env.ContentRootPath);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_RegistersEventSourceLogger()
        {
            var listener = new TestEventListener();
            var host = Host.CreateDefaultBuilder()
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<HostTests>>();
            logger.LogInformation("Request starting");

            var events = listener.EventData.ToArray();
            Assert.Contains(events, args =>
                args.EventSource.Name == "Microsoft-Extensions-Logging" &&
                args.Payload.OfType<string>().Any(p => p.Contains("Request starting")));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_EnablesActivityTracking()
        {
            var parentActivity = new Activity("ParentActivity");
            parentActivity.Start();
            var activity = new Activity("ChildActivity");
            activity.Start();
            var id = activity.Id;
            var logger = new ScopeDelegateLogger((scopeObjectList) =>
            {
                Assert.Equal(1, scopeObjectList.Count);
                var activityDictionary = (scopeObjectList.FirstOrDefault() as IEnumerable<KeyValuePair<string, object>>)
                                                .ToDictionary(x => x.Key, x => x.Value);
                switch (activity.IdFormat)
                {
                    case ActivityIdFormat.Hierarchical:
                        Assert.Equal(activity.Id, activityDictionary["SpanId"]);
                        Assert.Equal(activity.RootId, activityDictionary["TraceId"]);
                        Assert.Equal(activity.ParentId, activityDictionary["ParentId"]);
                        break;
                    case ActivityIdFormat.W3C:
                        Assert.Equal(activity.SpanId.ToHexString(), activityDictionary["SpanId"]);
                        Assert.Equal(activity.TraceId.ToHexString(), activityDictionary["TraceId"]);
                        Assert.Equal(activity.ParentSpanId.ToHexString(), activityDictionary["ParentId"]);
                        break;
                }
            });
            var loggerProvider = new ScopeDelegateLoggerProvider(logger);
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(loggerProvider);
                })
                .Build();

            logger.LogInformation("Dummy log");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_EnablesScopeValidation()
        {
            var host = Host.CreateDefaultBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(serices =>
                {
                    serices.AddScoped<ServiceA>();
                })
                .Build();

            Assert.Throws<InvalidOperationException>(() => { host.Services.GetRequiredService<ServiceA>(); });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void CreateDefaultBuilder_EnablesValidateOnBuild()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(serices =>
                {
                    serices.AddSingleton<ServiceB>();
                });

            Assert.Throws<AggregateException>(() => hostBuilder.Build());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public async Task CreateDefaultBuilder_ConfigJsonDoesNotReload()
        {
            var reloadFlagConfig = new Dictionary<string, string>() {{ "hostbuilder:reloadConfigOnChange", "false" }};
            var appSettingsPath = Path.Combine(Path.GetTempPath(), "appsettings.json");

            string SaveRandomConfig()
            {
                var newMessage = $"Hello ASP.NET Core: {Guid.NewGuid():N}";
                File.WriteAllText(appSettingsPath, $"{{ \"Hello\": \"{newMessage}\" }}");
                return newMessage;
            }

            var dynamicConfigMessage1 = SaveRandomConfig();

            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(Path.GetDirectoryName(appSettingsPath))
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(reloadFlagConfig);
                })
                .Build();

            var config = host.Services.GetRequiredService<IConfiguration>();

            Assert.Equal(dynamicConfigMessage1, config["Hello"]);

            var dynamicConfigMessage2 = SaveRandomConfig();
            await Task.Delay(1000); // Give reload time to fire if it's going to.
            Assert.NotEqual(dynamicConfigMessage1, dynamicConfigMessage2); // Messages are different.
            Assert.Equal(dynamicConfigMessage1, config["Hello"]); // Config did not reload
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34580", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public async Task CreateDefaultBuilder_ConfigJsonDoesReload()
        {
            var reloadFlagConfig = new Dictionary<string, string>() { { "hostbuilder:reloadConfigOnChange", "true" } };
            var appSettingsPath = Path.Combine(Path.GetTempPath(), "appsettings.json");

            string SaveRandomConfig()
            {
                var newMessage = $"Hello ASP.NET Core: {Guid.NewGuid():N}";
                File.WriteAllText(appSettingsPath, $"{{ \"Hello\": \"{newMessage}\" }}");
                return newMessage;
            }

            var dynamicConfigMessage1 = SaveRandomConfig();

            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(Path.GetDirectoryName(appSettingsPath))
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(reloadFlagConfig);
                })
                .Build();

            var config = host.Services.GetRequiredService<IConfiguration>();

            Assert.Equal(dynamicConfigMessage1, config["Hello"]);

            var dynamicConfigMessage2 = SaveRandomConfig();

            var configReloadedCancelTokenSource = new CancellationTokenSource();
            var configReloadedCancelToken = configReloadedCancelTokenSource.Token;

            config.GetReloadToken().RegisterChangeCallback(o =>
            {
                configReloadedCancelTokenSource.Cancel();
            }, null);
            // Wait for up to 10 seconds, if config reloads at any time, cancel the wait.
            await Task.WhenAny(Task.Delay(10000, configReloadedCancelToken)); // Task.WhenAny ignores the task throwing on cancellation.
            Assert.NotEqual(dynamicConfigMessage1, dynamicConfigMessage2); // Messages are different.
            Assert.Equal(dynamicConfigMessage2, config["Hello"]); // Config DID reload from disk
        }

        internal class ServiceA { }

        internal class ServiceB
        {
            public ServiceB(ServiceC c)
            {

            }
        }

        internal class ServiceC { }

        private class ScopeDelegateLoggerProvider : ILoggerProvider, ISupportExternalScope
        {
            private ScopeDelegateLogger _logger;
            private IExternalScopeProvider _scopeProvider;
            public ScopeDelegateLoggerProvider(ScopeDelegateLogger logger)
            {
                _logger = logger;
            }
            public ILogger CreateLogger(string categoryName)
            {
                _logger.ScopeProvider = _scopeProvider;
                return _logger;
            }

            public void Dispose()
            {
            }

            public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                _scopeProvider = scopeProvider;
            }
        }

        private class ScopeDelegateLogger : ILogger
        {
            private Action<List<object>> _logDelegate;
            internal IExternalScopeProvider ScopeProvider { get; set; }
            public ScopeDelegateLogger(Action<List<object>> logDelegate)
            {
                _logDelegate = logDelegate;
            }
            public IDisposable BeginScope<TState>(TState state)
            {
                Scopes.Add(state);
                return new Scope();
            }

            public List<object> Scopes { get; set; } = new List<object>();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                ScopeProvider.ForEachScope((scopeObject, state) =>
                {
                    Scopes.Add(scopeObject);
                }, 0);
                _logDelegate(Scopes);
            }

            private class Scope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;

            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-Extensions-Logging")
                {
                    EnableEvents(eventSource, EventLevel.Informational);
                }
            }

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
    }
}
