// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;
using Microsoft.Internal;

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(destination: typeof(System.Lazy<,>))]

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CompositionContainer : ExportProvider, ICompositionService, IDisposable
    {
        private CompositionOptions _compositionOptions;
        private ImportEngine _importEngine;
        private ComposablePartExportProvider _partExportProvider;
        private ExportProvider _rootProvider;
        private IDisposable _disposableRootProvider;
        private CatalogExportProvider _catalogExportProvider;
        private ExportProvider _localExportProvider;
        private IDisposable _disposableLocalExportProvider;
        private ExportProvider _ancestorExportProvider;
        private IDisposable _disposableAncestorExportProvider;
        
        private readonly ReadOnlyCollection<ExportProvider> _providers;
        private volatile bool _isDisposed = false;
        private object _lock = new object();
        private static ReadOnlyCollection<ExportProvider> EmptyProviders = new ReadOnlyCollection<ExportProvider>(new ExportProvider[]{});

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class.
        /// </summary>
        public CompositionContainer()
            : this((ComposablePartCatalog)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class 
        ///     with the specified export providers.
        /// </summary>
        /// <param name="providers">
        ///     A <see cref="Array"/> of <see cref="ExportProvider"/> objects which provide 
        ///     the <see cref="CompositionContainer"/> access to <see cref="Export"/> objects,
        ///     or <see langword="null"/> to set <see cref="Providers"/> to an empty
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public CompositionContainer(params ExportProvider[] providers) : 
            this((ComposablePartCatalog)null, providers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class 
        ///     with the specified export providers.
        /// </summary>
        /// <param name="compositionOPtionss">
        ///     <see cref="CompositionOptions"/> enumeration with flags controlling the composition.
        /// </param>
        /// <param name="providers">
        ///     A <see cref="Array"/> of <see cref="ExportProvider"/> objects which provide 
        ///     the <see cref="CompositionContainer"/> access to <see cref="Export"/> objects,
        ///     or <see langword="null"/> to set <see cref="Providers"/> to an empty
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public CompositionContainer(CompositionOptions compositionOptions, params ExportProvider[] providers) : 
            this((ComposablePartCatalog)null, compositionOptions, providers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class 
        ///     with the specified catalog and export providers.
        /// </summary>
        /// <param name="providers">
        ///     A <see cref="Array"/> of <see cref="ExportProvider"/> objects which provide 
        ///     the <see cref="CompositionContainer"/> access to <see cref="Export"/> objects,
        ///     or <see langword="null"/> to set <see cref="Providers"/> to an empty 
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public CompositionContainer(ComposablePartCatalog catalog, params ExportProvider[] providers): 
            this(catalog, false, providers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class 
        ///     with the specified catalog and export providers.
        /// </summary>
        /// <param name="isThreadSafe">
        ///     <see cref="bool"/> indicates whether container instances are threadsafe.
        /// </param>
        /// <param name="providers">
        ///     A <see cref="Array"/> of <see cref="ExportProvider"/> objects which provide 
        ///     the <see cref="CompositionContainer"/> access to <see cref="Export"/> objects,
        ///     or <see langword="null"/> to set <see cref="Providers"/> to an empty 
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public CompositionContainer(ComposablePartCatalog catalog, bool isThreadSafe, params ExportProvider[] providers)
            : this(catalog, isThreadSafe ? CompositionOptions.IsThreadSafe : CompositionOptions.Default, providers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContainer"/> class 
        ///     with the specified catalog and export providers.
        /// </summary>
        /// <param name="compositionSettings">
        ///     <see cref="CompositionOptions"/> enumeration with flags controlling the composition.
        /// </param>
        /// <param name="providers">
        ///     A <see cref="Array"/> of <see cref="ExportProvider"/> objects which provide 
        ///     the <see cref="CompositionContainer"/> access to <see cref="Export"/> objects,
        ///     or <see langword="null"/> to set <see cref="Providers"/> to an empty 
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public CompositionContainer(ComposablePartCatalog catalog, CompositionOptions compositionOptions, params ExportProvider[] providers)
        {
            if (compositionOptions > (CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe | CompositionOptions.ExportCompositionService))
            {
                throw new ArgumentOutOfRangeException("compositionOptions");
            }
            _compositionOptions = compositionOptions;

            // We always create the mutable provider
            _partExportProvider = new ComposablePartExportProvider(compositionOptions);
            _partExportProvider.SourceProvider = this;

            // Create the catalog export provider, only if necessary 
            if (catalog != null)
            {
                _catalogExportProvider = new CatalogExportProvider(catalog, compositionOptions);
                _catalogExportProvider.SourceProvider = this;
            }

            // Set the local export provider
            if (_catalogExportProvider != null)
            {
                _localExportProvider = new AggregateExportProvider(_partExportProvider, _catalogExportProvider);
                _disposableLocalExportProvider = _localExportProvider as IDisposable;
            }
            else
            {
                _localExportProvider = _partExportProvider;
            }

            // Set the ancestor export provider, if ancestors are supplied
            if ((providers != null) && (providers.Length > 0))
            {
                // Aggregate ancestors if and only if more than one passed
                if (providers.Length > 1)
                {
                    _ancestorExportProvider = new AggregateExportProvider(providers);
                    _disposableAncestorExportProvider = _ancestorExportProvider as IDisposable;
                }
                else
                {
                    if (providers[0] == null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement("providers");
                    }
                    _ancestorExportProvider = providers[0];
                }
            }

            // finally set the root provider
            if (_ancestorExportProvider == null)
            {
                // if no ancestors are passed, the local and the root are the same
                _rootProvider = _localExportProvider;
            }
            else
            {
                int exportProviderCount = 1 + ((catalog != null) ? 1 : 0) + ((providers != null) ? providers.Length : 0);
                ExportProvider[] rootProviders = new ExportProvider[exportProviderCount];

                rootProviders[0] = _partExportProvider;
                int customProviderStartIndex = 1;
                if (catalog != null)
                {
                    rootProviders[1] = _catalogExportProvider;
                    customProviderStartIndex = 2;
                }

                if (providers != null)
                {
                    for (int i = 0; i < providers.Length; i++)
                    {
                        rootProviders[customProviderStartIndex + i] = providers[i];
                    }
                }

                _rootProvider = new AggregateExportProvider(rootProviders);
                _disposableRootProvider = _rootProvider as IDisposable;
            }

//Insert Composition Service
            if(compositionOptions.HasFlag(CompositionOptions.ExportCompositionService))
            {
                this.ComposeExportedValue<ICompositionService>(new CompositionServiceShim(this));
            }           

            _rootProvider.ExportsChanged += OnExportsChangedInternal;
            _rootProvider.ExportsChanging += OnExportsChangingInternal;

            _providers = (providers != null) ? new ReadOnlyCollection<ExportProvider>((ExportProvider[])providers.Clone()) : EmptyProviders;
        }

        internal CompositionOptions CompositionOptions
        {
            get 
            {
                ThrowIfDisposed();
                return _compositionOptions; 
            }
        }                                                   

        /// <summary>
        ///     Gets the catalog which provides the container access to exports produced
        ///     from composable parts.
        /// </summary>
        /// <value>
        ///     The <see cref="ComposablePartCatalog"/> which provides the 
        ///     <see cref="CompositionContainer"/> access to exports produced from
        ///     <see cref="ComposablePart"/> objects. The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public ComposablePartCatalog Catalog
        {
            get 
            {
                ThrowIfDisposed();

                return (_catalogExportProvider != null) ? _catalogExportProvider.Catalog : null;
            }
        }

        internal CatalogExportProvider CatalogExportProvider
        {
            get 
            {
                ThrowIfDisposed();

                return _catalogExportProvider;
            }
        }

        /// <summary>
        ///     Gets the export providers which provide the container access to additional exports.
        /// </summary>
        /// <value>
        ///     A <see cref="ReadOnlyCollection{T}"/> of <see cref="ExportProvider"/> objects
        ///     which provide the <see cref="CompositionContainer"/> access to additional
        ///     <see cref="Export"/> objects. The default is an empty 
        ///     <see cref="ReadOnlyCollection{T}"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public ReadOnlyCollection<ExportProvider> Providers
        {
            get
            {
                ThrowIfDisposed();
                Contract.Ensures(Contract.Result<ReadOnlyCollection<ExportProvider>>() != null);

                return _providers;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    ExportProvider rootProvider = null;
                    IDisposable disposableAncestorExportProvider = null;
                    IDisposable disposableLocalExportProvider = null;
                    IDisposable disposableRootProvider = null;
                    ComposablePartExportProvider partExportProvider = null;
                    CatalogExportProvider catalogExportProvider = null;
                    ImportEngine importEngine = null;

                    lock(_lock)
                    {
                        if (!_isDisposed)
                        {
                            rootProvider = _rootProvider;
                            _rootProvider = null;

                            disposableRootProvider = _disposableRootProvider;
                            _disposableRootProvider = null;

                            disposableLocalExportProvider = _disposableLocalExportProvider ;
                            _disposableLocalExportProvider = null;
                            _localExportProvider = null;

                            disposableAncestorExportProvider = _disposableAncestorExportProvider;
                            _disposableAncestorExportProvider = null;
                            _ancestorExportProvider = null;

                            partExportProvider = _partExportProvider;
                            _partExportProvider = null;

                            catalogExportProvider = _catalogExportProvider;
                            _catalogExportProvider = null;

                            importEngine = _importEngine;
                            _importEngine = null;

                            _isDisposed = true;
                        }
                    }

                    if (rootProvider != null)
                    {
                        rootProvider.ExportsChanged -= OnExportsChangedInternal;
                        rootProvider.ExportsChanging -= OnExportsChangingInternal;
                    }

                    if (disposableRootProvider != null)
                    {
                        disposableRootProvider.Dispose();
                    }

                    if (disposableAncestorExportProvider != null)
                    {
                        disposableAncestorExportProvider.Dispose();
                    }

                    if (disposableLocalExportProvider != null)
                    {
                        disposableLocalExportProvider.Dispose();
                    }

                    if (catalogExportProvider != null)
                    {
                        catalogExportProvider.Dispose();
                    }

                    if (partExportProvider != null)
                    {
                        partExportProvider.Dispose();
                    }

                    if (importEngine != null)
                    {
                        importEngine.Dispose();
                    }
                }

            }
        }
  
        public void Compose(CompositionBatch batch)
        {
            Requires.NotNull(batch, nameof(batch));
            ThrowIfDisposed();

            _partExportProvider.Compose(batch);
        }

        /// <summary>
        ///     Releases the <see cref="Export"/> from the <see cref="CompositionContainer"/>. The behavior
        ///     may vary depending on the implementation of the <see cref="ExportProvider"/> that produced 
        ///     the <see cref="Export"/> instance. As a general rule non shared exports should be early 
        ///     released causing them to be detached from the container.
        ///
        ///     For example the <see cref="CatalogExportProvider"/> will only release 
        ///     an <see cref="Export"/> if it comes from a <see cref="ComposablePart"/> that was constructed
        ///     under a <see cref="CreationPolicy.NonShared" /> context. Release in this context means walking
        ///     the dependency chain of the <see cref="Export"/>s, detaching references from the container and 
        ///     calling Dispose on the <see cref="ComposablePart"/>s as needed. If the <see cref="Export"/> 
        ///     was constructed under a <see cref="CreationPolicy.Shared" /> context the 
        ///     <see cref="CatalogExportProvider"/> will do nothing as it may be in use by other requestors. 
        ///     Those will only be detached when the container is itself disposed.
        /// </summary>
        /// <param name="export"><see cref="Export"/> that needs to be released.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="export"/> is <see langword="null"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Performance", "CA1822")]
        public void ReleaseExport(Export export)
        {
            Requires.NotNull(export, nameof(export));

            IDisposable dependency = export as IDisposable;

            if (dependency != null)
            {
                dependency.Dispose();
            }
        }

        /// <summary>
        ///     Releases the <see cref="Lazy{T}"/> from the <see cref="CompositionContainer"/>. The behavior
        ///     may vary depending on the implementation of the <see cref="ExportProvider"/> that produced 
        ///     the <see cref="Export"/> instance. As a general rule non shared exports should be early 
        ///     released causing them to be detached from the container.
        ///
        ///     For example the <see cref="CatalogExportProvider"/> will only release 
        ///     an <see cref="Lazy{T}"/> if it comes from a <see cref="ComposablePart"/> that was constructed
        ///     under a <see cref="CreationPolicy.NonShared" /> context. Release in this context means walking
        ///     the dependency chain of the <see cref="Export"/>s, detaching references from the container and 
        ///     calling Dispose on the <see cref="ComposablePart"/>s as needed. If the <see cref="Export"/> 
        ///     was constructed under a <see cref="CreationPolicy.Shared" /> context the 
        ///     <see cref="CatalogExportProvider"/> will do nothing as it may be in use by other requestors. 
        ///     Those will only be detached when the container is itself disposed.
        /// </summary>
        /// <param name="export"><see cref="Export"/> that needs to be released.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="export"/> is <see langword="null"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Performance", "CA1822")]
        public void ReleaseExport<T>(Lazy<T> export)
        {
            Requires.NotNull(export, nameof(export));

            IDisposable dependency = export as IDisposable;

            if (dependency != null)
            {
                dependency.Dispose();
            }
        }

        /// <summary>
        ///     Releases a set of <see cref="Export"/>s from the <see cref="CompositionContainer"/>. 
        ///     See also <see cref="ReleaseExport"/>.
        /// </summary>
        /// <param name="exports"><see cref="Export"/>s that need to be released.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="exports"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="exports"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public void ReleaseExports(IEnumerable<Export> exports)
        {
            Requires.NotNullOrNullElements(exports, "exports");

            foreach (Export export in exports)
            {
                ReleaseExport(export);
            }
        }

        /// <summary>
        ///     Releases a set of <see cref="Export"/>s from the <see cref="CompositionContainer"/>. 
        ///     See also <see cref="ReleaseExport"/>.
        /// </summary>
        /// <param name="exports"><see cref="Export"/>s that need to be released.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="exports"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="exports"/> contains an element that is <see langword="null"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public void ReleaseExports<T>(IEnumerable<Lazy<T>> exports)
        {
            Requires.NotNullOrNullElements(exports, "exports");

            foreach (Lazy<T> export in exports)
            {
                ReleaseExport(export);
            }
        }

        /// <summary>
        ///     Releases a set of <see cref="Export"/>s from the <see cref="CompositionContainer"/>. 
        ///     See also <see cref="ReleaseExport"/>.
        /// </summary>
        /// <param name="exports"><see cref="Export"/>s that need to be released.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="exports"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="exports"/> contains an element that is <see langword="null"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public void ReleaseExports<T, TMetadataView>(IEnumerable<Lazy<T, TMetadataView>> exports)
        {
            Requires.NotNullOrNullElements(exports, "exports");

            foreach (Lazy<T, TMetadataView> export in exports)
            {
                ReleaseExport(export);
            }
        }

        /// <summary>
        ///     Sets the imports of the specified composable part exactly once and they will not
        ///     ever be recomposed.
        /// </summary>
        /// <param name="part">
        ///     The <see cref="ComposablePart"/> to set the imports.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="part"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will
        ///     contain a collection of errors that occurred.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ICompositionService"/> has been disposed of.
        /// </exception>
        public void SatisfyImportsOnce(ComposablePart part)
        {
            ThrowIfDisposed();
            
            if (_importEngine == null)
            {
                ImportEngine importEngine = new ImportEngine(this, _compositionOptions);
                
                lock(_lock)
                {
                    if (_importEngine == null)
                    {
                        Thread.MemoryBarrier();
                        _importEngine = importEngine;
                        importEngine = null;
                    }
                }
                if(importEngine != null)
                {
                    importEngine.Dispose();
                }
            }
            _importEngine.SatisfyImportsOnce(part);
        }

        internal void OnExportsChangedInternal(object sender, ExportsChangeEventArgs e)
        {
            OnExportsChanged(e);
        }

        internal void OnExportsChangingInternal(object sender, ExportsChangeEventArgs e)
        {
            OnExportsChanging(e);
        }

        /// <summary>
        /// Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">The <see cref="ImportDefinition"/> that defines the conditions of the
        /// <see cref="Export"/> to get.</param>
        /// <returns></returns>
        /// <result>
        /// An <see cref="IEnumerable{T}"/> of <see cref="Export"/> objects that match
        /// the conditions defined by <see cref="ImportDefinition"/>, if found; otherwise, an
        /// empty <see cref="IEnumerable{T}"/>.
        /// </result>
        /// <remarks>
        /// 	<note type="inheritinfo">
        /// The implementers should not treat the cardinality-related mismatches as errors, and are not
        /// expected to throw exceptions in those cases.
        /// For instance, if the import requests exactly one export and the provider has no matching exports or more than one,
        /// it should return an empty <see cref="IEnumerable{T}"/> of <see cref="Export"/>.
        /// </note>
        /// </remarks>
        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            ThrowIfDisposed();

            IEnumerable<Export> exports = null;

            object source;
            if(!definition.Metadata.TryGetValue(CompositionConstants.ImportSourceMetadataName, out source))
            {
                source = ImportSource.Any;
            }

            switch((ImportSource)source)
            {
                case ImportSource.Any:
                    Assumes.NotNull(_rootProvider);
                    _rootProvider.TryGetExports(definition, atomicComposition, out exports);
                    break;
                case ImportSource.Local:
                    Assumes.NotNull(_localExportProvider);
                    _localExportProvider.TryGetExports(definition.RemoveImportSource(), atomicComposition, out exports);
                    break;
                case ImportSource.NonLocal:
                    if(_ancestorExportProvider != null)
                    {
                        _ancestorExportProvider.TryGetExports(definition.RemoveImportSource(), atomicComposition, out exports);
                    }
                    break;
            }

            return exports;
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
