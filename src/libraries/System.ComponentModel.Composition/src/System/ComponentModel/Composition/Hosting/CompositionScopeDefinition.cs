// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    [DebuggerTypeProxy(typeof(CompositionScopeDefinitionDebuggerProxy))]
    public class CompositionScopeDefinition : ComposablePartCatalog, INotifyComposablePartCatalogChanged
    {
        private ComposablePartCatalog _catalog;
        private IEnumerable<ExportDefinition> _publicSurface = null;
        private IEnumerable<CompositionScopeDefinition> _children = Enumerable.Empty<CompositionScopeDefinition>();
        private volatile int _isDisposed = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionScopeDefinition"/> class.
        /// </summary>
        protected CompositionScopeDefinition() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionScopeDefinition"/> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="children">The children.</param>
        public CompositionScopeDefinition(ComposablePartCatalog catalog, IEnumerable<CompositionScopeDefinition> children)
        {
            Requires.NotNull(catalog, nameof(catalog));
            Requires.NullOrNotNullElements(children, "children");

            InitializeCompositionScopeDefinition(catalog, children, null);
        }

/// <summary>
        /// Initializes a new instance of the <see cref="CompositionScopeDefinition"/> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="children">The children.</param>
        /// <param name="publicSurface">The exports that can be used to create new scopes.</param>
        public CompositionScopeDefinition(ComposablePartCatalog catalog, IEnumerable<CompositionScopeDefinition> children, IEnumerable<ExportDefinition> publicSurface)
        {
            Requires.NotNull(catalog, nameof(catalog));
            Requires.NullOrNotNullElements(children, "children");
            Requires.NullOrNotNullElements(publicSurface, "publicSurface");

            InitializeCompositionScopeDefinition(catalog, children, publicSurface);
        }

/// <summary>
        /// Initializes a new instance of the <see cref="CompositionScopeDefinition"/> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="children">The children.</param>
        private void InitializeCompositionScopeDefinition(ComposablePartCatalog catalog, IEnumerable<CompositionScopeDefinition> children, IEnumerable<ExportDefinition> publicSurface)
        {
            _catalog = catalog;
            if (children != null)
            {
                _children = children.ToArray();
            }
            if(publicSurface != null)
            {
                _publicSurface = publicSurface;
            }

            INotifyComposablePartCatalogChanged notifyCatalog = _catalog as INotifyComposablePartCatalogChanged;
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
                    // NOTE : According to http://msdn.microsoft.com/en-us/library/4bw5ewxy.aspx, the warning is bogus when used with Interlocked API.
#pragma warning disable 420
                    if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
#pragma warning restore 420
                    {
                        INotifyComposablePartCatalogChanged notifyCatalog = _catalog as INotifyComposablePartCatalogChanged;
                        if (notifyCatalog != null)
                        {
                            notifyCatalog.Changed -= OnChangedInternal;
                            notifyCatalog.Changing -= OnChangingInternal;
                        }
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public virtual IEnumerable<CompositionScopeDefinition> Children
        {
            get
            {
                ThrowIfDisposed();

                return _children;
            }
        }

        /// <summary>
        ///     Gets the export definitions that describe the exports surfaced by the CompositionScopedefinition.
        /// </summary>
        /// <value>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ExportDefinition"/> objects describing
        ///     the exports surfaced by the <see cref="CompositionScopeDefinition"/>.
        /// </value>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property must not return <see langword="null"/>.
        ///     </note>
        /// </remarks>
        public virtual IEnumerable<ExportDefinition> PublicSurface
        {
            get
            {
                ThrowIfDisposed();
                if(_publicSurface == null)
                {
                    return this.SelectMany( (p) => p.ExportDefinitions );
                }

                return _publicSurface;
            }
        } 

        /// <summary>
        /// Gets an Enumerator for the ComposablePartDefinitions
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            return _catalog.GetEnumerator();
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

            return _catalog.GetExports(definition);
        }

        internal IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExportsFromPublicSurface(ImportDefinition definition)
        {
            Assumes.NotNull(definition, "definition");

            var exports = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();

            foreach(var exportDefinition in PublicSurface)
            {
                if (definition.IsConstraintSatisfiedBy(exportDefinition))
                {
                    foreach (var export in GetExports(definition))
                    {
                        if(export.Item2 == exportDefinition)
                        {
                            exports.Add(export);
                            break;
                        }
                    }
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
            OnChanged(e);
        }

        private void OnChangingInternal(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            OnChanging(e);
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed == 1)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }
    }
}
