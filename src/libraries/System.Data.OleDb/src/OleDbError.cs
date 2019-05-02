// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    public sealed class OleDbError
    {
        readonly private string message;
        readonly private string source;
        readonly private string sqlState;
        readonly private int nativeError;

        internal OleDbError(UnsafeNativeMethods.IErrorRecords errorRecords, int index)
        {
            OleDbHResult hr;
            int lcid = System.Globalization.CultureInfo.CurrentCulture.LCID;
            UnsafeNativeMethods.IErrorInfo errorInfo = errorRecords.GetErrorInfo(index, lcid);
            if (null != errorInfo)
            {
                hr = errorInfo.GetDescription(out this.message);

                if (OleDbHResult.DB_E_NOLOCALE == hr)
                {
                    Marshal.ReleaseComObject(errorInfo);
                    lcid = SafeNativeMethods.GetUserDefaultLCID();
                    errorInfo = errorRecords.GetErrorInfo(index, lcid);

                    if (null != errorInfo)
                    {
                        hr = errorInfo.GetDescription(out this.message);
                    }
                }
                if ((hr < 0) && ADP.IsEmpty(this.message))
                {
                    this.message = ODB.FailedGetDescription(hr);
                }
                if (null != errorInfo)
                {
                    hr = errorInfo.GetSource(out this.source);

                    if (OleDbHResult.DB_E_NOLOCALE == hr)
                    {
                        Marshal.ReleaseComObject(errorInfo);
                        lcid = SafeNativeMethods.GetUserDefaultLCID();
                        errorInfo = errorRecords.GetErrorInfo(index, lcid);

                        if (null != errorInfo)
                        {
                            hr = errorInfo.GetSource(out this.source);
                        }
                    }
                    if ((hr < 0) && ADP.IsEmpty(this.source))
                    {
                        this.source = ODB.FailedGetSource(hr);
                    }
                    Marshal.ReleaseComObject(errorInfo);
                }
            }

            UnsafeNativeMethods.ISQLErrorInfo sqlErrorInfo;
            hr = errorRecords.GetCustomErrorObject(index, ref ODB.IID_ISQLErrorInfo, out sqlErrorInfo);

            if (null != sqlErrorInfo)
            {
                this.nativeError = sqlErrorInfo.GetSQLInfo(out this.sqlState);
                Marshal.ReleaseComObject(sqlErrorInfo);
            }
        }

        public string Message
        {
            get
            {
                string message = this.message;
                return ((null != message) ? message : string.Empty);
            }
        }

        public int NativeError
        {
            get
            {
                return this.nativeError;
            }
        }

        public string Source
        {
            get
            {
                string source = this.source;
                return ((null != source) ? source : string.Empty);
            }
        }

        public string SQLState
        {
            get
            {
                string sqlState = this.sqlState;
                return ((null != sqlState) ? sqlState : string.Empty);
            }
        }

        override public string ToString()
        {
            return Message;
        }
    }
}
