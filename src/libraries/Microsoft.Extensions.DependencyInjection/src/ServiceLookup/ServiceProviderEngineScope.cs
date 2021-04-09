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
        private ScopePool.State _state;

        // This lock protects state on the scope, in particular, for the root scope, it protects
        // the list of disposable entries only, since ResolvedServices is a concurrent dictionary.
        // For other scopes, it protects ResolvedServices and the list of disposables
        private readonly object _scopeLock = new object();

        public ServiceProviderEngineScope(ServiceProviderEngine engine, bool isRoot = false)
        {
            Engine = engine;
            _state = isRoot ? new ScopePool.State() : engine.ScopePool.Rent();
        }

        internal IDictionary<ServiceCacheKey, object> ResolvedServices => _state?.ResolvedServices ?? ScopeDisposed();

        internal object Sync => _scopeLock;

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

            lock (_scopeLock)
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

        private IDictionary<ServiceCacheKey, object> ScopeDisposed()
        {
            ThrowHelper.ThrowObjectDisposedException();
            return null;
        }

        private void ClearState()
        {
            // We lock here since ResolvedServices is always accessed in the scope lock, this means we'll never
            // try to return to the pool while somebody is trying to access ResolvedServices.
            lock (_scopeLock)
            {
                // Don't attempt to dispose if we're already disposed
                if (_state == null)
                {
                    return;
                }

                // ResolvedServices is never cleared for singletons because there might be a compilation running in background
                // trying to get a cached singleton service. If it doesn't find it
                // it will try to create a new one which will result in an ObjectDisposedException.

                // Dispose the state, which will end up attempting to return the state pool.
                // This will return false if the pool is full or if this state object is the root scope
                if (_state.Return())
                {
                    _state = null;
                }
            }
        }

        private List<object> BeginDispose()
        {
            lock (_scopeLock)
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
