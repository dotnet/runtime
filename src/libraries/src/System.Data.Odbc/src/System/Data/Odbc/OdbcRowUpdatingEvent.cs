// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;       //DbDataAdapter

namespace System.Data.Odbc
{
    /////////////////////////////////////////////////////////////////////////
    // Event Handlers
    //
    /////////////////////////////////////////////////////////////////////////
    public delegate void OdbcRowUpdatingEventHandler(object sender, OdbcRowUpdatingEventArgs e);

    public delegate void OdbcRowUpdatedEventHandler(object sender, OdbcRowUpdatedEventArgs e);

    /////////////////////////////////////////////////////////////////////////
    // OdbcRowUpdatingEventArgs
    //
    /////////////////////////////////////////////////////////////////////////
    public sealed class OdbcRowUpdatingEventArgs : RowUpdatingEventArgs
    {
        public OdbcRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping)
        {
        }

        public new OdbcCommand Command
        {
            get { return (base.Command as OdbcCommand); }
            set
            {
                base.Command = value;
            }
        }

        protected override IDbCommand BaseCommand
        {
            get { return base.BaseCommand; }
            set { base.BaseCommand = (value as OdbcCommand); }
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // OdbcRowUpdatedEventArgs
    //
    /////////////////////////////////////////////////////////////////////////
    public sealed class OdbcRowUpdatedEventArgs : RowUpdatedEventArgs
    {
        public OdbcRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping)
        {
        }

        public new OdbcCommand Command
        {
            get { return (OdbcCommand)base.Command; }
        }
    }
}
