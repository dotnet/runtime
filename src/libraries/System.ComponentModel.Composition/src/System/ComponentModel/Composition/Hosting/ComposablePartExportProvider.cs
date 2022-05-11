// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public class ComposablePartExportProvider : ExportProvider, IDisposable
    {
        private List<ComposablePart> _parts = new List<ComposablePart>();
        private volatile bool _isDisposed;
        private volatile bool _isRunning;
        private readonly CompositionLock _lock;
        private ExportProvider? _sourceProvider;
        private ImportEngine? _importEngine;
        private volatile bool _currentlyComposing;
        private readonly CompositionOptions _compositionOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComposablePartExportProvider"/> class.
        /// </summary>
        public ComposablePartExportProvider() :
            this(false)
        {
        }

        public ComposablePartExportProvider(bool isThreadSafe)
            : this(isThreadSafe ? CompositionOptions.IsThreadSafe : CompositionOptions.Default)
        {
        }

        public ComposablePartExportProvider(CompositionOptions compositionOptions)
        {
            if (compositionOptions > (CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe | CompositionOptions.ExportCompositionService))
            {
                throw new ArgumentOutOfRangeException(nameof(compositionOptions));
            }

            _compositionOptions = compositionOptions;
            _lock = new CompositionLock(compositionOptions.HasFlag(CompositionOptions.IsThreadSafe));
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
                    ImportEngine? importEngine = null;
                    try
                    {
                        using (_lock.LockStateForWrite())
                        {
                            if (!_isDisposed)
                            {
                                importEngine = _importEngine;
                                _importEngine = null;

                                _sourceProvider = null;
                                _isDisposed = true;
                                disposeLock = true;
                            }
                        }
                    }
                    finally
                    {
                        if (importEngine != null)
                        {
                            importEngine.Dispose();
                        }

                        if (disposeLock)
                        {
                            _lock.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the export provider which provides the provider access to
        ///     exports.
        /// </summary>
        /// <value>
        ///     The <see cref="ExportProvider"/> which provides the
        ///     <see cref="ComposablePartExportProvider"/> access to <see cref="Export"/> objects.
        ///     The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     This property has already been set.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The methods on the <see cref="ComposablePartExportProvider"/>
        ///     have already been accessed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ComposablePartExportProvider"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     This property must be set before accessing any methods on the
        ///     <see cref="ComposablePartExportProvider"/>.
        /// </remarks>
        [DisallowNull]
        public ExportProvider? SourceProvider
        {
            get
            {
                ThrowIfDisposed();

                return _sourceProvider;
            }
            set
            {
                ThrowIfDisposed();

                Requires.NotNull(value!, nameof(value));
                using (_lock.LockStateForWrite())
                {
                    EnsureCanSet(_sourceProvider);
                    _sourceProvider = value;
                }
            }
        }

        private ImportEngine ImportEngine
        {
            get
            {
                if (_importEngine == null)
                {
                    if (_sourceProvider == null)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    ImportEngine? importEngine = new ImportEngine(_sourceProvider, _compositionOptions);
                    using (_lock.LockStateForWrite())
                    {
                        if (_importEngine == null)
                        {
                            Thread.MemoryBarrier();
                            _importEngine = importEngine;
                            importEngine = null;
                        }
                    }

                    // if we have created an engine and didn't set it because of a race condition, we need to dispose of it
                    if (importEngine != null)
                    {
                        importEngine.Dispose();
                    }
                }

                return _importEngine;
            }
        }

        /// <summary>
        /// Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">The <see cref="ImportDefinition"/> that defines the conditions of the
        /// <see cref="Export"/> to get.</param>
        /// <param name="atomicComposition">The transactional container for the composition.</param>
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
        protected override IEnumerable<Export>? GetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition)
        {
            ThrowIfDisposed();
            EnsureRunning();

            // Determine whether there is a composition atomicComposition-specific list of parts to use,
            // failing that use the usual list.  We never change the list of parts in place,
            // but rather copy, change and write a new list atomically.  Therefore all we need
            // to do here is to read the _parts member.
            List<ComposablePart>? parts = null;
            using (_lock.LockStateForRead())
            {
                parts = atomicComposition.GetValueAllowNull(this, _parts);
            }

            if (parts.Count == 0)
            {
                return null;
            }

            List<Export> exports = new List<Export>();
            foreach (var part in parts)
            {
                foreach (var exportDefinition in part.ExportDefinitions)
                {
                    if (definition.IsConstraintSatisfiedBy(exportDefinition))
                    {
                        exports.Add(CreateExport(part, exportDefinition));
                    }
                }
            }
            return exports;
        }

        public void Compose(CompositionBatch batch)
        {
            ThrowIfDisposed();
            EnsureRunning();

            Requires.NotNull(batch, nameof(batch));

            // Quick exit test can be done prior to cloning since it's just an optimization, not a
            // change in behavior
            if ((batch.PartsToAdd.Count == 0) && (batch.PartsToRemove.Count == 0))
            {
                return;
            }

            CompositionResult result = CompositionResult.SucceededResult;

            // Get updated parts list and a cloned batch
            var newParts = GetUpdatedPartsList(ref batch);

            // Allow only recursive calls from the import engine to see the changes until
            // they've been verified ...
            using (var atomicComposition = new AtomicComposition())
            {
                // Don't allow reentrant calls to compose during previewing to prevent
                // corrupted state.
                if (_currentlyComposing)
                {
                    throw new InvalidOperationException(SR.ReentrantCompose);
                }

                _currentlyComposing = true;

                try
                {
                    // In the meantime recursive calls need to be able to see the list as well
                    atomicComposition.SetValue(this, newParts);

                    // Recompose any existing imports effected by the these changes first so that
                    // adapters, resurrected parts, etc. can all play their role in satisfying
                    // imports for added parts
                    Recompose(batch, atomicComposition);

                    // Ensure that required imports can be satisfied
                    foreach (ComposablePart part in batch.PartsToAdd)
                    {
                        // collect the result of previewing all the adds in the batch
                        try
                        {
                            ImportEngine.PreviewImports(part, atomicComposition);
                        }
                        catch (ChangeRejectedException ex)
                        {
                            result = result.MergeResult(new CompositionResult(ex.Errors));
                        }
                    }

                    result.ThrowOnErrors(atomicComposition);

                    // Complete the new parts since they passed previewing.`
                    using (_lock.LockStateForWrite())
                    {
                        _parts = newParts;
                    }

                    atomicComposition.Complete();
                }
                finally
                {
                    _currentlyComposing = false;
                }
            }

            // Satisfy Imports
            // - Satisfy imports on all newly added component parts
            foreach (ComposablePart part in batch.PartsToAdd)
            {
                result = result.MergeResult(CompositionServices.TryInvoke(() =>
                    ImportEngine.SatisfyImports(part)));
            }

            // return errors
            result.ThrowOnErrors();
        }

        private List<ComposablePart> GetUpdatedPartsList(ref CompositionBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);

            // Copy the current list of parts - we are about to modify it
            // This is an OK thing to do as this is the only method that can modify the List AND Compose can
            // only be executed on one thread at a time - thus two different threads cannot tramp over each other
            List<ComposablePart>? parts = null;
            using (_lock.LockStateForRead())
            {
                parts = _parts.ToList(); // this copies the list
            }

            foreach (ComposablePart part in batch.PartsToAdd)
            {
                parts.Add(part);
            }

            List<ComposablePart>? partsToRemove = null;

            foreach (ComposablePart part in batch.PartsToRemove)
            {
                if (parts.Remove(part))
                {
                    if (partsToRemove == null)
                    {
                        partsToRemove = new List<ComposablePart>();
                    }
                    partsToRemove.Add(part);
                }
            }

            // Clone the batch, so that the external changes wouldn't happen half-way thorugh compose
            // NOTE : this does not guarantee the atomicity of cloning, which is not the goal anyway,
            // rather the fact that all subsequent calls will deal with an unchanging batch
            batch = new CompositionBatch(batch.PartsToAdd, partsToRemove);

            return parts;
        }

        private void Recompose(CompositionBatch batch, AtomicComposition atomicComposition)
        {
            ArgumentNullException.ThrowIfNull(batch);

            // Unregister any removed component parts
            foreach (ComposablePart part in batch.PartsToRemove)
            {
                ImportEngine.ReleaseImports(part, atomicComposition);
            }

            // Recompose any imports effected by the these changes (the changes are
            // observable through GetExports in the appropriate atomicComposition, thus we can fire
            // the event
            IEnumerable<ExportDefinition> addedExports = batch.PartsToAdd.Count != 0 ?
                batch.PartsToAdd.SelectMany(part => part.ExportDefinitions).ToArray() :
                Array.Empty<ExportDefinition>();

            IEnumerable<ExportDefinition> removedExports = batch.PartsToRemove.Count != 0 ?
                batch.PartsToRemove.SelectMany(part => part.ExportDefinitions).ToArray() :
                Array.Empty<ExportDefinition>();

            OnExportsChanging(
                new ExportsChangeEventArgs(addedExports, removedExports, atomicComposition));

            atomicComposition.AddCompleteAction(() => OnExportsChanged(
                new ExportsChangeEventArgs(addedExports, removedExports, null)));
        }

        private Export CreateExport(ComposablePart part, ExportDefinition export)
        {
            return new Export(export, () => GetExportedValue(part, export));
        }

        private object? GetExportedValue(ComposablePart part, ExportDefinition export)
        {
            ThrowIfDisposed();
            EnsureRunning();

            return CompositionServices.GetExportedValueFromComposedPart(ImportEngine, part, export);
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        [DebuggerStepThrough]
        private void EnsureCanRun()
        {
            if (_sourceProvider == null)
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

        [DebuggerStepThrough]
        private void EnsureCanSet<T>(T? currentValue)
            where T : class
        {
            if ((_isRunning) || (currentValue != null))
            {
                throw new InvalidOperationException(SR.ObjectAlreadyInitialized);
            }
        }
    }
}
