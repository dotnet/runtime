// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    public sealed partial class OleDbDataAdapter
    {
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
                                IntPtr pErrorInfo;
                                UnsafeNativeMethods.GetErrorInfo(0, out pErrorInfo);

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
                IntPtr pErrorInfo;
                UnsafeNativeMethods.GetErrorInfo(0, out pErrorInfo);
                string message = string.Empty;
                throw new COMException(message, (int)hr);
            }
        }
    }
}
