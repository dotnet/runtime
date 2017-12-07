// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class FilteredCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged
    {
        private Func<ComposablePartDefinition, bool> _filter;
        private ComposablePartCatalog _innerCatalog;
        private FilteredCatalog _complement;
        private object _lock = new object();
        private volatile bool _isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredCatalog"/> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="filter">The filter.</param>
        public FilteredCatalog(ComposablePartCatalog catalog, Func<ComposablePartDefinition, bool> filter) : 
            this(catalog, filter, null)
        {
        }

        internal FilteredCatalog(ComposablePartCatalog catalog, Func<ComposablePartDefinition, bool> filter, FilteredCatalog complement)
        {
            Requires.NotNull(catalog, nameof(catalog));
            Requires.NotNull(filter, nameof(filter));

            _innerCatalog = catalog;
            _filter = (p) => filter.Invoke(p.GetGenericPartDefinition() ?? p);
            _complement = complement;

            INotifyComposablePartCatalogChanged notifyCatalog = _innerCatalog as INotifyComposablePartCatalogChanged;
            if (notifyCatalog != null)
            {
                notifyCatalog.Changed += OnChangedInternal;
                notifyCatalog.Changing += OnChangingInternal;
            }
        }

/// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if(!_isDisposed)
                    {
                        INotifyComposablePartCatalogChanged notifyCatalog = null;
                        try
                        {
                            lock (_lock)
                            {
                                if (!_isDisposed)
                                {
                                    _isDisposed = true;
                                    notifyCatalog = _innerCatalog as INotifyComposablePartCatalogChanged;
                                    _innerCatalog = null;
                                }
                            }
                        }
                        finally
                        {
                            if (notifyCatalog != null)
                            {
                                notifyCatalog.Changed -= OnChangedInternal;
                                notifyCatalog.Changing -= OnChangingInternal;
                            }
                        }
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            return _innerCatalog.Where(_filter).GetEnumerator();
        }

        /// <summary>
        /// Gets the complement.
        /// </summary>
        /// <value>The complement.</value>
        public FilteredCatalog Complement
        {
            get
            {
                ThrowIfDisposed();

                if (_complement == null)
                {
                    FilteredCatalog complement = new FilteredCatalog(_innerCatalog, p => !_filter(p), this);
                    lock (_lock)
                    {
                        if (_complement == null)
                        {
                            Thread.MemoryBarrier();
                            _complement = complement;
                            complement = null;
                        }
                    }

                    if (complement != null)
                    {
                        complement.Dispose();
                    }
                }

                return _complement;
            }
        }

        /// <summary>
        /// Returns the export definitions that match the constraint defined by the specified definition.
        /// </summary>
        /// <param name="definition">The <see cref="ImportDefinition"/> that defines the conditions of the
        /// <see cref="ExportDefinition"/> objects to return.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="Tuple{T1, T2}"/> containing the
        /// <see cref="ExportDefinition"/> objects and their associated
        /// <see cref="ComposablePartDefinition"/> for objects that match the constraint defined
        /// by <paramref name="definition"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// 	<paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ComposablePartCatalog"/> has been disposed of.
        /// </exception>
        /// <remarks>
        /// 	<note type="inheritinfo">
        /// Overriders of this property should never return <see langword="null"/>, if no
        /// <see cref="ExportDefinition"/> match the conditions defined by
        /// <paramref name="definition"/>, return an empty <see cref="IEnumerable{T}"/>.
        /// </note>
        /// </remarks>
        public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
        {
            ThrowIfDisposed();
            Requires.NotNull(definition, nameof(definition));

            var exports = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();
            foreach(var export in _innerCatalog.GetExports(definition))
            {
                if (_filter(export.Item1))
                {
                    exports.Add(export);
                }
            }

            return exports;
        }

        /// <summary>
        /// Notify when the contents of the Catalog has changed.
        /// </summary>
        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;

/// <summary>
        /// Notify when the contents of the Catalog is changing.
        /// </summary>
        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;

/// <summary>
        /// Raises the <see cref="E:Changed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.Composition.Hosting.ComposablePartCatalogChangeEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChanged(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> changedEvent = Changed;
            if (changedEvent != null)
            {
                changedEvent.Invoke(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Changing"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.Composition.Hosting.ComposablePartCatalogChangeEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChanging(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> changingEvent = Changing;
            if (changingEvent != null)
            {
                changingEvent.Invoke(this, e);
            }
        }

        private void OnChangedInternal(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            var processedArgs = ProcessEventArgs(e);
            if (processedArgs != null)
            {
                OnChanged(ProcessEventArgs(processedArgs));
            }
        }

        private void OnChangingInternal(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            var processedArgs = ProcessEventArgs(e);
            if (processedArgs != null)
            {
                OnChanging(ProcessEventArgs(processedArgs));
            }
        }

        private ComposablePartCatalogChangeEventArgs ProcessEventArgs(ComposablePartCatalogChangeEventArgs e)
        {
            // the constructor for ComposablePartCatalogChangeEventArgs takes a snapshot of the arguments, so we don't have to
            var result = new ComposablePartCatalogChangeEventArgs(
                e.AddedDefinitions.Where(_filter),
                e.RemovedDefinitions.Where(_filter),
                e.AtomicComposition);

            // Only fire if we need to
            if (result.AddedDefinitions.FastAny() || result.RemovedDefinitions.FastAny())
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }
    }
}
