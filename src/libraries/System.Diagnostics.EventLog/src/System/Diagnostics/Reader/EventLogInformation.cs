// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                CreationTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogCreationTime);
                LastAccessTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogLastAccessTime);
                LastWriteTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogLastWriteTime);
                FileSize = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogFileSize));
                Attributes = (int?)((uint?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogAttributes));
                RecordCount = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogNumberOfLogRecords));
                OldestRecordNumber = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogOldestRecordNumber));
                IsLogFull = (bool?)NativeWrapper.EvtGetLogInfo(logHandle, Interop.Wevtapi.EVT_LOG_PROPERTY_ID.EvtLogFull);
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
