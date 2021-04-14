// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ServiceProviderEngineScope : IServiceScope, IServiceProvider, IAsyncDisposable
    {
        // For testing only
        internal Action<object> _captureDisposableCallback;

        private bool _disposed;
        private ScopeTracker.State _state;

        public ServiceProviderEngineScope(ServiceProviderEngine engine, bool isRoot = false)
        {
            Engine = engine;
            _state = isRoot ? new ScopeTracker.State() : engine.ScopeTracker.Allocate();
        }

        internal IDictionary<ServiceCacheKey, object> ResolvedServices => _state.ResolvedServices;

        // This lock protects state on the scope, in particular, for the root scope, it protects
        // the list of disposable entries only, since ResolvedServices is a concurrent dictionary.
        // For other scopes, it protects ResolvedServices and the list of disposables
        internal object Sync => _state;

        public ServiceProviderEngine Engine { get; }

        public object GetService(Type serviceType)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return Engine.GetService(serviceType, this);
        }

        public IServiceProvider ServiceProvider => this;

        internal object CaptureDisposable(object service)
        {
            _captureDisposableCallback?.Invoke(service);

            if (ReferenceEquals(this, service) || !(service is IDisposable || service is IAsyncDisposable))
            {
                return service;
            }

            lock (Sync)
            {
                if (_disposed)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else
                    {
                        // sync over async, for the rare case that an object only implements IAsyncDisposable and may end up starving the thread pool.
                        Task.Run(() => ((IAsyncDisposable)service).DisposeAsync().AsTask()).GetAwaiter().GetResult();
                    }

                    ThrowHelper.ThrowObjectDisposedException();
                }

                _state.Disposables ??= new List<object>();

                _state.Disposables.Add(service);
            }

            return service;
        }

        public void Dispose()
        {
            List<object> toDispose = BeginDispose();

            if (toDispose != null)
            {
                for (int i = toDispose.Count - 1; i >= 0; i--)
                {
                    if (toDispose[i] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.AsyncDisposableServiceDispose, TypeNameHelper.GetTypeDisplayName(toDispose[i])));
                    }
                }
            }

            ClearState();
        }

        public ValueTask DisposeAsync()
        {
            List<object> toDispose = BeginDispose();

            if (toDispose != null)
            {
                try
                {
                    for (int i = toDispose.Count - 1; i >= 0; i--)
                    {
                        object disposable = toDispose[i];
                        if (disposable is IAsyncDisposable asyncDisposable)
                        {
                            ValueTask vt = asyncDisposable.DisposeAsync();
                            if (!vt.IsCompletedSuccessfully)
                            {
                                return Await(this, i, vt, toDispose);
                            }

                            // If its a IValueTaskSource backed ValueTask,
                            // inform it its result has been read so it can reset
                            vt.GetAwaiter().GetResult();
                        }
                        else
                        {
                            ((IDisposable)disposable).Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new ValueTask(Task.FromException(ex));
                }
            }

            ClearState();

            return default;

            static async ValueTask Await(ServiceProviderEngineScope scope, int i, ValueTask vt, List<object> toDispose)
            {
                await vt.ConfigureAwait(false);
                // vt is acting on the disposable at index i,
                // decrement it and move to the next iteration
                i--;

                for (; i >= 0; i--)
                {
                    object disposable = toDispose[i];
                    if (disposable is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        ((IDisposable)disposable).Dispose();
                    }
                }

                scope.ClearState();
            }
        }

        private void ClearState()
        {
            // Don't attempt to dispose if we're already disposed
            if (_disposed)
            {
                return;
            }

            // ResolvedServices is never cleared for singletons because there might be a compilation running in background
            // trying to get a cached singleton service. If it doesn't find it
            // it will try to create a new one which will result in an ObjectDisposedException.

            // Track statistics about the scope (number of disposable objects and number of disposed services)
            _state.Track();
        }

        private List<object> BeginDispose()
        {
            lock (Sync)
            {
                if (_disposed)
                {
                    return null;
                }

                // We've transitioned to the disposed state, so future calls to
                // CaptureDisposable will immediately dispose the object.
                // No further changes to _state.Disposables, are allowed.
                _disposed = true;

                return _state.Disposables;
            }
        }
    }
}
