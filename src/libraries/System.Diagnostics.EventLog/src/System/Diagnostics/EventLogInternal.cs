// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal sealed class EventLogInternal : IDisposable, ISupportInitialize
    {
        private EventLogEntryCollection entriesCollection;
        internal string logName;
        // used in monitoring for event postings.
        private int lastSeenCount;
        // holds the machine we're on, or null if it's the local machine
        internal readonly string machineName;
        // the delegate to call when an event arrives
        internal EntryWrittenEventHandler onEntryWrittenHandler;
        // holds onto the handle for reading
        private SafeEventLogReadHandle readHandle;
        // the source name - used only when writing
        internal readonly string sourceName;
        // holds onto the handle for writing
        private SafeEventLogWriteHandle writeHandle;

        private string logDisplayName;
        // cache system state variables
        // the initial size of the buffer (it can be made larger if necessary)
        private const int BUF_SIZE = 40000;
        // the number of bytes in the cache that belong to entries (not necessarily
        // the same as BUF_SIZE, because the cache only holds whole entries)
        private int bytesCached;
        // the actual cache buffer
        private byte[] cache;
        // the number of the entry at the beginning of the cache
        private int firstCachedEntry = -1;
        // the number of the entry that we got out of the cache most recently
        private int lastSeenEntry;
        // where that entry was
        private int lastSeenPos;
        //support for threadpool based deferred execution
        private ISynchronizeInvoke synchronizingObject;
        // the EventLog object that publicly exposes this instance.
        private readonly EventLog parent;

        private const string EventLogKey = "SYSTEM\\CurrentControlSet\\Services\\EventLog";
        private const string eventLogMutexName = "netfxeventlog.1.0";
        private const int SecondsPerDay = 60 * 60 * 24;

        private const int Flag_notifying = 0x1;           // keeps track of whether we're notifying our listeners - to prevent double notifications
        private const int Flag_forwards = 0x2;     // whether the cache contains entries in forwards order (true) or backwards (false)
        private const int Flag_initializing = 0x4;
        internal const int Flag_monitoring = 0x8;
        private const int Flag_registeredAsListener = 0x10;
        private const int Flag_writeGranted = 0x20;
        private const int Flag_disposed = 0x100;
        private const int Flag_sourceVerified = 0x200;

        private BitVector32 boolFlags;

        private Hashtable messageLibraries;
        private static readonly Hashtable listenerInfos = new Hashtable(StringComparer.OrdinalIgnoreCase);

        private object m_InstanceLockObject;
        private object InstanceLockObject
        {
            get
            {
                if (m_InstanceLockObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref m_InstanceLockObject, o, null);
                }

                return m_InstanceLockObject;
            }
        }

        private static object s_InternalSyncObject;
        private static object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        public EventLogInternal(string logName, string machineName) : this(logName, machineName, "", null)
        {
        }

        public EventLogInternal(string logName, string machineName, string source) : this(logName, machineName, source, null)
        {
        }

        public EventLogInternal(string logName, string machineName, string source, EventLog parent)
        {
            ArgumentNullException.ThrowIfNull(logName);

            if (!ValidLogName(logName, true))
                throw new ArgumentException(SR.BadLogName);

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName));

            this.machineName = machineName;
            this.logName = logName;
            this.sourceName = source;
            readHandle = null;
            writeHandle = null;
            boolFlags[Flag_forwards] = true;
            this.parent = parent;
        }

        public EventLogEntryCollection Entries
        {
            get
            {
                return entriesCollection ??= new EventLogEntryCollection(this);
            }
        }

        internal int EntryCount
        {
            get
            {
                if (!IsOpenForRead)
                    OpenForRead(this.machineName);
                int count;
                bool success = Interop.Advapi32.GetNumberOfEventLogRecords(readHandle, out count);
                if (!success)
                    throw new Win32Exception();
                return count;
            }
        }

        private bool IsOpen
        {
            get
            {
                return readHandle != null || writeHandle != null;
            }
        }

        private bool IsOpenForRead
        {
            get
            {
                return readHandle != null;
            }
        }

        private bool IsOpenForWrite
        {
            get
            {
                return writeHandle != null;
            }
        }

        public string LogDisplayName
        {
            get
            {
                if (logDisplayName != null)
                    return logDisplayName;

                string currentMachineName = this.machineName;
                if (GetLogName(currentMachineName) != null)
                {
                    RegistryKey logkey = null;

                    try
                    {
                        // we figure out what logs are on the machine by looking in the registry.
                        logkey = GetLogRegKey(currentMachineName, false);
                        if (logkey == null)
                            throw new InvalidOperationException(SR.Format(SR.MissingLog, GetLogName(currentMachineName), currentMachineName));

                        string resourceDll = (string)logkey.GetValue("DisplayNameFile");
                        if (resourceDll == null)
                        {
                            logDisplayName = GetLogName(currentMachineName);
                        }
                        else
                        {
                            int resourceId = (int)logkey.GetValue("DisplayNameID");
                            logDisplayName = FormatMessageWrapper(resourceDll, (uint)resourceId, null);
                            logDisplayName ??= GetLogName(currentMachineName);
                        }
                    }
                    finally
                    {
                        logkey?.Close();
                    }
                }

                return logDisplayName;
            }
        }

        public string Log
        {
            get
            {
                string currentMachineName = this.machineName;
                return GetLogName(currentMachineName);
            }
        }

        private string GetLogName(string currentMachineName)
        {
            if (string.IsNullOrEmpty(logName) && !string.IsNullOrEmpty(sourceName))
            {
                logName = EventLog._InternalLogNameFromSourceName(sourceName, currentMachineName);
            }

            return logName;
        }

        public string MachineName
        {
            get
            {
                return this.machineName;
            }
        }

        public long MaximumKilobytes
        {
            get
            {
                string currentMachineName = this.machineName;

                object val = GetLogRegValue(currentMachineName, "MaxSize");
                if (val != null)
                {
                    int intval = (int)val;         // cast to an int first to unbox
                    return ((uint)intval) / 1024;   // then convert to kilobytes
                }
                // 512k is the default value
                return 0x200;
            }

            set
            {
                string currentMachineName = this.machineName;
                // valid range is 64 KB to 4 GB
                if (value < 64 || value > 0x3FFFC0 || value % 64 != 0)
                    throw new ArgumentOutOfRangeException("MaximumKilobytes", SR.MaximumKilobytesOutOfRange);

                long regvalue = value * 1024; // convert to bytes
                int i = unchecked((int)regvalue);

                using (RegistryKey logkey = GetLogRegKey(currentMachineName, true))
                    logkey.SetValue("MaxSize", i, RegistryValueKind.DWord);
            }
        }

        internal Hashtable MessageLibraries
        {
            get
            {
                return messageLibraries ??= new Hashtable(StringComparer.OrdinalIgnoreCase);
            }
        }

        public OverflowAction OverflowAction
        {
            get
            {
                string currentMachineName = this.machineName;

                object retentionobj = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj != null)
                {
                    int retention = (int)retentionobj;
                    if (retention == 0)
                        return OverflowAction.OverwriteAsNeeded;
                    else if (retention == -1)
                        return OverflowAction.DoNotOverwrite;
                    else
                        return OverflowAction.OverwriteOlder;
                }

                // default value as listed in MSDN
                return OverflowAction.OverwriteOlder;
            }
        }

        public int MinimumRetentionDays
        {
            get
            {
                string currentMachineName = this.machineName;

                object retentionobj = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj != null)
                {
                    int retention = (int)retentionobj;
                    if (retention == 0 || retention == -1)
                        return retention;
                    else
                        return (int)(((double)retention) / SecondsPerDay);
                }

                return 7;
            }
        }

        public bool EnableRaisingEvents
        {
            get
            {
                return boolFlags[Flag_monitoring];
            }
            set
            {
                string currentMachineName = this.machineName;

                if (parent.ComponentDesignMode)
                    this.boolFlags[Flag_monitoring] = value;
                else
                {
                    if (value)
                        StartRaisingEvents(currentMachineName, GetLogName(currentMachineName));
                    else
                        StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName));
                }
            }
        }

        private int OldestEntryNumber
        {
            get
            {
                if (!IsOpenForRead)
                    OpenForRead(this.machineName);
                int num;
                bool success = Interop.Advapi32.GetOldestEventLogRecord(readHandle, out num);
                if (!success)
                    throw new Win32Exception();

                if (num == 0)
                    num = 1;

                return num;
            }
        }

        internal SafeEventLogReadHandle ReadHandle
        {
            get
            {
                if (!IsOpenForRead)
                    OpenForRead(this.machineName);
                return readHandle;
            }
        }

        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                if (this.synchronizingObject == null && parent.ComponentDesignMode)
                {
                    IDesignerHost host = (IDesignerHost)parent.ComponentGetService(typeof(IDesignerHost));
                    if (host != null)
                    {
                        object baseComponent = host.RootComponent;
                        if (baseComponent != null && baseComponent is ISynchronizeInvoke)
                            this.synchronizingObject = (ISynchronizeInvoke)baseComponent;
                    }
                }

                return this.synchronizingObject;
            }

            set
            {
                this.synchronizingObject = value;
            }
        }

        public string Source
        {
            get
            {
                return sourceName;
            }
        }

        private static void AddListenerComponent(EventLogInternal component, string compMachineName, string compLogName)
        {
            lock (InternalSyncObject)
            {
                LogListeningInfo info = (LogListeningInfo)listenerInfos[compLogName];
                if (info != null)
                {
                    info.listeningComponents.Add(component);
                    return;
                }

                info = new LogListeningInfo();
                info.listeningComponents.Add(component);

                info.handleOwner = new EventLogInternal(compLogName, compMachineName);
                // tell the event log system about it
                info.waitHandle = new AutoResetEvent(false);
                bool success = Interop.Advapi32.NotifyChangeEventLog(info.handleOwner.ReadHandle, info.waitHandle.SafeWaitHandle);
                if (!success)
                    throw new InvalidOperationException(SR.CantMonitorEventLog, new Win32Exception());

                info.registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(info.waitHandle, new WaitOrTimerCallback(StaticCompletionCallback), info, -1, false);
                listenerInfos[compLogName] = info;
            }
        }

        public event EntryWrittenEventHandler EntryWritten
        {
            add
            {
                onEntryWrittenHandler += value;
            }
            remove
            {
                onEntryWrittenHandler -= value;
            }
        }

        public void BeginInit()
        {
            string currentMachineName = this.machineName;

            if (boolFlags[Flag_initializing])
                throw new InvalidOperationException(SR.InitTwice);
            boolFlags[Flag_initializing] = true;
            if (boolFlags[Flag_monitoring])
                StopListening(GetLogName(currentMachineName));
        }

        public void Clear()
        {
            string currentMachineName = this.machineName;

            if (!IsOpenForRead)
                OpenForRead(currentMachineName);
            bool success = Interop.Advapi32.ClearEventLog(readHandle, null);
            if (!success)
            {
                // Ignore file not found errors.  ClearEventLog seems to try to delete the file where the event log is
                // stored.  If it can't find it, it gives an error.
                int error = Marshal.GetLastWin32Error();
                if (error != Interop.Errors.ERROR_FILE_NOT_FOUND)
                    throw new Win32Exception();
            }
            // now that we've cleared the event log, we need to re-open our handles, because
            // the internal state of the event log has changed.
            Reset(currentMachineName);
        }

        public void Close()
        {
            Close(this.machineName);
        }

        private void Close(string currentMachineName)
        {
            if (readHandle != null)
            {
                try
                {
                    readHandle.Close();
                }
                catch (IOException)
                {
                    throw new Win32Exception();
                }
                readHandle = null;
            }

            if (writeHandle != null)
            {
                try
                {
                    writeHandle.Close();
                }
                catch (IOException)
                {
                    throw new Win32Exception();
                }
                writeHandle = null;
            }

            if (boolFlags[Flag_monitoring])
                StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName));

            if (messageLibraries != null)
            {
                foreach (SafeLibraryHandle handle in messageLibraries.Values)
                    handle.Close();

                messageLibraries = null;
            }

            boolFlags[Flag_sourceVerified] = false;
        }

        private void CompletionCallback(object context)
        {
            if (boolFlags[Flag_disposed])
            {
                // This object has been disposed previously, ignore firing the event.
                return;
            }

            lock (InstanceLockObject)
            {
                if (boolFlags[Flag_notifying])
                {
                    // don't do double notifications.
                    return;
                }

                boolFlags[Flag_notifying] = true;
            }

            int i = lastSeenCount;
            try
            {
                int oldest = OldestEntryNumber;
                int count = EntryCount + oldest;
                // Ensure lastSeenCount is within bounds.  This deals with the case where the event log has been cleared between
                // notifications.
                if (lastSeenCount < oldest || lastSeenCount > count)
                {
                    lastSeenCount = oldest;
                    i = lastSeenCount;
                }

                while (i < count)
                {
                    while (i < count)
                    {
                        EventLogEntry entry = GetEntryWithOldest(i);
                        if (this.SynchronizingObject != null && this.SynchronizingObject.InvokeRequired)
                            this.SynchronizingObject.BeginInvoke(this.onEntryWrittenHandler, new object[] { this, new EntryWrittenEventArgs(entry) });
                        else
                            onEntryWrittenHandler(this, new EntryWrittenEventArgs(entry));

                        i++;
                    }
                    oldest = OldestEntryNumber;
                    count = EntryCount + oldest;
                }
            }
            catch (Exception)
            {
            }

            try
            {
                // if the user cleared the log while we were receiving events, the call to GetEntryWithOldest above could have
                // thrown an exception and i could be too large.  Make sure we don't set lastSeenCount to something bogus.
                int newCount = EntryCount + OldestEntryNumber;
                if (i > newCount)
                    lastSeenCount = newCount;
                else
                    lastSeenCount = i;
            }
            catch (Win32Exception)
            {
            }

            lock (InstanceLockObject)
            {
                boolFlags[Flag_notifying] = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    //Dispose unmanaged and managed resources
                    if (IsOpen)
                    {
                        Close();
                    }
                    // This is probably unnecessary
                    if (readHandle != null)
                    {
                        readHandle.Close();
                        readHandle = null;
                    }

                    if (writeHandle != null)
                    {
                        writeHandle.Close();
                        writeHandle = null;
                    }
                }
            }
            finally
            {
                messageLibraries = null;
                this.boolFlags[Flag_disposed] = true;
            }
        }

        public void EndInit()
        {
            string currentMachineName = this.machineName;

            boolFlags[Flag_initializing] = false;
            if (boolFlags[Flag_monitoring])
                StartListening(currentMachineName, GetLogName(currentMachineName));
        }

        internal string FormatMessageWrapper(string dllNameList, uint messageNum, string[] insertionStrings)
        {
            if (dllNameList == null)
                return null;

            insertionStrings ??= Array.Empty<string>();

            string[] listDll = dllNameList.Split(';');

            // Find first mesage in DLL list
            foreach (string dllName in listDll)
            {
                if (string.IsNullOrEmpty(dllName))
                    continue;

                SafeLibraryHandle hModule;

                if (IsOpen)
                {
                    hModule = MessageLibraries[dllName] as SafeLibraryHandle;

                    if (hModule == null || hModule.IsInvalid)
                    {
                        hModule = Interop.Kernel32.LoadLibraryExW(dllName, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_AS_DATAFILE);
                        MessageLibraries[dllName] = hModule;
                    }
                }
                else
                {
                    hModule = Interop.Kernel32.LoadLibraryExW(dllName, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_AS_DATAFILE);
                }

                if (hModule.IsInvalid)
                    continue;

                string msg = null;
                try
                {
                    msg = EventLog.TryFormatMessage(hModule, messageNum, insertionStrings);
                }
                finally
                {
                    if (!IsOpen)
                    {
                        hModule.Close();
                    }
                }

                if (msg != null)
                {
                    return msg;
                }
            }

            return null;
        }

        internal EventLogEntry[] GetAllEntries()
        {
            // we could just call getEntryAt() on all the entries, but it'll be faster
            // if we grab multiple entries at once.
            string currentMachineName = this.machineName;

            if (!IsOpenForRead)
                OpenForRead(currentMachineName);

            EventLogEntry[] entries = new EventLogEntry[EntryCount];
            int idx = 0;
            int oldestEntry = OldestEntryNumber;

            int bytesRead;
            int minBytesNeeded;
            int error = 0;
            while (idx < entries.Length)
            {
                byte[] buf = new byte[BUF_SIZE];
                bool success = Interop.Advapi32.ReadEventLog(readHandle, Interop.Advapi32.FORWARDS_READ | Interop.Advapi32.SEEK_READ,
                                                      oldestEntry + idx, buf, buf.Length, out bytesRead, out minBytesNeeded);
                if (!success)
                {
                    error = Marshal.GetLastWin32Error();

                    if (error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER || error == Interop.Errors.ERROR_EVENTLOG_FILE_CHANGED)
                    {
                        if (error == Interop.Errors.ERROR_EVENTLOG_FILE_CHANGED)
                        {
                            Reset(currentMachineName);
                        }
                        // try again with a bigger buffer if necessary
                        else if (minBytesNeeded > buf.Length)
                        {
                            buf = new byte[minBytesNeeded];
                        }
                        success = Interop.Advapi32.ReadEventLog(readHandle, Interop.Advapi32.FORWARDS_READ | Interop.Advapi32.SEEK_READ,
                                                         oldestEntry + idx, buf, buf.Length, out bytesRead, out _);
                        if (!success)
                            break;
                    }
                    else
                    {
                        break;
                    }

                    error = 0;
                }
                entries[idx] = new EventLogEntry(buf, 0, this);
                int sum = IntFrom(buf, 0);
                idx++;
                while (sum < bytesRead && idx < entries.Length)
                {
                    entries[idx] = new EventLogEntry(buf, sum, this);
                    sum += IntFrom(buf, sum);
                    idx++;
                }
            }
            if (idx != entries.Length)
            {
                if (error != 0)
                    throw new InvalidOperationException(SR.CantRetrieveEntries, new Win32Exception(error));
                else
                    throw new InvalidOperationException(SR.CantRetrieveEntries);
            }
            return entries;
        }

        private int GetCachedEntryPos(int entryIndex)
        {
            if (cache == null || (boolFlags[Flag_forwards] && entryIndex < firstCachedEntry) ||
                (!boolFlags[Flag_forwards] && entryIndex > firstCachedEntry) || firstCachedEntry == -1)
            {
                // the index falls before anything we have in the cache, or the cache
                // is not yet valid
                return -1;
            }

            while (lastSeenEntry < entryIndex)
            {
                lastSeenEntry++;
                if (boolFlags[Flag_forwards])
                {
                    lastSeenPos = GetNextEntryPos(lastSeenPos);
                    if (lastSeenPos >= bytesCached)
                        break;
                }
                else
                {
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos);
                    if (lastSeenPos < 0)
                        break;
                }
            }

            while (lastSeenEntry > entryIndex)
            {
                lastSeenEntry--;
                if (boolFlags[Flag_forwards])
                {
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos);
                    if (lastSeenPos < 0)
                        break;
                }
                else
                {
                    lastSeenPos = GetNextEntryPos(lastSeenPos);
                    if (lastSeenPos >= bytesCached)
                        break;
                }
            }

            if (lastSeenPos >= bytesCached)
            {
                // we ran past the end. move back to the last one and return -1
                lastSeenPos = GetPreviousEntryPos(lastSeenPos);
                if (boolFlags[Flag_forwards])
                    lastSeenEntry--;
                else
                    lastSeenEntry++;
                return -1;
            }
            else if (lastSeenPos < 0)
            {
                // we ran past the beginning. move back to the first one and return -1
                lastSeenPos = 0;
                if (boolFlags[Flag_forwards])
                    lastSeenEntry++;
                else
                    lastSeenEntry--;
                return -1;
            }
            else
            {
                // we found it.
                return lastSeenPos;
            }
        }

        internal EventLogEntry GetEntryAt(int index)
        {
            EventLogEntry entry = GetEntryAtNoThrow(index);
            if (entry == null)
                throw new ArgumentException(SR.Format(SR.IndexOutOfBounds, index.ToString()));
            return entry;
        }

        internal EventLogEntry GetEntryAtNoThrow(int index)
        {
            if (!IsOpenForRead)
                OpenForRead(this.machineName);

            if (index < 0 || index >= EntryCount)
                return null;
            index += OldestEntryNumber;
            EventLogEntry entry = null;

            try
            {
                entry = GetEntryWithOldest(index);
            }
            catch (InvalidOperationException)
            {
            }

            return entry;
        }

        private EventLogEntry GetEntryWithOldest(int index)
        {
            int entryPos = GetCachedEntryPos(index);
            if (entryPos >= 0)
            {
                return new EventLogEntry(cache, entryPos, this);
            }

            string currentMachineName = this.machineName;
            int flags;
            if (GetCachedEntryPos(index + 1) < 0)
            {
                flags = Interop.Advapi32.FORWARDS_READ | Interop.Advapi32.SEEK_READ;
                boolFlags[Flag_forwards] = true;
            }
            else
            {
                flags = Interop.Advapi32.BACKWARDS_READ | Interop.Advapi32.SEEK_READ;
                boolFlags[Flag_forwards] = false;
            }

            cache = new byte[BUF_SIZE];
            int bytesRead;
            int minBytesNeeded;
            bool success = Interop.Advapi32.ReadEventLog(readHandle, flags, index,
                                                  cache, cache.Length, out bytesRead, out minBytesNeeded);
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER || error == Interop.Errors.ERROR_EVENTLOG_FILE_CHANGED)
                {
                    if (error == Interop.Errors.ERROR_EVENTLOG_FILE_CHANGED)
                    {
                        byte[] tempcache = cache;
                        Reset(currentMachineName);
                        cache = tempcache;
                    }
                    else
                    {
                        // try again with a bigger buffer.
                        if (minBytesNeeded > cache.Length)
                        {
                            cache = new byte[minBytesNeeded];
                        }
                    }
                    success = Interop.Advapi32.ReadEventLog(readHandle, Interop.Advapi32.FORWARDS_READ | Interop.Advapi32.SEEK_READ, index,
                                                     cache, cache.Length, out bytesRead, out _);
                }

                if (!success)
                {
                    throw new InvalidOperationException(SR.Format(SR.CantReadLogEntryAt, index.ToString()), new Win32Exception());
                }
            }

            bytesCached = bytesRead;
            firstCachedEntry = index;
            lastSeenEntry = index;
            lastSeenPos = 0;
            return new EventLogEntry(cache, 0, this);
        }

        internal static RegistryKey GetEventLogRegKey(string machine, bool writable)
        {
            RegistryKey lmkey = null;

            try
            {
                if (machine.Equals("."))
                {
                    lmkey = Registry.LocalMachine;
                }
                else
                {
                    lmkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machine);
                }
                if (lmkey != null)
                    return lmkey.OpenSubKey(EventLogKey, writable);
            }
            finally
            {
                lmkey?.Close();
            }

            return null;
        }

        private RegistryKey GetLogRegKey(string currentMachineName, bool writable)
        {
            string logname = GetLogName(currentMachineName);
            if (!ValidLogName(logname, false))
                throw new InvalidOperationException(SR.BadLogName);

            RegistryKey eventkey = null;
            RegistryKey logkey = null;

            try
            {
                eventkey = GetEventLogRegKey(currentMachineName, false);
                if (eventkey == null)
                    throw new InvalidOperationException(SR.Format(SR.RegKeyMissingShort, EventLogKey, currentMachineName));

                logkey = eventkey.OpenSubKey(logname, writable);
                if (logkey == null)
                    throw new InvalidOperationException(SR.Format(SR.MissingLog, logname, currentMachineName));
            }
            finally
            {
                eventkey?.Close();
            }

            return logkey;
        }

        private object GetLogRegValue(string currentMachineName, string valuename)
        {
            RegistryKey logkey = null;

            try
            {
                logkey = GetLogRegKey(currentMachineName, false);
                if (logkey == null)
                    throw new InvalidOperationException(SR.Format(SR.MissingLog, GetLogName(currentMachineName), currentMachineName));

                object val = logkey.GetValue(valuename);
                return val;
            }
            finally
            {
                logkey?.Close();
            }
        }

        private int GetNextEntryPos(int pos)
        {
            return pos + IntFrom(cache, pos);
        }

        private int GetPreviousEntryPos(int pos)
        {
            return pos - IntFrom(cache, pos - 4);
        }

        private static int IntFrom(byte[] buf, int offset)
        {
            // assumes Little Endian byte order.
            return (unchecked((int)0xFF000000) & (buf[offset + 3] << 24)) | (0xFF0000 & (buf[offset + 2] << 16)) |
            (0xFF00 & (buf[offset + 1] << 8)) | (0xFF & (buf[offset]));
        }

        public void ModifyOverflowPolicy(OverflowAction action, int retentionDays)
        {
            string currentMachineName = this.machineName;

            if (action < OverflowAction.DoNotOverwrite || action > OverflowAction.OverwriteOlder)
                throw new InvalidEnumArgumentException(nameof(action), (int)action, typeof(OverflowAction));
            // this is a long because in the if statement we may need to store values as
            // large as UInt32.MaxValue - 1.  This would overflow an int.
            long retentionvalue = (long)action;
            if (action == OverflowAction.OverwriteOlder)
            {
                if (retentionDays < 1 || retentionDays > 365)
                    throw new ArgumentOutOfRangeException(SR.RentionDaysOutOfRange);

                retentionvalue = (long)retentionDays * SecondsPerDay;
            }

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true))
                logkey.SetValue("Retention", retentionvalue, RegistryValueKind.DWord);
        }

        private void OpenForRead(string currentMachineName)
        {
            if (this.boolFlags[Flag_disposed])
                throw new ObjectDisposedException(GetType().Name);

            string logname = GetLogName(currentMachineName);

            if (string.IsNullOrEmpty(logname))
                throw new ArgumentException(SR.MissingLogProperty);

            if (!EventLog.Exists(logname, currentMachineName))        // do not open non-existing Log [alexvec]
                throw new InvalidOperationException(SR.Format(SR.LogDoesNotExists, logname, currentMachineName));

            // Clean up cache variables.
            // The initilizing code is put here to guarantee, that first read of events
            // from log file will start by filling up the cache buffer.
            lastSeenEntry = 0;
            lastSeenPos = 0;
            bytesCached = 0;
            firstCachedEntry = -1;

            SafeEventLogReadHandle handle = Interop.Advapi32.OpenEventLog(currentMachineName, logname);
            if (handle.IsInvalid)
            {
                Win32Exception e = null;
                if (Marshal.GetLastWin32Error() != 0)
                {
                    e = new Win32Exception();
                }
                handle.Dispose();
                throw new InvalidOperationException(SR.Format(SR.CantOpenLog, logname, currentMachineName, e?.Message ?? ""));
            }

            readHandle = handle;
        }

        private void OpenForWrite(string currentMachineName)
        {
            //Cannot allocate the writeHandle if the object has been disposed, since finalization has been suppressed.
            if (this.boolFlags[Flag_disposed])
                throw new ObjectDisposedException(GetType().Name);

            if (string.IsNullOrEmpty(sourceName))
                throw new ArgumentException(SR.NeedSourceToOpen);

            SafeEventLogWriteHandle handle = Interop.Advapi32.RegisterEventSource(currentMachineName, sourceName);
            if (handle.IsInvalid)
            {
                Win32Exception e = null;
                if (Marshal.GetLastWin32Error() != 0)
                {
                    e = new Win32Exception();
                }
                handle.Dispose();
                throw new InvalidOperationException(SR.Format(SR.CantOpenLogAccess, sourceName), e);
            }
            writeHandle = handle;
        }

        public void RegisterDisplayName(string resourceFile, long resourceId)
        {
            string currentMachineName = this.machineName;

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true))
            {
                logkey.SetValue("DisplayNameFile", resourceFile, RegistryValueKind.ExpandString);
                logkey.SetValue("DisplayNameID", resourceId, RegistryValueKind.DWord);
            }
        }

        private void Reset(string currentMachineName)
        {
            // save the state we're in now
            bool openRead = IsOpenForRead;
            bool openWrite = IsOpenForWrite;
            bool isMonitoring = boolFlags[Flag_monitoring];
            bool isListening = boolFlags[Flag_registeredAsListener];
            // close everything down
            Close(currentMachineName);
            cache = null;
            // and get us back into the same state as before
            if (openRead)
                OpenForRead(currentMachineName);
            if (openWrite)
                OpenForWrite(currentMachineName);
            if (isListening)
                StartListening(currentMachineName, GetLogName(currentMachineName));

            boolFlags[Flag_monitoring] = isMonitoring;
        }

        private static void RemoveListenerComponent(EventLogInternal component, string compLogName)
        {
            lock (InternalSyncObject)
            {
                LogListeningInfo info = (LogListeningInfo)listenerInfos[compLogName];
                Debug.Assert(info != null);
                // remove the requested component from the list.
                info.listeningComponents.Remove(component);
                if (info.listeningComponents.Count != 0)
                    return;
                // if that was the last interested compononent, destroy the handles and stop listening.
                info.handleOwner.Dispose();
                //Unregister the thread pool wait handle
                info.registeredWaitHandle.Unregister(info.waitHandle);
                // close the handle
                info.waitHandle.Close();
                listenerInfos[compLogName] = null;
            }
        }

        private void StartListening(string currentMachineName, string currentLogName)
        {
            // make sure we don't fire events for entries that are already there
            Debug.Assert(!boolFlags[Flag_registeredAsListener], "StartListening called with boolFlags[Flag_registeredAsListener] true.");
            lastSeenCount = EntryCount + OldestEntryNumber;
            AddListenerComponent(this, currentMachineName, currentLogName);
            boolFlags[Flag_registeredAsListener] = true;
        }

        private void StartRaisingEvents(string currentMachineName, string currentLogName)
        {
            if (!boolFlags[Flag_initializing] && !boolFlags[Flag_monitoring] && !parent.ComponentDesignMode)
            {
                StartListening(currentMachineName, currentLogName);
            }
            boolFlags[Flag_monitoring] = true;
        }

        private static void StaticCompletionCallback(object context, bool wasSignaled)
        {
            LogListeningInfo info = (LogListeningInfo)context;
            if (info == null)
                return;
            // get a snapshot of the components to fire the event on
            EventLogInternal[] interestedComponents;
            lock (InternalSyncObject)
            {
                interestedComponents = info.listeningComponents.ToArray();
            }

            for (int i = 0; i < interestedComponents.Length; i++)
            {
                try
                {
                    interestedComponents[i]?.CompletionCallback(null);
                }
                catch (ObjectDisposedException)
                {
                    // The EventLog that was registered to listen has been disposed.  Nothing much we can do here
                    // we don't want to propigate this error up as it will likely be unhandled and will cause the app
                    // to crash.
                }
            }
        }

        private void StopListening(/*string currentMachineName,*/ string currentLogName)
        {
            Debug.Assert(boolFlags[Flag_registeredAsListener], "StopListening called without StartListening.");
            RemoveListenerComponent(this, currentLogName);
            boolFlags[Flag_registeredAsListener] = false;
        }

        private void StopRaisingEvents(/*string currentMachineName,*/ string currentLogName)
        {
            if (!boolFlags[Flag_initializing] && boolFlags[Flag_monitoring] && !parent.ComponentDesignMode)
            {
                StopListening(currentLogName);
            }
            boolFlags[Flag_monitoring] = false;
        }

        private static bool CharIsPrintable(char c)
        {
            UnicodeCategory uc = char.GetUnicodeCategory(c);
            return (!(uc == UnicodeCategory.Control) || (uc == UnicodeCategory.Format) ||
                    (uc == UnicodeCategory.LineSeparator) || (uc == UnicodeCategory.ParagraphSeparator) ||
            (uc == UnicodeCategory.OtherNotAssigned));
        }

        internal static bool ValidLogName(string logName, bool ignoreEmpty)
        {
            if (logName.Length == 0 && !ignoreEmpty)
                return false;
            //any space, backslash, asterisk, or question mark is bad
            //any non-printable characters are also bad
            foreach (char c in logName)
                if (!CharIsPrintable(c) || (c == '\\') || (c == '*') || (c == '?'))
                    return false;
            return true;
        }

        private void VerifyAndCreateSource(string sourceName, string currentMachineName)
        {
            if (boolFlags[Flag_sourceVerified])
                return;

            if (!EventLog.SourceExists(sourceName, currentMachineName, true))
            {
                Mutex mutex = null;

                try
                {
                    NetFrameworkUtils.EnterMutex(eventLogMutexName, ref mutex);
                    if (!EventLog.SourceExists(sourceName, currentMachineName, true))
                    {
                        if (GetLogName(currentMachineName) == null)
                            this.logName = "Application";
                        // we automatically add an entry in the registry if there's not already
                        // one there for this source
                        EventLog.CreateEventSource(new EventSourceCreationData(sourceName, GetLogName(currentMachineName), currentMachineName));
                        // The user may have set a custom log and tried to read it before trying to
                        // write. Due to a quirk in the event log API, we would have opened the Application
                        // log to read (because the custom log wasn't there). Now that we've created
                        // the custom log, we should close so that when we re-open, we get a read
                        // handle on the _new_ log instead of the Application log.
                        Reset(currentMachineName);
                    }
                    else
                    {
                        string rightLogName = EventLog.LogNameFromSourceName(sourceName, currentMachineName);
                        string currentLogName = GetLogName(currentMachineName);
                        if (rightLogName != null && currentLogName != null && !string.Equals(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentException(SR.Format(SR.LogSourceMismatch, Source, currentLogName, rightLogName));
                    }
                }
                finally
                {
                    if (mutex != null)
                    {
                        mutex.ReleaseMutex();
                        mutex.Close();
                    }
                }
            }
            else
            {
                string rightLogName = EventLog._InternalLogNameFromSourceName(sourceName, currentMachineName);
                string currentLogName = GetLogName(currentMachineName);
                if (rightLogName != null && currentLogName != null && !string.Equals(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(SR.Format(SR.LogSourceMismatch, Source, currentLogName, rightLogName));
            }
            boolFlags[Flag_sourceVerified] = true;
        }

        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category,
                               byte[] rawData)
        {
            if (eventID < 0 || eventID > ushort.MaxValue)

                throw new ArgumentException(SR.Format(SR.EventID, eventID.ToString(), 0, ushort.MaxValue));

            if (Source.Length == 0)
                throw new ArgumentException(SR.NeedSourceToWrite);

            if (!Enum.IsDefined(typeof(EventLogEntryType), type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(EventLogEntryType));

            string currentMachineName = machineName;
            if (!boolFlags[Flag_writeGranted])
            {
                boolFlags[Flag_writeGranted] = true;
            }
            VerifyAndCreateSource(sourceName, currentMachineName);
            // now that the source has been hooked up to our DLL, we can use "normal"
            // (message-file driven) logging techniques.
            // Our DLL has 64K different entries; all of them just display the first
            // insertion string.
            InternalWriteEvent((uint)eventID, (ushort)category, type, new string[] { message }, rawData, currentMachineName);
        }

        public void WriteEvent(EventInstance instance, byte[] data, params object[] values)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (Source.Length == 0)
                throw new ArgumentException(SR.NeedSourceToWrite);

            string currentMachineName = machineName;
            if (!boolFlags[Flag_writeGranted])
            {
                boolFlags[Flag_writeGranted] = true;
            }

            VerifyAndCreateSource(Source, currentMachineName);

            string[] strings = null;

            if (values != null)
            {
                strings = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] != null)
                        strings[i] = values[i].ToString();
                    else
                        strings[i] = string.Empty;
                }
            }

            InternalWriteEvent((uint)instance.InstanceId, (ushort)instance.CategoryId, instance.EntryType, strings, data, currentMachineName);
        }

        private void InternalWriteEvent(uint eventID, ushort category, EventLogEntryType type, string[] strings,
                                byte[] rawData, string currentMachineName)
        {
            strings ??= Array.Empty<string>();
            if (strings.Length >= 256)
                throw new ArgumentException(SR.TooManyReplacementStrings);

            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] ??= string.Empty;

                // make sure the strings aren't too long.  MSDN says each string has a limit of 32k (32768) characters, but
                // experimentation shows that it doesn't like anything larger than 32766
                if (strings[i].Length > 32766)
                    throw new ArgumentException(SR.LogEntryTooLong);
            }
            rawData ??= Array.Empty<byte>();

            if (Source.Length == 0)
                throw new ArgumentException(SR.NeedSourceToWrite);

            if (!IsOpenForWrite)
                OpenForWrite(currentMachineName);

            // pin each of the strings in memory
            IntPtr[] stringRoots = new IntPtr[strings.Length];
            GCHandle[] stringHandles = new GCHandle[strings.Length];
            GCHandle stringsRootHandle = GCHandle.Alloc(stringRoots, GCHandleType.Pinned);
            try
            {
                for (int strIndex = 0; strIndex < strings.Length; strIndex++)
                {
                    stringHandles[strIndex] = GCHandle.Alloc(strings[strIndex], GCHandleType.Pinned);
                    stringRoots[strIndex] = stringHandles[strIndex].AddrOfPinnedObject();
                }

                byte[] sid = null;
                // actually report the event
                bool success = Interop.Advapi32.ReportEvent(writeHandle, (short)type, category, eventID,
                                                     sid, (short)strings.Length, rawData.Length, stringsRootHandle.AddrOfPinnedObject(), rawData);
                if (!success)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                // now free the pinned strings
                for (int i = 0; i < strings.Length; i++)
                {
                    if (stringHandles[i].IsAllocated)
                        stringHandles[i].Free();
                }
                stringsRootHandle.Free();
            }
        }

        private sealed class LogListeningInfo
        {
            public EventLogInternal handleOwner;
            public RegisteredWaitHandle registeredWaitHandle;
            public WaitHandle waitHandle;
            public List<EventLogInternal> listeningComponents = new();
        }
    }
}
