// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;

namespace System.Data.SqlClient
{
    public sealed class SqlRowUpdatingEventArgs : RowUpdatingEventArgs
    {
        public SqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping)
        {
        }

        new public SqlCommand Command
        {
            get { return (base.Command as SqlCommand); }
            set { base.Command = value; }
        }

        override protected IDbCommand BaseCommand
        {
            get { return base.BaseCommand; }
            set { base.BaseCommand = (value as SqlCommand); }
        }
    }
}
