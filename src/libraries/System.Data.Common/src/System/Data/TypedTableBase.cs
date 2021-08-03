// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Data
{
    /// <summary>
    /// This is the generic base class for TypedDataSet
    /// </summary>
    [Serializable]
    // This coarse suppression silences all RequiresUnreferencedCode warnings in the class.
    // https://github.com/mono/linker/issues/2136 tracks making it possible to add more granular suppressions at the member level, and with a different warning code.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "DataTable.CreateInstance's use of GetType uses only the parameterless constructor. Warnings are about serialization related constructors.")]
    public abstract class TypedTableBase<T> : DataTable, IEnumerable<T> where T : DataRow
    {

        /// <summary>
        /// Default constructor for generic TypedTableBase.
        /// Will be called by generated Typed DataSet classes and is not for public use.
        /// </summary>
        protected TypedTableBase() : base() { }

        /// <summary>
        /// Constructor for the generic TypedTableBase with takes SerializationInfo and StreamingContext.
        /// Will be called by generated Typed DataSet classes and
        /// is not for public use.
        /// </summary>
        /// <param name="info">SerializationInfo containing data to construct the object.</param>
        /// <param name="context">The streaming context for the object being deserialized.</param>
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        protected TypedTableBase(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// This property returns an enumerator of T for the TypedTable.  Note, this could
        /// execute the underlying Linq expression.
        /// </summary>
        /// <returns>IEnumerable of T.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Rows.Cast<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Casts an EnumerableDataTable_TSource into EnumerableDataTable_TResult
        /// </summary>
        public EnumerableRowCollection<TResult> Cast<TResult>()
        {
            EnumerableRowCollection<T> erc = new EnumerableRowCollection<T>(this);
            return erc.Cast<TResult>();
        }

    }
}
