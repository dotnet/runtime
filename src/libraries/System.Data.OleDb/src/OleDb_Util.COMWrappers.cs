// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.OleDb
{
    internal static partial class ODB
    {
        internal unsafe static OleDbHResult GetErrorDescription(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult hresult, out string message)
        {
            OleDbHResult hr = errorInfo.GetDescription(out message);
            if (((int)hr < 0) && ADP.IsEmpty(message))
            {
                message = FailedGetDescription(hr) + Environment.NewLine + ODB.ELookup(hresult);
            }
            if (ADP.IsEmpty(message))
            {
                message = ODB.ELookup(hresult);
            }
            return hr;
        }
    }
}
