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
        private unsafe void SetLastErrorInfo(OleDbHResult lastErrorHr)
        {
            // note: OleDbHResult is actually a simple wrapper over HRESULT with OLEDB-specific codes
            string message = string.Empty;
            IntPtr pErrorInfo;
            OleDbHResult errorInfoHr = UnsafeNativeMethods.GetErrorInfo(0, &pErrorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((errorInfoHr == OleDbHResult.S_OK) && (pErrorInfo != IntPtr.Zero))
            {
                UnsafeNativeMethods.IErrorInfo errorInfo = (UnsafeNativeMethods.IErrorInfo)OleDbComWrappers.Instance
                    .GetOrCreateObjectForComInstance(pErrorInfo, CreateObjectFlags.UniqueInstance);;
                try
                {
                    ODB.GetErrorDescription(errorInfo, lastErrorHr, out message);
                    // note that either GetErrorInfo or GetErrorDescription might fail in which case we will have only the HRESULT value in exception message
                }
                finally
                {
                    ((IDisposable)errorInfo).Dispose();
                }
            }

            lastErrorFromProvider = new COMException(message, (int)lastErrorHr);
        }
    }
}
