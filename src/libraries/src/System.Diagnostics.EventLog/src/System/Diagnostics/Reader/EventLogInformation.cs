// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the run-time properties of logs and external log files. An instance
    /// of this class is obtained from EventLogSession.
    /// </summary>
    public sealed class EventLogInformation
    {
        internal EventLogInformation(EventLogSession session, string channelName, PathType pathType)
        {
            EventLogHandle logHandle = NativeWrapper.EvtOpenLog(session.Handle, channelName, pathType);

            using (logHandle)
            {
                CreationTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogCreationTime);
                LastAccessTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogLastAccessTime);
                LastWriteTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogLastWriteTime);
                FileSize = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogFileSize));
                Attributes = (int?)((uint?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogAttributes));
                RecordCount = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogNumberOfLogRecords));
                OldestRecordNumber = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogOldestRecordNumber));
                IsLogFull = (bool?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogFull);
            }
        }

        public DateTime? CreationTime { get; }
        public DateTime? LastAccessTime { get; }
        public DateTime? LastWriteTime { get; }
        public long? FileSize { get; }
        public int? Attributes { get; }
        public long? RecordCount { get; }
        public long? OldestRecordNumber { get; }
        public bool? IsLogFull { get; }
    }
}
