// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.OleDb;
using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal static partial class UnsafeNativeMethods
    {
        //
        // Oleaut32
        //

        [GeneratedDllImport(Interop.Libraries.OleAut32)]
        internal static unsafe partial OleDbHResult GetErrorInfo(
            int dwReserved,
            System.IntPtr* ppIErrorInfo);

        internal static unsafe OleDbHResult GetErrorInfo(
            int dwReserved,
            out UnsafeNativeMethods.IErrorInfo? ppIErrorInfo)
        {
            ppIErrorInfo = null;
            IntPtr pErrorInfo;
            var hr = GetErrorInfo(dwReserved, &pErrorInfo);
            if (hr == OleDbHResult.S_OK)
            {
                ppIErrorInfo = (UnsafeNativeMethods.IErrorInfo)OleDbComWrappers.Instance
                    .GetOrCreateObjectForComInstance(pErrorInfo, CreateObjectFlags.UniqueInstance);
            }

            return hr;
        }

        internal static void ReleaseErrorInfoObject(UnsafeNativeMethods.IErrorInfo errorInfo)
        {
            ((IDisposable)errorInfo).Dispose();
        }
    }
}
