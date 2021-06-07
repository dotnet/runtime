// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace Microsoft.Extensions.Hosting
{
    internal sealed class HostFactoryResolver
    {
        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public const string BuildWebHost = nameof(BuildWebHost);
        public const string CreateWebHostBuilder = nameof(CreateWebHostBuilder);
        public const string CreateHostBuilder = nameof(CreateHostBuilder);

        public static Func<string[], TWebHost>? ResolveWebHostFactory<TWebHost>(Assembly assembly)
        {
            return ResolveFactory<TWebHost>(assembly, BuildWebHost);
        }

        public static Func<string[], TWebHostBuilder>? ResolveWebHostBuilderFactory<TWebHostBuilder>(Assembly assembly)
        {
            return ResolveFactory<TWebHostBuilder>(assembly, CreateWebHostBuilder);
        }

        public static Func<string[], THostBuilder>? ResolveHostBuilderFactory<THostBuilder>(Assembly assembly)
        {
            return ResolveFactory<THostBuilder>(assembly, CreateHostBuilder);
        }

        public static Func<string[], IHostBuilder>? ResolveHostBuilderFactory(Assembly assembly)
        {
            if (assembly.EntryPoint is null)
            {
                return null;
            }

            return args => new DeferredHostBuilder(args, assembly.EntryPoint);
        }

        private static Func<string[], T>? ResolveFactory<T>(Assembly assembly, string name)
        {
            var programType = assembly?.EntryPoint?.DeclaringType;
            if (programType == null)
            {
                return null;
            }

            var factory = programType.GetMethod(name, DeclaredOnlyLookup);
            if (!IsFactory<T>(factory))
            {
                return null;
            }

            return args => (T)factory!.Invoke(null, new object[] { args })!;
        }

        // TReturn Factory(string[] args);
        private static bool IsFactory<TReturn>(MethodInfo? factory)
        {
            return factory != null
                && typeof(TReturn).IsAssignableFrom(factory.ReturnType)
                && factory.GetParameters().Length == 1
                && typeof(string[]).Equals(factory.GetParameters()[0].ParameterType);
        }

        // Used by EF tooling without any Hosting references. Looses some return type safety checks.
        public static Func<string[], IServiceProvider?>? ResolveServiceProviderFactory(Assembly assembly)
        {
            // Prefer the older patterns by default for back compat.
            var webHostFactory = ResolveWebHostFactory<object>(assembly);
            if (webHostFactory != null)
            {
                return args =>
                {
                    var webHost = webHostFactory(args);
                    return GetServiceProvider(webHost);
                };
            }

            var webHostBuilderFactory = ResolveWebHostBuilderFactory<object>(assembly);
            if (webHostBuilderFactory != null)
            {
                return args =>
                {
                    var webHostBuilder = webHostBuilderFactory(args);
                    var webHost = Build(webHostBuilder);
                    return GetServiceProvider(webHost);
                };
            }

            var hostBuilderFactory = ResolveHostBuilderFactory<object>(assembly);
            if (hostBuilderFactory != null)
            {
                return args =>
                {
                    var hostBuilder = hostBuilderFactory(args);
                    var host = Build(hostBuilder);
                    return GetServiceProvider(host);
                };
            }

            var deferredFactory = ResolveHostBuilderFactory(assembly);
            if (deferredFactory != null)
            {
                return args =>
                {
                    var hostBuilder = deferredFactory(args);
                    var host = hostBuilder.Build();
                    return host.Services;
                };
            }

            return null;
        }

        private static object? Build(object builder)
        {
            var buildMethod = builder.GetType().GetMethod("Build");
            return buildMethod?.Invoke(builder, Array.Empty<object>());
        }

        private static IServiceProvider? GetServiceProvider(object? host)
        {
            if (host == null)
            {
                return null;
            }
            var hostType = host.GetType();
            var servicesProperty = hostType.GetProperty("Services", DeclaredOnlyLookup);
            return (IServiceProvider?)servicesProperty?.GetValue(host);
        }

        // This host builder captures calls to the IHostBuilder then replays them on the application's
        // IHostBuilder when the event fires
        private class DeferredHostBuilder : IHostBuilder, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
        {
            public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

            private readonly string[] _args;
            private readonly MethodInfo _entryPoint;

            private readonly TaskCompletionSource<IHost> _hostTcs = new();
            private IDisposable? _disposable;

            private Action<IHostBuilder> _configure;

            // The amount of time we wait for the diagnostic source events to fire
            private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(20);

            public DeferredHostBuilder(string[] args, MethodInfo entryPoint)
            {
                _args = args;
                _entryPoint = entryPoint;
                _configure = b =>
                {
                    // Copy the properties from this builder into the builder
                    // that we're going to receive
                    foreach (var pair in Properties)
                    {
                        b.Properties[pair.Key] = pair.Value;
                    }
                };
            }

            public IHost Build()
            {
                using var subscription = DiagnosticListener.AllListeners.Subscribe(this);

                // Kick off the entry point on a new thread so we don't block the current one
                // in case we need to timeout the execution
                var thread = new Thread(() =>
                {
                    try
                    {
                        var parameters = _entryPoint.GetParameters();
                        if (parameters.Length == 0)
                        {
                            _entryPoint.Invoke(null, Array.Empty<object>());
                        }
                        else
                        {
                            _entryPoint.Invoke(null, new object[] { _args });
                        }

                        // Try to set an exception if the entrypoint returns gracefully, this will force
                        // build to throw
                        _hostTcs.TrySetException(new InvalidOperationException("Unable to build IHost"));
                    }
                    catch (TargetInvocationException tie) when (tie.InnerException is StopTheHostException)
                    {
                        // The host was stopped by our own logic
                    }
                    catch (TargetInvocationException tie)
                    {
                        // Another exception happened, propagate that to the caller
                        _hostTcs.TrySetException(tie.InnerException ?? tie);
                    }
                    catch (Exception ex)
                    {
                        // Another exception happened, propagate that to the caller
                        _hostTcs.TrySetException(ex);
                    }
                })
                {
                    // Make sure this doesn't hang the process
                    IsBackground = true
                };

                // Start the thread
                thread.Start();

                try
                {
                    // Wait before throwing an exception
                    if (!_hostTcs.Task.Wait(_waitTimeout))
                    {
                        throw new InvalidOperationException("Unable to build IHost");
                    }
                }
                catch (AggregateException) when (_hostTcs.Task.IsCompleted)
                {
                    // Lets this propogate out of the call to GetAwaiter().GetResult()
                }

                Debug.Assert(_hostTcs.Task.IsCompleted);

                return _hostTcs.Task.GetAwaiter().GetResult();
            }

            public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                _configure += b => b.ConfigureAppConfiguration(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
            {
                _configure += b => b.ConfigureContainer(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
            {
                _configure += b => b.ConfigureHostConfiguration(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
            {
                _configure += b => b.ConfigureServices(configureDelegate);
                return this;
            }

            public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
            {
                _configure += b => b.UseServiceProviderFactory(factory);
                return this;
            }

            public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
            {
                _configure += b => b.UseServiceProviderFactory(factory);
                return this;
            }
            
            public void OnCompleted()
            {
                _disposable?.Dispose();
            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(DiagnosticListener value)
            {
                if (value.Name == "Microsoft.Extensions.Hosting")
                {
                    _disposable = value.Subscribe(this);
                }
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (value.Key == "HostBuilding")
                {
                    if (value.Value is IHostBuilder builder)
                    {
                        _configure(builder);
                    }
                }

                if (value.Key == "HostBuilt")
                {
                    if (value.Value is IHost host)
                    {
                        _hostTcs.TrySetResult(host);

                        // Stop the host from running further
                        throw new StopTheHostException();
                    }
                }
            }

            private class StopTheHostException : Exception
            {

            }
        }
    }
}
