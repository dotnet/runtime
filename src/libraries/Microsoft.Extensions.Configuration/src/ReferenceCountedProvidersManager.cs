// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    // ReferenceCountedProviderManager is used by ConfigurationManager to provide copy-on-write references that support concurrently
    // reading config while modifying sources. It waits for readers to unreference the providers before disposing them
    // without blocking on any concurrent operations.
    internal sealed class ReferenceCountedProviderManager : IDisposable
    {
        private readonly object _replaceProvidersLock = new object();
        private ReferenceCountedProviders _refCountedProviders = ReferenceCountedProviders.Create(new List<IConfigurationProvider>());
        private bool _disposed;

        // This is only used to support IConfigurationRoot.Providers because we cannot track the lifetime of that reference.
        public IEnumerable<IConfigurationProvider> NonReferenceCountedProviders => _refCountedProviders.NonReferenceCountedProviders;

        public ReferenceCountedProviders GetReference()
        {
            // Lock to ensure oldRefCountedProviders.Dispose() in ReplaceProviders() or Dispose() doesn't decrement ref count to zero
            // before calling _refCountedProviders.AddReference().
            lock (_replaceProvidersLock)
            {
                if (_disposed)
                {
                    // Return a non-reference-counting ReferenceCountedProviders instance now that the ConfigurationManager is disposed.
                    // We could preemptively throw an ODE instead, but this might break existing apps that were previously able to
                    // continue to read configuration after disposing an ConfigurationManager.
                    return ReferenceCountedProviders.CreateDisposed(_refCountedProviders.NonReferenceCountedProviders);
                }

                _refCountedProviders.AddReference();
                return _refCountedProviders;
            }
        }

        // Providers should never be concurrently modified. Reading during modification is allowed.
        public void ReplaceProviders(List<IConfigurationProvider> providers)
        {
            ReferenceCountedProviders oldRefCountedProviders = _refCountedProviders;

            lock (_replaceProvidersLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ConfigurationManager));
                }

                _refCountedProviders = ReferenceCountedProviders.Create(providers);
            }

            // Decrement the reference count to the old providers. If they are being concurrently read from
            // the actual disposal of the old providers will be delayed until the final reference is released.
            // Never dispose ReferenceCountedProviders with a lock because this may call into user code.
            oldRefCountedProviders.Dispose();
        }

        public void AddProvider(IConfigurationProvider provider)
        {
            lock (_replaceProvidersLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ConfigurationManager));
                }

                // Maintain existing references, but replace list with copy containing new item.
                _refCountedProviders.Providers = new List<IConfigurationProvider>(_refCountedProviders.Providers)
                {
                    provider
                };
            }
        }

        public void Dispose()
        {
            ReferenceCountedProviders oldRefCountedProviders = _refCountedProviders;

            // This lock ensures that we cannot reduce the ref count to zero before GetReference() calls AddReference().
            // Once _disposed is set, GetReference() stops reference counting.
            lock (_replaceProvidersLock)
            {
                _disposed = true;
            }

            // Never dispose ReferenceCountedProviders with a lock because this may call into user code.
            oldRefCountedProviders.Dispose();
        }
    }
}
