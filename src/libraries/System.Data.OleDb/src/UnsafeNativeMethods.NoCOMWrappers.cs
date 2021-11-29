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

        [DllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static extern System.Data.OleDb.OleDbHResult GetErrorInfo(
            int dwReserved,
            [MarshalAs(UnmanagedType.Interface)] out UnsafeNativeMethods.IErrorInfo? ppIErrorInfo);

        internal static void ReleaseErrorInfoObject(UnsafeNativeMethods.IErrorInfo errorInfo)
        {
            Marshal.ReleaseComObject(errorInfo);
        }
    }
}
