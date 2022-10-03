// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class ImportEngine
    {
        /// <summary>
        ///     Used by the <see cref="ImportEngine"/> to manage the composition of a given part.
        ///     It stores things like the list of disposable exports used to satisfy the imports as
        ///     well as the caching of the exports discovered during previewing of a part.
        /// </summary>
        private sealed class PartManager
        {
            private Dictionary<ImportDefinition, List<IDisposable>>? _importedDisposableExports;
            private Dictionary<ImportDefinition, Export[]?>? _importCache;
            private string[]? _importedContractNames;
            private readonly ComposablePart _part;
            private ImportState _state = ImportState.NoImportsSatisfied;
            private readonly ImportEngine _importEngine;

            public PartManager(ImportEngine importEngine, ComposablePart part)
            {
                _importEngine = importEngine;
                _part = part;
            }

            public ComposablePart Part
            {
                get
                {
                    return _part;
                }
            }

            public ImportState State
            {
                get
                {
                    using (_importEngine._lock.LockStateForRead())
                    {
                        return _state;
                    }
                }
                set
                {
                    using (_importEngine._lock.LockStateForWrite())
                    {
                        _state = value;
                    }
                }
            }

            public bool TrackingImports { get; set; }

            public IEnumerable<string> GetImportedContractNames()
            {
                if (Part == null)
                {
                    return Enumerable.Empty<string>();
                }

                return _importedContractNames ??= Part.ImportDefinitions.Select(import => import.ContractName ?? ImportDefinition.EmptyContractName).Distinct().ToArray();
            }

            public CompositionResult TrySetImport(ImportDefinition import, Export[] exports)
            {
                try
                {
                    Part.SetImport(import, exports);
                    UpdateDisposableDependencies(import, exports);
                    return CompositionResult.SucceededResult;
                }
                catch (CompositionException ex)
                {   // Pulling on one of the exports failed

                    return new CompositionResult(
                        ErrorBuilder.CreatePartCannotSetImport(Part, import, ex));
                }
                catch (ComposablePartException ex)
                {   // Type mismatch between export and import

                    return new CompositionResult(
                        ErrorBuilder.CreatePartCannotSetImport(Part, import, ex));
                }
            }

            public void SetSavedImport(ImportDefinition import, Export[]? exports, AtomicComposition? atomicComposition)
            {
                if (atomicComposition != null)
                {
                    var savedExports = GetSavedImport(import);

                    // Add a revert action to revert the stored exports
                    // in the case that this atomicComposition gets rolled back.
                    atomicComposition.AddRevertAction(() =>
                        SetSavedImport(import, savedExports, null));
                }

                _importCache ??= new Dictionary<ImportDefinition, Export[]?>();
                _importCache[import] = exports;
            }

            public Export[]? GetSavedImport(ImportDefinition import)
            {
                Export[]? exports = null;
                // We don't care about the return value we just want the exports
                // and if it isn't present we just return the initialized null value
                _importCache?.TryGetValue(import, out exports);
                return exports;
            }

            public void ClearSavedImports()
            {
                _importCache = null;
            }

            public CompositionResult TryOnComposed()
            {
                try
                {
                    Part.Activate();
                    return CompositionResult.SucceededResult;
                }
                catch (ComposablePartException ex)
                {   // Type failed to be constructed, imports could not be set, etc
                    return new CompositionResult(
                        ErrorBuilder.CreatePartCannotActivate(Part, ex));
                }
            }

            public void UpdateDisposableDependencies(ImportDefinition import, Export[] exports)
            {
                // Determine if there are any new disposable exports, optimizing for the most
                // likely case, which is that there aren't any
                List<IDisposable>? disposableExports = null;
                foreach (var export in exports)
                {
                    if (export is IDisposable disposableExport)
                    {
                        disposableExports ??= new List<IDisposable>();
                        disposableExports.Add(disposableExport);
                    }
                }

                // Dispose any existing references previously set on this import
                List<IDisposable>? oldDisposableExports = null;
                if (_importedDisposableExports != null &&
                    _importedDisposableExports.TryGetValue(import, out oldDisposableExports))
                {
                    oldDisposableExports.ForEach(disposable => disposable.Dispose());

                    // If there aren't any replacements, get rid of the old storage
                    if (disposableExports == null)
                    {
                        _importedDisposableExports.Remove(import);
                        if (!_importedDisposableExports.Any())
                        {
                            _importedDisposableExports = null;
                        }

                        return;
                    }
                }

                // Record the new collection
                if (disposableExports != null)
                {
                    _importedDisposableExports ??= new Dictionary<ImportDefinition, List<IDisposable>>();
                    _importedDisposableExports[import] = disposableExports;
                }
            }

            public void DisposeAllDependencies()
            {
                if (_importedDisposableExports != null)
                {
                    IEnumerable<IDisposable> dependencies = _importedDisposableExports.Values
                        .SelectMany(exports => exports);

                    _importedDisposableExports = null;

                    dependencies.ForEach(disposableExport => disposableExport.Dispose());
                }
            }
        }
    }
}
