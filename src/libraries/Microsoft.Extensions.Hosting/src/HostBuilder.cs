// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// A program initialization utility.
    /// </summary>
    public partial class HostBuilder : IHostBuilder
    {
        private const string HostBuildingDiagnosticListenerName = "Microsoft.Extensions.Hosting";
        private const string HostBuildingEventName = "HostBuilding";
        private const string HostBuiltEventName = "HostBuilt";

        private readonly List<Action<IConfigurationBuilder>> _configureHostConfigActions = new List<Action<IConfigurationBuilder>>();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();
        private readonly List<IConfigureContainerAdapter> _configureContainerActions = new List<IConfigureContainerAdapter>();
        private IServiceFactoryAdapter _serviceProviderFactory;
        private bool _hostBuilt;
        private IConfiguration? _hostConfiguration;
        private IConfiguration? _appConfiguration;
        private HostBuilderContext? _hostBuilderContext;
        private HostingEnvironment? _hostingEnvironment;
        private IServiceProvider? _appServices;
        private PhysicalFileProvider? _defaultProvider;
        private readonly bool? _defaultProviderFactoryUsed;

        /// <summary>
        /// Initializes a new instance of <see cref="HostBuilder"/>.
        /// </summary>
        public HostBuilder()
        {
            _serviceProviderFactory = new ServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory());
            _defaultProviderFactoryUsed = true;
        }

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Set up the configuration for the builder itself. This will be used to initialize the <see cref="IHostEnvironment"/>
        /// for use later in the build process. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            ThrowHelper.ThrowIfNull(configureDelegate);

            _configureHostConfigActions.Add(configureDelegate);
            return this;
        }

        /// <summary>
        /// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
        /// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
        /// subsequent operations, as well as in <see cref="IHost.Services"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            ThrowHelper.ThrowIfNull(configureDelegate);

            _configureAppConfigActions.Add(configureDelegate);
            return this;
        }

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            ThrowHelper.ThrowIfNull(configureDelegate);

            _configureServicesActions.Add(configureDelegate);
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <param name="factory">A factory used for creating service providers.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            ThrowHelper.ThrowIfNull(factory);

            _serviceProviderFactory = new ServiceFactoryAdapter<TContainerBuilder>(factory);
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <param name="factory">A factory used for creating service providers.</param>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            ThrowHelper.ThrowIfNull(factory);

            _serviceProviderFactory = new ServiceFactoryAdapter<TContainerBuilder>(() => _hostBuilderContext!, factory);
            return this;
        }

        /// <summary>
        /// Enables configuring the instantiated dependency container. This can be called multiple times and
        /// the results will be additive.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            ThrowHelper.ThrowIfNull(configureDelegate);

            _configureContainerActions.Add(new ConfigureContainerAdapter<TContainerBuilder>(configureDelegate));
            return this;
        }

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IHost"/></returns>
        /// <remarks>Adds basic services to the host such as application lifetime, host environment, and logging.</remarks>
        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException(SR.BuildCalled);
            }
            _hostBuilt = true;

            // REVIEW: If we want to raise more events outside of these calls then we will need to
            // stash this in a field.
            using DiagnosticListener diagnosticListener = LogHostBuilding(this);

            InitializeHostConfiguration();
            InitializeHostingEnvironment();
            InitializeHostBuilderContext();
            InitializeAppConfiguration();
            InitializeServiceProvider();

            return ResolveHost(_appServices, diagnosticListener);
        }

        private static DiagnosticListener LogHostBuilding(IHostBuilder hostBuilder)
        {
            var diagnosticListener = new DiagnosticListener(HostBuildingDiagnosticListenerName);

            if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(HostBuildingEventName))
            {
                Write(diagnosticListener, HostBuildingEventName, hostBuilder);
            }

            return diagnosticListener;
        }

        internal static DiagnosticListener LogHostBuilding(HostApplicationBuilder hostApplicationBuilder)
        {
            var diagnosticListener = new DiagnosticListener(HostBuildingDiagnosticListenerName);

            if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(HostBuildingEventName))
            {
                Write(diagnosticListener, HostBuildingEventName, hostApplicationBuilder.AsHostBuilder());
            }

            return diagnosticListener;
        }

// Remove when https://github.com/dotnet/runtime/pull/78532 is merged and consumed by the used SDK.
#if NET7_0
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
            Justification = "DiagnosticSource is used here to pass objects in-memory to code using HostFactoryResolver. This won't require creating new generic types.")]
#endif
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "The values being passed into Write are being consumed by the application already.")]
        private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            DiagnosticListener diagnosticSource,
            string name,
            T value)
        {
            diagnosticSource.Write(name, value);
        }

        [MemberNotNull(nameof(_hostConfiguration))]
        private void InitializeHostConfiguration()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(); // Make sure there's some default storage since there are no default providers

            foreach (Action<IConfigurationBuilder> buildAction in _configureHostConfigActions)
            {
                buildAction(configBuilder);
            }
            _hostConfiguration = configBuilder.Build();
        }

        [MemberNotNull(nameof(_defaultProvider))]
        [MemberNotNull(nameof(_hostingEnvironment))]
        private void InitializeHostingEnvironment()
        {
            (_hostingEnvironment, _defaultProvider) = CreateHostingEnvironment(_hostConfiguration!); // TODO-NULLABLE: https://github.com/dotnet/csharplang/discussions/5778. The same pattern exists below as well.
        }

        internal static (HostingEnvironment, PhysicalFileProvider) CreateHostingEnvironment(IConfiguration hostConfiguration)
        {
            var hostingEnvironment = new HostingEnvironment()
            {
                EnvironmentName = hostConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production,
                ContentRootPath = ResolveContentRootPath(hostConfiguration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            };

            string? applicationName = hostConfiguration[HostDefaults.ApplicationKey];
            if (string.IsNullOrEmpty(applicationName))
            {
                // Note GetEntryAssembly returns null for the net4x console test runner.
                applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            }

            if (applicationName is not null)
            {
                hostingEnvironment.ApplicationName = applicationName;
            }

            var physicalFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);
            hostingEnvironment.ContentRootFileProvider = physicalFileProvider;

            return (hostingEnvironment, physicalFileProvider);
        }

        internal static string ResolveContentRootPath(string? contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }

        [MemberNotNull(nameof(_hostBuilderContext))]
        private void InitializeHostBuilderContext()
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment!,
                Configuration = _hostConfiguration!
            };
        }

        [MemberNotNull(nameof(_appConfiguration))]
        private void InitializeAppConfiguration()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(_hostingEnvironment!.ContentRootPath)
                .AddConfiguration(_hostConfiguration!, shouldDisposeConfiguration: true);

            foreach (Action<HostBuilderContext, IConfigurationBuilder> buildAction in _configureAppConfigActions)
            {
                buildAction(_hostBuilderContext!, configBuilder);
            }
            _appConfiguration = configBuilder.Build();
            _hostBuilderContext!.Configuration = _appConfiguration;
        }

        [MemberNotNull(nameof(_appServices))]
        internal static void PopulateServiceCollection(
            IServiceCollection services,
            HostBuilderContext hostBuilderContext,
            HostingEnvironment hostingEnvironment,
            PhysicalFileProvider defaultFileProvider,
            IConfiguration appConfiguration,
            Func<IServiceProvider> serviceProviderGetter)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddSingleton<IHostingEnvironment>(hostingEnvironment);
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<IHostEnvironment>(hostingEnvironment);
            services.AddSingleton(hostBuilderContext);
            // register configuration as factory to make it dispose with the service provider
            services.AddSingleton(_ => appConfiguration);
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddSingleton(s => (IApplicationLifetime)s.GetRequiredService<IHostApplicationLifetime>());
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<IHostApplicationLifetime, ApplicationLifetime>();

            AddLifetime(services);

            services.AddSingleton<IHost>(_ =>
            {
                // We use serviceProviderGetter() instead of the _ parameter because these can be different given a custom IServiceProviderFactory.
                // We want the host to always dispose the IServiceProvider returned by the IServiceProviderFactory.
                // https://github.com/dotnet/runtime/issues/36060
                IServiceProvider appServices = serviceProviderGetter();
                return new Internal.Host(appServices,
                    hostingEnvironment,
                    defaultFileProvider,
                    appServices.GetRequiredService<IHostApplicationLifetime>(),
                    appServices.GetRequiredService<ILogger<Internal.Host>>(),
                    appServices.GetRequiredService<IHostLifetime>(),
                    appServices.GetRequiredService<IOptions<HostOptions>>());
            });
            services.AddOptions().Configure<HostOptions>(options => { options.Initialize(hostBuilderContext.Configuration); });
            services.AddLogging();
            services.AddMetrics();
        }

        [MemberNotNull(nameof(_appServices))]
        private void InitializeServiceProvider()
        {
            var services = new ServiceCollection();

            PopulateServiceCollection(
                services,
                _hostBuilderContext!,
                _hostingEnvironment!,
                _defaultProvider!,
                _appConfiguration!,
                () => _appServices!);

            foreach (Action<HostBuilderContext, IServiceCollection> configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostBuilderContext!, services);
            }

            if (_hostBuilderContext!.HostingEnvironment.IsDevelopment() && _defaultProviderFactoryUsed.HasValue && _defaultProviderFactoryUsed.Value)
            {
                _serviceProviderFactory = new ServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));
            }

            object containerBuilder = _serviceProviderFactory.CreateBuilder(services);

            foreach (IConfigureContainerAdapter containerAction in _configureContainerActions)
            {
                containerAction.ConfigureContainer(_hostBuilderContext!, containerBuilder);
            }

            _appServices = _serviceProviderFactory.CreateServiceProvider(containerBuilder);
        }

        internal static IHost ResolveHost(IServiceProvider serviceProvider, DiagnosticListener diagnosticListener)
        {
            if (serviceProvider is null)
            {
                throw new InvalidOperationException(SR.NullIServiceProvider);
            }

            // resolve configuration explicitly once to mark it as resolved within the
            // service provider, ensuring it will be properly disposed with the provider
            _ = serviceProvider.GetService<IConfiguration>();

            var host = serviceProvider.GetRequiredService<IHost>();

            if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(HostBuiltEventName))
            {
                Write(diagnosticListener, HostBuiltEventName, host);
            }

            return host;
        }
    }
}
