// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data
{
    internal static partial class LocalDBAPI
    {
        private static IntPtr LoadProcAddress() => SafeNativeMethods.GetProcAddress(UserInstanceDLLHandle, "LocalDBFormatMessage");

        private static IntPtr UserInstanceDLLHandle
        {
            get
            {
                if (s_userInstanceDLLHandle == IntPtr.Zero)
                {
                    lock (s_dllLock)
                    {
                        if (s_userInstanceDLLHandle == IntPtr.Zero)
                        {
                            SNINativeMethodWrapper.SNIQueryInfo(SNINativeMethodWrapper.QTypes.SNI_QUERY_LOCALDB_HMODULE, ref s_userInstanceDLLHandle);
                            if (s_userInstanceDLLHandle == IntPtr.Zero)
                            {
                                SNINativeMethodWrapper.SNI_Error sniError;
                                SNINativeMethodWrapper.SNIGetLastError(out sniError);
                                throw CreateLocalDBException(errorMessage: SR.GetString("LocalDB_FailedGetDLLHandle"), sniError: (int)sniError.sniError);
                            }
                        }
                    }
                }
                return s_userInstanceDLLHandle;
            }
        }
    }
}
