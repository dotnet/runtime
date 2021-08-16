// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Diagnostics;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider : ExportProvider, IDisposable
    {
        private sealed class InnerCatalogExportProvider : ExportProvider
        {
            private readonly CatalogExportProvider _outerExportProvider;

            public InnerCatalogExportProvider(CatalogExportProvider outerExportProvider)
            {
                if (outerExportProvider == null)
                {
                    throw new ArgumentNullException(nameof(outerExportProvider));
                }
                _outerExportProvider = outerExportProvider;
            }

            protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition)
            {
                return _outerExportProvider.InternalGetExportsCore(definition, atomicComposition);
            }
        }

        private readonly CompositionLock _lock;
        private readonly Dictionary<ComposablePartDefinition, CatalogPart> _activatedParts = new Dictionary<ComposablePartDefinition, CatalogPart>();
        private readonly HashSet<ComposablePartDefinition> _rejectedParts = new HashSet<ComposablePartDefinition>();
        private ConditionalWeakTable<object, List<ComposablePart>>? _gcRoots;
        private readonly HashSet<IDisposable> _partsToDispose = new HashSet<IDisposable>();
        private ComposablePartCatalog _catalog;
        private volatile bool _isDisposed;
        private volatile bool _isRunning;
        private readonly bool _disableSilentRejection;
        private ExportProvider? _sourceProvider;
        private ImportEngine? _importEngine;
        private readonly CompositionOptions _compositionOptions;
        private ExportProvider? _innerExportProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogExportProvider"/> class.
        /// </summary>
        /// <param name="catalog">
        ///     The <see cref="ComposablePartCatalog"/> that the <see cref="CatalogExportProvider"/>
        ///     uses to produce <see cref="Export"/> objects.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="catalog"/> is <see langword="null"/>.
        /// </exception>
        public CatalogExportProvider(ComposablePartCatalog catalog)
            : this(catalog, CompositionOptions.Default)
        {
        }

        public CatalogExportProvider(ComposablePartCatalog catalog, bool isThreadSafe)
            : this(catalog, isThreadSafe ? CompositionOptions.IsThreadSafe : CompositionOptions.Default)
        {
        }

        public CatalogExportProvider(ComposablePartCatalog catalog, CompositionOptions compositionOptions)
        {
            Requires.NotNull(catalog, nameof(catalog));
            if (compositionOptions > (CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe | CompositionOptions.ExportCompositionService))
            {
                throw new ArgumentOutOfRangeException(nameof(compositionOptions));
            }

            _catalog = catalog;
            _compositionOptions = compositionOptions;
            if (_catalog is INotifyComposablePartCatalogChanged notifyCatalogChanged)
            {
                notifyCatalogChanged.Changing += OnCatalogChanging;
            }

            if (_catalog is CompositionScopeDefinition scopeDefinition)
            {
                _innerExportProvider = new AggregateExportProvider(new ScopeManager(this, scopeDefinition), new InnerCatalogExportProvider(this));
            }
            else
            {
                _innerExportProvider = new InnerCatalogExportProvider(this);
            }
            _lock = new CompositionLock(compositionOptions.HasFlag(CompositionOptions.IsThreadSafe));
            _disableSilentRejection = compositionOptions.HasFlag(CompositionOptions.DisableSilentRejection);
        }

        /// <summary>
        ///     Gets the composable part catalog that the provider users to
        ///     produce exports.
        /// </summary>
        /// <value>
        ///     The <see cref="ComposablePartCatalog"/> that the
        ///     <see cref="CatalogExportProvider"/>
        ///     uses to produce <see cref="Export"/> objects.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public ComposablePartCatalog Catalog
        {
            get
            {
                ThrowIfDisposed();
                Debug.Assert(_catalog != null);

                return _catalog;
            }
        }

        /// <summary>
        ///     Gets the export provider which provides the provider access to additional
        ///     exports.
        /// </summary>
        /// <value>
        ///     The <see cref="ExportProvider"/> which provides the
        ///     <see cref="CatalogExportProvider"/> access to additional
        ///     <see cref="Export"/> objects. The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     This property has already been set.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The methods on the <see cref="CatalogExportProvider"/>
        ///     have already been accessed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CatalogExportProvider"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     This property must be set before accessing any methods on the
        ///     <see cref="CatalogExportProvider"/>.
        /// </remarks>
        [DisallowNull]
        public ExportProvider? SourceProvider
        {
            get
            {
                ThrowIfDisposed();
                using (_lock.LockStateForRead())
                {
                    return _sourceProvider;
                }
            }
            set
            {
                ThrowIfDisposed();

                Requires.NotNull(value!, nameof(value));

                ImportEngine? newImportEngine = null;
                AggregateExportProvider? aggregateExportProvider = null;
                ExportProvider sourceProvider = value!;

                bool isThrowing = true;
                try
                {
                    newImportEngine = new ImportEngine(sourceProvider, _compositionOptions);

                    sourceProvider.ExportsChanging += OnExportsChangingInternal;

                    using (_lock.LockStateForWrite())
                    {
                        EnsureCanSet(_sourceProvider);

                        _sourceProvider = sourceProvider;
                        _importEngine = newImportEngine;

                        isThrowing = false;
                    }
                }
                finally
                {
                    if (isThrowing)
                    {
                        sourceProvider.ExportsChanging -= OnExportsChangingInternal;
                        newImportEngine!.Dispose();
                        if (aggregateExportProvider != null)
                        {
                            aggregateExportProvider.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
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
                    //Note:  We do not dispose _lock on dispose because DisposePart needs it to check isDisposed state
                    //          to eliminate race conditions between it and Dispose
                    INotifyComposablePartCatalogChanged? catalogToUnsubscribeFrom = null;
                    HashSet<IDisposable>? partsToDispose = null;
                    ImportEngine? importEngine = null;
                    ExportProvider? sourceProvider = null;
                    AggregateExportProvider? aggregateExportProvider = null;
                    try
                    {
                        using (_lock.LockStateForWrite())
                        {
                            if (!_isDisposed)
                            {
                                catalogToUnsubscribeFrom = _catalog as INotifyComposablePartCatalogChanged;
                                _catalog = null!;

                                aggregateExportProvider = _innerExportProvider as AggregateExportProvider;
                                _innerExportProvider = null;

                                sourceProvider = _sourceProvider;
                                _sourceProvider = null;

                                importEngine = _importEngine;
                                _importEngine = null;

                                partsToDispose = _partsToDispose;
                                _gcRoots = null;
                                _isDisposed = true;
                            }
                        }
                    }
                    finally
                    {
                        if (catalogToUnsubscribeFrom != null)
                        {
                            catalogToUnsubscribeFrom.Changing -= OnCatalogChanging;
                        }

                        if (aggregateExportProvider != null)
                        {
                            aggregateExportProvider.Dispose();
                        }

                        if (sourceProvider != null)
                        {
                            sourceProvider.ExportsChanging -= OnExportsChangingInternal;
                        }

                        if (importEngine != null)
                        {
                            importEngine.Dispose();
                        }

                        if (partsToDispose != null)
                        {
                            foreach (var part in partsToDispose)
                            {
                                part.Dispose();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">The <see cref="ImportDefinition"/> that defines the conditions of the
        /// <param name="atomicComposition">The transactional container for the composition.</param>
        /// <see cref="Export"/> to get.</param>
        /// <returns></returns>
        /// <result>
        /// An <see cref="IEnumerable{T}"/> of <see cref="Export"/> objects that match
        /// the conditions defined by <see cref="ImportDefinition"/>, if found; otherwise, an
        /// empty <see cref="IEnumerable{T}"/>.
        /// </result>
        /// <remarks>
        /// <note type="inheritinfo">
        /// The implementers should not treat the cardinality-related mismatches as errors, and are not
        /// expected to throw exceptions in those cases.
        /// For instance, if the import requests exactly one export and the provider has no matching exports or more than one,
        /// it should return an empty <see cref="IEnumerable{T}"/> of <see cref="Export"/>.
        /// </note>
        /// </remarks>
        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition)
        {
            ThrowIfDisposed();
            EnsureRunning();

            if (_innerExportProvider == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            _innerExportProvider.TryGetExports(definition, atomicComposition, out IEnumerable<Export>? exports);
            return exports!;
        }

        private IEnumerable<Export> InternalGetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition)
        {
            ThrowIfDisposed();
            EnsureRunning();

            // Use the version of the catalog appropriate to this atomicComposition
            ComposablePartCatalog currentCatalog = atomicComposition.GetValueAllowNull(_catalog);

            bool isExportFactory = false;

            if (definition is IPartCreatorImportDefinition partCreatorDefinition)
            {
                definition = partCreatorDefinition.ProductImportDefinition;
                isExportFactory = true;
            }

            CreationPolicy importPolicy = definition.GetRequiredCreationPolicy();

            List<Export> exports = new List<Export>();
            bool ensureRejection = EnsureRejection(atomicComposition);
            foreach (var partDefinitionAndExportDefinition in currentCatalog.GetExports(definition))
            {
                bool isPartRejected = ensureRejection && IsRejected(partDefinitionAndExportDefinition.Item1, atomicComposition);
                if (!isPartRejected)
                {
                    exports.Add(CreateExport(partDefinitionAndExportDefinition.Item1, partDefinitionAndExportDefinition.Item2, isExportFactory, importPolicy));
                }
            }

            return exports;
        }

        private Export CreateExport(ComposablePartDefinition partDefinition, ExportDefinition exportDefinition, bool isExportFactory, CreationPolicy importPolicy)
        {
            if (isExportFactory)
            {
                return new PartCreatorExport(this,
                            partDefinition,
                            exportDefinition);
            }
            else
            {
                return CatalogExport.CreateExport(this,
                            partDefinition,
                            exportDefinition,
                            importPolicy);
            }
        }

        private void OnExportsChangingInternal(object? sender, ExportsChangeEventArgs e)
        {
            UpdateRejections(e.AddedExports.Concat(e.RemovedExports), e.AtomicComposition);
        }

        private static ExportDefinition[] GetExportsFromPartDefinitions(IEnumerable<ComposablePartDefinition> partDefinitions)
        {
            List<ExportDefinition> exports = new List<ExportDefinition>();

            foreach (var partDefinition in partDefinitions)
            {
                foreach (var export in partDefinition.ExportDefinitions)
                {
                    exports.Add(export);

                    // While creating a PartCreatorExportDefinition for every changed definition may not be the most
                    // efficient way to do this the PartCreatorExportDefinition is very efficient and doesn't do any
                    // real work unless its metadata is pulled on. If this turns out to be a bottleneck then we
                    // will need to start tracking all the PartCreator's we hand out and only send those which we
                    // have handed out. In fact we could do the same thing for all the Exports if we wished but
                    // that requires a cache management which we don't want to do at this point.
                    exports.Add(new PartCreatorExportDefinition(export));
                }
            }

            return exports.ToArray();
        }

        private void OnCatalogChanging(object? sender, ComposablePartCatalogChangeEventArgs e)
        {
            using (var atomicComposition = new AtomicComposition(e.AtomicComposition))
            {
                // Save the preview catalog to use in place of the original while handling
                // this event
                atomicComposition.SetValue(_catalog,
                    new CatalogChangeProxy(_catalog, e.AddedDefinitions, e.RemovedDefinitions));

                IEnumerable<ExportDefinition> addedExports = GetExportsFromPartDefinitions(e.AddedDefinitions);
                IEnumerable<ExportDefinition> removedExports = GetExportsFromPartDefinitions(e.RemovedDefinitions);

                // Remove any parts based on eliminated definitions (in a atomicComposition-friendly
                // fashion)
                foreach (var definition in e.RemovedDefinitions)
                {
                    CatalogPart? removedPart = null;
                    bool removed = false;

                    using (_lock.LockStateForRead())
                    {
                        removed = _activatedParts.TryGetValue(definition, out removedPart);
                    }

                    if (removed)
                    {
                        var capturedDefinition = definition;
                        DisposePart(null, removedPart!, atomicComposition);
                        atomicComposition.AddCompleteActionAllowNull(() =>
                        {
                            using (_lock.LockStateForWrite())
                            {
                                _activatedParts.Remove(capturedDefinition);
                            }
                        });
                    }
                }

                UpdateRejections(addedExports.ConcatAllowingNull(removedExports)!, atomicComposition);

                OnExportsChanging(
                    new ExportsChangeEventArgs(addedExports, removedExports, atomicComposition));

                atomicComposition.AddCompleteAction(() => OnExportsChanged(
                    new ExportsChangeEventArgs(addedExports, removedExports, null)));

                atomicComposition.Complete();
            }
        }

        private CatalogPart GetComposablePart(ComposablePartDefinition partDefinition, bool isSharedPart)
        {
            ThrowIfDisposed();
            EnsureRunning();

            CatalogPart catalogPart;

            if (isSharedPart)
            {
                catalogPart = GetSharedPart(partDefinition);
            }
            else
            {
                ComposablePart part = partDefinition.CreatePart();
                catalogPart = new CatalogPart(part);

                if (part is IDisposable disposablePart)
                {
                    using (_lock.LockStateForWrite())
                    {
                        _partsToDispose.Add(disposablePart);
                    }
                }
            }

            return catalogPart;
        }

        private CatalogPart GetSharedPart(ComposablePartDefinition partDefinition)
        {
            CatalogPart? catalogPart = null;

            // look up the part
            using (_lock.LockStateForRead())
            {
                if (_activatedParts.TryGetValue(partDefinition, out catalogPart))
                {
                    return catalogPart;
                }
            }

            // create a part outside of the lock
            ComposablePart? newPart = partDefinition.CreatePart();
            IDisposable? disposableNewPart = newPart as IDisposable;

            using (_lock.LockStateForWrite())
            {
                // check if the part is still not there
                if (!_activatedParts.TryGetValue(partDefinition, out catalogPart))
                {
                    catalogPart = new CatalogPart(newPart);
                    _activatedParts.Add(partDefinition, catalogPart);
                    if (disposableNewPart != null)
                    {
                        _partsToDispose.Add(disposableNewPart);
                    }

                    // indicates the part has been added
                    newPart = null;
                    disposableNewPart = null;
                }
            }

            // if disposableNewPart != null, this means we have created a new instance of something disposable and not used it
            // Dispose of it now
            if (disposableNewPart != null)
            {
                disposableNewPart.Dispose();
            }

            return catalogPart;
        }

        private object? GetExportedValue(CatalogPart part, ExportDefinition export, bool isSharedPart)
        {
            ThrowIfDisposed();
            EnsureRunning();
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (export == null)
            {
                throw new ArgumentNullException(nameof(export));
            }

            // We don't protect against thread racing here, as "importsSatisfied" is merely an optimization
            // if two threads satisfy imports twice, the results is the same, just the perf hit is heavier.

            bool importsSatisfied = part.ImportsSatisfied;
            ImportEngine? importEngine = importsSatisfied ? null : _importEngine;

            object? exportedValue = CompositionServices.GetExportedValueFromComposedPart(
                importEngine, part.Part, export);

            if (!importsSatisfied)
            {
                // and set "ImportsSatisfied" to true
                part.ImportsSatisfied = true;
            }

            // Only hold conditional references for recomposable non-shared parts because we are
            // already holding strong references to the shared parts.
            if (exportedValue != null && !isSharedPart && part.Part.IsRecomposable())
            {
                PreventPartCollection(exportedValue, part.Part);
            }

            return exportedValue;
        }

        private void ReleasePart(object exportedValue, CatalogPart catalogPart, AtomicComposition atomicComposition)
        {
            ThrowIfDisposed();
            EnsureRunning();

            DisposePart(exportedValue, catalogPart, atomicComposition);
        }

        private void DisposePart(object? exportedValue, CatalogPart catalogPart, AtomicComposition? atomicComposition)
        {
            if (catalogPart == null)
            {
                throw new ArgumentNullException(nameof(catalogPart));
            }

            if (_isDisposed)
                return;

            ImportEngine? importEngine = null;
            using (_lock.LockStateForWrite())
            {
                if (_isDisposed)
                    return;

                importEngine = _importEngine;
            }
            if (importEngine != null)
            {
                importEngine.ReleaseImports(catalogPart.Part, atomicComposition);
            }
            if (exportedValue != null)
            {
                atomicComposition.AddCompleteActionAllowNull(() =>
                {
                    AllowPartCollection(exportedValue);
                });
            }

            if (catalogPart.Part is IDisposable diposablePart)
            {
                atomicComposition.AddCompleteActionAllowNull(() =>
                {
                    bool removed = false;

                    if (_isDisposed)
                        return;
                    using (_lock.LockStateForWrite())
                    {
                        if (_isDisposed)
                            return;

                        removed = _partsToDispose.Remove(diposablePart);
                    }

                    if (removed)
                    {
                        diposablePart.Dispose();
                    }
                });
            }
        }

        private void PreventPartCollection(object exportedValue, ComposablePart part)
        {
            if (exportedValue == null)
            {
                throw new ArgumentNullException(nameof(exportedValue));
            }

            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            using (_lock.LockStateForWrite())
            {
                List<ComposablePart>? partList;

                ConditionalWeakTable<object, List<ComposablePart>>? gcRoots = _gcRoots;
                if (gcRoots == null)
                {
                    gcRoots = new ConditionalWeakTable<object, List<ComposablePart>>();
                }

                if (!gcRoots.TryGetValue(exportedValue, out partList))
                {
                    partList = new List<ComposablePart>();
                    gcRoots.Add(exportedValue, partList);
                }

                partList.Add(part);

                if (_gcRoots == null)
                {
                    Thread.MemoryBarrier();
                    _gcRoots = gcRoots;
                }
            }
        }

        private void AllowPartCollection(object gcRoot)
        {
            if (_gcRoots != null)
            {
                using (_lock.LockStateForWrite())
                {
                    _gcRoots.Remove(gcRoot);
                }
            }
        }

        private bool IsRejected(ComposablePartDefinition definition, AtomicComposition? atomicComposition)
        {
            // Check to see if we're currently working on the definition in question.
            // Recursive queries always answer optimistically, as if the definition hasn't
            // been rejected - because if it is we can discard all decisions that were based
            // on the faulty assumption in the first place.
            var forceRejectionTest = false;
            if (atomicComposition != null)
            {
                AtomicCompositionQueryState state = QueryPartState(atomicComposition, definition);
                switch (state)
                {
                    case AtomicCompositionQueryState.TreatAsRejected:
                        return true;
                    case AtomicCompositionQueryState.TreatAsValidated:
                        return false;
                    case AtomicCompositionQueryState.NeedsTesting:
                        forceRejectionTest = true;
                        break;
                    default:
                        if (state != AtomicCompositionQueryState.Unknown)
                        {
                            throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                        }
                        // Need to do the work to determine the state
                        break;
                }
            }

            if (!forceRejectionTest)
            {
                // Next, anything that has been activated is not rejected
                using (_lock.LockStateForRead())
                {
                    if (_activatedParts.ContainsKey(definition))
                    {
                        return false;
                    }

                    // Last stop before doing the hard work: check a specific registry of rejected parts
                    if (_rejectedParts.Contains(definition))
                    {
                        return true;
                    }
                }
            }

            // Determine whether or not the definition's imports can be satisfied
            return DetermineRejection(definition, atomicComposition);
        }

        private bool EnsureRejection(AtomicComposition? atomicComposition)
        {
            return !(_disableSilentRejection && (atomicComposition == null));
        }

        private bool DetermineRejection(ComposablePartDefinition definition, AtomicComposition? parentAtomicComposition)
        {
            ChangeRejectedException? exception = null;

            // if there is no active atomic composition and rejection is disabled, there's no need to do any of the below
            if (!EnsureRejection(parentAtomicComposition))
            {
                return false;
            }

            using (var localAtomicComposition = new AtomicComposition(parentAtomicComposition))
            {
                // The part definition we're currently working on is treated optimistically
                // as if we know it hasn't been rejected.  This handles recursion, and if we
                // later decide that it has been rejected we'll discard all nested progress so
                // all side-effects of the mistake are erased.
                //
                // Note that this means that recursive failures that would be detected by the
                // import engine are not discovered by rejection currently.  Loops among
                // prerequisites, runaway import chains involving factories, and prerequisites
                // that cannot be fully satisfied still result in runtime errors.  Doing
                // otherwise would be possible but potentially expensive - and could be a v2
                // improvement if deemed worthwhile.
                UpdateAtomicCompositionQueryForPartEquals(localAtomicComposition,
                    definition, AtomicCompositionQueryState.TreatAsValidated);

                var newPart = definition.CreatePart();
                try
                {
                    Debug.Assert(_importEngine != null);
                    _importEngine.PreviewImports(newPart, localAtomicComposition);

                    // Reuse the partially-fleshed out part the next time we need a shared
                    // instance to keep the expense of pre-validation to a minimum.  Note that
                    // _activatedParts holds references to both shared and non-shared parts.
                    // The non-shared parts will only be used for rejection purposes only but
                    // the shared parts will be handed out when requested via GetExports as
                    // well as be used for rejection purposes.
                    localAtomicComposition.AddCompleteActionAllowNull(() =>
                    {
                        using (_lock.LockStateForWrite())
                        {
                            if (!_activatedParts.ContainsKey(definition))
                            {
                                _activatedParts.Add(definition, new CatalogPart(newPart));
                                if (newPart is IDisposable newDisposablePart)
                                {
                                    _partsToDispose.Add(newDisposablePart);
                                }
                            }
                        }
                    });

                    // Success! Complete any recursive work that was conditioned on this part's validation
                    localAtomicComposition.Complete();

                    return false;
                }
                catch (ChangeRejectedException ex)
                {
                    exception = ex;
                }
            }

            // If we've reached this point then this part has been rejected so we need to
            // record the rejection in our parent composition or execute it immediately if
            // one doesn't exist.
            parentAtomicComposition.AddCompleteActionAllowNull(() =>
            {
                using (_lock.LockStateForWrite())
                {
                    _rejectedParts.Add(definition);
                }

                CompositionTrace.PartDefinitionRejected(definition, exception);

            });
            if (parentAtomicComposition != null)
            {
                UpdateAtomicCompositionQueryForPartEquals(parentAtomicComposition,
                    definition, AtomicCompositionQueryState.TreatAsRejected);
            }

            return true;
        }

        private void UpdateRejections(IEnumerable<ExportDefinition> changedExports, AtomicComposition? atomicComposition)
        {
            using (var localAtomicComposition = new AtomicComposition(atomicComposition))
            {
                // Reconsider every part definition that has been previously
                // rejected to see if any of them can be added back.
                var affectedRejections = new HashSet<ComposablePartDefinition>();

                ComposablePartDefinition[] rejectedParts;
                using (_lock.LockStateForRead())
                {
                    rejectedParts = _rejectedParts.ToArray();
                }
                foreach (var definition in rejectedParts)
                {
                    if (QueryPartState(localAtomicComposition, definition) == AtomicCompositionQueryState.TreatAsValidated)
                    {
                        continue;
                    }

                    foreach (var import in definition.ImportDefinitions.Where(ImportEngine.IsRequiredImportForPreview))
                    {
                        if (changedExports.Any(export => import.IsConstraintSatisfiedBy(export)))
                        {
                            affectedRejections.Add(definition);
                            break;
                        }
                    }
                }
                UpdateAtomicCompositionQueryForPartInHashSet(localAtomicComposition,
                    affectedRejections, AtomicCompositionQueryState.NeedsTesting);

                // Determine if any of the resurrectable parts is now available so that we can
                // notify listeners of the exact changes to exports
                var resurrectedExports = new List<ExportDefinition>();

                foreach (var partDefinition in affectedRejections)
                {
                    if (!IsRejected(partDefinition, localAtomicComposition))
                    {
                        // Notify listeners of the newly available exports and
                        // prepare to remove the rejected part from the list of rejections
                        resurrectedExports.AddRange(partDefinition.ExportDefinitions);

                        // Capture the local so that the closure below refers to the current definition
                        // in the loop and not the value of 'partDefinition' when the closure executes
                        var capturedPartDefinition = partDefinition;
                        localAtomicComposition.AddCompleteAction(() =>
                        {
                            using (_lock.LockStateForWrite())
                            {
                                _rejectedParts.Remove(capturedPartDefinition);
                            }

                            CompositionTrace.PartDefinitionResurrected(capturedPartDefinition);
                        });
                    }
                }

                // Notify anyone sourcing exports that the resurrected exports have appeared
                if (resurrectedExports.Any())
                {
                    OnExportsChanging(
                        new ExportsChangeEventArgs(resurrectedExports, Array.Empty<ExportDefinition>(), localAtomicComposition));

                    localAtomicComposition.AddCompleteAction(() => OnExportsChanged(
                        new ExportsChangeEventArgs(resurrectedExports, Array.Empty<ExportDefinition>(), null)));
                }

                localAtomicComposition.Complete();
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

        /// <summary>
        ///  EnsureCanRun must be called from within a lock.
        /// </summary>
        [DebuggerStepThrough]
        private void EnsureCanRun()
        {
            if ((_sourceProvider == null) || (_importEngine == null))
            {
                throw new InvalidOperationException(SR.Format(SR.ObjectMustBeInitialized, "SourceProvider")); // NOLOC
            }
        }

        [DebuggerStepThrough]
        private void EnsureRunning()
        {
            if (!_isRunning)
            {
                using (_lock.LockStateForWrite())
                {
                    if (!_isRunning)
                    {
                        EnsureCanRun();
                        _isRunning = true;
                    }
                }
            }
        }

        /// <summary>
        /// EnsureCanSet{T} must be called from within a lock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentValue"></param>
        [DebuggerStepThrough]
        private void EnsureCanSet<T>(T? currentValue)
            where T : class
        {
            if ((_isRunning) || (currentValue != null))
            {
                throw new InvalidOperationException(SR.ObjectAlreadyInitialized);
            }
        }

        private AtomicCompositionQueryState QueryPartState(AtomicComposition atomicComposition, ComposablePartDefinition definition)
        {
            PartQueryStateNode? node = GetPartQueryStateNode(atomicComposition);
            if (node == null)
            {
                return AtomicCompositionQueryState.Unknown;
            }
            else
            {
                return node.GetQueryState(definition);
            }
        }

        private PartQueryStateNode? GetPartQueryStateNode(AtomicComposition atomicComposition)
        {
            atomicComposition.TryGetValue(this, out PartQueryStateNode? node);
            return node;
        }

        private void UpdateAtomicCompositionQueryForPartEquals(
            AtomicComposition atomicComposition,
            ComposablePartDefinition part,
            AtomicCompositionQueryState state)
        {
            PartQueryStateNode? previousNode = GetPartQueryStateNode(atomicComposition);
            atomicComposition.SetValue(this, new PartEqualsQueryStateNode(part, previousNode, state));
        }

        private void UpdateAtomicCompositionQueryForPartInHashSet(
            AtomicComposition atomicComposition,
            HashSet<ComposablePartDefinition> hashset,
            AtomicCompositionQueryState state)
        {
            PartQueryStateNode? previousNode = GetPartQueryStateNode(atomicComposition);
            atomicComposition.SetValue(this, new PartInHashSetQueryStateNode(hashset, previousNode, state));
        }

        private enum AtomicCompositionQueryState
        {
            Unknown,
            TreatAsRejected,
            TreatAsValidated,
            NeedsTesting
        };

        private abstract class PartQueryStateNode
        {
            private readonly PartQueryStateNode? _previousNode;
            private readonly AtomicCompositionQueryState _state;

            protected PartQueryStateNode(PartQueryStateNode? previousNode, AtomicCompositionQueryState state)
            {
                _previousNode = previousNode;
                _state = state;
            }

            protected abstract bool IsMatchingDefinition(ComposablePartDefinition part, int partHashCode);

            public AtomicCompositionQueryState GetQueryState(ComposablePartDefinition definition)
            {
                int hashCode = definition.GetHashCode();
                PartQueryStateNode? node = this;
                do
                {
                    if (node.IsMatchingDefinition(definition, hashCode))
                    {
                        return node._state;
                    }
                    node = node._previousNode;
                }
                while (node != null);

                return AtomicCompositionQueryState.Unknown;
            }
        }

        private sealed class PartEqualsQueryStateNode : PartQueryStateNode
        {
            private readonly ComposablePartDefinition _part;
            private readonly int _hashCode;
            public PartEqualsQueryStateNode(ComposablePartDefinition part, PartQueryStateNode? previousNode, AtomicCompositionQueryState state) :
                base(previousNode, state)
            {
                _part = part;
                _hashCode = part.GetHashCode();
            }

            protected override bool IsMatchingDefinition(ComposablePartDefinition part, int partHashCode)
            {
                if (partHashCode != _hashCode)
                {
                    return false;
                }
                return _part.Equals(part);
            }
        }

        private sealed class PartInHashSetQueryStateNode : PartQueryStateNode
        {
            private readonly HashSet<ComposablePartDefinition> _parts;
            public PartInHashSetQueryStateNode(HashSet<ComposablePartDefinition> parts, PartQueryStateNode? previousNode, AtomicCompositionQueryState state) :
                base(previousNode, state)
            {
                _parts = parts;
            }

            protected override bool IsMatchingDefinition(ComposablePartDefinition part, int partHashCode)
            {
                return _parts.Contains(part);
            }
        }

        private sealed class CatalogPart
        {
            private volatile bool _importsSatisfied;
            public CatalogPart(ComposablePart part)
            {
                Part = part;
            }
            public ComposablePart Part { get; private set; }

            public bool ImportsSatisfied
            {
                get
                {
                    return _importsSatisfied;
                }
                set
                {
                    _importsSatisfied = value;
                }
            }
        }
    }
}
