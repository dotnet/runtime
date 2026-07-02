// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implements <see cref="IOptionsMonitor{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public class OptionsMonitor<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptionsMonitor<TOptions>,
        IDisposable
        where TOptions : class
    {
        private readonly IOptionsMonitorCache<TOptions> _cache;
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly IServiceScopeFactory? _asyncValidationScopeFactory;
        private readonly AsyncValidationState? _asyncValidationState;
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        internal event Action<TOptions, string>? _onChange;

        private ConcurrentDictionary<string, bool> CacheKeys => GetAsyncValidationState().CacheKeys;
        private ConcurrentDictionary<string, int> ReloadVersions => GetAsyncValidationState().ReloadVersions;
        private ConcurrentDictionary<string, ExceptionDispatchInfo> ReloadFailures => GetAsyncValidationState().ReloadFailures;
        private ConcurrentDictionary<string, TaskCompletionSource<object?>> PendingReloads => GetAsyncValidationState().PendingReloads;
        private ConcurrentDictionary<string, object> CacheLocks => GetAsyncValidationState().CacheLocks;

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsMonitor{TOptions}"/> with the specified factory, sources, and cache.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        /// <param name="cache">The cache used to store options.</param>
        public OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache)
            : this(factory, sources, cache, asyncValidationScopeFactory: null)
        {
        }

        internal OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache, IServiceScopeFactory? asyncValidationScopeFactory)
        {
            _factory = factory;
            _cache = cache;
            _asyncValidationScopeFactory = asyncValidationScopeFactory;
            _asyncValidationState = asyncValidationScopeFactory is not null ? new AsyncValidationState() : null;

            void RegisterSource(IOptionsChangeTokenSource<TOptions> source)
            {
                IDisposable registration = ChangeToken.OnChange(
                          source.GetChangeToken,
                          InvokeChanged,
                          source.Name);

                _registrations.Add(registration);
            }

            // The default DI container uses arrays under the covers. Take advantage of this knowledge
            // by checking for an array and enumerate over that, so we don't need to allocate an enumerator.
            if (sources is IOptionsChangeTokenSource<TOptions>[] sourcesArray)
            {
                foreach (IOptionsChangeTokenSource<TOptions> source in sourcesArray)
                {
                    RegisterSource(source);
                }
            }
            else
            {
                foreach (IOptionsChangeTokenSource<TOptions> source in sources)
                {
                    RegisterSource(source);
                }
            }
        }

        private AsyncValidationState GetAsyncValidationState()
        {
            AsyncValidationState? state = _asyncValidationState;
            Debug.Assert(state is not null);
            return state;
        }

        private void InvokeChanged(string? name)
        {
            name ??= Options.DefaultName;
            if (_asyncValidationScopeFactory is null)
            {
                InvokeChangedSynchronously(name);
                return;
            }

            object cacheLock = GetCacheLock(name);
            int version;
            TaskCompletionSource<object?> pendingReload = CreatePendingReload();
            lock (cacheLock)
            {
                version = ReloadVersions.AddOrUpdate(name, 1, static (_, currentVersion) => currentVersion + 1);
                PendingReloads[name] = pendingReload;
            }

            AsyncServiceScope scope;
            try
            {
                scope = _asyncValidationScopeFactory.CreateAsyncScope();
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                throw;
            }

            IAsyncValidateOptions<TOptions>[] asyncValidations;
            try
            {
                asyncValidations = ResolveAsyncValidations(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                DisposeAsyncValidationScope(scope);
                throw;
            }

            if (asyncValidations.Length == 0)
            {
                try
                {
                    DisposeAsyncValidationScope(scope);
                }
                catch (Exception ex)
                {
                    RecordReloadFailure(name, version, ex);
                    CompletePendingReload(name, version, pendingReload);
                    throw;
                }

                InvokeChangedSynchronously(name, version, pendingReload);
                return;
            }

            TOptions candidateOptions;
            try
            {
                candidateOptions = _factory.Create(name);
                ValidateAsync(name, candidateOptions, asyncValidations).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                DisposeAsyncValidationScope(scope);
                throw;
            }

            try
            {
                DisposeAsyncValidationScope(scope);
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                throw;
            }

            Action<TOptions, string>? onChange;
            try
            {
                lock (cacheLock)
                {
                    if (!ReloadVersions.TryGetValue(name, out int currentVersion) ||
                        currentVersion != version)
                    {
                        pendingReload.TrySetResult(null);
                        return;
                    }

                    SetCachedOptions(name, candidateOptions);
                    ReloadFailures.TryRemove(name, out _);
                    RemoveCurrentPendingReload(name, version, pendingReload);
                    pendingReload.TrySetResult(null);
                    onChange = _onChange;
                }
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                throw;
            }

            onChange?.Invoke(candidateOptions, name);
        }

        /// <summary>
        /// Gets the present value of the options (equivalent to <c>Get(Options.DefaultName)</c>).
        /// </summary>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating the <typeparamref name="TOptions"/> instance created.</exception>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public TOptions CurrentValue
        {
            get => Get(Options.DefaultName);
        }

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance. If <see langword="null"/>, <see cref="Options.DefaultName"/>, which is the empty string, is used.</param>
        /// <returns>The <typeparamref name="TOptions"/> instance that matches the given <paramref name="name"/>.</returns>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating the <typeparamref name="TOptions"/> instance created.</exception>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public virtual TOptions Get(string? name)
        {
            if (_asyncValidationScopeFactory is not null)
            {
                string localName = name ?? Options.DefaultName;
                IOptionsFactory<TOptions> localFactory = _factory;

                if (_cache.GetType() == typeof(OptionsCache<TOptions>))
                {
                    OptionsCache<TOptions> asyncOptionsCache = (OptionsCache<TOptions>)_cache;
                    return GetWithAsyncValidation(localName, asyncOptionsCache, localFactory);
                }

                while (true)
                {
                    Task? pendingReload = null;

                    lock (GetCacheLock(localName))
                    {
                        if (CacheKeys.ContainsKey(localName))
                        {
                            TOptions options = _cache.GetOrAdd(localName, () => localFactory.Create(localName));
                            return options;
                        }

                        if (ReloadFailures.TryGetValue(localName, out ExceptionDispatchInfo? reloadFailure))
                        {
                            reloadFailure.Throw();
                        }

                        if (PendingReloads.TryGetValue(localName, out TaskCompletionSource<object?>? pendingReloadSource))
                        {
                            pendingReload = pendingReloadSource.Task;
                        }
                        else
                        {
                            TOptions options = _cache.GetOrAdd(localName, () => localFactory.Create(localName));
                            CacheKeys.TryAdd(localName, true);
                            return options;
                        }
                    }

                    pendingReload.GetAwaiter().GetResult();
                }
            }

            if (_cache is not OptionsCache<TOptions> optionsCache)
            {
                // copying captured variables to locals avoids allocating a closure if we don't enter the if
                string localName = name ?? Options.DefaultName;
                IOptionsFactory<TOptions> localFactory = _factory;
                return _cache.GetOrAdd(localName, () => localFactory.Create(localName));
            }

            // non-allocating fast path
            return optionsCache.GetOrAdd(name, static (name, factory) => factory.Create(name), _factory);

        }

        private TOptions GetWithAsyncValidation(string name, OptionsCache<TOptions> optionsCache, IOptionsFactory<TOptions> factory)
        {
            while (true)
            {
                if (optionsCache.TryGetValue(name, out TOptions? cachedOptions))
                {
                    return cachedOptions;
                }

                Task? pendingReload = null;

                lock (GetCacheLock(name))
                {
                    if (optionsCache.TryGetValue(name, out cachedOptions))
                    {
                        return cachedOptions;
                    }

                    if (ReloadFailures.TryGetValue(name, out ExceptionDispatchInfo? reloadFailure))
                    {
                        reloadFailure.Throw();
                    }

                    if (PendingReloads.TryGetValue(name, out TaskCompletionSource<object?>? pendingReloadSource))
                    {
                        pendingReload = pendingReloadSource.Task;
                    }
                    else
                    {
                        TOptions options = optionsCache.GetOrAdd(name, static (name, factory) => factory.Create(name), factory);
                        CacheKeys.TryAdd(name, true);
                        return options;
                    }
                }

                pendingReload.GetAwaiter().GetResult();
            }
        }

        private void InvokeChangedSynchronously(string name)
        {
            _cache.TryRemove(name);
            _asyncValidationState?.CacheKeys.TryRemove(name, out _);
            TOptions syncOptions = Get(name);
            _onChange?.Invoke(syncOptions, name);
        }

        private void InvokeChangedSynchronously(string name, int version, TaskCompletionSource<object?> pendingReload)
        {
            TOptions syncOptions;
            try
            {
                syncOptions = _factory.Create(name);
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                throw;
            }

            Action<TOptions, string>? onChange;
            try
            {
                lock (GetCacheLock(name))
                {
                    if (!ReloadVersions.TryGetValue(name, out int currentVersion) ||
                        currentVersion != version)
                    {
                        pendingReload.TrySetResult(null);
                        return;
                    }

                    SetCachedOptions(name, syncOptions);
                    ReloadFailures.TryRemove(name, out _);
                    RemoveCurrentPendingReload(name, version, pendingReload);
                    pendingReload.TrySetResult(null);
                    onChange = _onChange;
                }
            }
            catch (Exception ex)
            {
                RecordReloadFailure(name, version, ex);
                CompletePendingReload(name, version, pendingReload);
                throw;
            }

            onChange?.Invoke(syncOptions, name);
        }

        private static IAsyncValidateOptions<TOptions>[] ResolveAsyncValidations(IServiceProvider serviceProvider)
        {
            IEnumerable<IAsyncValidateOptions<TOptions>> asyncValidations = serviceProvider.GetServices<IAsyncValidateOptions<TOptions>>();
            return asyncValidations as IAsyncValidateOptions<TOptions>[] ?? new List<IAsyncValidateOptions<TOptions>>(asyncValidations).ToArray();
        }

        private static TaskCompletionSource<object?> CreatePendingReload() =>
            new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static void DisposeAsyncValidationScope(AsyncServiceScope scope)
        {
            ValueTask disposeTask = scope.DisposeAsync();
            if (disposeTask.IsCompleted)
            {
                disposeTask.GetAwaiter().GetResult();
                return;
            }

            disposeTask.AsTask().GetAwaiter().GetResult();
        }

        private static async Task ValidateAsync(string name, TOptions options, IAsyncValidateOptions<TOptions>[] asyncValidations)
        {
            List<string>? failures = null;
            foreach (IAsyncValidateOptions<TOptions> validation in asyncValidations)
            {
                ValidateOptionsResult result = await validation.ValidateAsync(name, options, CancellationToken.None).ConfigureAwait(false);
                if (result is not null && result.Failed)
                {
                    failures ??= new List<string>();
                    failures.AddRange(result.Failures);
                }
            }

            if (failures is not null && failures.Count > 0)
            {
                throw new OptionsValidationException(name, typeof(TOptions), failures);
            }
        }

        private void CompletePendingReload(string name, int version, TaskCompletionSource<object?> pendingReload)
        {
            lock (GetCacheLock(name))
            {
                RemoveCurrentPendingReload(name, version, pendingReload);
            }

            pendingReload.TrySetResult(null);
        }

        private void RemoveCurrentPendingReload(string name, int version, TaskCompletionSource<object?> pendingReload)
        {
            if (ReloadVersions.TryGetValue(name, out int currentVersion) &&
                currentVersion == version &&
                PendingReloads.TryGetValue(name, out TaskCompletionSource<object?>? currentPendingReload) &&
                ReferenceEquals(currentPendingReload, pendingReload))
            {
                PendingReloads.TryRemove(name, out _);
            }
        }

        private void RecordReloadFailure(string name, int version, Exception ex)
        {
            lock (GetCacheLock(name))
            {
                if ((!ReloadVersions.TryGetValue(name, out int currentVersion) ||
                     currentVersion == version) &&
                    !TryGetCachedOptions(name, out _))
                {
                    ReloadFailures[name] = ExceptionDispatchInfo.Capture(ex);
                }
            }
        }

        private bool TryGetCachedOptions(string name, [MaybeNullWhen(false)] out TOptions options)
        {
            if (_cache.GetType() == typeof(OptionsCache<TOptions>))
            {
                OptionsCache<TOptions> optionsCache = (OptionsCache<TOptions>)_cache;
                return optionsCache.TryGetValue(name, out options);
            }

            options = default;
            return CacheKeys.ContainsKey(name);
        }

        private void SetCachedOptions(string name, TOptions options)
        {
            if (_cache is OptionsCache<TOptions> optionsCache)
            {
                optionsCache.Set(name, options);
                CacheKeys.TryAdd(name, true);
                return;
            }

            lock (GetCacheLock(name))
            {
                _cache.TryRemove(name);
                _cache.TryAdd(name, options);
                CacheKeys.TryAdd(name, true);
            }
        }

        private object GetCacheLock(string name) =>
            CacheLocks.GetOrAdd(name, static _ => new object());

        /// <summary>
        /// Registers a listener to be called whenever <typeparamref name="TOptions"/> changes.
        /// </summary>
        /// <param name="listener">The action to be invoked when <typeparamref name="TOptions"/> has changed.</param>
        /// <returns>An <see cref="IDisposable"/> that should be disposed to stop listening for changes.</returns>
        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            var disposable = new ChangeTrackerDisposable(this, listener);
            _onChange += disposable.OnChange;
            return disposable;
        }

        /// <summary>
        /// Removes all change registration subscriptions.
        /// </summary>
        public void Dispose()
        {
            // Remove all subscriptions to the change tokens
            foreach (IDisposable registration in _registrations)
            {
                registration.Dispose();
            }

            _registrations.Clear();
        }

        private sealed class AsyncValidationState
        {
            public readonly ConcurrentDictionary<string, bool> CacheKeys = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
            public readonly ConcurrentDictionary<string, int> ReloadVersions = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            public readonly ConcurrentDictionary<string, ExceptionDispatchInfo> ReloadFailures = new ConcurrentDictionary<string, ExceptionDispatchInfo>(StringComparer.Ordinal);
            public readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> PendingReloads = new ConcurrentDictionary<string, TaskCompletionSource<object?>>(StringComparer.Ordinal);
            public readonly ConcurrentDictionary<string, object> CacheLocks = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
        }

        internal sealed class ChangeTrackerDisposable : IDisposable
        {
            private readonly Action<TOptions, string> _listener;
            private readonly OptionsMonitor<TOptions> _monitor;

            public ChangeTrackerDisposable(OptionsMonitor<TOptions> monitor, Action<TOptions, string> listener)
            {
                _listener = listener;
                _monitor = monitor;
            }

            public void OnChange(TOptions options, string name) => _listener.Invoke(options, name);

            public void Dispose() => _monitor._onChange -= OnChange;
        }
    }

    internal sealed class OptionsMonitorWithAsyncValidation<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        OptionsMonitor<TOptions>
        where TOptions : class
    {
        public OptionsMonitorWithAsyncValidation(
            IOptionsFactory<TOptions> factory,
            IEnumerable<IOptionsChangeTokenSource<TOptions>> sources,
            IOptionsMonitorCache<TOptions> cache,
            IServiceProvider serviceProvider)
            : base(factory, sources, cache, GetAsyncValidationScopeFactory(serviceProvider))
        {
        }

        private static IServiceScopeFactory? GetAsyncValidationScopeFactory(IServiceProvider serviceProvider)
        {
            IServiceProviderIsService? serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
            if (serviceProviderIsService is not null &&
                !serviceProviderIsService.IsService(typeof(IAsyncValidateOptions<TOptions>)))
            {
                return null;
            }

            return serviceProvider.GetService<IServiceScopeFactory>();
        }
    }
}
