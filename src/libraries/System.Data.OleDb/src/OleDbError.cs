// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    public sealed class OleDbError
    {
        private readonly string? message;
        private readonly string? source;
        private readonly string? sqlState;
        private readonly int nativeError;

        internal OleDbError(UnsafeNativeMethods.IErrorRecords errorRecords, int index)
        {
            OleDbHResult hr;
            int lcid = System.Globalization.CultureInfo.CurrentCulture.LCID;
            UnsafeNativeMethods.IErrorInfo errorInfo = errorRecords.GetErrorInfo(index, lcid);
            if (errorInfo != null)
            {
                hr = errorInfo.GetDescription(out this.message);

                if (hr == OleDbHResult.DB_E_NOLOCALE)
                {
                    Marshal.ReleaseComObject(errorInfo);
                    lcid = SafeNativeMethods.GetUserDefaultLCID();
                    errorInfo = errorRecords.GetErrorInfo(index, lcid);

                    if (errorInfo != null)
                    {
                        hr = errorInfo.GetDescription(out this.message);
                    }
                }
                if ((hr < 0) && ADP.IsEmpty(this.message))
                {
                    this.message = ODB.FailedGetDescription(hr);
                }
                if (errorInfo != null)
                {
                    hr = errorInfo.GetSource(out this.source);

                    if (hr == OleDbHResult.DB_E_NOLOCALE)
                    {
                        Marshal.ReleaseComObject(errorInfo);
                        lcid = SafeNativeMethods.GetUserDefaultLCID();
                        errorInfo = errorRecords.GetErrorInfo(index, lcid);

                        if (errorInfo != null)
                        {
                            hr = errorInfo.GetSource(out this.source);
                        }
                    }
                    if ((hr < 0) && ADP.IsEmpty(this.source))
                    {
                        this.source = ODB.FailedGetSource(hr);
                    }
                    Marshal.ReleaseComObject(errorInfo!);
                }
            }

            UnsafeNativeMethods.ISQLErrorInfo sqlErrorInfo;
            hr = errorRecords.GetCustomErrorObject(index, ref ODB.IID_ISQLErrorInfo, out sqlErrorInfo);

            if (sqlErrorInfo != null)
            {
                this.nativeError = sqlErrorInfo.GetSQLInfo(out this.sqlState);
                Marshal.ReleaseComObject(sqlErrorInfo);
            }
        }

        public string Message
        {
            get
            {
                string? message = this.message;
                return ((message != null) ? message : string.Empty);
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
                string? source = this.source;
                return ((source != null) ? source : string.Empty);
            }
        }

        public string SQLState
        {
            get
            {
                string? sqlState = this.sqlState;
                return ((sqlState != null) ? sqlState : string.Empty);
            }
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
