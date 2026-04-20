// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private readonly OptionsCache<TOptions>? _fastCache;
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        internal event Action<TOptions, string>? _onChange;
        private TOptions? _currentValue;
        private int _currentValueGeneration;

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsMonitor{TOptions}"/> with the specified factory, sources, and cache.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        /// <param name="cache">The cache used to store options.</param>
        public OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache)
        {
            _factory = factory;
            _cache = cache;
            _fastCache = cache as OptionsCache<TOptions>;

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
            _cache.TryRemove(name);
            TOptions options = Get(name);
            _onChange?.Invoke(options, name);
        }

        /// <summary>
        /// Gets the present value of the options (equivalent to <c>Get(Options.DefaultName)</c>).
        /// </summary>
        /// <exception cref="OptionsValidationException">One or more <see cref="IValidateOptions{TOptions}"/> return failed <see cref="ValidateOptionsResult"/> when validating the <typeparamref name="TOptions"/> instance created.</exception>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public TOptions CurrentValue
        {
            get
            {
                OptionsCache<TOptions>? fastCache = _fastCache;
                if (fastCache is null)
                {
                    // User-supplied IOptionsMonitorCache: no generation tracking, always go through Get.
                    return Get(Options.DefaultName);
                }

                // Read generation before value. If a mutation bumps the generation after we read it
                // but before we read the cached value, we'll see a stale cached value at most once —
                // the very next access will detect the generation mismatch and refresh.
                int gen = fastCache.Generation;
                TOptions? value = Volatile.Read(ref _currentValue);
                if (value is not null && _currentValueGeneration == gen)
                {
                    return value;
                }

                return RefreshCurrentValue(gen);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TOptions RefreshCurrentValue(int gen)
        {
            TOptions value = Get(Options.DefaultName);
            // Write generation before value so that a reader whose Volatile.Read of _currentValue
            // sees the new value is guaranteed (via the acquire fence) to also see the updated generation.
            Volatile.Write(ref _currentValueGeneration, gen);
            Volatile.Write(ref _currentValue, value);
            return value;
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
