// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    [Designer("Microsoft.VSDesigner.Data.VS.OleDbDataAdapterDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ToolboxItem("Microsoft.VSDesigner.Data.VS.OleDbDataAdapterToolboxItem, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class OleDbDataAdapter : DbDataAdapter, IDbDataAdapter, ICloneable
    {
        private static readonly object EventRowUpdated = new object();
        private static readonly object EventRowUpdating = new object();

        private OleDbCommand? _deleteCommand, _insertCommand, _selectCommand, _updateCommand;

        public OleDbDataAdapter() : base()
        {
            GC.SuppressFinalize(this);
        }

        public OleDbDataAdapter(OleDbCommand? selectCommand) : this()
        {
            SelectCommand = selectCommand;
        }

        public OleDbDataAdapter(string? selectCommandText, string? selectConnectionString) : this()
        {
            OleDbConnection connection = new OleDbConnection(selectConnectionString);
            SelectCommand = new OleDbCommand(selectCommandText, connection);
        }

        public OleDbDataAdapter(string? selectCommandText, OleDbConnection? selectConnection) : this()
        {
            SelectCommand = new OleDbCommand(selectCommandText, selectConnection);
        }

        private OleDbDataAdapter(OleDbDataAdapter from) : base(from)
        {
            GC.SuppressFinalize(this);
        }

        [DefaultValue(null)]
        [Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new OleDbCommand? DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = value; }
        }

        IDbCommand? IDbDataAdapter.DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = (OleDbCommand?)value; }
        }

        [DefaultValue(null)]
        [Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new OleDbCommand? InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = value; }
        }

        IDbCommand? IDbDataAdapter.InsertCommand
        {
            get { return _insertCommand; }
            set { _insertCommand = (OleDbCommand?)value; }
        }

        [DefaultValue(null)]
        [Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new OleDbCommand? SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = value; }
        }

        IDbCommand? IDbDataAdapter.SelectCommand
        {
            get { return _selectCommand; }
            set { _selectCommand = (OleDbCommand?)value; }
        }

        [DefaultValue(null)]
        [Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new OleDbCommand? UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = value; }
        }

        IDbCommand? IDbDataAdapter.UpdateCommand
        {
            get { return _updateCommand; }
            set { _updateCommand = (OleDbCommand?)value; }
        }

        public event OleDbRowUpdatedEventHandler? RowUpdated
        {
            add { Events.AddHandler(EventRowUpdated, value); }
            remove { Events.RemoveHandler(EventRowUpdated, value); }
        }

        public event OleDbRowUpdatingEventHandler? RowUpdating
        {
            add
            {
                OleDbRowUpdatingEventHandler? handler = (OleDbRowUpdatingEventHandler?)Events[EventRowUpdating];

                // prevent someone from registering two different command builders on the adapter by
                // silently removing the old one
                if ((null != handler) && (value!.Target is DbCommandBuilder))
                {
                    OleDbRowUpdatingEventHandler? d = (OleDbRowUpdatingEventHandler?)ADP.FindBuilder(handler);
                    if (null != d)
                    {
                        Events.RemoveHandler(EventRowUpdating, d);
                    }
                }
                Events.AddHandler(EventRowUpdating, value);
            }
            remove { Events.RemoveHandler(EventRowUpdating, value); }
        }

        object ICloneable.Clone()
        {
            return new OleDbDataAdapter(this);
        }

        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OleDbRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new OleDbRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        internal static void FillDataTable(OleDbDataReader dataReader, params DataTable[] dataTables)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.Fill(dataTables, dataReader, 0, 0);
        }

        public int Fill(DataTable dataTable, object ADODBRecordSet)
        {
            if (null == dataTable)
            {
                throw ADP.ArgumentNull("dataTable");
            }
            if (null == ADODBRecordSet)
            {
                throw ADP.ArgumentNull("adodb");
            }
            return FillFromADODB((object)dataTable, ADODBRecordSet, null, false);
        }

        public int Fill(DataSet dataSet, object ADODBRecordSet, string srcTable)
        {
            if (null == dataSet)
            {
                throw ADP.ArgumentNull("dataSet");
            }
            if (null == ADODBRecordSet)
            {
                throw ADP.ArgumentNull("adodb");
            }
            if (ADP.IsEmpty(srcTable))
            {
                throw ADP.FillRequiresSourceTableName("srcTable");
            }
            return FillFromADODB((object)dataSet, ADODBRecordSet, srcTable, true);
        }

        private int FillFromADODB(object data, object adodb, string? srcTable, bool multipleResults)
        {
            Debug.Assert(null != data, "FillFromADODB: null data object");
            Debug.Assert(null != adodb, "FillFromADODB: null ADODB");
            Debug.Assert(!(adodb is DataTable), "call Fill( (DataTable) value)");
            Debug.Assert(!(adodb is DataSet), "call Fill( (DataSet) value)");

            /*
            IntPtr adodbptr = ADP.PtrZero;
            try { // generate a new COM Callable Wrapper around the user object so they can't ReleaseComObject on us.
                adodbptr = Marshal.GetIUnknownForObject(adodb);
                adodb = System.Runtime.Remoting.Services.EnterpriseServicesHelper.WrapIUnknownWithComObject(adodbptr);
            }
            finally {
                if (ADP.PtrZero != adodbptr) {
                    Marshal.Release(adodbptr);
                }
            }
            */

            bool closeRecordset = multipleResults;
            UnsafeNativeMethods.ADORecordsetConstruction? recordset = (adodb as UnsafeNativeMethods.ADORecordsetConstruction);
            UnsafeNativeMethods.ADORecordConstruction? record = null;

            if (null != recordset)
            {
                if (multipleResults)
                {
                    // The NextRecordset method is not available on a disconnected Recordset object, where ActiveConnection has been set to NULL
                    object activeConnection;
                    activeConnection = ((UnsafeNativeMethods.Recordset15)adodb).get_ActiveConnection();

                    if (null == activeConnection)
                    {
                        multipleResults = false;
                    }
                }
            }
            else
            {
                record = (adodb as UnsafeNativeMethods.ADORecordConstruction);

                if (null != record)
                {
                    multipleResults = false; // IRow implies CommandBehavior.SingleRow which implies CommandBehavior.SingleResult
                }
            }
            // else throw ODB.Fill_NotADODB("adodb"); /* throw later, less code here*/

            int results = 0;
            if (null != recordset)
            {
                int resultCount = 0;
                bool incrementResultCount;
                object[] value = new object[1];

                do
                {
                    string? tmp = null;
                    if (data is DataSet)
                    {
                        tmp = GetSourceTableName(srcTable!, resultCount);
                    }
                    results += FillFromRecordset(data, recordset, tmp, out incrementResultCount);

                    if (multipleResults)
                    {
                        value[0] = DBNull.Value;

                        object recordsAffected;
                        object nextresult;
                        OleDbHResult hr = ((UnsafeNativeMethods.Recordset15)adodb).NextRecordset(out recordsAffected, out nextresult);

                        if (0 > hr)
                        {
                            // Current provider does not support returning multiple recordsets from a single execution.
                            if (ODB.ADODB_NextResultError != (int)hr)
                            {
                                UnsafeNativeMethods.IErrorInfo? errorInfo = null;
                                UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);

                                string message = string.Empty;
                                throw new COMException(message, (int)hr);
                            }
                            break;
                        }
                        adodb = nextresult;
                        if (null != adodb)
                        {
                            recordset = (UnsafeNativeMethods.ADORecordsetConstruction)adodb;

                            if (incrementResultCount)
                            {
                                resultCount++;
                            }
                            continue;
                        }
                    }
                    break;
                } while (null != recordset);

                if ((null != recordset) && (closeRecordset || (null == adodb)))
                {
                    FillClose(true, recordset);
                }
            }
            else if (null != record)
            {
                results = FillFromRecord(data, record, srcTable!);
                if (closeRecordset)
                {
                    FillClose(false, record);
                }
            }
            else
            {
                throw ODB.Fill_NotADODB("adodb");
            }
            return results;
        }

        //protected override int Fill(DataTable dataTable, IDataReader dataReader) {
        //    return base.Fill(dataTable, dataReader);
        //}

        private int FillFromRecordset(object data, UnsafeNativeMethods.ADORecordsetConstruction recordset, string? srcTable, out bool incrementResultCount)
        {
            incrementResultCount = false;

            IntPtr chapter; /*ODB.DB_NULL_HCHAPTER*/
            object? result = null;
            try
            {
                result = recordset.get_Rowset();
                chapter = recordset.get_Chapter();
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                throw ODB.Fill_EmptyRecordSet("ADODBRecordSet", e);
            }

            if (null != result)
            {
                CommandBehavior behavior = (MissingSchemaAction.AddWithKey != MissingSchemaAction) ? 0 : CommandBehavior.KeyInfo;
                behavior |= CommandBehavior.SequentialAccess;

                OleDbDataReader? dataReader = null;
                try
                {
                    // intialized with chapter only since we don't want ReleaseChapter called for this chapter handle
                    ChapterHandle chapterHandle = ChapterHandle.CreateChapterHandle(chapter);

                    dataReader = new OleDbDataReader(null, null, 0, behavior);
                    dataReader.InitializeIRowset(result, chapterHandle, ADP.RecordsUnaffected);
                    dataReader.BuildMetaInfo();

                    incrementResultCount = (0 < dataReader.FieldCount);
                    if (incrementResultCount)
                    {
                        if (data is DataTable)
                        {
                            return base.Fill((DataTable)data, dataReader);
                        }
                        else
                        {
                            return base.Fill((DataSet)data, srcTable!, dataReader, 0, 0);
                        }
                    }
                }
                finally
                {
                    if (null != dataReader)
                    {
                        dataReader.Close();
                    }
                }
            }
            return 0;
        }

        private int FillFromRecord(object data, UnsafeNativeMethods.ADORecordConstruction record, string srcTable)
        {
            object? result = null;
            try
            {
                result = record.get_Row();
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                throw ODB.Fill_EmptyRecord("adodb", e);
            }

            if (null != result)
            {
                CommandBehavior behavior = (MissingSchemaAction.AddWithKey != MissingSchemaAction) ? 0 : CommandBehavior.KeyInfo;
                behavior |= CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

                OleDbDataReader? dataReader = null;
                try
                {
                    dataReader = new OleDbDataReader(null, null, 0, behavior);
                    dataReader.InitializeIRow(result, ADP.RecordsUnaffected);
                    dataReader.BuildMetaInfo();

                    if (data is DataTable)
                    {
                        return base.Fill((DataTable)data, dataReader);
                    }
                    else
                    {
                        return base.Fill((DataSet)data, srcTable, dataReader, 0, 0);
                    }
                }
                finally
                {
                    if (null != dataReader)
                    {
                        dataReader.Close();
                    }
                }
            }
            return 0;
        }

        private void FillClose(bool isrecordset, object value)
        {
            OleDbHResult hr;
            if (isrecordset)
            {
                hr = ((UnsafeNativeMethods.Recordset15)value).Close();
            }
            else
            {
                hr = ((UnsafeNativeMethods._ADORecord)value).Close();
            }
            if ((0 < (int)hr) && (ODB.ADODB_AlreadyClosedError != (int)hr))
            {
                UnsafeNativeMethods.IErrorInfo? errorInfo = null;
                UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);
                string message = string.Empty;
                throw new COMException(message, (int)hr);
            }
        }

        protected override void OnRowUpdated(RowUpdatedEventArgs value)
        {
            OleDbRowUpdatedEventHandler? handler = (OleDbRowUpdatedEventHandler?)Events[EventRowUpdated];
            if ((null != handler) && (value is OleDbRowUpdatedEventArgs))
            {
                handler(this, (OleDbRowUpdatedEventArgs)value);
            }
            base.OnRowUpdated(value);
        }

        protected override void OnRowUpdating(RowUpdatingEventArgs value)
        {
            OleDbRowUpdatingEventHandler? handler = (OleDbRowUpdatingEventHandler?)Events[EventRowUpdating];
            if ((null != handler) && (value is OleDbRowUpdatingEventArgs))
            {
                handler(this, (OleDbRowUpdatingEventArgs)value);
            }
            base.OnRowUpdating(value);
        }

        private static string GetSourceTableName(string srcTable, int index)
        {
            //if ((null != srcTable) && (0 <= index) && (index < srcTable.Length)) {
            if (0 == index)
            {
                return srcTable; //[index];
            }
            return srcTable + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
