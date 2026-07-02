// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    internal interface IAsyncValidateOptionsName
    {
        string? Name { get; }
    }

    internal static class OptionsAsyncValidation
    {
        public static bool IsSyncGuardSuppressed<TOptions>(string name)
            where TOptions : class
        {
            for (SyncGuardSuppression<TOptions>? suppression = SyncGuardSuppressionState<TOptions>.Current.Value;
                 suppression is not null;
                 suppression = suppression.Parent)
            {
                if (suppression.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static IDisposable SuppressSyncGuard<TOptions>(string name)
            where TOptions : class
        {
            var suppression = new SyncGuardSuppression<TOptions>(
                name,
                SyncGuardSuppressionState<TOptions>.Current.Value);
            SyncGuardSuppressionState<TOptions>.Current.Value = suppression;
            return suppression;
        }

        public static bool MayHaveAsyncValidators<TOptions>(IServiceProvider serviceProvider)
            where TOptions : class
        {
            IServiceProviderIsService? serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
            if (serviceProviderIsService is not null &&
                !serviceProviderIsService.IsService(typeof(IAsyncValidateOptions<TOptions>)))
            {
                return false;
            }

            return serviceProvider.GetService<IServiceScopeFactory>() is not null;
        }

        public static OptionsAsyncValidationCoordinator<TOptions>? GetCoordinator<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions>(IServiceProvider serviceProvider)
            where TOptions : class
        {
            IServiceProviderIsService? serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
            if (serviceProviderIsService is not null &&
                !serviceProviderIsService.IsService(typeof(IAsyncValidateOptions<TOptions>)))
            {
                return null;
            }

            return serviceProvider.GetService<OptionsAsyncValidationCoordinator<TOptions>>();
        }

        public static bool HasApplicableAsyncValidators<TOptions>(IServiceProvider serviceProvider, string name, bool asyncOnly)
            where TOptions : class
        {
            if (!MayHaveAsyncValidators<TOptions>(serviceProvider))
            {
                return false;
            }

            IServiceScopeFactory? scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            if (scopeFactory is null)
            {
                return false;
            }

            using IServiceScope scope = scopeFactory.CreateScope();
            IEnumerable<IAsyncValidateOptions<TOptions>> validators = scope.ServiceProvider.GetServices<IAsyncValidateOptions<TOptions>>();
            foreach (IAsyncValidateOptions<TOptions> validator in validators)
            {
                if (asyncOnly && validator is IValidateOptions<TOptions>)
                {
                    continue;
                }

                if (AppliesToName(validator, name))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AppliesToName<TOptions>(IAsyncValidateOptions<TOptions> validator, string name)
            where TOptions : class
        {
            return validator is not IAsyncValidateOptionsName namedValidator ||
                namedValidator.Name is null ||
                namedValidator.Name == name;
        }

        public static string GetSyncValidationFailureMessage<TOptions>(string name)
            where TOptions : class
        {
            return name.Length == 0
                ? $"Asynchronous validation is registered for options type '{typeof(TOptions)}' and must complete before the value can be accessed synchronously. Call ValidateOnStart() or use IOptionsMonitor<TOptions> to establish an asynchronously validated value."
                : $"Asynchronous validation is registered for options type '{typeof(TOptions)}' and name '{name}' and must complete before the value can be accessed synchronously. Call ValidateOnStart() or use IOptionsMonitor<TOptions> to establish an asynchronously validated value.";
        }

        private static class SyncGuardSuppressionState<TOptions>
            where TOptions : class
        {
            public static readonly AsyncLocal<SyncGuardSuppression<TOptions>?> Current = new AsyncLocal<SyncGuardSuppression<TOptions>?>();
        }

        private sealed class SyncGuardSuppression<TOptions> : IDisposable
            where TOptions : class
        {
            public SyncGuardSuppression(string name, SyncGuardSuppression<TOptions>? parent)
            {
                Name = name;
                Parent = parent;
            }

            public string Name { get; }

            public SyncGuardSuppression<TOptions>? Parent { get; }

            public void Dispose() => SyncGuardSuppressionState<TOptions>.Current.Value = Parent;
        }
    }

    internal sealed class OptionsAsyncValidationApplicability<TOptions>
        where TOptions : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, bool> _hasApplicableAsyncValidators = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, bool> _hasApplicableAsyncOnlyValidators = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        public OptionsAsyncValidationApplicability(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool HasApplicableAsyncValidators(string name) =>
            _hasApplicableAsyncValidators.GetOrAdd(name, name =>
                OptionsAsyncValidation.HasApplicableAsyncValidators<TOptions>(_serviceProvider, name, asyncOnly: false));

        public bool HasApplicableAsyncOnlyValidators(string name) =>
            _hasApplicableAsyncOnlyValidators.GetOrAdd(name, name =>
                OptionsAsyncValidation.HasApplicableAsyncValidators<TOptions>(_serviceProvider, name, asyncOnly: true));
    }

    internal sealed class OptionsAsyncValidationCoordinator<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly IOptionsMonitorCache<TOptions> _cache;
        private readonly OptionsAsyncValidationApplicability<TOptions> _asyncValidationApplicability;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly ConcurrentDictionary<string, ValidationEntry> _entries = new ConcurrentDictionary<string, ValidationEntry>(StringComparer.Ordinal);

        public OptionsAsyncValidationCoordinator(
            IOptionsFactory<TOptions> factory,
            IOptionsMonitorCache<TOptions> cache,
            IServiceProvider serviceProvider,
            OptionsAsyncValidationApplicability<TOptions> asyncValidationApplicability)
        {
            _factory = factory;
            _cache = cache;
            _asyncValidationApplicability = asyncValidationApplicability;
            _scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
        }

        public bool HasApplicableAsyncValidators(string name) => _asyncValidationApplicability.HasApplicableAsyncValidators(name);

        public bool HasApplicableAsyncOnlyValidators(string name) => _asyncValidationApplicability.HasApplicableAsyncOnlyValidators(name);

        public TOptions GetValidatedValueOrThrow(string name)
        {
            ValidationEntry entry = GetEntry(name);
            lock (entry.StateLock)
            {
                entry.Failure?.Throw();
                if (entry.HasValidatedValue)
                {
                    return entry.ValidatedValue!;
                }
            }

            throw new OptionsValidationException(
                name,
                typeof(TOptions),
                new[] { OptionsAsyncValidation.GetSyncValidationFailureMessage<TOptions>(name) });
        }

        public TOptions GetOrValidate(string name)
        {
            ValidationEntry entry = GetEntry(name);
            while (true)
            {
                Task? pendingValidation;
                lock (entry.StateLock)
                {
                    entry.Failure?.Throw();
                    if (entry.HasValidatedValue)
                    {
                        return entry.ValidatedValue!;
                    }

                    pendingValidation = entry.PendingValidation;
                }

                if (pendingValidation is null)
                {
                    return ValidateAndPublish(name, forceValidation: false);
                }

                pendingValidation.GetAwaiter().GetResult();
            }
        }

        public TOptions ValidateSync(string name)
        {
            using (OptionsAsyncValidation.SuppressSyncGuard<TOptions>(name))
            {
                return _factory.Create(name);
            }
        }

        public TOptions ValidateAndPublish(string name) =>
            ValidateAndPublish(name, forceValidation: true);

        private TOptions ValidateAndPublish(string name, bool forceValidation) =>
            ValidateAndPublishAsync(name, options: null, forceValidation, CancellationToken.None).GetAwaiter().GetResult();

        public async Task<TOptions> ValidateAndPublishAsync(string name, CancellationToken cancellationToken = default)
            => await ValidateAndPublishAsync(name, options: null, forceValidation: true, cancellationToken).ConfigureAwait(false);

        public async Task<TOptions> ValidateAndPublishAsync(string name, TOptions options, CancellationToken cancellationToken = default)
            => await ValidateAndPublishAsync(name, options, forceValidation: true, cancellationToken).ConfigureAwait(false);

        private async Task<TOptions> ValidateAndPublishAsync(string name, TOptions? options, bool forceValidation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidationEntry entry = GetEntry(name);
            await entry.ValidationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            TaskCompletionSource<object?>? pendingValidation = null;
            try
            {
                if (!forceValidation)
                {
                    lock (entry.StateLock)
                    {
                        entry.Failure?.Throw();
                        if (entry.HasValidatedValue)
                        {
                            return entry.ValidatedValue!;
                        }
                    }
                }

                pendingValidation = CreatePendingValidation();
                lock (entry.StateLock)
                {
                    entry.PendingValidation = pendingValidation.Task;
                }

                TOptions optionsToValidate = options!;
                if (optionsToValidate is null)
                {
                    using (OptionsAsyncValidation.SuppressSyncGuard<TOptions>(name))
                    {
                        optionsToValidate = _factory.Create(name);
                    }
                }

                await ValidateAsync(name, optionsToValidate, cancellationToken).ConfigureAwait(false);
                PublishValidatedOptions(name, optionsToValidate, entry);

                return optionsToValidate;
            }
            catch (Exception ex) when (pendingValidation is not null)
            {
                if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                RecordFailure(entry, ex);
                throw;
            }
            finally
            {
                if (pendingValidation is not null)
                {
                    CompletePendingValidation(entry, pendingValidation);
                }

                entry.ValidationLock.Release();
            }
        }

        private ValidationEntry GetEntry(string name) =>
            _entries.GetOrAdd(name, static _ => new ValidationEntry());

        private async Task ValidateAsync(string name, TOptions options, CancellationToken cancellationToken)
        {
            IServiceScopeFactory? scopeFactory = _scopeFactory;
            if (scopeFactory is null)
            {
                return;
            }

            AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            try
            {
                IEnumerable<IAsyncValidateOptions<TOptions>> asyncValidators = scope.ServiceProvider.GetServices<IAsyncValidateOptions<TOptions>>();
                IAsyncValidateOptions<TOptions>[] validators = asyncValidators as IAsyncValidateOptions<TOptions>[] ?? new List<IAsyncValidateOptions<TOptions>>(asyncValidators).ToArray();
                await ValidateAsync(name, options, validators, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await DisposeAsyncValidationScopeAsync(scope).ConfigureAwait(false);
            }
        }

        private static async Task ValidateAsync(string name, TOptions options, IAsyncValidateOptions<TOptions>[] asyncValidators, CancellationToken cancellationToken)
        {
            List<string>? failures = null;
            foreach (IAsyncValidateOptions<TOptions> validation in asyncValidators)
            {
                ValidateOptionsResult result = await validation.ValidateAsync(name, options, cancellationToken).ConfigureAwait(false);
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

        private void PublishValidatedOptions(string name, TOptions options, ValidationEntry entry)
        {
            SetCachedOptions(name, options);
            lock (entry.StateLock)
            {
                entry.ValidatedValue = options;
                entry.HasValidatedValue = true;
                entry.Failure = null;
            }
        }

        private static void RecordFailure(ValidationEntry entry, Exception ex)
        {
            lock (entry.StateLock)
            {
                entry.Failure = ExceptionDispatchInfo.Capture(ex);
            }
        }

        private void SetCachedOptions(string name, TOptions options)
        {
            if (_cache is OptionsCache<TOptions> optionsCache)
            {
                optionsCache.Set(name, options);
                return;
            }

            _cache.TryRemove(name);
            _cache.TryAdd(name, options);
        }

        private static TaskCompletionSource<object?> CreatePendingValidation() =>
            new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static void CompletePendingValidation(ValidationEntry entry, TaskCompletionSource<object?> pendingValidation)
        {
            lock (entry.StateLock)
            {
                if (ReferenceEquals(entry.PendingValidation, pendingValidation.Task))
                {
                    entry.PendingValidation = null;
                }
            }

            pendingValidation.TrySetResult(null);
        }

        private static async ValueTask DisposeAsyncValidationScopeAsync(AsyncServiceScope scope)
        {
            ValueTask disposeTask = scope.DisposeAsync();
            if (disposeTask.IsCompleted)
            {
                disposeTask.GetAwaiter().GetResult();
                return;
            }

            await disposeTask.ConfigureAwait(false);
        }

        private sealed class ValidationEntry
        {
            public readonly SemaphoreSlim ValidationLock = new SemaphoreSlim(1, 1);
            public readonly object StateLock = new object();
            public TOptions? ValidatedValue;
            public bool HasValidatedValue;
            public ExceptionDispatchInfo? Failure;
            public Task? PendingValidation;
        }
    }
}
