// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    internal sealed partial class DBPropSet
    {
        private void SetLastErrorInfo(OleDbHResult lastErrorHr)
        {
            // note: OleDbHResult is actually a simple wrapper over HRESULT with OLEDB-specific codes
            UnsafeNativeMethods.IErrorInfo? errorInfo = null;
            string message = string.Empty;

            OleDbHResult errorInfoHr = UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((errorInfoHr == OleDbHResult.S_OK) && (errorInfo != null))
            {
                ODB.GetErrorDescription(errorInfo, lastErrorHr, out message);
                // note that either GetErrorInfo or GetErrorDescription might fail in which case we will have only the HRESULT value in exception message
            }
            lastErrorFromProvider = new COMException(message, (int)lastErrorHr);
        }
    }
}
