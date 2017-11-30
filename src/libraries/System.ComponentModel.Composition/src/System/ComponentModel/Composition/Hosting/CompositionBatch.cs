// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.Contracts;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CompositionBatch
    {
        private object _lock = new object();
        private bool _copyNeededForAdd;
        private bool _copyNeededForRemove;
        private List<ComposablePart> _partsToAdd;
        private ReadOnlyCollection<ComposablePart> _readOnlyPartsToAdd;
        private List<ComposablePart> _partsToRemove;
        private ReadOnlyCollection<ComposablePart> _readOnlyPartsToRemove;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionBatch"/> class.
        /// </summary>
        public CompositionBatch() : 
            this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionBatch"/> class.
        /// </summary>
        /// <param name="partsToAdd">The parts to add.</param>
        /// <param name="partsToRemove">The parts to remove.</param>
        public CompositionBatch(IEnumerable<ComposablePart> partsToAdd, IEnumerable<ComposablePart> partsToRemove)
        {
            _partsToAdd = new List<ComposablePart>();
            if (partsToAdd != null)
            {
                foreach (var part in partsToAdd)
                {
                    if (part == null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement("partsToAdd");
                    }
                    _partsToAdd.Add(part);
                }
            }
            _readOnlyPartsToAdd = _partsToAdd.AsReadOnly();

            _partsToRemove = new List<ComposablePart>();
            if (partsToRemove != null)
            {
                foreach (var part in partsToRemove)
                {
                    if (part == null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement("partsToRemove");
                    }
                    _partsToRemove.Add(part);
                }
            }
            _readOnlyPartsToRemove = _partsToRemove.AsReadOnly();
        }

        /// <summary>
        /// Returns the collection of parts that will be added.
        /// </summary>
        /// <value>The parts to be added.</value>
        public ReadOnlyCollection<ComposablePart> PartsToAdd
        {
            get
            {
                Contract.Ensures(Contract.Result<ReadOnlyCollection<ComposablePart>>() != null);

                lock (_lock)
                {
                    _copyNeededForAdd = true;
                    return _readOnlyPartsToAdd;
                }
            }
        }

        /// <summary>
        /// Returns the collection of parts that will be removed.
        /// </summary>
        /// <value>The parts to be removed.</value>
        public ReadOnlyCollection<ComposablePart> PartsToRemove
        {
            get
            {
                Contract.Ensures(Contract.Result <ReadOnlyCollection<ComposablePart>>() != null);

                lock (_lock)
                {
                    _copyNeededForRemove = true;
                    return _readOnlyPartsToRemove;
                }
            }
        }

        /// <summary>
        ///     Adds the specified part to the <see cref="CompositionBatch"/>.
        /// </summary>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="part"/> is <see langword="null"/>.
        /// </exception>
        public void AddPart(ComposablePart part)
        {
            Requires.NotNull(part, nameof(part));
            lock (_lock)
            {
                if (_copyNeededForAdd)
                {
                    _partsToAdd = new List<ComposablePart>(_partsToAdd);
                    _readOnlyPartsToAdd = _partsToAdd.AsReadOnly();
                    _copyNeededForAdd = false;
                }
                _partsToAdd.Add(part);
            }
        }

        /// <summary>
        ///     Removes the specified part from the <see cref="CompositionBatch"/>.
        /// </summary>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="part"/> is <see langword="null"/>.
        /// </exception>
        public void RemovePart(ComposablePart part)
        {
            Requires.NotNull(part, nameof(part));
            lock (_lock)
            {
                if (_copyNeededForRemove)
                {
                    _partsToRemove = new List<ComposablePart>(_partsToRemove);
                    _readOnlyPartsToRemove = _partsToRemove.AsReadOnly();
                    _copyNeededForRemove = false;
                }
                _partsToRemove.Add(part);
            }
        }

        /// <summary>
        ///     Adds the specified export to the <see cref="CompositionBatch"/>.
        /// </summary>
        /// <param name="export">
        ///     The <see cref="Export"/> to add to the <see cref="CompositionBatch"/>.
        /// </param>
        /// <returns>
        ///     A <see cref="ComposablePart"/> that can be used remove the <see cref="Export"/>
        ///     from the <see cref="CompositionBatch"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="export"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// </remarks>
        public ComposablePart AddExport(Export export)
        {
            Requires.NotNull(export, nameof(export));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            ComposablePart part = new SingleExportComposablePart(export);

            AddPart(part);

            return part;
        }
    }
}
