// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace System.Data.OleDb
{
    public sealed class OleDbRowUpdatingEventArgs : RowUpdatingEventArgs
    {
        public OleDbRowUpdatingEventArgs(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        : base(dataRow, command, statementType, tableMapping)
        {
        }

        public new OleDbCommand? Command
        {
            get { return (base.Command as OleDbCommand); }
            set { base.Command = value; }
        }

        protected override IDbCommand? BaseCommand
        {
            get { return base.BaseCommand; }
            set { base.BaseCommand = (value as OleDbCommand); }
        }
    }
}
