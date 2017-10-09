// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32
{
    internal static class UnsafeNativeMethods
    {
        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReportEvent(SafeHandle hEventLog, short type, ushort category,
                                                uint eventID, byte[] userSID, short numStrings, int dataLen, HandleRef strings,
                                                byte[] rawData);

        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ClearEventLog(SafeHandle hEventLog, HandleRef lpctstrBackupFileName);

        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetNumberOfEventLogRecords(SafeHandle hEventLog, out int count);

        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetOldestEventLogRecord(SafeHandle hEventLog, out int number);

        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadEventLog(SafeHandle hEventLog, int dwReadFlags,
                                                 int dwRecordOffset, byte[] buffer, int numberOfBytesToRead, out int bytesRead,
                                                 out int minNumOfBytesNeeded);
        
        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool NotifyChangeEventLog(SafeHandle hEventLog, SafeWaitHandle hEvent);
    }
}