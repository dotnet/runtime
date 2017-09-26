// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Data
{

    /// <summary>
    /// This class provides a wrapper for DataTables representing an ordered sequence.
    /// </summary>
    public sealed class OrderedEnumerableRowCollection<TRow> : EnumerableRowCollection<TRow>
    {
        /// <summary>
        /// Copy Constructor that sets enumerableRows to the one given in the input
        /// </summary>
        internal OrderedEnumerableRowCollection(EnumerableRowCollection<TRow> enumerableTable, IEnumerable<TRow> enumerableRows)
            : base(enumerableTable, enumerableRows, null)
        {

        }
    }
}