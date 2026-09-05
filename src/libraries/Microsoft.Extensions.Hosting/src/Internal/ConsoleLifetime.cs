// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    /// <summary>
    /// Listens for Ctrl+C or SIGTERM and initiates shutdown.
    /// </summary>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public partial class ConsoleLifetime : IHostLifetime, IDisposable
    {
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        /// <summary>
        /// Initializes a <see cref="ConsoleLifetime"/> instance using the specified console lifetime options, host environment, host application lifetime, and host options.
        /// </summary>
        /// <param name="options">An object used to retrieve <see cref="ConsoleLifetimeOptions"/> instances.</param>
        /// <param name="environment">Information about the hosting environment an application is running in.</param>
        /// <param name="applicationLifetime">An object that allows consumers to be notified of application lifetime events.</param>
        /// <param name="hostOptions">An object used to retrieve internal host options instances.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="environment"/> or <paramref name="applicationLifetime"/> or <paramref name="hostOptions"/> is <see langword="null"/>.</exception>
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions)
            : this(options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Initializes a <see cref="ConsoleLifetime"/> instance using the specified console lifetime options, host environment, host options, and logger factory.
        /// </summary>
        /// <param name="options">An object used to retrieve <see cref="ConsoleLifetimeOptions"/> instances.</param>
        /// <param name="environment">Information about the hosting environment an application is running in.</param>
        /// <param name="applicationLifetime">An object that allows consumers to be notified of application lifetime events.</param>
        /// <param name="hostOptions">An object used to retrieve <see cref="HostOptions"/> instances.</param>
        /// <param name="loggerFactory">An object to configure the logging system and create instances of <see cref="ILogger"/> from the registered <see cref="ILoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="environment"/> or <paramref name="applicationLifetime"/> or <paramref name="hostOptions"/> or <paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory)
            : this(options, environment, applicationLifetime, hostOptions, loggerFactory, configuration: null) { }

        // Internal ctor accepting IConfiguration for diagnostic logging. Kept internal to avoid
        // adding a new public API surface to a pubternal class; ConsoleLifetime is registered via
        // a factory (see HostingHostBuilderExtensions.AddConsoleLifetime) so DI doesn't need
        // to pick this ctor automatically.
        internal ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory, IConfiguration? configuration)
        {
            ArgumentNullException.ThrowIfNull(options?.Value, nameof(options));
            ArgumentNullException.ThrowIfNull(applicationLifetime);
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(hostOptions?.Value, nameof(hostOptions));
            ArgumentNullException.ThrowIfNull(loggerFactory);

            Options = options.Value;
            Environment = environment;
            ApplicationLifetime = applicationLifetime;
            HostOptions = hostOptions.Value;
            Configuration = configuration;
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        private ConsoleLifetimeOptions Options { get; }

        private IHostEnvironment Environment { get; }

        private IHostApplicationLifetime ApplicationLifetime { get; }

        private HostOptions HostOptions { get; }

        private IConfiguration? Configuration { get; }

        private ILogger Logger { get; }

        /// <summary>
        /// Registers the application start, application stop and shutdown handlers for this application.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous registration operation.</returns>
        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            if (!Options.SuppressStatusMessages)
            {
                // Log a diagnostic when the content root is the current working directory and looks
                // suspicious. Logging here (rather than in OnApplicationStarted) ensures the message
                // is still emitted when the host fails to start (e.g. a hosted service can't find
                // appsettings.json because the working directory is unintentionally "/" in a
                // container without WORKDIR or when launched by systemd without WorkingDirectory).
                if (Logger.IsEnabled(LogLevel.Information) && ShouldWarnAboutContentRoot())
                {
                    Logger.LogInformation("Content root path is the current working directory ({ContentRoot}). To override, set the content root explicitly.", Environment.ContentRootPath);
                }

                _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
                {
                    ((ConsoleLifetime)state!).OnApplicationStarted();
                },
                this);
                _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
                {
                    ((ConsoleLifetime)state!).OnApplicationStopping();
                },
                this);
            }

            RegisterShutdownHandlers();

            // Console applications start immediately.
            return Task.CompletedTask;
        }

        private bool ShouldWarnAboutContentRoot()
        {
            try
            {
                string contentRootPath = Environment.ContentRootPath;

                // Hosting does not normalize ContentRootPath, so an exact match
                // indicates the working directory is being used as the content root.
                // A user-specified content root that happens to expand to the same directory
                // but as a different string (e.g. with a trailing separator, or via "./")
                // is assumed to be intentional.
                if (!string.Equals(contentRootPath, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
                {
                    return false;
                }

                // Case 1: the content root is a filesystem root (e.g. "/" or "C:\").
                // Almost certainly not where the user intended their app to be rooted - log
                // regardless of how it got set.
                if (string.Equals(Path.GetPathRoot(contentRootPath), contentRootPath, StringComparison.Ordinal))
                {
                    return true;
                }

                // Case 2: at least one file-based configuration source is rooted at the content
                // root but none of those files exist on disk. This typically means appsettings.json
                // was expected (hosting defaults registered it) but the working directory doesn't
                // actually contain it - a sign the working directory is not the intended app
                // directory.
                return AllContentRootFileSourcesAreMissing(contentRootPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ArgumentException)
            {
                // This diagnostic is a heuristic. I/O and security failures from
                // querying the current directory or probing file-based configuration providers
                // should simply skip the diagnostic.
                return false;
            }
        }

        private bool AllContentRootFileSourcesAreMissing(string contentRootPath)
        {
            if (Configuration is not IConfigurationRoot configRoot)
            {
                return false;
            }

            bool sawContentRootSource = false;

            foreach (IConfigurationProvider provider in configRoot.Providers)
            {
                if (provider is not FileConfigurationProvider fileProvider)
                {
                    continue;
                }

                FileConfigurationSource source = fileProvider.Source;
                if (source.FileProvider is not PhysicalFileProvider physicalProvider)
                {
                    // We can only compare paths against the content root for physical providers.
                    continue;
                }

                if (!TrimTrailingDirectorySeparator(physicalProvider.Root).Equals(contentRootPath.AsSpan(), StringComparison.Ordinal))
                {
                    continue;
                }

                if (source.Path is not string sourcePath)
                {
                    continue;
                }

                sawContentRootSource = true;

                if (source.FileProvider.GetFileInfo(sourcePath).Exists)
                {
                    return false;
                }
            }

            return sawContentRootSource;
        }

        private static ReadOnlySpan<char> TrimTrailingDirectorySeparator(string path)
        {
            if (path.Length <= 1)
            {
                return path;
            }

            char last = path[path.Length - 1];
            if (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar)
            {
                return path.AsSpan(0, path.Length - 1);
            }

            return path;
        }

        private partial void RegisterShutdownHandlers();

        private void OnApplicationStarted()
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation("Application started. Press Ctrl+C to shut down.");
                Logger.LogInformation("Hosting environment: {EnvName}", Environment.EnvironmentName);
                Logger.LogInformation("Content root path: {ContentRoot}", Environment.ContentRootPath);
            }
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down...");
        }

        /// <summary>
        /// This method does nothing.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token instance.</param>
        /// <returns>A <see cref="Task"/> that represents a completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // There's nothing to do here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Unregisters the shutdown handlers and disposes the application start and application stop registrations.
        /// </summary>
        public void Dispose()
        {
            UnregisterShutdownHandlers();

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
        }

        private partial void UnregisterShutdownHandlers();
    }
}
