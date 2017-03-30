// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    public sealed class SqlDataAdapter : DbDataAdapter, IDbDataAdapter, ICloneable
    {
        private static readonly object EventRowUpdated = new object();
        private static readonly object EventRowUpdating = new object();

        private SqlCommand _deleteCommand, _insertCommand, _selectCommand, _updateCommand;

        private int _updateBatchSize = 1;

        public SqlDataAdapter() : base()
        {
            GC.SuppressFinalize(this);
        }

        public SqlDataAdapter(SqlCommand selectCommand) : this()
        {
            SelectCommand = selectCommand;
        }

        public SqlDataAdapter(string selectCommandText, string selectConnectionString) : this()
        {
            SqlConnection connection = new SqlConnection(selectConnectionString);
            SelectCommand = new SqlCommand(selectCommandText, connection);
        }

        public SqlDataAdapter(string selectCommandText, SqlConnection selectConnection) : this()
        {
            SelectCommand = new SqlCommand(selectCommandText, selectConnection);
        }

        private SqlDataAdapter(SqlDataAdapter from) : base(from)
        {   // Clone
            GC.SuppressFinalize(this);
        }

        new public SqlCommand DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = value; }
        }

        IDbCommand IDbDataAdapter.DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = (SqlCommand)value; }
        }

        new public SqlCommand InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = value; }
        }

        IDbCommand IDbDataAdapter.InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = (SqlCommand)value; }
        }

        new public SqlCommand SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = value; }
        }

        IDbCommand IDbDataAdapter.SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = (SqlCommand)value; }
        }

        override public int UpdateBatchSize
        {
            get
            {
                return _updateBatchSize;
            }
            set
            {
                if (0 > value)
                {
                    throw ADP.ArgumentOutOfRange(nameof(UpdateBatchSize));
                }
                _updateBatchSize = value;
            }
        }

        new public SqlCommand UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = value; }
        }

        IDbCommand IDbDataAdapter.UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = (SqlCommand)value; }
        }

        public event SqlRowUpdatedEventHandler RowUpdated
        {
            add
            {
                Events.AddHandler(EventRowUpdated, value);
            }
            remove
            {
                Events.RemoveHandler(EventRowUpdated, value);
            }
        }

        public event SqlRowUpdatingEventHandler RowUpdating
        {
            add
            {
                SqlRowUpdatingEventHandler handler = (SqlRowUpdatingEventHandler)Events[EventRowUpdating];

                // Prevent someone from registering two different command builders on the adapter by
                // silently removing the old one.
                if ((null != handler) && (value.Target is DbCommandBuilder))
                {
                    SqlRowUpdatingEventHandler d = (SqlRowUpdatingEventHandler)ADP.FindBuilder(handler);
                    if (null != d)
                    {
                        Events.RemoveHandler(EventRowUpdating, d);
                    }
                }
                Events.AddHandler(EventRowUpdating, value);
            }
            remove
            {
                Events.RemoveHandler(EventRowUpdating, value);
            }
        }

        object ICloneable.Clone()
        {
            return new SqlDataAdapter(this);
        }

        override protected void OnRowUpdated(RowUpdatedEventArgs value)
        {
            SqlRowUpdatedEventHandler handler = (SqlRowUpdatedEventHandler)Events[EventRowUpdated];
            if ((null != handler) && (value is SqlRowUpdatedEventArgs))
            {
                handler(this, (SqlRowUpdatedEventArgs)value);
            }
            base.OnRowUpdated(value);
        }

        override protected void OnRowUpdating(RowUpdatingEventArgs value)
        {
            SqlRowUpdatingEventHandler handler = (SqlRowUpdatingEventHandler)Events[EventRowUpdating];
            if ((null != handler) && (value is SqlRowUpdatingEventArgs))
            {
                handler(this, (SqlRowUpdatingEventArgs)value);
            }
            base.OnRowUpdating(value);
        }
    }
}
