// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace System.Reflection
{
    public sealed partial class MetadataLoadContext
    {
        // Objects (e.g. PEReaders) to dispose when this MetadataLoadContext is disposed.
        private ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        internal bool IsDisposed { get; private set; }

        internal void DisposeCheck()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(message: SR.MetadataLoadContextDisposed, innerException: null);
        }

        /// <summary>
        /// Adds an object to an internal list of objects to be disposed when the MetadataLoadContext is disposed.
        /// </summary>
        internal void RegisterForDisposal(IDisposable disposable) => _disposables.Add(disposable);

        private void Dispose(bool disposing)
        {
            IsDisposed = true;

            if (disposing)
            {
                // Dispose all IDisposables given to this MetadataLoadContext. This releases any file locks on the underlying 
                // assembly files.
                ConcurrentBag<IDisposable> disposables = _disposables;
                if (disposables != null)
                {
                    _disposables = null;

                    foreach (IDisposable disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
