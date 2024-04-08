// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelQuery.cs
//
// ParallelQuery is an abstract class that represents a PLINQ query.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Parallel;

namespace System.Linq
{
    /// <summary>
    /// Represents a parallel sequence.
    /// </summary>
    public class ParallelQuery : IEnumerable
    {
        // Settings that have been specified on the query so far.
        private QuerySettings _specifiedSettings;

        internal ParallelQuery(QuerySettings specifiedSettings)
        {
            _specifiedSettings = specifiedSettings;
        }

        //-----------------------------------------------------------------------------------
        // Settings that have been specified on the query so far. Some settings may still
        // be unspecified and will be replaced either by operators further in the query,
        // or filled in with defaults at query opening time.
        //

        internal QuerySettings SpecifiedQuerySettings
        {
            get { return _specifiedSettings; }
        }

        //-----------------------------------------------------------------------------------
        // Returns a parallel enumerable that represents 'this' enumerable, with each element
        // casted to TCastTo. If some element is not of type TCastTo, InvalidCastException
        // is thrown.
        //

        [ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
        internal virtual ParallelQuery<TCastTo> Cast<TCastTo>()
        {
            Debug.Fail("The derived class must override this method.");
            throw new NotSupportedException();
        }

        //-----------------------------------------------------------------------------------
        // Returns a parallel enumerable that represents 'this' enumerable, with each element
        // casted to TCastTo. Elements that are not of type TCastTo will be left out from
        // the results.
        //

        [ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
        internal virtual ParallelQuery<TCastTo> OfType<TCastTo>()
        {
            Debug.Fail("The derived class must override this method.");
            throw new NotSupportedException();
        }

        //-----------------------------------------------------------------------------------
        // Derived classes implement GetEnumeratorUntyped() instead of IEnumerable.GetEnumerator()
        // This is to avoid the method name conflict if the derived classes also implement
        // IEnumerable<T>.
        //

        [ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
        internal virtual IEnumerator GetEnumeratorUntyped()
        {
            Debug.Fail("The derived class must override this method.");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the sequence.
        /// </summary>
        /// <returns>An enumerator that iterates through the sequence.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorUntyped();
        }
    }

    /// <summary>
    /// Represents a parallel sequence.
    /// </summary>
    public class ParallelQuery<TSource> : ParallelQuery, IEnumerable<TSource>
    {
        internal ParallelQuery(QuerySettings settings)
            : base(settings)
        {
        }

        internal sealed override ParallelQuery<TCastTo> Cast<TCastTo>()
        {
            return ParallelEnumerable.Select<TSource, TCastTo>(this, elem => (TCastTo)(object)elem!);
        }

        internal sealed override ParallelQuery<TCastTo> OfType<TCastTo>()
        {
            // @PERF: Currently defined in terms of other operators. This isn't the most performant
            //      solution (because it results in two operators) but is simple to implement.
            return this
                .Where<TSource>(elem => elem is TCastTo)
                .Select<TSource, TCastTo>(elem => (TCastTo)(object)elem!);
        }

        internal override IEnumerator GetEnumeratorUntyped()
        {
            return ((IEnumerable<TSource>)this).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the sequence.
        /// </summary>
        /// <returns>An enumerator that iterates through the sequence.</returns>
        public virtual IEnumerator<TSource> GetEnumerator()
        {
            Debug.Fail("The derived class must override this method.");
            throw new NotSupportedException();
        }
    }
}
