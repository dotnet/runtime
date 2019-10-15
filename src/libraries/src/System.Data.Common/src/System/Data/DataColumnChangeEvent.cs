// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public class DataColumnChangeEventArgs : EventArgs
    {
        private DataColumn _column;

        internal DataColumnChangeEventArgs(DataRow row)
        {
            Row = row;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataColumnChangeEventArgs'/> class.
        /// </summary>
        public DataColumnChangeEventArgs(DataRow row, DataColumn column, object value)
        {
            Row = row;
            _column = column;
            ProposedValue = value;
        }

        /// <summary>
        /// Gets the column whose value is changing.
        /// </summary>
        public DataColumn Column => _column;

        /// <summary>
        /// Gets the row whose value is changing.
        /// </summary>
        public DataRow Row { get; }

        /// <summary>
        /// Gets or sets the proposed value.
        /// </summary>
        public object ProposedValue { get; set; }

        internal void InitializeColumnChangeEvent(DataColumn column, object value)
        {
            _column = column;
            ProposedValue = value;
        }
    }
}
