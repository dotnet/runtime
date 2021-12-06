// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Configuration
{
    // ReferenceCountedProviders is used by ConfigurationManager to wait until all readers unreference it before disposing any providers.
    internal sealed class ReferenceCountedProviders : IDisposable
    {
        private long _refCount = 1;
        // volatile is not strictly necessary because the runtime adds a barrier either way, but volatile indicates that this field has
        // unsynchronized readers meaning the all writes initializing the list must be published before updating the _providers reference.
        private volatile List<IConfigurationProvider> _providers;

        public ReferenceCountedProviders(List<IConfigurationProvider> providers)
        {
            _providers = providers;
        }

        public List<IConfigurationProvider> Providers
        {
            get
            {
                Debug.Assert(_refCount > 0);
                return _providers;
            }
            set
            {
                Debug.Assert(_refCount > 0);
                _providers = value;
            }
        }

        // This is only used to support IConfigurationRoot.Providers because we cannot track the lifetime of that reference.
        public List<IConfigurationProvider> NonReferenceCountedProviders => _providers;

        public void AddReference()
        {
            // AddReference() is always called with a lock to ensure _refCount hasn't already decremented to zero.
            Debug.Assert(_refCount > 0);
            Interlocked.Increment(ref _refCount);
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                foreach (var provider in _providers)
                {
                    (provider as IDisposable)?.Dispose();
                }
            }
        }
    }
}
