// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    public sealed class SqlRowUpdatedEventArgs : RowUpdatedEventArgs
    {
        public SqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping)
        {
        }

        new public SqlCommand Command
        {
            get
            {
                return (SqlCommand)base.Command;
            }
        }
    }
}
