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

        // This protects the disposed state
        private readonly object _disposeLock = new object();

        // This protects resolved services, this is only used if isRoot is false
        private readonly object _scopeLock;

        public ServiceProviderEngineScope(ServiceProviderEngine engine, bool isRoot = false)
        {
            Engine = engine;
            _state = isRoot ? new ScopePool.State() : engine.ScopePool.Rent();
            _scopeLock = isRoot ? null : new object();
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

            lock (_disposeLock)
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

                    return service;
                }

                _state.Disposables ??= new();

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
                                return Await(i, vt, toDispose);
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

            async ValueTask Await(int i, ValueTask vt, List<object> toDispose)
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

                ClearState();
            }
        }

        private IDictionary<ServiceCacheKey, object> ScopeDisposed()
        {
            ThrowHelper.ThrowObjectDisposedException();
            return null;
        }

        private void ClearState()
        {
            // Root scope doesn't need to lock anything
            if (_scopeLock == null)
            {
                return;
            }

            // We lock here since ResolvedServices is always accessed in the scope lock, this means we'll never
            // try to return to the pool while somebody is trying to access ResolvedServices.
            lock (_scopeLock)
            {
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
            lock (_disposeLock)
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

                // Not clearing ResolvedServices here because there might be a compilation running in background
                // trying to get a cached singleton service instance and if it won't find
                // it it will try to create a new one tripping the Debug.Assert in CaptureDisposable
                // and leaking a Disposable object in Release mode
            }
        }
    }
}
