// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     Provides data for the <see cref="ExportProvider.ExportsChanged"/> and
    ///     <see cref="ExportProvider.ExportsChanging"/> events.
    /// </summary>
    public class ExportsChangeEventArgs : EventArgs
    {
        private readonly IEnumerable<ExportDefinition> _addedExports;
        private readonly IEnumerable<ExportDefinition> _removedExports;
        private IEnumerable<string>? _changedContractNames;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportsChangeEventArgs"/> class with
        ///     the specified changed export definitions.
        /// </summary>
        /// <param name="addedExports">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ExportDefinition"/>s of the exports
        ///     that have been added.
        /// </param>
        /// <param name="removedExports">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ExportDefinition"/>s of the exports
        ///     that have been removed.
        /// </param>
        /// <param name="atomicComposition">
        ///     A <see cref="AtomicComposition"/> representing all tentative changes that will
        ///     be completed if the change is successful, or discarded if it is not.
        ///     <see langword="null"/> if being applied outside a <see cref="AtomicComposition"/>
        ///     or during a <see cref="ExportProvider.ExportsChanged"/> event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="addedExports"/> or <paramref name="removedExports"/> is <see langword="null"/>.
        /// </exception>
        public ExportsChangeEventArgs(IEnumerable<ExportDefinition> addedExports,
                IEnumerable<ExportDefinition> removedExports, AtomicComposition? atomicComposition)
        {
            Requires.NotNull(addedExports, nameof(addedExports));
            Requires.NotNull(removedExports, nameof(removedExports));

            _addedExports = addedExports.AsArray();
            _removedExports = removedExports.AsArray();
            AtomicComposition = atomicComposition;
        }

        /// <summary>
        ///     Gets the export definitions for the exports that have been added.
        /// </summary>
        /// <value>
        ///     A <see cref="IEnumerable{T}"/> of ExportDefinitions representing
        ///     the exports that have been added to the <see cref="CompositionContainer"/>.
        /// </value>
        public IEnumerable<ExportDefinition> AddedExports
        {
            get
            {
                Debug.Assert(_addedExports != null);

                return _addedExports;
            }
        }

        /// <summary>
        ///     Gets the export definitions for the exports that have been removed.
        /// </summary>
        /// <value>
        ///     A <see cref="IEnumerable{T}"/> of ExportDefinitions representing
        ///     the exports that have been added to the <see cref="CompositionContainer"/>.
        /// </value>
        public IEnumerable<ExportDefinition> RemovedExports
        {
            get
            {
                Debug.Assert(_removedExports != null);

                return _removedExports;
            }
        }

        /// <summary>
        ///     Gets the contract names of the exports that have changed.
        /// </summary>
        /// <value>
        ///     A <see cref="IEnumerable{T}"/> of strings representing the contract names of
        ///     the exports that have changed in the <see cref="CompositionContainer"/>.
        /// </value>
        public IEnumerable<string> ChangedContractNames =>
            _changedContractNames ??= AddedExports
                                      .Concat(RemovedExports)
                                      .Select(export => export.ContractName)
                                      .Distinct()
                                      .ToArray();

        /// <summary>
        ///     Gets the atomicComposition, if any, that this change applies to.
        /// </summary>
        /// <value>
        ///     A <see cref="AtomicComposition"/> that this set of changes applies too.
        ///
        ///     It can be <see langword="null"/> if the changes are being applied outside a
        ///     <see cref="AtomicComposition"/> or during a
        ///     <see cref="ExportProvider.ExportsChanged"/> event.
        ///
        ///     When the value is non-null it should be used to record temporary changed state
        ///     and actions that will be executed when the atomicComposition is completeed.
        /// </value>
        public AtomicComposition? AtomicComposition { get; private set; }
    }
}
