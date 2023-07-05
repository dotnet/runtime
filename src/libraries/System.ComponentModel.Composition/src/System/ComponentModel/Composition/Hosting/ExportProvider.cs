// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     Defines the <see langword="abstract"/> base class for export providers, which provide
    ///     methods for retrieving <see cref="Export"/> objects.
    /// </summary>
    public abstract partial class ExportProvider
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportProvider"/> class.
        /// </summary>
        protected ExportProvider()
        {
        }

        /// <summary>
        ///     Occurs when the exports in the <see cref="ExportProvider"/> have changed.
        /// </summary>
        public event EventHandler<ExportsChangeEventArgs>? ExportsChanged;

        /// <summary>
        ///     Occurs when the exports in the <see cref="ExportProvider"/> are changing.
        /// </summary>
        public event EventHandler<ExportsChangeEventArgs>? ExportsChanging;

        /// <summary>
        ///     Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the
        ///     <see cref="Export"/> objects to get.
        /// </param>
        /// <result>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Export"/> objects that match
        ///     the conditions defined by <see cref="ImportDefinition"/>, if found; otherwise, an
        ///     empty <see cref="IEnumerable{T}"/>.
        /// </result>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ExactlyOne"/> and
        ///         there are zero <see cref="Export"/> objects that match the conditions of the specified
        ///         <see cref="ImportDefinition"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ZeroOrOne"/> or
        ///         <see cref="ImportCardinality.ExactlyOne"/> and there are more than one <see cref="Export"/>
        ///         objects that match the conditions of the specified <see cref="ImportDefinition"/>.
        ///     </para>
        /// </exception>
        public IEnumerable<Export> GetExports(ImportDefinition definition)
        {
            return GetExports(definition, null);
        }

        /// <summary>
        ///     Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the
        ///     <see cref="Export"/> objects to get.
        /// </param>
        /// <param name="atomicComposition">The transactional container for the composition.</param>
        /// <result>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Export"/> objects that match
        ///     the conditions defined by <see cref="ImportDefinition"/>, if found; otherwise, an
        ///     empty <see cref="IEnumerable{T}"/>.
        /// </result>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ExactlyOne"/> and
        ///         there are zero <see cref="Export"/> objects that match the conditions of the specified
        ///         <see cref="ImportDefinition"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ZeroOrOne"/> or
        ///         <see cref="ImportCardinality.ExactlyOne"/> and there are more than one <see cref="Export"/>
        ///         objects that match the conditions of the specified <see cref="ImportDefinition"/>.
        ///     </para>
        /// </exception>
        public IEnumerable<Export> GetExports(ImportDefinition definition, AtomicComposition? atomicComposition)
        {
            Requires.NotNull(definition, nameof(definition));

            ExportCardinalityCheckResult result = TryGetExportsCore(definition, atomicComposition, out IEnumerable<Export>? exports);
            switch (result)
            {
                case ExportCardinalityCheckResult.Match:
                    Debug.Assert(exports != null);
                    return exports;
                case ExportCardinalityCheckResult.NoExports:
                    throw new ImportCardinalityMismatchException(SR.Format(SR.CardinalityMismatch_NoExports, definition));
                default:
                    if (result != ExportCardinalityCheckResult.TooManyExports)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    throw new ImportCardinalityMismatchException(SR.Format(SR.CardinalityMismatch_TooManyExports_Constraint, definition));
            }
        }

        /// <summary>
        ///     Returns all exports that match the conditions of the specified import.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the
        ///     <see cref="Export"/> objects to get.
        /// </param>
        /// <param name="atomicComposition">The transactional container for the composition.</param>
        /// <param name="exports">
        ///     When this method returns, contains an <see cref="IEnumerable{T}"/> of <see cref="Export"/>
        ///     objects that match the conditions defined by <see cref="ImportDefinition"/>, if found;
        ///     otherwise, an empty <see cref="IEnumerable{T}"/>.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if <see cref="ImportDefinition.Cardinality"/> is
        ///     <see cref="ImportCardinality.ZeroOrOne"/> or <see cref="ImportCardinality.ZeroOrMore"/> and
        ///     there are zero <see cref="Export"/> objects that match the conditions of the specified
        ///     <see cref="ImportDefinition"/>. <see langword="true"/> if
        ///     <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ZeroOrOne"/> or
        ///     <see cref="ImportCardinality.ExactlyOne"/> and there is exactly one <see cref="Export"/>
        ///     that matches the conditions of the specified <see cref="ImportDefinition"/>; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetExports(ImportDefinition definition, AtomicComposition? atomicComposition, out IEnumerable<Export>? exports)
        {
            Requires.NotNull(definition, nameof(definition));

            ExportCardinalityCheckResult result = TryGetExportsCore(definition, atomicComposition, out exports);
            return (result == ExportCardinalityCheckResult.Match);
        }

        /// <summary>
        ///     Returns all exports that match the constraint defined by the specified definition.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the
        ///     <see cref="Export"/> objects to return.
        /// </param>
        /// <param name="atomicComposition">The transactional container for the composition.</param>
        /// <result>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Export"/> objects that match
        ///     the conditions defined by <see cref="ImportDefinition"/>, if found; otherwise, an
        ///     empty <see cref="IEnumerable{T}"/>.
        /// </result>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this method should not treat cardinality-related mismatches
        ///         as errors, and should not throw exceptions in those cases. For instance,
        ///         if <see cref="ImportDefinition.Cardinality"/> is <see cref="ImportCardinality.ExactlyOne"/>
        ///         and there are zero <see cref="Export"/> objects that match the conditions of the
        ///         specified <see cref="ImportDefinition"/>, an <see cref="IEnumerable{T}"/> should be returned.
        ///     </note>
        /// </remarks>
        protected abstract IEnumerable<Export>? GetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition);

        /// <summary>
        ///     Raises the <see cref="ExportsChanged"/> event.
        /// </summary>
        /// <param name="e">
        ///     An <see cref="ExportsChangeEventArgs"/> containing the data for the event.
        /// </param>
        protected virtual void OnExportsChanged(ExportsChangeEventArgs e)
        {
            EventHandler<ExportsChangeEventArgs>? changedEvent = ExportsChanged;
            if (changedEvent != null)
            {
                CompositionResult result = CompositionServices.TryFire(changedEvent, this, e);
                result.ThrowOnErrors(e.AtomicComposition);
            }
        }

        /// <summary>
        ///     Raises the <see cref="ExportsChanging"/> event.
        /// </summary>
        /// <param name="e">
        ///     An <see cref="ExportsChangeEventArgs"/> containing the data for the event.
        /// </param>
        protected virtual void OnExportsChanging(ExportsChangeEventArgs e)
        {
            EventHandler<ExportsChangeEventArgs>? changingEvent = ExportsChanging;
            if (changingEvent != null)
            {
                CompositionResult result = CompositionServices.TryFire(changingEvent, this, e);
                result.ThrowOnErrors(e.AtomicComposition);
            }
        }

        private ExportCardinalityCheckResult TryGetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition, out IEnumerable<Export>? exports)
        {
            ArgumentNullException.ThrowIfNull(definition);

            exports = GetExportsCore(definition, atomicComposition);

            var checkResult = ExportServices.CheckCardinality(definition, exports);

            // Export providers treat >1 match as zero for cardinality 0-1 imports
            // If this policy is moved we need to revisit the assumption that the
            // ImportEngine made during previewing the only required imports to
            // now also preview optional imports.
            if (checkResult == ExportCardinalityCheckResult.TooManyExports &&
                definition.Cardinality == ImportCardinality.ZeroOrOne)
            {
                checkResult = ExportCardinalityCheckResult.Match;
                exports = null;
            }

            exports ??= Array.Empty<Export>();

            return checkResult;
        }
    }
}
