// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        private readonly Dictionary<string, ReloadValidationConfiguration<TOptions>>? _reloadConfigs;
        private readonly ConcurrentDictionary<string, EagerReloadState>? _eagerStates;
        internal event Action<TOptions, string>? _onChange;

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsMonitor{TOptions}"/> with the specified factory, sources, and cache.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        /// <param name="cache">The cache used to store options.</param>
        public OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache)
            : this(factory, sources, cache, reloadValidationConfigs: Array.Empty<ReloadValidationConfiguration<TOptions>>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsMonitor{TOptions}"/> with the specified factory, sources, cache, and reload-validation opt-ins.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        /// <param name="cache">The cache used to store options.</param>
        /// <param name="reloadValidationConfigs">The reload-validation opt-ins registered through the <c>ValidateOnChange</c> options-builder extension.</param>
        public OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache, IEnumerable<ReloadValidationConfiguration<TOptions>> reloadValidationConfigs)
        {
            _factory = factory;
            _cache = cache;

            foreach (ReloadValidationConfiguration<TOptions> config in reloadValidationConfigs)
            {
                // The last opt-in for a given name wins, mirroring the "greediest configuration wins" behavior elsewhere.
                (_reloadConfigs ??= new Dictionary<string, ReloadValidationConfiguration<TOptions>>(StringComparer.Ordinal))[config.Name] = config;
            }

            if (_reloadConfigs is not null)
            {
                _eagerStates = new ConcurrentDictionary<string, EagerReloadState>(StringComparer.Ordinal);
            }

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

        private void InvokeChanged(string? name)
        {
            name ??= Options.DefaultName;

            if (_reloadConfigs is not null && _reloadConfigs.TryGetValue(name, out ReloadValidationConfiguration<TOptions>? config))
            {
                // Eager revalidation: keep serving the last validated value while the reloaded configuration is
                // re-created and validated in the background, then swap it in only if validation succeeds.
                InvokeEagerReload(name, config);
                return;
            }

            _cache.TryRemove(name);
            TOptions options = Get(name);
            _onChange?.Invoke(options, name);
        }

        private void InvokeEagerReload(string name, ReloadValidationConfiguration<TOptions> config)
        {
            EagerReloadState state = _eagerStates!.GetOrAdd(name, static _ => new EagerReloadState());

            int generation;
            CancellationToken cancellationToken;
            lock (state)
            {
                // Latest-wins: supersede any in-flight refresh for this name so only the newest reload publishes.
                state.Cancel();
                state.Cts = new CancellationTokenSource();
                cancellationToken = state.Cts.Token;
                generation = ++state.Generation;
            }

            _ = RefreshAsync(name, config, state, generation, cancellationToken);
        }

        private async Task RefreshAsync(string name, ReloadValidationConfiguration<TOptions> config, EagerReloadState state, int generation, CancellationToken cancellationToken)
        {
            TOptions? published = null;
            try
            {
                TOptions validated = _factory is OptionsFactory<TOptions> asyncFactory
                    ? await asyncFactory.CreateAsync(name, cancellationToken).ConfigureAwait(false)
                    : _factory.Create(name);

                lock (state)
                {
                    // Re-check under the lock and publish atomically so a superseded reload can never overwrite a newer
                    // published value (the generation check and the cache write must not be interleaved with another reload).
                    if (state.Generation != generation)
                    {
                        return;
                    }

                    if (_cache is OptionsCache<TOptions> optionsCache)
                    {
                        optionsCache.AddOrReplace(name, validated);
                    }
                    else
                    {
                        _cache.TryRemove(name);
                        _cache.TryAdd(name, validated);
                    }

                    published = validated;
                }
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer reload (or disposal); nothing to publish.
                return;
            }
            catch (Exception ex)
            {
                lock (state)
                {
                    if (state.Generation != generation)
                    {
                        return;
                    }

                    if (config.Behavior == OptionsReloadValidationBehavior.FailReads)
                    {
                        // Drop the cached value so the next read re-creates and surfaces the failure.
                        _cache.TryRemove(name);
                    }
                }

                // Always surface the failure through the event source so a reload failure is observable even when no
                // error callback was supplied. This is independent of the user callback below.
                OptionsEventSource.Log.ReloadValidationFailed(name, typeof(TOptions), ex);

                try
                {
                    config.OnError?.Invoke(name, ex);
                }
                catch
                {
                    // The error callback is user code. We catch here to prevent an unobserved task exception.
                }

                return;
            }

            if (published is not null)
            {
                _onChange?.Invoke(published, name);
            }
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

            if (_eagerStates is not null)
            {
                // Cancel any in-flight eager revalidation so a superseded refresh does not publish after disposal.
                foreach (EagerReloadState state in _eagerStates.Values)
                {
                    lock (state)
                    {
                        state.Cancel();
                    }
                }
            }
        }

        private sealed class EagerReloadState
        {
            public int Generation;
            public CancellationTokenSource? Cts;

            public void Cancel()
            {
                CancellationTokenSource? cts = Cts;
                if (cts is not null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    Cts = null;
                }
            }
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
}
