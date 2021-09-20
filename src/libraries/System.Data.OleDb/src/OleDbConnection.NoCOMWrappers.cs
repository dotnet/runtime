// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.OleDb
{
    public sealed partial class OleDbConnection
    {
        internal static Exception? ProcessResults(OleDbHResult hresult, OleDbConnection? connection, object? src)
        {
            if ((0 <= (int)hresult) && ((null == connection) || (null == connection.Events[EventInfoMessage])))
            {
                SafeNativeMethods.Wrapper.ClearErrorInfo();
                return null;
            }

            // ErrorInfo object is to be checked regardless the hresult returned by the function called
            Exception? e = null;
            UnsafeNativeMethods.IErrorInfo? errorInfo = null;
            OleDbHResult hr = UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((OleDbHResult.S_OK == hr) && (null != errorInfo))
            {
                if (hresult < 0)
                {
                    // UNDONE: if authentication failed - throw a unique exception object type
                    //if (/*OLEDB_Error.DB_SEC_E_AUTH_FAILED*/unchecked((int)0x80040E4D) == hr) {
                    //}
                    //else if (/*OLEDB_Error.DB_E_CANCELED*/unchecked((int)0x80040E4E) == hr) {
                    //}
                    // else {
                    e = OleDbException.CreateException(errorInfo, hresult, null);
                    //}

                    if (OleDbHResult.DB_E_OBJECTOPEN == hresult)
                    {
                        e = ADP.OpenReaderExists(e);
                    }

                    ResetState(connection);
                }
                else if (null != connection)
                {
                    connection.OnInfoMessage(errorInfo, hresult);
                }
                else
                {
                }
                Marshal.ReleaseComObject(errorInfo);
            }
            else if (0 < hresult)
            {
                // @devnote: OnInfoMessage with no ErrorInfo
            }
            else if ((int)hresult < 0)
            {
                e = ODB.NoErrorInformation((null != connection) ? connection.Provider : null, hresult, null); // OleDbException

                ResetState(connection);
            }
            if (null != e)
            {
                ADP.TraceExceptionAsReturnValue(e);
            }
            return e;
        }
    }
}
