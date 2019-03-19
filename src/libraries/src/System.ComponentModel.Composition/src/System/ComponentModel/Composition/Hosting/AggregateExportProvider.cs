// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    public class AggregateExportProvider : ExportProvider , IDisposable
    {
        private readonly ReadOnlyCollection<ExportProvider> _readOnlyProviders;
        private readonly ExportProvider[] _providers;
        private volatile int _isDisposed = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateExportProvider"/> class.
        /// </summary>
        /// <param name="providers">The prioritized list of export providers.</param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="providers"/> contains an element that is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         The <see cref="AggregateExportProvider"/> will consult the providers in the order they have been specfied when 
        ///         executing <see cref="ExportProvider.GetExports(ImportDefinition,AtomicComposition)"/>. 
        ///     </para>
        ///     <para>
        ///         The <see cref="AggregateExportProvider"/> does not take ownership of the specified providers. 
        ///         That is, it will not try to dispose of any of them when it gets disposed.
        ///     </para>
        /// </remarks>
        public AggregateExportProvider(params ExportProvider[] providers) 
        {
            // NOTE : we optimize for the array case here, because the collection of providers is typically tiny
            // Arrays are much more compact to store and much faster to create and enumerate
            ExportProvider[] copiedProviders = null;
            if (providers != null)
            {
                copiedProviders = new ExportProvider[providers.Length];
                for (int i = 0; i < providers.Length; i++)
                {
                    ExportProvider provider = providers[i];
                    if (provider == null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement(nameof(providers));
                    }

                    copiedProviders[i] = provider;

                    provider.ExportsChanged += OnExportChangedInternal;
                    provider.ExportsChanging += OnExportChangingInternal;
                }
            }
            else
            {
                copiedProviders = Array.Empty<ExportProvider>();
            }

            _providers = copiedProviders;
            _readOnlyProviders = new ReadOnlyCollection<ExportProvider>(_providers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateExportProvider"/> class.
        /// </summary>
        /// <param name="providers">The prioritized list of export providers. The providers are consulted in order in which they are supplied.</param>
        /// <remarks>
        ///     <para>
        ///         The <see cref="AggregateExportProvider"/> will consult the providers in the order they have been specfied when 
        ///         executing <see cref="ExportProvider.GetExports(ImportDefinition,AtomicComposition)"/>. 
        ///     </para>
        ///     <para>
        ///         The <see cref="AggregateExportProvider"/> does not take ownership of the specified providers. 
        ///         That is, it will not try to dispose of any of them when it gets disposed.
        ///     </para>
        /// </remarks>
        public AggregateExportProvider(IEnumerable<ExportProvider> providers)
            : this((providers!= null) ? providers.AsArray() : null)
        {
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
                if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
                {
                    foreach (ExportProvider provider in _providers)
                    {
                        provider.ExportsChanged -= OnExportChangedInternal;
                        provider.ExportsChanging -= OnExportChangingInternal;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the export providers which the aggregate export provider aggregates.
        /// </summary>
        /// <value>
        ///     A <see cref="ReadOnlyCollection{T}"/> of <see cref="ExportProvider"/> objects
        ///     which the <see cref="AggregateExportProvider"/> aggregates.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="AggregateExportProvider"/> has been disposed of.
        /// </exception>
        public ReadOnlyCollection<ExportProvider> Providers
        {
            get
            {
                ThrowIfDisposed();
                Debug.Assert(_readOnlyProviders != null);

                return _readOnlyProviders;
            }
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

            if (definition.Cardinality == ImportCardinality.ZeroOrMore)
            {
                var exports = new List<Export>();
                foreach (var provider in _providers)
                {
                    foreach (var export in provider.GetExports(definition, atomicComposition))
                    {
                        exports.Add(export);
                    }
                }
                return exports;
            }
            else
            {
                IEnumerable<Export> allExports = null;

                // if asked for "one or less", the prioriry is at play - the first provider that agrees to return the value 
                // which best complies with the request, wins.
                foreach (ExportProvider provider in _providers)
                {
                    IEnumerable<Export> exports;
                    bool cardinalityCheckResult = provider.TryGetExports(definition, atomicComposition, out exports);
                    bool anyExports = exports.FastAny();
                    if (cardinalityCheckResult && anyExports)
                    {
                        // NOTE : if the provider returned nothing, we need to proceed, even if it indicated that the
                        // cardinality is correct - when asked for "one or less", the provider might - correctly - 
                        // return an empty sequence, but we shouldn't be satisfied with that as providers down the list
                        // might have a value we are interested in.
                        return exports;
                    }
                    else
                    {
                        // This is a sneaky thing that we do - if in the end no provider returns the exports with the right cardinality
                        // we simply return the aggregation of all exports they have returned. This way the end result is still not what we want
                        // but no information is lost.
                        if (anyExports)
                        {
                            allExports = (allExports != null) ? allExports.Concat(exports) : exports;
                        }
                    }
                }

                return allExports;
            }
        }

        private void OnExportChangedInternal(object sender, ExportsChangeEventArgs e)
        {
            OnExportsChanged(e);
        }

        private void OnExportChangingInternal(object sender, ExportsChangeEventArgs e)
        {
            OnExportsChanging(e);
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
