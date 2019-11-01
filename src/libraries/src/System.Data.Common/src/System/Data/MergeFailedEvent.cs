// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public class MergeFailedEventArgs : EventArgs
    {
        public MergeFailedEventArgs(DataTable table, string conflict)
        {
            Table = table;
            Conflict = conflict;
        }

        /// <summary>
        /// Gets the name of the <see cref='System.Data.DataTable'/>.
        /// </summary>
        public DataTable Table { get; }

        /// <summary>
        /// Gets a description of the merge conflict.
        /// </summary>
        public string Conflict { get; }
    }
}
