// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.OleDb;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Data.Common
{
    internal static partial class UnsafeNativeMethods
    {
        //
        // Oleaut32
        //

        [LibraryImport(Interop.Libraries.OleAut32)]
        internal static unsafe partial OleDbHResult GetErrorInfo(
            int dwReserved,
            out IErrorInfo? ppIErrorInfo);

        internal static void ReleaseErrorInfoObject(IErrorInfo errorInfo)
        {
            ((ComObject)(object)errorInfo).FinalRelease();
        }
    }
}
