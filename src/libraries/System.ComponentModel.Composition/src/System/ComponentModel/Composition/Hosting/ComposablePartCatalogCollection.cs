// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     This class implements a threadsafe ICollection{T} of ComposablePartCatalog.
    ///     It is exposed as an ICollection(ComposablePartCatalog)
    ///     It is threadsafe, notifications are not marshalled using a SynchronizationContext.
    ///     It is Disposable.
    /// </summary>
    internal sealed class ComposablePartCatalogCollection : ICollection<ComposablePartCatalog>, INotifyComposablePartCatalogChanged, IDisposable
    {
        private readonly ReadWriteLock _lock = new ReadWriteLock();
        private readonly Action<ComposablePartCatalogChangeEventArgs>? _onChanged;
        private readonly Action<ComposablePartCatalogChangeEventArgs>? _onChanging;
        private List<ComposablePartCatalog> _catalogs = new List<ComposablePartCatalog>();
        private volatile bool _isCopyNeeded;
        private volatile bool _isDisposed;
        private bool _hasChanged;

        public ComposablePartCatalogCollection(
            IEnumerable<ComposablePartCatalog>? catalogs,
            Action<ComposablePartCatalogChangeEventArgs>? onChanged,
            Action<ComposablePartCatalogChangeEventArgs>? onChanging)
        {
            catalogs ??= Enumerable.Empty<ComposablePartCatalog>();
            _catalogs = new List<ComposablePartCatalog>(catalogs);
            _onChanged = onChanged;
            _onChanging = onChanging;

            SubscribeToCatalogNotifications(catalogs);
        }

        public void Add(ComposablePartCatalog item)
        {
            Requires.NotNull(item, nameof(item));

            ThrowIfDisposed();

            var addedParts = new Lazy<IEnumerable<ComposablePartDefinition>>(() => item.ToArray(), LazyThreadSafetyMode.PublicationOnly);

            using (var atomicComposition = new AtomicComposition())
            {
                RaiseChangingEvent(addedParts, null, atomicComposition);

                using (new WriteLock(_lock))
                {
                    if (_isCopyNeeded)
                    {
                        _catalogs = new List<ComposablePartCatalog>(_catalogs);
                        _isCopyNeeded = false;
                    }
                    _hasChanged = true;
                    _catalogs.Add(item);
                }

                SubscribeToCatalogNotifications(item);

                // Complete after the catalog changes are written
                atomicComposition.Complete();
            }

            RaiseChangedEvent(addedParts, null);
        }

        /// <summary>
        /// Notify when the contents of the Catalog has changed.
        /// </summary>
        public event EventHandler<ComposablePartCatalogChangeEventArgs>? Changed;

        /// <summary>
        /// Notify when the contents of the Catalog has changing.
        /// </summary>
        public event EventHandler<ComposablePartCatalogChangeEventArgs>? Changing;

        public void Clear()
        {
            ThrowIfDisposed();

            // No action is required if we are already empty
            ComposablePartCatalog[]? catalogs = null;
            using (new ReadLock(_lock))
            {
                if (_catalogs.Count == 0)
                {
                    return;
                }
                catalogs = _catalogs.ToArray();
            }

            // We are doing this outside of the lock, so it's possible that the catalog will continute propagating events from things
            // we are about to unsubscribe from. Given the non-specificity of our event, in the worst case scenario we would simply fire
            // unnecessary events.
            var removedParts = new Lazy<IEnumerable<ComposablePartDefinition>>(() => catalogs.SelectMany(catalog => catalog).ToArray(), LazyThreadSafetyMode.PublicationOnly);

            // Validate the changes before applying them
            using (var atomicComposition = new AtomicComposition())
            {
                RaiseChangingEvent(null, removedParts, atomicComposition);
                UnsubscribeFromCatalogNotifications(catalogs);

                using (new WriteLock(_lock))
                {
                    _catalogs = new List<ComposablePartCatalog>();

                    _isCopyNeeded = false;
                    _hasChanged = true;
                }

                // Complete after the catalog changes are written
                atomicComposition.Complete();
            }

            RaiseChangedEvent(null, removedParts);
        }

        public bool Contains(ComposablePartCatalog item)
        {
            Requires.NotNull(item, nameof(item));

            ThrowIfDisposed();

            using (new ReadLock(_lock))
            {
                return _catalogs.Contains(item);
            }
        }

        public void CopyTo(ComposablePartCatalog[] array, int arrayIndex)
        {
            ThrowIfDisposed();

            using (new ReadLock(_lock))
            {
                _catalogs.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                ThrowIfDisposed();

                using (new ReadLock(_lock))
                {
                    return _catalogs.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                ThrowIfDisposed();

                return false;
            }
        }

        public bool Remove(ComposablePartCatalog item)
        {
            Requires.NotNull(item, nameof(item));

            ThrowIfDisposed();

            using (new ReadLock(_lock))
            {
                if (!_catalogs.Contains(item))
                {
                    return false;
                }
            }

            bool isSuccessfulRemoval = false;

            var removedParts = new Lazy<IEnumerable<ComposablePartDefinition>>(() => item.ToArray(), LazyThreadSafetyMode.PublicationOnly);
            using (var atomicComposition = new AtomicComposition())
            {
                RaiseChangingEvent(null, removedParts, atomicComposition);

                using (new WriteLock(_lock))
                {
                    if (_isCopyNeeded)
                    {
                        _catalogs = new List<ComposablePartCatalog>(_catalogs);
                        _isCopyNeeded = false;
                    }

                    isSuccessfulRemoval = _catalogs.Remove(item);
                    if (isSuccessfulRemoval)
                    {
                        _hasChanged = true;
                    }
                }

                UnsubscribeFromCatalogNotifications(item);

                // Complete after the catalog changes are written
                atomicComposition.Complete();
            }

            RaiseChangedEvent(null, removedParts);

            return isSuccessfulRemoval;
        }

        internal bool HasChanged
        {
            get
            {
                ThrowIfDisposed();

                using (new ReadLock(_lock))
                {
                    return _hasChanged;
                }
            }
        }

        public IEnumerator<ComposablePartCatalog> GetEnumerator()
        {
            ThrowIfDisposed();

            using (new WriteLock(_lock))
            {
                IEnumerator<ComposablePartCatalog> enumerator = _catalogs.GetEnumerator();
                _isCopyNeeded = true;
                return enumerator;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    bool disposeLock = false;
                    IEnumerable<ComposablePartCatalog>? catalogs = null;
                    try
                    {
                        using (new WriteLock(_lock))
                        {
                            if (!_isDisposed)
                            {
                                disposeLock = true;

                                catalogs = _catalogs;
                                _catalogs = null!;

                                _isDisposed = true;
                            }
                        }
                    }
                    finally
                    {
                        if (catalogs != null)
                        {
                            UnsubscribeFromCatalogNotifications(catalogs);
                            catalogs.ForEach(catalog => catalog.Dispose());
                        }

                        if (disposeLock)
                        {
                            _lock.Dispose();
                        }
                    }
                }
            }
        }

        private void RaiseChangedEvent(
            Lazy<IEnumerable<ComposablePartDefinition>>? addedDefinitions,
            Lazy<IEnumerable<ComposablePartDefinition>>? removedDefinitions)
        {
            if (_onChanged == null || Changed == null)
            {
                return;
            }

            var added = (addedDefinitions == null ? Enumerable.Empty<ComposablePartDefinition>() : addedDefinitions.Value);
            var removed = (removedDefinitions == null ? Enumerable.Empty<ComposablePartDefinition>() : removedDefinitions.Value);

            _onChanged.Invoke(new ComposablePartCatalogChangeEventArgs(added, removed, null));
        }

        public void OnChanged(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        private void RaiseChangingEvent(
           Lazy<IEnumerable<ComposablePartDefinition>>? addedDefinitions,
           Lazy<IEnumerable<ComposablePartDefinition>>? removedDefinitions,
           AtomicComposition? atomicComposition)
        {
            if (_onChanging == null || Changing == null)
            {
                return;
            }
            var added = (addedDefinitions == null ? Enumerable.Empty<ComposablePartDefinition>() : addedDefinitions.Value);
            var removed = (removedDefinitions == null ? Enumerable.Empty<ComposablePartDefinition>() : removedDefinitions.Value);

            _onChanging.Invoke(new ComposablePartCatalogChangeEventArgs(added, removed, atomicComposition));
        }

        public void OnChanging(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            Changing?.Invoke(sender, e);
        }

        private void OnContainedCatalogChanged(object? sender, ComposablePartCatalogChangeEventArgs e)
        {
            if (_onChanged == null || Changed == null)
            {
                return;
            }

            _onChanged.Invoke(e);
        }

        private void OnContainedCatalogChanging(object? sender, ComposablePartCatalogChangeEventArgs e)
        {
            if (_onChanging == null || Changing == null)
            {
                return;
            }

            _onChanging.Invoke(e);
        }

        private void SubscribeToCatalogNotifications(ComposablePartCatalog catalog)
        {
            if (catalog is INotifyComposablePartCatalogChanged notifyCatalog)
            {
                notifyCatalog.Changed += OnContainedCatalogChanged;
                notifyCatalog.Changing += OnContainedCatalogChanging;
            }
        }

        private void SubscribeToCatalogNotifications(IEnumerable<ComposablePartCatalog> catalogs)
        {
            foreach (var catalog in catalogs)
            {
                SubscribeToCatalogNotifications(catalog);
            }
        }

        private void UnsubscribeFromCatalogNotifications(ComposablePartCatalog catalog)
        {
            if (catalog is INotifyComposablePartCatalogChanged notifyCatalog)
            {
                notifyCatalog.Changed -= OnContainedCatalogChanged;
                notifyCatalog.Changing -= OnContainedCatalogChanging;
            }
        }

        private void UnsubscribeFromCatalogNotifications(IEnumerable<ComposablePartCatalog> catalogs)
        {
            foreach (var catalog in catalogs)
            {
                UnsubscribeFromCatalogNotifications(catalog);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }
    }
}
