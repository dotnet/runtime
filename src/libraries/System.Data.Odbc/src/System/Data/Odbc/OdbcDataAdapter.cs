// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace System.Data.Odbc
{
    [
    DefaultEvent("RowUpdated"),
    ToolboxItem("Microsoft.VSDesigner.Data.VS.OdbcDataAdapterToolboxItem, " + AssemblyRef.MicrosoftVSDesigner), // WebData 97832
    Designer("Microsoft.VSDesigner.Data.VS.OdbcDataAdapterDesigner, " + AssemblyRef.MicrosoftVSDesigner)
    ]
    public sealed class OdbcDataAdapter : DbDataAdapter, IDbDataAdapter, ICloneable
    {
        static private readonly object s_eventRowUpdated = new object();
        static private readonly object s_eventRowUpdating = new object();

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

        [
        DefaultValue(null),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbDataAdapter_DeleteCommand),
        Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ]
        new public OdbcCommand DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = value; }
        }

        IDbCommand IDbDataAdapter.DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = (OdbcCommand)value; }
        }

        [
        DefaultValue(null),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbDataAdapter_InsertCommand),
        Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ]
        new public OdbcCommand InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = value; }
        }

        IDbCommand IDbDataAdapter.InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = (OdbcCommand)value; }
        }

        [
        DefaultValue(null),
        ResCategoryAttribute(Res.DataCategory_Fill),
        ResDescriptionAttribute(Res.DbDataAdapter_SelectCommand),
        Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ]
        new public OdbcCommand SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = value; }
        }

        IDbCommand IDbDataAdapter.SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = (OdbcCommand)value; }
        }

        [
        DefaultValue(null),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbDataAdapter_UpdateCommand),
        Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ]
        new public OdbcCommand UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = value; }
        }

        IDbCommand IDbDataAdapter.UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = (OdbcCommand)value; }
        }

        [
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbDataAdapter_RowUpdated),
        ]
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

        [
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbDataAdapter_RowUpdating),
        ]
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

        override protected RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OdbcRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        override protected RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OdbcRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        override protected void OnRowUpdated(RowUpdatedEventArgs value)
        {
            OdbcRowUpdatedEventHandler handler = (OdbcRowUpdatedEventHandler)Events[s_eventRowUpdated];
            if ((null != handler) && (value is OdbcRowUpdatedEventArgs))
            {
                handler(this, (OdbcRowUpdatedEventArgs)value);
            }
            base.OnRowUpdated(value);
        }

        override protected void OnRowUpdating(RowUpdatingEventArgs value)
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
