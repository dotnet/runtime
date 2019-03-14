// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Defines the <see langword="abstract"/> base class for composable part catalogs, which produce
    ///     and return <see cref="ComposablePartDefinition"/> objects.
    /// </summary>
    /// <remarks>
    ///     This type is thread safe.
    /// </remarks>
    [DebuggerTypeProxy(typeof(ComposablePartCatalogDebuggerProxy))]
    public abstract class ComposablePartCatalog : IEnumerable<ComposablePartDefinition>, IDisposable
    {
        private bool _isDisposed;
        private volatile IQueryable<ComposablePartDefinition> _queryableParts = null;

        internal static readonly List<Tuple<ComposablePartDefinition, ExportDefinition>> _EmptyExportsList = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartCatalog"/> class.
        /// </summary>
        protected ComposablePartCatalog()
        {
        }

        /// <summary>
        ///     Gets the part definitions of the catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="IQueryable{T}"/> of <see cref="ComposablePartDefinition"/> objects of the 
        ///     <see cref="ComposablePartCatalog"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ComposablePartCatalog"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/>.
        ///     </note>
        /// </remarks>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        public virtual IQueryable<ComposablePartDefinition> Parts 
        {
            get
            {
                ThrowIfDisposed();
                if(_queryableParts == null)
                {
                    // Guarantee one time only set _queryableParts
                    var p = this.AsQueryable();
                    Interlocked.CompareExchange(ref _queryableParts, p, null);
                    if (_queryableParts == null)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                }
                return _queryableParts;
            }
        }

        /// <summary>
        ///     Returns the export definitions that match the constraint defined by the specified definition.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the 
        ///     <see cref="ExportDefinition"/> objects to return.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Tuple{T1, T2}"/> containing the 
        ///     <see cref="ExportDefinition"/> objects and their associated 
        ///     <see cref="ComposablePartDefinition"/> for objects that match the constraint defined 
        ///     by <paramref name="definition"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ComposablePartCatalog"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/>, if no 
        ///         <see cref="ExportDefinition"/> match the conditions defined by 
        ///         <paramref name="definition"/>, return an empty <see cref="IEnumerable{T}"/>.
        ///     </note>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public virtual IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
        {
            ThrowIfDisposed();

            Requires.NotNull(definition, nameof(definition));

            List<Tuple<ComposablePartDefinition, ExportDefinition>> exports = null;
            var candidateParts = GetCandidateParts(definition);
            if (candidateParts != null)
            {
                foreach (var part in candidateParts)
                {
                    Tuple<ComposablePartDefinition, ExportDefinition> singleMatch;
                    IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> multipleMatches;

                    if (part.TryGetExports(definition, out singleMatch, out multipleMatches))
                    {
                        exports = exports.FastAppendToListAllowNulls(singleMatch, multipleMatches);
                    }
                }
            }

            Debug.Assert(exports != null || _EmptyExportsList != null);
            return exports ?? _EmptyExportsList;
        }

        internal virtual IEnumerable<ComposablePartDefinition> GetCandidateParts(ImportDefinition definition)
        {
            return this;
        }

        /// <summary>
        ///     Releases the unmanaged and managed resources used by the <see cref="ComposablePartCatalog"/>. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) 
        {
            _isDisposed = true;
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }

        //
        // If neither Parts nor GetEnumerator() is overriden then return an empty list
        // If GetEnumerator is overridden this code should not be invoked:  ReferenceAssemblies mark it as Abstract or Not present
        // We verify whether Parts is overriden by seeing if the object returns matched the one cached for this instance
        // Note: a query object is only cached if Parts is invoked on a catalog which did not implement it
        //      Because reference assemblies do not expose Parts and we no longer use it, it should not get invoked by 3rd parties
        //      Because the reference assemblies mark GetEnumerator as Abstract 3rd party code should not lack an implementation
        //      That implementation should not try to call this implementation
        // Our code doies delegate to Parts in the DebuggerProxies of course.
        //
        public virtual IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            var parts = Parts;
            if(object.ReferenceEquals(parts, _queryableParts))
            {
                return Enumerable.Empty<ComposablePartDefinition>().GetEnumerator();
            }
            return parts.GetEnumerator();
        }

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
