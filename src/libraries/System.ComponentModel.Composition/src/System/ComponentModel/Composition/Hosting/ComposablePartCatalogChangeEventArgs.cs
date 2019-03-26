// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     Provides data for the <see cref="INotifyComposablePartCatalogChanged.Changed"/> and
    ///     <see cref="INotifyComposablePartCatalogChanged.Changing"/> events.
    /// </summary>
    public class ComposablePartCatalogChangeEventArgs : EventArgs
    {
        private readonly IEnumerable<ComposablePartDefinition> _addedDefinitions;
        private readonly IEnumerable<ComposablePartDefinition> _removedDefinitions;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartCatalogChangeEventArgs"/>.
        /// </summary>
        /// <param name="addedDefinitions">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ComposablePartDefinition"/> objects that 
        ///     are being added to the <see cref="ComposablePartCatalog"/>.
        /// </param>
        /// <param name="removedDefinitions">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ComposablePartDefinition"/> objects that 
        ///     are being removed from the <see cref="ComposablePartCatalog"/>.
        /// </param>
        /// <param name="atomicComposition">
        ///     A <see cref="AtomicComposition"/> representing all tentative changes that will
        ///     be completed if the change is successful, or discarded if it is not. 
        ///     <see langword="null"/> if being applied outside a <see cref="AtomicComposition"/> 
        ///     or during a <see cref="INotifyComposablePartCatalogChanged.Changed"/> event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="addedDefinitions"/> or <paramref name="removedDefinitions"/> is <see langword="null"/>.
        /// </exception>
        public ComposablePartCatalogChangeEventArgs(IEnumerable<ComposablePartDefinition> addedDefinitions,
            IEnumerable<ComposablePartDefinition> removedDefinitions, AtomicComposition atomicComposition)
        {
            Requires.NotNull(addedDefinitions, nameof(addedDefinitions));
            Requires.NotNull(removedDefinitions, nameof(removedDefinitions));

            _addedDefinitions = addedDefinitions.AsArray();
            _removedDefinitions = removedDefinitions.AsArray();
            AtomicComposition = atomicComposition;
        }

        /// <summary>
        ///     Gets the identifiers of the parts that have been added.
        /// </summary>
        /// <value>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ComposablePartDefinition"/> objects that 
        ///     have been added to the <see cref="ComposablePartCatalog"/>.
        /// </value>
        public IEnumerable<ComposablePartDefinition> AddedDefinitions 
        {
            get
            {
                Debug.Assert(_addedDefinitions != null);

                return _addedDefinitions;
            }
        }

        /// <summary>
        ///     Gets the identifiers of the parts that have been removed.
        /// </summary>
        /// <value>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="ComposablePartDefinition"/> objects that 
        ///     have been removed from the <see cref="ComposablePartCatalog"/>.
        /// </value>
        public IEnumerable<ComposablePartDefinition> RemovedDefinitions 
        {
            get
            {
                Debug.Assert(_removedDefinitions != null);

                return _removedDefinitions;
            }
        }

        /// <summary>
        ///     Gets the atomicComposition, if any, that this change applies to.
        /// </summary>
        /// <value>
        ///     A <see cref="AtomicComposition"/> that this set of changes applies too. 
        ///     It can be <see langword="null"/> if the changes are being applied outside a 
        ///     <see cref="AtomicComposition"/> or during a 
        ///     <see cref="INotifyComposablePartCatalogChanged.Changed"/> event.
        ///     
        ///     When the value is non-null it should be used to record temporary changed state
        ///     and actions that will be executed when the atomicComposition is completeed.
        /// </value>
        public AtomicComposition AtomicComposition { get; private set; }
    }
}
