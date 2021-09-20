// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal static partial class UnsafeNativeMethods
    {
        //
        // Oleaut32
        //

        [DllImport(Interop.Libraries.OleAut32)]
        internal static unsafe extern System.Data.OleDb.OleDbHResult GetErrorInfo(
            int dwReserved,
            System.IntPtr* ppIErrorInfo);

        internal static extern System.Data.OleDb.OleDbHResult GetErrorInfo(
            int dwReserved,
            out UnsafeNativeMethods.IErrorInfo? ppIErrorInfo)
        {
            ppIErrorInfo = null;
            var hr = GetErrorInfo(dwReserved, out IntPtr pErrorInfo);
            if (hr == OleDbHResult.S_OK)
            {
                ppIErrorInfo = (UnsafeNativeMethods.IErrorInfo)OleDbComWrappers.Instance
                    .GetOrCreateObjectForComInstance(pErrorInfo, CreateObjectFlags.UniqueInstance);
            }
        }

        internal static void ReleaseErrorInfoObject(UnsafeNativeMethods.IErrorInfo errorInfo)
        {
            ((IDisposable)errorInfo).Dispose();
        }
    }
}
