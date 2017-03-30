// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public class DataRowChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataRowChangeEventArgs'/> class.
        /// </summary>
        public DataRowChangeEventArgs(DataRow row, DataRowAction action)
        {
            Row = row;
            Action = action;
        }

        /// <summary>
        /// Gets the row upon which an action has occurred.
        /// </summary>
        public DataRow Row { get; }

        /// <summary>
        /// Gets the action that has occurred on a <see cref='System.Data.DataRow'/>.
        /// </summary>
        public DataRowAction Action { get; }
    }
}
