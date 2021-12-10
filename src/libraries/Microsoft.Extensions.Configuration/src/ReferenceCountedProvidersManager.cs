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
        private ReferenceCountedProviders _refCountedProviders = new(new List<IConfigurationProvider>());

        // This is only used to support IConfigurationRoot.Providers because we cannot track the lifetime of that reference.
        public IEnumerable<IConfigurationProvider> NonReferenceCountedProviders => _refCountedProviders.NonReferenceCountedProviders;

        public ReferenceCountedProviders GetReference()
        {
            // Lock to ensure oldRefCountedProviders.Dispose() in ReplaceProviders() doesn't decrement ref count to zero
            // before calling _refCountedProviders.AddReference().
            lock (_replaceProvidersLock)
            {
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
                _refCountedProviders = new ReferenceCountedProviders(providers);
            }

            oldRefCountedProviders.Dispose();
        }

        public void AddProvider(IConfigurationProvider provider)
        {
            // Maintain existing references, but replace list with copy containing new item.
            _refCountedProviders.Providers = new List<IConfigurationProvider>(_refCountedProviders.Providers)
            {
                provider
            };
        }

        public void Dispose() => _refCountedProviders.Dispose();
    }
}
