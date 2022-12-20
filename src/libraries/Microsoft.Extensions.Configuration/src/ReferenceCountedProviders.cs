// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Configuration
{
    // ReferenceCountedProviders is used by ConfigurationManager to wait until all readers unreference it before disposing any providers.
    internal abstract class ReferenceCountedProviders : IDisposable
    {
        public static ReferenceCountedProviders Create(List<IConfigurationProvider> providers) => new ActiveReferenceCountedProviders(providers);

        // If anything references DisposedReferenceCountedProviders, it indicates something is using the ConfigurationManager after it's been disposed.
        // We could preemptively throw an ODE from ReferenceCountedProviderManager.GetReference() instead of returning this type, but this might
        // break existing apps that are previously able to continue to read configuration after disposing an ConfigurationManager.
        public static ReferenceCountedProviders CreateDisposed(List<IConfigurationProvider> providers) => new DisposedReferenceCountedProviders(providers);

        public abstract List<IConfigurationProvider> Providers { get; set; }

        // NonReferenceCountedProviders is only used to:
        // 1. Support IConfigurationRoot.Providers because we cannot track the lifetime of that reference.
        // 2. Construct DisposedReferenceCountedProviders because the providers are disposed anyway and no longer reference counted.
        public abstract List<IConfigurationProvider> NonReferenceCountedProviders { get; }

        public abstract void AddReference();
        // This is Dispose() rather than RemoveReference() so we can conveniently release a reference at the end of a using block.
        public abstract void Dispose();

        private sealed class ActiveReferenceCountedProviders : ReferenceCountedProviders
        {
            private long _refCount = 1;
            // volatile is not strictly necessary because the runtime adds a barrier either way, but volatile indicates that this field has
            // unsynchronized readers meaning the all writes initializing the list must be published before updating the _providers reference.
            private volatile List<IConfigurationProvider> _providers;

            public ActiveReferenceCountedProviders(List<IConfigurationProvider> providers)
            {
                _providers = providers;
            }

            public override List<IConfigurationProvider> Providers
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

            public override List<IConfigurationProvider> NonReferenceCountedProviders => _providers;

            public override void AddReference()
            {
                // AddReference() is always called with a lock to ensure _refCount hasn't already decremented to zero.
                Debug.Assert(_refCount > 0);
                Interlocked.Increment(ref _refCount);
            }

            public override void Dispose()
            {
                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    foreach (IConfigurationProvider provider in _providers)
                    {
                        (provider as IDisposable)?.Dispose();
                    }
                }
            }
        }

        private sealed class DisposedReferenceCountedProviders : ReferenceCountedProviders
        {
            public DisposedReferenceCountedProviders(List<IConfigurationProvider> providers)
            {
                Providers = providers;
            }

            public override List<IConfigurationProvider> Providers { get; set; }
            public override List<IConfigurationProvider> NonReferenceCountedProviders => Providers;

            public override void AddReference() { }
            public override void Dispose() { }
        }
    }
}
