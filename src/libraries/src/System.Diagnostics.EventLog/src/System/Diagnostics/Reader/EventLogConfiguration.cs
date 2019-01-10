// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Log Type
    /// </summary>
    public enum EventLogType
    {
        Administrative = 0,
        Operational,
        Analytical,
        Debug
    }

    /// <summary>
    /// Log Isolation
    /// </summary>
    public enum EventLogIsolation
    {
        Application = 0,
        System,
        Custom
    }

    /// <summary>
    /// Log Mode
    /// </summary>
    public enum EventLogMode
    {
        Circular = 0,
        AutoBackup,
        Retain
    }

    /// <summary>
    /// Provides access to static log information and configures
    /// log publishing and log file properties.
    /// </summary>
    public class EventLogConfiguration : IDisposable
    {
        private EventLogHandle _handle = EventLogHandle.Zero;

        private EventLogSession _session = null;

        public EventLogConfiguration(string logName) : this(logName, null) { }

        public EventLogConfiguration(string logName, EventLogSession session)
        {
            if (session == null)
                session = EventLogSession.GlobalSession;

            _session = session;
            LogName = logName;

            _handle = NativeWrapper.EvtOpenChannelConfig(_session.Handle, LogName, 0);
        }

        public string LogName { get; }

        public EventLogType LogType
        {
            get
            {
                return (EventLogType)((uint)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigType));
            }
        }

        public EventLogIsolation LogIsolation
        {
            get
            {
                return (EventLogIsolation)((uint)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigIsolation));
            }
        }

        public bool IsEnabled
        {
            get
            {
                return (bool)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigEnabled);
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigEnabled, (object)value);
            }
        }

        public bool IsClassicLog
        {
            get
            {
                return (bool)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigClassicEventlog);
            }
        }

        public string SecurityDescriptor
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigAccess);
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigAccess, (object)value);
            }
        }

        public string LogFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigLogFilePath);
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigLogFilePath, (object)value);
            }
        }

        public long MaximumSizeInBytes
        {
            get
            {
                return (long)((ulong)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigMaxSize));
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigMaxSize, (object)value);
            }
        }

        public EventLogMode LogMode
        {
            get
            {
                object nativeRetentionObject = NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention);
                object nativeAutoBackupObject = NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup);

                bool nativeRetention = nativeRetentionObject == null ? false : (bool)nativeRetentionObject;
                bool nativeAutoBackup = nativeAutoBackupObject == null ? false : (bool)nativeAutoBackupObject;

                if (nativeAutoBackup)
                    return EventLogMode.AutoBackup;

                if (nativeRetention)
                    return EventLogMode.Retain;

                return EventLogMode.Circular;
            }
            set
            {
                switch (value)
                {
                    case EventLogMode.Circular:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)false);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)false);
                        break;
                    case EventLogMode.AutoBackup:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)true);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)true);
                        break;
                    case EventLogMode.Retain:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)false);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)true);
                        break;
                }
            }
        }

        public string OwningProviderName
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigOwningPublisher);
            }
        }

        public IEnumerable<string> ProviderNames
        {
            get
            {
                return (string[])NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublisherList);
            }
        }

        public int? ProviderLevel
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLevel));
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLevel, (object)value);
            }
        }

        public long? ProviderKeywords
        {
            get
            {
                return (long?)((ulong?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigKeywords));
            }
            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigKeywords, (object)value);
            }
        }

        public int? ProviderBufferSize
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigBufferSize));
            }
        }

        public int? ProviderMinimumNumberOfBuffers
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigMinBuffers));
            }
        }

        public int? ProviderMaximumNumberOfBuffers
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigMaxBuffers));
            }
        }

        public int? ProviderLatency
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLatency));
            }
        }

        public Guid? ProviderControlGuid
        {
            get
            {
                return (Guid?)(NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigControlGuid));
            }
        }

        public void SaveChanges()
        {
            NativeWrapper.EvtSaveChannelConfig(_handle, 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }
    }
}
