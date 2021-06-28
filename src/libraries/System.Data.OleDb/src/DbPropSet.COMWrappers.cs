// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            string message = string.Empty;
            IntPtr pErrorInfo;
            OleDbHResult errorInfoHr = UnsafeNativeMethods.GetErrorInfo(0, out pErrorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((errorInfoHr == OleDbHResult.S_OK) && (pErrorInfo != IntPtr.Zero))
            {
                using OleDbComWrappers.IErrorInfo errorInfo = (OleDbComWrappers.IErrorInfo)OleDbComWrappers.Instance
                    .GetOrCreateObjectForComInstance(pErrorInfo, CreateObjectFlags.UniqueInstance);;
                ODB.GetErrorDescription(errorInfo, lastErrorHr, out message);
                // note that either GetErrorInfo or GetErrorDescription might fail in which case we will have only the HRESULT value in exception message
            }
            lastErrorFromProvider = new COMException(message, (int)lastErrorHr);
        }
    }
}
