// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    // This class guarantees thread-safety under the following conditions:
    // - Each composition is executed on a single thread
    // - No recomposition ever takes place
    // - The class is created with isThreadSafe=true
    public partial class ImportEngine : ICompositionService, IDisposable
    {
        private const int MaximumNumberOfCompositionIterations = 100;

        private volatile bool _isDisposed;
        private ExportProvider? _sourceProvider;
        private readonly Stack<PartManager> _recursionStateStack = new Stack<PartManager>();
        private ConditionalWeakTable<ComposablePart, PartManager> _partManagers = new ConditionalWeakTable<ComposablePart, PartManager>();
        private RecompositionManager _recompositionManager = new RecompositionManager();
        private readonly CompositionLock _lock;
        private readonly CompositionOptions _compositionOptions;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportEngine"/> class.
        /// </summary>
        /// <param name="sourceProvider">
        ///     The <see cref="ExportProvider"/> which provides the
        ///     <see cref="ImportEngine"/> access to <see cref="Export"/>s.
        /// </param>
        public ImportEngine(ExportProvider sourceProvider)
            : this(sourceProvider, CompositionOptions.Default)
        {
        }

        public ImportEngine(ExportProvider sourceProvider, bool isThreadSafe)
            : this(sourceProvider, isThreadSafe ? CompositionOptions.IsThreadSafe : CompositionOptions.Default)
        {
        }

        public ImportEngine(ExportProvider sourceProvider, CompositionOptions compositionOptions)
        {
            Requires.NotNull(sourceProvider, nameof(sourceProvider));

            _compositionOptions = compositionOptions;
            _sourceProvider = sourceProvider;
            _sourceProvider.ExportsChanging += OnExportsChanging;
            _lock = new CompositionLock(compositionOptions.HasFlag(CompositionOptions.IsThreadSafe));
        }

        /// <summary>
        ///     Previews all the required imports for the given <see cref="ComposablePart"/> to
        ///     ensure they can all be satisified. The preview does not actually set the imports
        ///     only ensures that they exist in the source provider. If the preview succeeds then
        ///     the <see cref="ImportEngine"/> also enforces that changes to exports in the source
        ///     provider will not break any of the required imports. If this enforcement needs to be
        ///     lifted for this part then <see cref="ReleaseImports"/> needs to be called for this
        ///     <see cref="ComposablePart"/>.
        /// </summary>
        /// <param name="part">
        ///     The <see cref="ComposablePart"/> to preview the required imports.
        /// </param>
        /// <param name="atomicComposition"></param>
        /// <exception cref="CompositionException">
        ///     An error occurred during previewing and <paramref name="atomicComposition"/> is null.
        ///     <see cref="CompositionException.Errors"/> will contain a collection of errors that occurred.
        ///     The pre-existing composition is in an unknown state, depending on the errors that occurred.
        /// </exception>
        /// <exception cref="ChangeRejectedException">
        ///     An error occurred during the previewing and <paramref name="atomicComposition"/> is not null.
        ///     <see cref="CompositionException.Errors"/> will contain a collection of errors that occurred.
        ///     The pre-existing composition remains in valid state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ImportEngine"/> has been disposed of.
        /// </exception>
        public void PreviewImports(ComposablePart part, AtomicComposition? atomicComposition)
        {
            ThrowIfDisposed();

            Requires.NotNull(part, nameof(part));

            // Do not do any previewing if SilentRejection is disabled.
            if (_compositionOptions.HasFlag(CompositionOptions.DisableSilentRejection))
            {
                return;
            }

            // NOTE : this is a very intricate area threading-wise, please use caution when changing, otherwise state corruption or deadlocks will ensue
            // The gist of what we are doing is as follows:
            // We need to lock the composition, as we will proceed modifying our internal state. The tricky part is when we release the lock
            // Due to the fact that some actions will take place AFTER we leave this method, we need to KEEP THAT LOCK HELD until the transation is commiited or rolled back
            // This is the reason we CAN'T use "using here.
            // Instead, if the transaction is present we will queue up the release of the lock, otherwise we will release it when we exit this method
            // We add the "release" lock to BOTH Commit and Revert queues, because they are mutually exclusive, and we need to release the lock regardless.

            // This will take the lock, if necessary
            IDisposable? compositionLockHolder = _lock.IsThreadSafe ? _lock.LockComposition() : null;
            bool compositionLockTaken = (compositionLockHolder != null);
            try
            {
                // revert actions are processed in the reverse order, so we have to add the "release lock" action now
                if (compositionLockTaken && (atomicComposition != null))
                {
                    atomicComposition.AddRevertAction(() => compositionLockHolder!.Dispose());
                }

                var partManager = GetPartManager(part, true)!;
                var result = TryPreviewImportsStateMachine(partManager, part, atomicComposition);
                result.ThrowOnErrors(atomicComposition);

                StartSatisfyingImports(partManager, atomicComposition);

                // Add the "release lock" to the commit actions
                if (compositionLockTaken && (atomicComposition != null))
                {
                    atomicComposition.AddCompleteAction(() => compositionLockHolder!.Dispose());
                }
            }
            finally
            {
                // We haven't updated the queues, so we can release the lock now
                if (compositionLockTaken && (atomicComposition == null))
                {
                    compositionLockHolder!.Dispose();
                }
            }
        }

        /// <summary>
        ///     Satisfies the imports of the specified composable part. If the satisfy succeeds then
        ///     the <see cref="ImportEngine"/> also enforces that changes to exports in the source
        ///     provider will not break any of the required imports. If this enforcement needs to be
        ///     lifted for this part then <see cref="ReleaseImports"/> needs to be called for this
        ///     <see cref="ComposablePart"/>.
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
        ///     The <see cref="ImportEngine"/> has been disposed of.
        /// </exception>
        public void SatisfyImports(ComposablePart part)
        {
            ThrowIfDisposed();

            Requires.NotNull(part, nameof(part));

            // NOTE : the following two calls use the state lock
            PartManager partManager = GetPartManager(part, true)!;
            if (partManager.State == ImportState.Composed)
            {
                return;
            }

            using (_lock.LockComposition())
            {
                var result = TrySatisfyImports(partManager, part, true);
                result.ThrowOnErrors(); // throw CompositionException not ChangeRejectedException
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

            Requires.NotNull(part, nameof(part));

            // NOTE : the following two calls use the state lock
            PartManager partManager = GetPartManager(part, true)!;
            if (partManager.State == ImportState.Composed)
            {
                return;
            }

            using (_lock.LockComposition())
            {
                var result = TrySatisfyImports(partManager, part, false);
                result.ThrowOnErrors(); // throw CompositionException not ChangeRejectedException
            }
        }

        /// <summary>
        ///     Removes any state stored in the <see cref="ImportEngine"/> for the associated
        ///     <see cref="ComposablePart"/> and releases all the <see cref="Export"/>s used to
        ///     satisfy the imports on the <see cref="ComposablePart"/>.
        ///
        ///     Also removes the enforcement for changes that would break a required import on
        ///     <paramref name="part"/>.
        /// </summary>
        /// <param name="part">
        ///     The <see cref="ComposablePart"/> to release the imports on.
        /// </param>
        /// <param name="atomicComposition">
        ///     The <see cref="AtomicComposition"/> that the release imports is running under.
        /// </param>
        public void ReleaseImports(ComposablePart part, AtomicComposition? atomicComposition)
        {
            ThrowIfDisposed();

            Requires.NotNull(part, nameof(part));

            using (_lock.LockComposition())
            {
                PartManager? partManager = GetPartManager(part, false);
                if (partManager != null)
                {
                    StopSatisfyingImports(partManager, atomicComposition);
                }
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
                    bool disposeLock = false;
                    ExportProvider? sourceProviderToUnsubscribeFrom = null;
                    using (_lock.LockStateForWrite())
                    {
                        if (!_isDisposed)
                        {
                            sourceProviderToUnsubscribeFrom = _sourceProvider;
                            _sourceProvider = null;
                            _recompositionManager = null!;
                            _partManagers = null!;
                            _isDisposed = true;
                            disposeLock = true;
                        }
                    }

                    if (sourceProviderToUnsubscribeFrom != null)
                    {
                        sourceProviderToUnsubscribeFrom.ExportsChanging -= OnExportsChanging;
                    }

                    if (disposeLock)
                    {
                        _lock.Dispose();
                    }
                }
            }
        }

        private CompositionResult TryPreviewImportsStateMachine(PartManager partManager,
            ComposablePart part, AtomicComposition? atomicComposition)
        {
            var result = CompositionResult.SucceededResult;

            if (partManager.State == ImportState.ImportsPreviewing)
            {
                // We shouldn't nomally ever hit this case but if we do
                // then we should just error with a cycle error.
                return new CompositionResult(ErrorBuilder.CreatePartCycle(part));
            }

            // Transition from NoImportsStatisified to ImportsPreviewed
            if (partManager.State == ImportState.NoImportsSatisfied)
            {
                partManager.State = ImportState.ImportsPreviewing;

                var requiredImports = part.ImportDefinitions.Where(IsRequiredImportForPreview);

                // If this atomicComposition gets rolledback for any reason we need to reset our state
                atomicComposition.AddRevertActionAllowNull(() => partManager.State = ImportState.NoImportsSatisfied);

                result = result.MergeResult(
                    TrySatisfyImportSubset(partManager, requiredImports, atomicComposition));

                if (!result.Succeeded)
                {
                    partManager.State = ImportState.NoImportsSatisfied;
                    return result;
                }

                partManager.State = ImportState.ImportsPreviewed;
            }

            return result;
        }

        private CompositionResult TrySatisfyImportsStateMachine(PartManager partManager, ComposablePart part)
        {
            var result = CompositionResult.SucceededResult;

            while (partManager.State < ImportState.Composed)
            {
                var previousState = partManager.State;

                switch (partManager.State)
                {
                    // "ed" states which represent a some sort of steady state and will
                    // attempt to do a state transition
                    case ImportState.NoImportsSatisfied:
                    case ImportState.ImportsPreviewed:
                        {
                            partManager.State = ImportState.PreExportImportsSatisfying;

                            var prereqImports = part.ImportDefinitions.Where(import => import.IsPrerequisite);
                            result = result.MergeResult(
                                TrySatisfyImportSubset(partManager, prereqImports, null));

                            partManager.State = ImportState.PreExportImportsSatisfied;
                            break;
                        }
                    case ImportState.PreExportImportsSatisfied:
                        {
                            partManager.State = ImportState.PostExportImportsSatisfying;

                            var requiredImports = part.ImportDefinitions.Where(import => !import.IsPrerequisite);

                            result = result.MergeResult(
                                TrySatisfyImportSubset(partManager, requiredImports, null));

                            partManager.State = ImportState.PostExportImportsSatisfied;
                            break;
                        }
                    case ImportState.PostExportImportsSatisfied:
                        {
                            partManager.State = ImportState.ComposedNotifying;

                            partManager.ClearSavedImports();
                            result = result.MergeResult(partManager.TryOnComposed());

                            partManager.State = ImportState.Composed;
                            break;
                        }

                    // "ing" states which represent some sort of cycle
                    // These state should always return, error or not, instead of breaking
                    case ImportState.ImportsPreviewing:
                        {
                            // We shouldn't nomally ever hit this case but if we do
                            // then we should just error with a cycle error.
                            return new CompositionResult(ErrorBuilder.CreatePartCycle(part));
                        }
                    case ImportState.PreExportImportsSatisfying:
                    case ImportState.PostExportImportsSatisfying:
                        {
                            if (InPrerequisiteLoop())
                            {
                                return result.MergeError(ErrorBuilder.CreatePartCycle(part));
                            }
                            // Cycles in post export imports are allowed so just return in that case
                            return result;
                        }
                    case ImportState.ComposedNotifying:
                        {
                            // We are currently notifying so don't notify again just return
                            return result;
                        }
                }

                // if an error occurred while doing a state transition
                if (!result.Succeeded)
                {
                    // revert to the previous state and return the error
                    partManager.State = previousState;
                    return result;
                }
            }
            return result;
        }

        private CompositionResult TrySatisfyImports(PartManager partManager, ComposablePart part, bool shouldTrackImports)
        {
            ArgumentNullException.ThrowIfNull(part);

            var result = CompositionResult.SucceededResult;

            // get out if the part is already composed
            if (partManager.State == ImportState.Composed)
            {
                return result;
            }

            // Track number of recursive iterations and throw an exception before the stack
            // fills up and debugging the root cause becomes tricky
            if (_recursionStateStack.Count >= MaximumNumberOfCompositionIterations)
            {
                return result.MergeError(
                    ErrorBuilder.ComposeTookTooManyIterations(MaximumNumberOfCompositionIterations));
            }

            // Maintain the stack to detect whether recursive loops cross prerequisites
            _recursionStateStack.Push(partManager);
            try
            {
                result = result.MergeResult(
                    TrySatisfyImportsStateMachine(partManager, part));
            }
            finally
            {
                _recursionStateStack.Pop();
            }

            if (shouldTrackImports)
            {
                StartSatisfyingImports(partManager, null);
            }

            return result;
        }

        private CompositionResult TrySatisfyImportSubset(PartManager partManager,
            IEnumerable<ImportDefinition> imports, AtomicComposition? atomicComposition)
        {
            CompositionResult result = CompositionResult.SucceededResult;

            var part = partManager.Part;
            foreach (ImportDefinition import in imports)
            {
                var exports = partManager.GetSavedImport(import);

                if (exports == null)
                {
                    CompositionResult<IEnumerable<Export>> exportsResult = TryGetExports(
                        _sourceProvider, part, import, atomicComposition);

                    if (!exportsResult.Succeeded)
                    {
                        result = result.MergeResult(exportsResult.ToResult());
                        continue;
                    }
                    exports = exportsResult.Value.AsArray();
                }

                if (atomicComposition == null)
                {
                    result = result.MergeResult(
                        partManager.TrySetImport(import, exports));
                }
                else
                {
                    partManager.SetSavedImport(import, exports, atomicComposition);
                }
            }
            return result;
        }

        private void OnExportsChanging(object? sender, ExportsChangeEventArgs e)
        {
            CompositionResult result = CompositionResult.SucceededResult;

            // Prepare for the recomposition effort by minimizing the amount of work we'll have to do later
            AtomicComposition? atomicComposition = e.AtomicComposition;

            IEnumerable<PartManager> affectedParts = _recompositionManager.GetAffectedParts(e.ChangedContractNames);

            // When in a atomicComposition account for everything that isn't yet reflected in the
            // index
            if (atomicComposition != null)
            {
                if (atomicComposition.TryGetValue(this, out EngineContext? engineContext))
                {
                    // always added the new part managers to see if they will also be
                    // affected by these changes
                    affectedParts = affectedParts.ConcatAllowingNull(engineContext!.GetAddedPartManagers())!
                        .Except(engineContext.GetRemovedPartManagers()!);
                }
            }

            var changedExports = e.AddedExports.ConcatAllowingNull(e.RemovedExports);

            foreach (var partManager in affectedParts)
            {
                result = result.MergeResult(TryRecomposeImports(partManager, changedExports!, atomicComposition));
            }

            result.ThrowOnErrors(atomicComposition);
        }

        private CompositionResult TryRecomposeImports(PartManager partManager,
            IEnumerable<ExportDefinition> changedExports, AtomicComposition? atomicComposition)
        {
            var result = CompositionResult.SucceededResult;

            switch (partManager.State)
            {
                case ImportState.ImportsPreviewed:
                case ImportState.Composed:
                    // Validate states to continue.
                    break;

                default:
                    {
                        // All other states are invalid and for recomposition.
                        return new CompositionResult(ErrorBuilder.InvalidStateForRecompposition(partManager.Part));
                    }
            }

            var affectedImports = RecompositionManager.GetAffectedImports(partManager.Part, changedExports);
            bool partComposed = (partManager.State == ImportState.Composed);

            bool recomposedImport = false;
            foreach (var import in affectedImports)
            {
                result = result.MergeResult(
                    TryRecomposeImport(partManager, partComposed, import, atomicComposition));

                recomposedImport = true;
            }

            // Knowing that the part has already been composed before and that the only possible
            // changes are to recomposable imports, we can safely go ahead and do this now or
            // schedule it for later
            if (result.Succeeded && recomposedImport && partComposed)
            {
                if (atomicComposition == null)
                {
                    result = result.MergeResult(partManager.TryOnComposed());
                }
                else
                {
                    atomicComposition.AddCompleteAction(() => partManager.TryOnComposed().ThrowOnErrors());
                }
            }

            return result;
        }

        private CompositionResult TryRecomposeImport(PartManager partManager, bool partComposed,
            ImportDefinition import, AtomicComposition? atomicComposition)
        {
            if (partComposed && !import.IsRecomposable)
            {
                return new CompositionResult(ErrorBuilder.PreventedByExistingImport(partManager.Part, import));
            }

            // During recomposition you must always requery with the new atomicComposition you cannot use any
            // cached value in the part manager
            var exportsResult = TryGetExports(_sourceProvider, partManager.Part, import, atomicComposition);
            if (!exportsResult.Succeeded)
            {
                return exportsResult.ToResult();
            }
            var exports = exportsResult.Value.AsArray();

            if (partComposed)
            {
                // Knowing that the part has already been composed before and that the only possible
                // changes are to recomposable imports, we can safely go ahead and do this now or
                // schedule it for later
                if (atomicComposition == null)
                {
                    return partManager.TrySetImport(import, exports);
                }
                else
                {
                    atomicComposition.AddCompleteAction(() => partManager.TrySetImport(import, exports).ThrowOnErrors());
                }
            }
            else
            {
                partManager.SetSavedImport(import, exports, atomicComposition);
            }

            return CompositionResult.SucceededResult;
        }

        private void StartSatisfyingImports(PartManager partManager, AtomicComposition? atomicComposition)
        {
            // When not running in a atomicCompositional state, schedule reindexing after ensuring
            // that this isn't a redundant addition
            if (atomicComposition == null)
            {
                if (!partManager.TrackingImports)
                {
                    partManager.TrackingImports = true;
                    _recompositionManager.AddPartToIndex(partManager);
                }
            }
            else
            {
                // While in a atomicCompositional state use a less efficient but effective means
                // of achieving the same results
                GetEngineContext(atomicComposition).AddPartManager(partManager);
            }
        }

        private void StopSatisfyingImports(PartManager partManager, AtomicComposition? atomicComposition)
        {
            // When not running in a atomicCompositional state, schedule reindexing after ensuring
            // that this isn't a redundant removal
            if (atomicComposition == null)
            {
                ConditionalWeakTable<ComposablePart, PartManager>? partManagers = null;
                RecompositionManager? recompositionManager = null;

                using (_lock.LockStateForRead())
                {
                    partManagers = _partManagers;
                    recompositionManager = _recompositionManager;
                }
                if (partManagers != null)                            // Disposal race may have been won by dispose
                {
                    partManagers.Remove(partManager.Part);

                    // Take care of lifetime requirements
                    partManager.DisposeAllDependencies();

                    if (partManager.TrackingImports)
                    {
                        partManager.TrackingImports = false;
                        recompositionManager.AddPartToUnindex(partManager);
                    }
                }
            }
            else
            {
                // While in a atomicCompositional state use a less efficient but effective means
                // of achieving the same results
                GetEngineContext(atomicComposition).RemovePartManager(partManager);
            }
        }

        private PartManager? GetPartManager(ComposablePart part, bool createIfNotpresent)
        {
            PartManager? partManager = null;
            using (_lock.LockStateForRead())
            {
                if (_partManagers.TryGetValue(part, out partManager))
                {
                    return partManager;
                }
            }

            if (createIfNotpresent)
            {
                using (_lock.LockStateForWrite())
                {
                    if (!_partManagers.TryGetValue(part, out partManager))
                    {
                        partManager = new PartManager(this, part);
                        _partManagers.Add(part, partManager);
                    }
                }
            }
            return partManager;
        }

        private EngineContext GetEngineContext(AtomicComposition atomicComposition)
        {
            ArgumentNullException.ThrowIfNull(atomicComposition);

            if (!atomicComposition.TryGetValue(this, true, out EngineContext? engineContext))
            {
                atomicComposition.TryGetValue(this, false, out EngineContext? parentContext);
                engineContext = new EngineContext(this, parentContext);
                atomicComposition.SetValue(this, engineContext);
                atomicComposition.AddCompleteAction(engineContext.Complete);
            }
            return engineContext!;
        }

        private bool InPrerequisiteLoop()
        {
            PartManager firstPart = _recursionStateStack.First();
            PartManager? lastPart = null;

            foreach (PartManager testPart in _recursionStateStack.Skip(1))
            {
                if (testPart.State == ImportState.PreExportImportsSatisfying)
                {
                    return true;
                }

                if (testPart == firstPart)
                {
                    lastPart = testPart;
                    break;
                }
            }

            // This should only be called when a loop has been detected - so it should always be on the stack
            if (lastPart != firstPart)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }
            return false;
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }

        private static CompositionResult<IEnumerable<Export>> TryGetExports(ExportProvider? provider,
            ComposablePart part, ImportDefinition definition, AtomicComposition? atomicComposition)
        {
            try
            {
                IEnumerable<Export>? exports = null;
                if (provider != null)
                {
                    exports = provider.GetExports(definition, atomicComposition).AsArray();
                }
                return new CompositionResult<IEnumerable<Export>>(exports!);
            }
            catch (ImportCardinalityMismatchException ex)
            {
                // Either not enough or too many exports that match the definition
                CompositionException exception = new CompositionException(ErrorBuilder.CreateImportCardinalityMismatch(ex, definition));

                return new CompositionResult<IEnumerable<Export>>(
                    ErrorBuilder.CreatePartCannotSetImport(part, definition, exception));
            }
        }

        internal static bool IsRequiredImportForPreview(ImportDefinition import)
        {
            return import.Cardinality == ImportCardinality.ExactlyOne;
        }

        // Ordering of this enum is important so be sure to use caution if you
        // try to reorder them.
        private enum ImportState
        {
            NoImportsSatisfied = 0,
            ImportsPreviewing = 1,
            ImportsPreviewed = 2,
            PreExportImportsSatisfying = 3,
            PreExportImportsSatisfied = 4,
            PostExportImportsSatisfying = 5,
            PostExportImportsSatisfied = 6,
            ComposedNotifying = 7,
            Composed = 8,
        }
    }
}
