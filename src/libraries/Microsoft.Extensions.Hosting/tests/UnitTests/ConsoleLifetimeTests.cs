// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class ConsoleLifetimeTests
    {
        private const string ContentRootDiagnosticMessagePrefix = "Content root path is the current working directory";
        private const string ApplicationStartedMessage = "Application started. Press Ctrl+C to shut down.";

        [Fact]
        public async Task LogsBasicStartupMessages()
        {
            string cwd = Directory.GetCurrentDirectory();
            string[] messages = await RunWithDefaultsAsync(
                contentRootPath: cwd,
                environmentName: "Production");

            Assert.Contains(ApplicationStartedMessage, messages);
            Assert.Contains("Hosting environment: Production", messages);
            Assert.Contains($"Content root path: {cwd}", messages);
        }

        [Fact]
        public async Task DoesNotLogStartupMessages_WhenSuppressed()
        {
            string[] messages = await RunWithDefaultsAsync(
                contentRootPath: Directory.GetCurrentDirectory(),
                configureLifetime: o => o.SuppressStatusMessages = true);

            Assert.Empty(messages);
        }

        [Fact]
        public async Task DoesNotLogContentRootDiagnostic_WhenContentRootDiffersFromCwd()
        {
            using var differentDirectory = new TempDirectory();

            string[] messages = await RunWithDefaultsAsync(contentRootPath: differentDirectory.Path);

            Assert.DoesNotContain(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
        }

        // The tests below mutate the process-global current working directory, so they run in
        // their own remote process.

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DoesNotLogContentRootDiagnostic_WhenAppSettingsExists()
        {
            RemoteExecutor.Invoke(static async () =>
            {
                using var dir = new CwdTempDirectory();
                File.WriteAllText(Path.Combine(dir.ResolvedPath, "appsettings.json"), "{}");

                string[] messages = await RunWithDefaultsAsync(contentRootPath: dir.ResolvedPath);

                Assert.DoesNotContain(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LogsContentRootDiagnostic_WhenDefaultsAppliedAndAppSettingsMissing()
        {
            RemoteExecutor.Invoke(static async () =>
            {
                using var dir = new CwdTempDirectory();
                Assert.False(File.Exists(Path.Combine(dir.ResolvedPath, "appsettings.json")));

                string[] messages = await RunWithDefaultsAsync(contentRootPath: dir.ResolvedPath);

                Assert.Contains(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DoesNotLogContentRootDiagnostic_WhenNoFileSourceRegistered()
        {
            // No file-based configuration sources rooted at the content root means the user opted
            // out of file-based config; absence of appsettings.json is not a signal.
            RemoteExecutor.Invoke(static async () =>
            {
                using var dir = new CwdTempDirectory();
                Assert.False(File.Exists(Path.Combine(dir.ResolvedPath, "appsettings.json")));

                string[] messages = await RunWithoutDefaultsAsync(contentRootPath: dir.ResolvedPath);

                Assert.DoesNotContain(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LogsContentRootDiagnostic_WhenContentRootIsFilesystemRoot()
        {
            RemoteExecutor.Invoke(static async () =>
            {
                string root = Path.GetPathRoot(Directory.GetCurrentDirectory());
                Assert.False(string.IsNullOrEmpty(root));
                Directory.SetCurrentDirectory(root);

                // Even with no file sources registered, a filesystem-root content root is
                // suspicious enough to always log.
                string[] messages = await RunWithoutDefaultsAsync(contentRootPath: Directory.GetCurrentDirectory());

                Assert.Contains(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LogsContentRootDiagnostic_EvenWhenApplicationFailsToStart()
        {
            // The diagnostic should fire when the lifetime starts waiting, before any hosted
            // services run, so users still see it when the host fails during startup.
            RemoteExecutor.Invoke(static async () =>
            {
                using var dir = new CwdTempDirectory();

                var loggerProvider = new TestLoggerProvider();
                IHostBuilder builder = new HostBuilder()
                    .ConfigureDefaults(Array.Empty<string>())
                    .UseContentRoot(dir.ResolvedPath)
                    .ConfigureLogging(logging => logging.AddProvider(loggerProvider))
                    .ConfigureServices(services => services.AddHostedService<ThrowingHostedService>());

                using IHost host = builder.Build();
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

                string[] messages = loggerProvider.GetEvents().Select(e => e.Message).ToArray();
                Assert.Contains(messages, m => m.StartsWith(ContentRootDiagnosticMessagePrefix));
                Assert.DoesNotContain(ApplicationStartedMessage, messages);
            }).Dispose();
        }

        private static async Task<string[]> RunWithDefaultsAsync(
            string contentRootPath,
            string environmentName = "Production",
            Action<ConsoleLifetimeOptions> configureLifetime = null)
        {
            var loggerProvider = new TestLoggerProvider();
            IHostBuilder builder = new HostBuilder()
                .ConfigureDefaults(Array.Empty<string>())
                .UseContentRoot(contentRootPath)
                .UseEnvironment(environmentName)
                .ConfigureLogging(logging => logging.AddProvider(loggerProvider));

            if (configureLifetime is not null)
            {
                builder = builder.UseConsoleLifetime(configureLifetime);
            }

            using IHost host = builder.Build();
            await host.StartAsync();
            await host.StopAsync();

            return loggerProvider.GetEvents().Select(e => e.Message).ToArray();
        }

        private static async Task<string[]> RunWithoutDefaultsAsync(string contentRootPath)
        {
            var loggerProvider = new TestLoggerProvider();
            using IHost host = new HostBuilder()
                .UseContentRoot(contentRootPath)
                .ConfigureLogging(logging => logging.AddProvider(loggerProvider))
                .UseConsoleLifetime()
                .Build();

            await host.StartAsync();
            await host.StopAsync();

            return loggerProvider.GetEvents().Select(e => e.Message).ToArray();
        }

        // TempDirectory that also sets the current working directory to its path for the lifetime
        // of the instance. Always used inside RemoteExecutor.Invoke to keep the CWD change
        // isolated from the test runner process.
        //
        // On macOS, Path.GetTempPath() returns a path under /tmp while Directory.GetCurrentDirectory()
        // returns the resolved path (under /private/tmp). Capture the resolved CWD so callers can use
        // a path that matches what ConsoleLifetime sees at runtime.
        private sealed class CwdTempDirectory : TempDirectory
        {
            private readonly string _previousCwd;

            /// <summary>The resolved current directory after this instance's <c>SetCurrentDirectory</c> call.</summary>
            public string ResolvedPath { get; }

            public CwdTempDirectory()
            {
                _previousCwd = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path);
                ResolvedPath = Directory.GetCurrentDirectory();
            }

            protected override void DeleteDirectory()
            {
                // Restore the CWD first - on Windows, a directory can't be deleted while it is
                // the process's current working directory.
                try { Directory.SetCurrentDirectory(_previousCwd); }
                catch { }
                base.DeleteDirectory();
            }
        }

        private sealed class ThrowingHostedService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken) =>
                throw new InvalidOperationException("startup failed");

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
