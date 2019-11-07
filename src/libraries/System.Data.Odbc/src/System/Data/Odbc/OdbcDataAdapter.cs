// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace System.Data.Odbc
{
    public sealed class OdbcDataAdapter : DbDataAdapter, IDbDataAdapter, ICloneable
    {
        private static readonly object s_eventRowUpdated = new object();
        private static readonly object s_eventRowUpdating = new object();

        private OdbcCommand _deleteCommand, _insertCommand, _selectCommand, _updateCommand;

        public OdbcDataAdapter() : base()
        {
            GC.SuppressFinalize(this);
        }

        public OdbcDataAdapter(OdbcCommand selectCommand) : this()
        {
            SelectCommand = selectCommand;
        }

        public OdbcDataAdapter(string selectCommandText, OdbcConnection selectConnection) : this()
        {
            SelectCommand = new OdbcCommand(selectCommandText, selectConnection);
        }

        public OdbcDataAdapter(string selectCommandText, string selectConnectionString) : this()
        {
            OdbcConnection connection = new OdbcConnection(selectConnectionString);
            SelectCommand = new OdbcCommand(selectCommandText, connection);
        }

        private OdbcDataAdapter(OdbcDataAdapter from) : base(from)
        {
            GC.SuppressFinalize(this);
        }

        public new OdbcCommand DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = value; }
        }

        IDbCommand IDbDataAdapter.DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = (OdbcCommand)value; }
        }

        public new OdbcCommand InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = value; }
        }

        IDbCommand IDbDataAdapter.InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = (OdbcCommand)value; }
        }

        public new OdbcCommand SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = value; }
        }

        IDbCommand IDbDataAdapter.SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = (OdbcCommand)value; }
        }

        public new OdbcCommand UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = value; }
        }

        IDbCommand IDbDataAdapter.UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = (OdbcCommand)value; }
        }

        public event OdbcRowUpdatedEventHandler RowUpdated
        {
            add
            {
                Events.AddHandler(s_eventRowUpdated, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventRowUpdated, value);
            }
        }

        public event OdbcRowUpdatingEventHandler RowUpdating
        {
            add
            {
                OdbcRowUpdatingEventHandler handler = (OdbcRowUpdatingEventHandler)Events[s_eventRowUpdating];

                // MDAC 58177, 64513
                // prevent someone from registering two different command builders on the adapter by
                // silently removing the old one
                if ((null != handler) && (value.Target is OdbcCommandBuilder))
                {
                    OdbcRowUpdatingEventHandler d = (OdbcRowUpdatingEventHandler)ADP.FindBuilder(handler);
                    if (null != d)
                    {
                        Events.RemoveHandler(s_eventRowUpdating, d);
                    }
                }
                Events.AddHandler(s_eventRowUpdating, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventRowUpdating, value);
            }
        }


        object ICloneable.Clone()
        {
            return new OdbcDataAdapter(this);
        }

        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OdbcRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OdbcRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override void OnRowUpdated(RowUpdatedEventArgs value)
        {
            OdbcRowUpdatedEventHandler handler = (OdbcRowUpdatedEventHandler)Events[s_eventRowUpdated];
            if ((null != handler) && (value is OdbcRowUpdatedEventArgs))
            {
                handler(this, (OdbcRowUpdatedEventArgs)value);
            }
            base.OnRowUpdated(value);
        }

        protected override void OnRowUpdating(RowUpdatingEventArgs value)
        {
            OdbcRowUpdatingEventHandler handler = (OdbcRowUpdatingEventHandler)Events[s_eventRowUpdating];
            if ((null != handler) && (value is OdbcRowUpdatingEventArgs))
            {
                handler(this, (OdbcRowUpdatingEventArgs)value);
            }
            base.OnRowUpdating(value);
        }
    }
}
