// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Data
{

    /// <summary>
    /// This static class defines the DataTable extension methods.
    /// </summary>
    public static partial class DataTableExtensions
    {
        /// <summary>
        /// Creates a LinkDataView of DataRow over the input table.
        /// </summary>
        /// <param name="table">DataTable that the view is over.</param>
        /// <returns>An instance of LinkDataView.</returns>
        public static DataView AsDataView(this DataTable table)
        {
            DataSetUtil.CheckArgumentNull<DataTable>(table, nameof(table));
            return new LinqDataView(table, null);
        }

        /// <summary>
        /// Creates a LinqDataView from EnumerableDataTable
        /// </summary>
        /// <typeparam name="T">Type of the row in the table. Must inherit from DataRow</typeparam>
        /// <param name="source">The enumerable-datatable over which view must be created.</param>
        /// <returns>Generated LinkDataView of type T</returns>
        public static DataView AsDataView<T>(this EnumerableRowCollection<T> source) where T : DataRow
        {
            DataSetUtil.CheckArgumentNull<EnumerableRowCollection<T>>(source, nameof(source));
            return source.GetLinqDataView();
        }
    }
}
