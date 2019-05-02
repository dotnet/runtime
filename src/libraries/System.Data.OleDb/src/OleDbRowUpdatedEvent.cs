// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace System.Data.OleDb
{
    public sealed class OleDbRowUpdatedEventArgs : RowUpdatedEventArgs
    {
        public OleDbRowUpdatedEventArgs(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(dataRow, command, statementType, tableMapping)
        {
        }

        new public OleDbCommand Command
        {
            get
            {
                return (OleDbCommand)base.Command;
            }
        }
    }
}
