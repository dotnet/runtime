// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
#if ES_BUILD_STANDALONE
using System.Security.Permissions;
#endif
#if CORECLR && PLATFORM_WINDOWS
using Internal.Win32;
#endif
using System.Threading;
using System;

#if ES_BUILD_STANDALONE
using BitOperations = Microsoft.Diagnostics.Tracing.Internal.BitOperations;
#endif

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_AGAINST_DOTNET_V35
using Microsoft.Internal;       // for Tuple (can't define alias for open generic types so we "use" the whole namespace)
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    internal enum EventProviderType
    {
        None = 0,
        ETW,
        EventPipe
    };

    // New in CLR4.0
    internal enum ControllerCommand
    {
        // Strictly Positive numbers are for provider-specific commands, negative number are for 'shared' commands. 256
        // The first 256 negative numbers are reserved for the framework.
        Update = 0, // Not used by EventProviderBase.
        SendManifest = -1,
        Enable = -2,
        Disable = -3,
    };

    /// <summary>
    /// Only here because System.Diagnostics.EventProvider needs one more extensibility hook (when it gets a 
    /// controller callback)
    /// </summary>
#if ES_BUILD_STANDALONE
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
#endif
    internal class EventProvider : IDisposable
    {
        // This is the windows EVENT_DATA_DESCRIPTOR structure.  We expose it because this is what
        // subclasses of EventProvider use when creating efficient (but unsafe) version of
        // EventWrite.   We do make it a nested type because we really don't expect anyone to use 
        // it except subclasses (and then only rarely).  
        [StructLayout(LayoutKind.Sequential)]
        public struct EventData
        {
            internal unsafe ulong Ptr;
            internal uint Size;
            internal uint Reserved;
        }

        /// <summary>
        /// A struct characterizing ETW sessions (identified by the etwSessionId) as
        /// activity-tracing-aware or legacy. A session that's activity-tracing-aware
        /// has specified one non-zero bit in the reserved range 44-47 in the 
        /// 'allKeywords' value it passed in for a specific EventProvider.
        /// </summary>
        public struct SessionInfo
        {
            internal int sessionIdBit;      // the index of the bit used for tracing in the "reserved" field of AllKeywords
            internal int etwSessionId;      // the machine-wide ETW session ID

            internal SessionInfo(int sessionIdBit_, int etwSessionId_)
            { sessionIdBit = sessionIdBit_; etwSessionId = etwSessionId_; }
        }

        internal IEventProvider m_eventProvider;         // The interface that implements the specific logging mechanism functions.
        Interop.Advapi32.EtwEnableCallback? m_etwCallback;     // Trace Callback function
        private long m_regHandle;                        // Trace Registration Handle
        private byte m_level;                            // Tracing Level
        private long m_anyKeywordMask;                   // Trace Enable Flags
        private long m_allKeywordMask;                   // Match all keyword
        private List<SessionInfo>? m_liveSessions;       // current live sessions (Tuple<sessionIdBit, etwSessionId>)
        private bool m_enabled;                          // Enabled flag from Trace callback
        private string? m_providerName;                  // Control name
        private Guid m_providerId;                       // Control Guid
        internal bool m_disposed;                        // when true provider has unregistered

        [ThreadStatic]
        private static WriteEventErrorCode s_returnCode; // The last return code 

        private const int s_basicTypeAllocationBufferSize = 16;
        private const int s_etwMaxNumberArguments = 128;
        private const int s_etwAPIMaxRefObjCount = 8;
        private const int s_maxEventDataDescriptors = 128;
        private const int s_traceEventMaximumSize = 65482;
        private const int s_traceEventMaximumStringSize = 32724;

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public enum WriteEventErrorCode : int
        {
            //check mapping to runtime codes
            NoError = 0,
            NoFreeBuffers = 1,
            EventTooBig = 2,
            NullInput = 3,
            TooManyArgs = 4,
            Other = 5,
        };

        // Because callbacks happen on registration, and we need the callbacks for those setup
        // we can't call Register in the constructor.   
        //
        // Note that EventProvider should ONLY be used by EventSource.  In particular because
        // it registers a callback from native code you MUST dispose it BEFORE shutdown, otherwise
        // you may get native callbacks during shutdown when we have destroyed the delegate.  
        // EventSource has special logic to do this, no one else should be calling EventProvider.  
        internal EventProvider(EventProviderType providerType)
        {
            switch (providerType)
            {
                case EventProviderType.ETW:
#if PLATFORM_WINDOWS
                    m_eventProvider = new EtwEventProvider();
#else
                    m_eventProvider = new NoOpEventProvider();
#endif
                    break;
                case EventProviderType.EventPipe:
#if FEATURE_PERFTRACING
                    m_eventProvider = new EventPipeEventProvider();
#else
                    m_eventProvider = new NoOpEventProvider();
#endif
                    break;
                default:
                    m_eventProvider = new NoOpEventProvider();
                    break;
            };
        }

        /// <summary>
        /// This method registers the controlGuid of this class with ETW. We need to be running on
        /// Vista or above. If not a PlatformNotSupported exception will be thrown. If for some 
        /// reason the ETW Register call failed a NotSupported exception will be thrown. 
        /// </summary>
        // <SecurityKernel Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="Interop.Advapi32.EventRegister(System.Guid&,Microsoft.Win32.Interop.Advapi32+EtwEnableCallback,System.Void*,System.Int64&):System.UInt32" />
        // <SatisfiesLinkDemand Name="Win32Exception..ctor(System.Int32)" />
        // <ReferencesCritical Name="Method: EtwEnableCallBack(Guid&, Int32, Byte, Int64, Int64, Void*, Void*):Void" Ring="1" />
        // </SecurityKernel>
        internal unsafe void Register(EventSource eventSource)
        {
            uint status;
            m_etwCallback = new Interop.Advapi32.EtwEnableCallback(EtwEnableCallBack);

            status = EventRegister(eventSource, m_etwCallback);
            if (status != 0)
            {
#if PLATFORM_WINDOWS && !ES_BUILD_STANDALONE
                throw new ArgumentException(Interop.Kernel32.GetMessage(unchecked((int)status)));
#else
                throw new ArgumentException(Convert.ToString(unchecked((int)status)));
#endif
            }
        }

        //
        // implement Dispose Pattern to early deregister from ETW insted of waiting for 
        // the finalizer to call deregistration.
        // Once the user is done with the provider it needs to call Close() or Dispose()
        // If neither are called the finalizer will unregister the provider anyway
        //
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // <SecurityKernel Critical="True" TreatAsSafe="Does not expose critical resource" Ring="1">
        // <ReferencesCritical Name="Method: Deregister():Void" Ring="1" />
        // </SecurityKernel>
        protected virtual void Dispose(bool disposing)
        {
            //
            // explicit cleanup is done by calling Dispose with true from 
            // Dispose() or Close(). The disposing arguement is ignored because there
            // are no unmanaged resources.
            // The finalizer calls Dispose with false.
            //

            //
            // check if the object has been already disposed
            //
            if (m_disposed)
                return;

            // Disable the provider.  
            m_enabled = false;

            // Do most of the work under a lock to avoid shutdown race.

            long registrationHandle = 0;
            lock (EventListener.EventListenersLock)
            {
                // Double check
                if (m_disposed)
                    return;

                registrationHandle = m_regHandle;
                m_regHandle = 0;
                m_disposed = true;
            }

            // We do the Unregistration outside the EventListenerLock because there is a lock
            // inside the ETW routines.   This lock is taken before ETW issues commands
            // Thus the ETW lock gets taken first and then our EventListenersLock gets taken
            // in SendCommand(), and also here.  If we called EventUnregister after taking
            // the EventListenersLock then the take-lock order is reversed and we can have
            // deadlocks in race conditions (dispose racing with an ETW command).   
            // 
            // We solve by Unregistering after releasing the EventListenerLock.     
            if (registrationHandle != 0)
                EventUnregister(registrationHandle);

        }

        /// <summary>
        /// This method deregisters the controlGuid of this class with ETW.
        /// 
        /// </summary>
        public virtual void Close()
        {
            Dispose();
        }

        ~EventProvider()
        {
            Dispose(false);
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <UsesUnsafeCode Name="Parameter filterData of type: Void*" />
        // <UsesUnsafeCode Name="Parameter callbackContext of type: Void*" />
        // </SecurityKernel>
        unsafe void EtwEnableCallBack(
                        in System.Guid sourceId,
                        int controlCode,
                        byte setLevel,
                        long anyKeyword,
                        long allKeyword,
                        Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData,
                        void* callbackContext
                        )
        {
            // This is an optional callback API. We will therefore ignore any failures that happen as a 
            // result of turning on this provider as to not crash the app.
            // EventSource has code to validate whether initialization it expected to occur actually occurred
            try
            {
                ControllerCommand command = ControllerCommand.Update;
                IDictionary<string, string>? args = null;
                bool skipFinalOnControllerCommand = false;
                if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_ENABLE_PROVIDER)
                {
                    m_enabled = true;
                    m_level = setLevel;
                    m_anyKeywordMask = anyKeyword;
                    m_allKeywordMask = allKeyword;

                    List<Tuple<SessionInfo, bool>> sessionsChanged = GetSessions();

                    // The GetSessions() logic was here to support the idea that different ETW sessions
                    // could have different user-defined filters.   (I believe it is currently broken but that is another matter.)
                    // However in particular GetSessions() does not support EventPipe, only ETW, which is
                    // the immediate problem.   We work-around establishing the invariant that we always get a
                    // OnControllerCallback under all circumstances, even if we can't find a delta in the
                    // ETW logic.  This fixes things for the EventPipe case.
                    //
                    // All this session based logic should be reviewed and likely removed, but that is a larger
                    // change that needs more careful staging.
                    if (sessionsChanged.Count == 0)
                        sessionsChanged.Add(new Tuple<SessionInfo, bool>(new SessionInfo(0, 0), true));

                    foreach (var session in sessionsChanged)
                    {
                        int sessionChanged = session.Item1.sessionIdBit;
                        int etwSessionId = session.Item1.etwSessionId;
                        bool bEnabling = session.Item2;

                        skipFinalOnControllerCommand = true;
                        args = null;                                // reinitialize args for every session...

                        // if we get more than one session changed we have no way
                        // of knowing which one "filterData" belongs to
                        if (sessionsChanged.Count > 1)
                            filterData = null;

                        // read filter data only when a session is being *added*
                        byte[]? data;
                        int keyIndex;
                        if (bEnabling &&
                            GetDataFromController(etwSessionId, filterData, out command, out data, out keyIndex))
                        {
                            args = new Dictionary<string, string>(4);
                            Debug.Assert(data != null);
                            while (keyIndex < data.Length)
                            {
                                int keyEnd = FindNull(data, keyIndex);
                                int valueIdx = keyEnd + 1;
                                int valueEnd = FindNull(data, valueIdx);
                                if (valueEnd < data.Length)
                                {
                                    string key = System.Text.Encoding.UTF8.GetString(data, keyIndex, keyEnd - keyIndex);
                                    string value = System.Text.Encoding.UTF8.GetString(data, valueIdx, valueEnd - valueIdx);
                                    args[key] = value;
                                }
                                keyIndex = valueEnd + 1;
                            }
                        }

                        // execute OnControllerCommand once for every session that has changed.
                        OnControllerCommand(command, args, (bEnabling ? sessionChanged : -sessionChanged), etwSessionId);
                    }
                }
                else if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_DISABLE_PROVIDER)
                {
                    m_enabled = false;
                    m_level = 0;
                    m_anyKeywordMask = 0;
                    m_allKeywordMask = 0;
                    m_liveSessions = null;
                }
                else if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_CAPTURE_STATE)
                {
                    command = ControllerCommand.SendManifest;
                }
                else
                    return;     // per spec you ignore commands you don't recognize.  

                if (!skipFinalOnControllerCommand)
                    OnControllerCommand(command, args, 0, 0);
            }
            catch (Exception)
            {
                // We want to ignore any failures that happen as a result of turning on this provider as to
                // not crash the app.
            }
        }

        // New in CLR4.0
        protected virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string>? arguments, int sessionId, int etwSessionId) { }
        protected EventLevel Level { get { return (EventLevel)m_level; } set { m_level = (byte)value; } }
        protected EventKeywords MatchAnyKeyword { get { return (EventKeywords)m_anyKeywordMask; } set { m_anyKeywordMask = unchecked((long)value); } }
        protected EventKeywords MatchAllKeyword { get { return (EventKeywords)m_allKeywordMask; } set { m_allKeywordMask = unchecked((long)value); } }

        private static int FindNull(byte[] buffer, int idx)
        {
            while (idx < buffer.Length && buffer[idx] != 0)
                idx++;
            return idx;
        }

        /// <summary>
        /// Determines the ETW sessions that have been added and/or removed to the set of
        /// sessions interested in the current provider. It does so by (1) enumerating over all
        /// ETW sessions that enabled 'this.m_Guid' for the current process ID, and (2)
        /// comparing the current list with a list it cached on the previous invocation.
        ///
        /// The return value is a list of tuples, where the SessionInfo specifies the
        /// ETW session that was added or remove, and the bool specifies whether the
        /// session was added or whether it was removed from the set.
        /// </summary>
        private List<Tuple<SessionInfo, bool>> GetSessions()
        {
            List<SessionInfo>? liveSessionList = null;

            GetSessionInfo(
                (int etwSessionId, long matchAllKeywords, ref List<SessionInfo>? sessionList) =>
                    GetSessionInfoCallback(etwSessionId, matchAllKeywords, ref sessionList),
                ref liveSessionList);

            List<Tuple<SessionInfo, bool>> changedSessionList = new List<Tuple<SessionInfo, bool>>();

            // first look for sessions that have gone away (or have changed)
            // (present in the m_liveSessions but not in the new liveSessionList)
            if (m_liveSessions != null)
            {
                foreach (SessionInfo s in m_liveSessions)
                {
                    int idx;
                    if ((idx = IndexOfSessionInList(liveSessionList, s.etwSessionId)) < 0 ||
                        (liveSessionList![idx].sessionIdBit != s.sessionIdBit))
                        changedSessionList.Add(Tuple.Create(s, false));

                }
            }
            // next look for sessions that were created since the last callback  (or have changed)
            // (present in the new liveSessionList but not in m_liveSessions)
            if (liveSessionList != null)
            {
                foreach (SessionInfo s in liveSessionList)
                {
                    int idx;
                    if ((idx = IndexOfSessionInList(m_liveSessions, s.etwSessionId)) < 0 ||
                        (m_liveSessions![idx].sessionIdBit != s.sessionIdBit))
                        changedSessionList.Add(Tuple.Create(s, true));
                }
            }

            m_liveSessions = liveSessionList;
            return changedSessionList;
        }

        /// <summary>
        /// This method is the callback used by GetSessions() when it calls into GetSessionInfo().
        /// It updates a List{SessionInfo} based on the etwSessionId and matchAllKeywords that
        /// GetSessionInfo() passes in.
        /// </summary>
        private static void GetSessionInfoCallback(int etwSessionId, long matchAllKeywords,
                                ref List<SessionInfo>? sessionList)
        {
            uint sessionIdBitMask = (uint)SessionMask.FromEventKeywords(unchecked((ulong)matchAllKeywords));
            // an ETW controller that specifies more than the mandated bit for our EventSource
            // will be ignored...
            int val = BitOperations.PopCount(sessionIdBitMask);
            if (val > 1)
                return;

            if (sessionList == null)
                sessionList = new List<SessionInfo>(8);

            if (val == 1)
            {
                // activity-tracing-aware etw session
                val = BitOperations.TrailingZeroCount(sessionIdBitMask);
            }
            else
            {
                // legacy etw session
                val = BitOperations.PopCount((uint)SessionMask.All);
            }

            sessionList.Add(new SessionInfo(val + 1, etwSessionId));
        }

        private delegate void SessionInfoCallback(int etwSessionId, long matchAllKeywords, ref List<SessionInfo>? sessionList);

        /// <summary>
        /// This method enumerates over all active ETW sessions that have enabled 'this.m_Guid' 
        /// for the current process ID, calling 'action' for each session, and passing it the
        /// ETW session and the 'AllKeywords' the session enabled for the current provider.
        /// </summary>
        private unsafe void GetSessionInfo(SessionInfoCallback action, ref List<SessionInfo>? sessionList)
        {
            // We wish the EventSource package to be legal for Windows Store applications.   
            // Currently EnumerateTraceGuidsEx is not an allowed API, so we avoid its use here
            // and use the information in the registry instead.  This means that ETW controllers
            // that do not publish their intent to the registry (basically all controllers EXCEPT 
            // TraceEventSesion) will not work properly 

            // However the framework version of EventSource DOES have ES_SESSION_INFO defined and thus
            // does not have this issue.  
#if (PLATFORM_WINDOWS && (ES_SESSION_INFO || !ES_BUILD_STANDALONE))
            int buffSize = 256;     // An initial guess that probably works most of the time.  
            byte* buffer;
            for (; ; )
            {
                var space = stackalloc byte[buffSize];
                buffer = space;
                var hr = 0;

                fixed (Guid* provider = &m_providerId)
                {
                    hr = Interop.Advapi32.EnumerateTraceGuidsEx(Interop.Advapi32.TRACE_QUERY_INFO_CLASS.TraceGuidQueryInfo,
                        provider, sizeof(Guid), buffer, buffSize, out buffSize);
                }
                if (hr == 0)
                    break;
                if (hr != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                    return;
            }

            var providerInfos = (Interop.Advapi32.TRACE_GUID_INFO*)buffer;
            var providerInstance = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)&providerInfos[1];
            int processId = unchecked((int)Interop.Kernel32.GetCurrentProcessId());
            // iterate over the instances of the EventProvider in all processes
            for (int i = 0; i < providerInfos->InstanceCount; i++)
            {
                if (providerInstance->Pid == processId)
                {
                    var enabledInfos = (Interop.Advapi32.TRACE_ENABLE_INFO*)&providerInstance[1];
                    // iterate over the list of active ETW sessions "listening" to the current provider
                    for (int j = 0; j < providerInstance->EnableCount; j++)
                        action(enabledInfos[j].LoggerId, enabledInfos[j].MatchAllKeyword, ref sessionList);
                }
                if (providerInstance->NextOffset == 0)
                    break;
                Debug.Assert(0 <= providerInstance->NextOffset && providerInstance->NextOffset < buffSize);
                var structBase = (byte*)providerInstance;
                providerInstance = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)&structBase[providerInstance->NextOffset];
            }
#else
#if !ES_BUILD_PCL && PLATFORM_WINDOWS  // TODO command arguments don't work on PCL builds...
            // This code is only used in the Nuget Package Version of EventSource.  because
            // the code above is using APIs baned from UWP apps.     
            // 
            // TODO: In addition to only working when TraceEventSession enables the provider, this code
            // also has a problem because TraceEvent does not clean up if the registry is stale 
            // It is unclear if it is worth keeping, but for now we leave it as it does work
            // at least some of the time.  

            // Determine our session from what is in the registry.  
            string regKey = @"\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + m_providerName + "}";
            if (IntPtr.Size == 8)
                regKey = @"Software" + @"\Wow6432Node" + regKey;
            else
                regKey = @"Software" + regKey;

            using (var key = Registry.LocalMachine.OpenSubKey(regKey))
            {
                if (key != null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (valueName.StartsWith("ControllerData_Session_", StringComparison.Ordinal))
                        {
                            string strId = valueName.Substring(23);      // strip of the ControllerData_Session_
                            int etwSessionId;
                            if (int.TryParse(strId, out etwSessionId))
                            {
#if ES_BUILD_STANDALONE
                                // we need to assert this permission for partial trust scenarios
                                (new RegistryPermission(RegistryPermissionAccess.Read, regKey)).Assert();
#endif
                                var data = key.GetValue(valueName) as byte[];
                                if (data != null)
                                {
                                    var dataAsString = System.Text.Encoding.UTF8.GetString(data);
                                    int keywordIdx = dataAsString.IndexOf("EtwSessionKeyword", StringComparison.Ordinal);
                                    if (0 <= keywordIdx)
                                    {
                                        int startIdx = keywordIdx + 18;
                                        int endIdx = dataAsString.IndexOf('\0', startIdx);
                                        string keywordBitString = dataAsString.Substring(startIdx, endIdx-startIdx);
                                        int keywordBit;
                                        if (0 < endIdx && int.TryParse(keywordBitString, out keywordBit))
                                            action(etwSessionId, 1L << keywordBit, ref sessionList);
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
#endif
        }

        /// <summary>
        /// Returns the index of the SesisonInfo from 'sessions' that has the specified 'etwSessionId'
        /// or -1 if the value is not present.
        /// </summary>
        private static int IndexOfSessionInList(List<SessionInfo>? sessions, int etwSessionId)
        {
            if (sessions == null)
                return -1;
            // for non-coreclr code we could use List<T>.FindIndex(Predicate<T>), but we need this to compile
            // on coreclr as well
            for (int i = 0; i < sessions.Count; ++i)
                if (sessions[i].etwSessionId == etwSessionId)
                    return i;

            return -1;
        }

        /// <summary>
        /// Gets any data to be passed from the controller to the provider.  It starts with what is passed
        /// into the callback, but unfortunately this data is only present for when the provider is active
        /// at the time the controller issues the command.  To allow for providers to activate after the
        /// controller issued a command, we also check the registry and use that to get the data.  The function
        /// returns an array of bytes representing the data, the index into that byte array where the data
        /// starts, and the command being issued associated with that data.  
        /// </summary>
        private unsafe bool GetDataFromController(int etwSessionId,
            Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, out ControllerCommand command, out byte[]? data, out int dataStart)
        {
            data = null;
            dataStart = 0;
            if (filterData == null)
            {
#if (!ES_BUILD_PCL && !ES_BUILD_PN && PLATFORM_WINDOWS)
                string regKey = @"\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + m_providerId + "}";
                if (IntPtr.Size == 8)
                    regKey = @"Software" + @"\Wow6432Node" + regKey;
                else
                    regKey = @"Software" + regKey;

                string valueName = "ControllerData_Session_" + etwSessionId.ToString(CultureInfo.InvariantCulture);

                // we need to assert this permission for partial trust scenarios
#if ES_BUILD_STANDALONE
                (new RegistryPermission(RegistryPermissionAccess.Read, regKey)).Assert();
#endif
                using (var key = Registry.LocalMachine.OpenSubKey(regKey))
                {
                    data = key?.GetValue(valueName, null) as byte[];
                    if (data != null)
                    {
                        // We only used the persisted data from the registry for updates.   
                        command = ControllerCommand.Update;
                        return true;
                    }
                }
#endif
            }
            else
            {
                if (filterData->Ptr != 0 && 0 < filterData->Size && filterData->Size <= 1024)
                {
                    data = new byte[filterData->Size];
                    Marshal.Copy((IntPtr)filterData->Ptr, data, 0, data.Length);
                }
                command = (ControllerCommand)filterData->Type;
                return true;
            }

            command = ControllerCommand.Update;
            return false;
        }

        /// <summary>
        /// IsEnabled, method used to test if provider is enabled
        /// </summary>
        public bool IsEnabled()
        {
            return m_enabled;
        }

        /// <summary>
        /// IsEnabled, method used to test if event is enabled
        /// </summary>
        /// <param name="level">
        /// Level  to test
        /// </param>
        /// <param name="keywords">
        /// Keyword  to test
        /// </param>
        public bool IsEnabled(byte level, long keywords)
        {
            //
            // If not enabled at all, return false.
            //
            if (!m_enabled)
            {
                return false;
            }

            // This also covers the case of Level == 0.
            if ((level <= m_level) ||
                (m_level == 0))
            {

                //
                // Check if Keyword is enabled
                //

                if ((keywords == 0) ||
                    (((keywords & m_anyKeywordMask) != 0) &&
                     ((keywords & m_allKeywordMask) == m_allKeywordMask)))
                {
                    return true;
                }
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static WriteEventErrorCode GetLastWriteEventError()
        {
            return s_returnCode;
        }

        //
        // Helper function to set the last error on the thread
        //
        private static void SetLastError(WriteEventErrorCode error)
        {
            s_returnCode = error;
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <UsesUnsafeCode Name="Local intptrPtr of type: IntPtr*" />
        // <UsesUnsafeCode Name="Local intptrPtr of type: Int32*" />
        // <UsesUnsafeCode Name="Local longptr of type: Int64*" />
        // <UsesUnsafeCode Name="Local uintptr of type: UInt32*" />
        // <UsesUnsafeCode Name="Local ulongptr of type: UInt64*" />
        // <UsesUnsafeCode Name="Local charptr of type: Char*" />
        // <UsesUnsafeCode Name="Local byteptr of type: Byte*" />
        // <UsesUnsafeCode Name="Local shortptr of type: Int16*" />
        // <UsesUnsafeCode Name="Local sbyteptr of type: SByte*" />
        // <UsesUnsafeCode Name="Local ushortptr of type: UInt16*" />
        // <UsesUnsafeCode Name="Local floatptr of type: Single*" />
        // <UsesUnsafeCode Name="Local doubleptr of type: Double*" />
        // <UsesUnsafeCode Name="Local boolptr of type: Boolean*" />
        // <UsesUnsafeCode Name="Local guidptr of type: Guid*" />
        // <UsesUnsafeCode Name="Local decimalptr of type: Decimal*" />
        // <UsesUnsafeCode Name="Local booleanptr of type: Boolean*" />
        // <UsesUnsafeCode Name="Parameter dataDescriptor of type: EventData*" />
        // <UsesUnsafeCode Name="Parameter dataBuffer of type: Byte*" />
        // </SecurityKernel>
        private static unsafe object? EncodeObject(ref object data, ref EventData* dataDescriptor, ref byte* dataBuffer, ref uint totalEventSize)
        /*++

        Routine Description:

           This routine is used by WriteEvent to unbox the object type and
           to fill the passed in ETW data descriptor. 

        Arguments:

           data - argument to be decoded

           dataDescriptor - pointer to the descriptor to be filled (updated to point to the next empty entry)

           dataBuffer - storage buffer for storing user data, needed because cant get the address of the object
                        (updated to point to the next empty entry)

        Return Value:

           null if the object is a basic type other than string or byte[]. String otherwise

        --*/
        {
        Again:
            dataDescriptor->Reserved = 0;

            string? sRet = data as string;
            byte[]? blobRet = null;

            if (sRet != null)
            {
                dataDescriptor->Size = ((uint)sRet.Length + 1) * 2;
            }
            else if ((blobRet = data as byte[]) != null)
            {
                // first store array length
                *(int*)dataBuffer = blobRet.Length;
                dataDescriptor->Ptr = (ulong)dataBuffer;
                dataDescriptor->Size = 4;
                totalEventSize += dataDescriptor->Size;

                // then the array parameters
                dataDescriptor++;
                dataBuffer += s_basicTypeAllocationBufferSize;
                dataDescriptor->Size = (uint)blobRet.Length;
            }
            else if (data is IntPtr)
            {
                dataDescriptor->Size = (uint)sizeof(IntPtr);
                IntPtr* intptrPtr = (IntPtr*)dataBuffer;
                *intptrPtr = (IntPtr)data;
                dataDescriptor->Ptr = (ulong)intptrPtr;
            }
            else if (data is int)
            {
                dataDescriptor->Size = (uint)sizeof(int);
                int* intptr = (int*)dataBuffer;
                *intptr = (int)data;
                dataDescriptor->Ptr = (ulong)intptr;
            }
            else if (data is long)
            {
                dataDescriptor->Size = (uint)sizeof(long);
                long* longptr = (long*)dataBuffer;
                *longptr = (long)data;
                dataDescriptor->Ptr = (ulong)longptr;
            }
            else if (data is uint)
            {
                dataDescriptor->Size = (uint)sizeof(uint);
                uint* uintptr = (uint*)dataBuffer;
                *uintptr = (uint)data;
                dataDescriptor->Ptr = (ulong)uintptr;
            }
            else if (data is ulong)
            {
                dataDescriptor->Size = (uint)sizeof(ulong);
                ulong* ulongptr = (ulong*)dataBuffer;
                *ulongptr = (ulong)data;
                dataDescriptor->Ptr = (ulong)ulongptr;
            }
            else if (data is char)
            {
                dataDescriptor->Size = (uint)sizeof(char);
                char* charptr = (char*)dataBuffer;
                *charptr = (char)data;
                dataDescriptor->Ptr = (ulong)charptr;
            }
            else if (data is byte)
            {
                dataDescriptor->Size = (uint)sizeof(byte);
                byte* byteptr = (byte*)dataBuffer;
                *byteptr = (byte)data;
                dataDescriptor->Ptr = (ulong)byteptr;
            }
            else if (data is short)
            {
                dataDescriptor->Size = (uint)sizeof(short);
                short* shortptr = (short*)dataBuffer;
                *shortptr = (short)data;
                dataDescriptor->Ptr = (ulong)shortptr;
            }
            else if (data is sbyte)
            {
                dataDescriptor->Size = (uint)sizeof(sbyte);
                sbyte* sbyteptr = (sbyte*)dataBuffer;
                *sbyteptr = (sbyte)data;
                dataDescriptor->Ptr = (ulong)sbyteptr;
            }
            else if (data is ushort)
            {
                dataDescriptor->Size = (uint)sizeof(ushort);
                ushort* ushortptr = (ushort*)dataBuffer;
                *ushortptr = (ushort)data;
                dataDescriptor->Ptr = (ulong)ushortptr;
            }
            else if (data is float)
            {
                dataDescriptor->Size = (uint)sizeof(float);
                float* floatptr = (float*)dataBuffer;
                *floatptr = (float)data;
                dataDescriptor->Ptr = (ulong)floatptr;
            }
            else if (data is double)
            {
                dataDescriptor->Size = (uint)sizeof(double);
                double* doubleptr = (double*)dataBuffer;
                *doubleptr = (double)data;
                dataDescriptor->Ptr = (ulong)doubleptr;
            }
            else if (data is bool)
            {
                // WIN32 Bool is 4 bytes
                dataDescriptor->Size = 4;
                int* intptr = (int*)dataBuffer;
                if (((bool)data))
                {
                    *intptr = 1;
                }
                else
                {
                    *intptr = 0;
                }
                dataDescriptor->Ptr = (ulong)intptr;
            }
            else if (data is Guid)
            {
                dataDescriptor->Size = (uint)sizeof(Guid);
                Guid* guidptr = (Guid*)dataBuffer;
                *guidptr = (Guid)data;
                dataDescriptor->Ptr = (ulong)guidptr;
            }
            else if (data is decimal)
            {
                dataDescriptor->Size = (uint)sizeof(decimal);
                decimal* decimalptr = (decimal*)dataBuffer;
                *decimalptr = (decimal)data;
                dataDescriptor->Ptr = (ulong)decimalptr;
            }
            else if (data is DateTime)
            {
                const long UTCMinTicks = 504911232000000000;
                long dateTimeTicks = 0;
                // We cannot translate dates sooner than 1/1/1601 in UTC. 
                // To avoid getting an ArgumentOutOfRangeException we compare with 1/1/1601 DateTime ticks
                if (((DateTime)data).Ticks > UTCMinTicks)
                    dateTimeTicks = ((DateTime)data).ToFileTimeUtc();
                dataDescriptor->Size = (uint)sizeof(long);
                long* longptr = (long*)dataBuffer;
                *longptr = dateTimeTicks;
                dataDescriptor->Ptr = (ulong)longptr;
            }
            else
            {
                if (data is System.Enum)
                {
                    try
                    {
                        Type underlyingType = Enum.GetUnderlyingType(data.GetType());
                        if (underlyingType == typeof(ulong))
                            data = (ulong)data;
                        else if (underlyingType == typeof(long))
                            data = (long)data;
                        else
                            data = (int)Convert.ToInt64(data);  // This handles all int/uint or below (we treat them like 32 bit ints)   
                        goto Again;
                    }
                    catch { }   // On wierd cases (e.g. enums of type double), give up and for compat simply tostring.  
                }

                // To our eyes, everything else is a just a string
                if (data == null)
                    sRet = "";
                else
                    sRet = data.ToString()!;
                dataDescriptor->Size = ((uint)sRet.Length + 1) * 2;
            }

            totalEventSize += dataDescriptor->Size;

            // advance buffers
            dataDescriptor++;
            dataBuffer += s_basicTypeAllocationBufferSize;

            return (object?)sRet ?? (object?)blobRet;
        }

        /// <summary>
        /// WriteEvent, method to write a parameters with event schema properties
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event. 
        /// </param>
        /// <param name="activityID">
        /// A pointer to the activity ID GUID to log 
        /// </param>
        /// <param name="childActivityID">
        /// childActivityID is marked as 'related' to the current activity ID. 
        /// </param>
        /// <param name="eventPayload">
        /// Payload for the ETW event. 
        /// </param>
        // <SecurityKernel Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="Interop.Advapi32.EventWrite(System.Int64,EventDescriptor&,System.UInt32,System.Void*):System.UInt32" />
        // <UsesUnsafeCode Name="Local dataBuffer of type: Byte*" />
        // <UsesUnsafeCode Name="Local pdata of type: Char*" />
        // <UsesUnsafeCode Name="Local userData of type: EventData*" />
        // <UsesUnsafeCode Name="Local userDataPtr of type: EventData*" />
        // <UsesUnsafeCode Name="Local currentBuffer of type: Byte*" />
        // <UsesUnsafeCode Name="Local v0 of type: Char*" />
        // <UsesUnsafeCode Name="Local v1 of type: Char*" />
        // <UsesUnsafeCode Name="Local v2 of type: Char*" />
        // <UsesUnsafeCode Name="Local v3 of type: Char*" />
        // <UsesUnsafeCode Name="Local v4 of type: Char*" />
        // <UsesUnsafeCode Name="Local v5 of type: Char*" />
        // <UsesUnsafeCode Name="Local v6 of type: Char*" />
        // <UsesUnsafeCode Name="Local v7 of type: Char*" />
        // <ReferencesCritical Name="Method: EncodeObject(Object&, EventData*, Byte*):String" Ring="1" />
        // </SecurityKernel>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Performance-critical code")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, params object[] eventPayload)
        {
            WriteEventErrorCode status = WriteEventErrorCode.NoError;

            if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
            {
                int argCount = 0;
                unsafe
                {
                    argCount = eventPayload.Length;

                    if (argCount > s_etwMaxNumberArguments)
                    {
                        s_returnCode = WriteEventErrorCode.TooManyArgs;
                        return false;
                    }

                    uint totalEventSize = 0;
                    int index;
                    int refObjIndex = 0;
                    List<int> refObjPosition = new List<int>(s_etwAPIMaxRefObjCount);
                    List<object?> dataRefObj = new List<object?>(s_etwAPIMaxRefObjCount);
                    EventData* userData = stackalloc EventData[2 * argCount];
                    for (int i = 0; i < 2 * argCount; i++)
                        userData[i] = default;
                    EventData* userDataPtr = (EventData*)userData;
                    byte* dataBuffer = stackalloc byte[s_basicTypeAllocationBufferSize * 2 * argCount]; // Assume 16 chars for non-string argument
                    byte* currentBuffer = dataBuffer;

                    //
                    // The loop below goes through all the arguments and fills in the data 
                    // descriptors. For strings save the location in the dataString array.
                    // Calculates the total size of the event by adding the data descriptor
                    // size value set in EncodeObject method.
                    //
                    bool hasNonStringRefArgs = false;
                    for (index = 0; index < eventPayload.Length; index++)
                    {
                        if (eventPayload[index] != null)
                        {
                            object? supportedRefObj = EncodeObject(ref eventPayload[index], ref userDataPtr, ref currentBuffer, ref totalEventSize);

                            if (supportedRefObj != null)
                            {
                                // EncodeObject advanced userDataPtr to the next empty slot
                                int idx = (int)(userDataPtr - userData - 1);
                                if (!(supportedRefObj is string))
                                {
                                    if (eventPayload.Length + idx + 1 - index > s_etwMaxNumberArguments)
                                    {
                                        s_returnCode = WriteEventErrorCode.TooManyArgs;
                                        return false;
                                    }
                                    hasNonStringRefArgs = true;
                                }
                                dataRefObj.Add(supportedRefObj);
                                refObjPosition.Add(idx);
                                refObjIndex++;
                            }
                        }
                        else
                        {
                            s_returnCode = WriteEventErrorCode.NullInput;
                            return false;
                        }
                    }

                    // update argCount based on actual number of arguments written to 'userData'
                    argCount = (int)(userDataPtr - userData);

                    if (totalEventSize > s_traceEventMaximumSize)
                    {
                        s_returnCode = WriteEventErrorCode.EventTooBig;
                        return false;
                    }

                    // the optimized path (using "fixed" instead of allocating pinned GCHandles
                    if (!hasNonStringRefArgs && (refObjIndex < s_etwAPIMaxRefObjCount))
                    {
                        // Fast path: at most 8 string arguments

                        // ensure we have at least s_etwAPIMaxStringCount in dataString, so that
                        // the "fixed" statement below works
                        while (refObjIndex < s_etwAPIMaxRefObjCount)
                        {
                            dataRefObj.Add(null);
                            ++refObjIndex;
                        }

                        //
                        // now fix any string arguments and set the pointer on the data descriptor 
                        //
                        fixed (char* v0 = (string?)dataRefObj[0], v1 = (string?)dataRefObj[1], v2 = (string?)dataRefObj[2], v3 = (string?)dataRefObj[3],
                                v4 = (string?)dataRefObj[4], v5 = (string?)dataRefObj[5], v6 = (string?)dataRefObj[6], v7 = (string?)dataRefObj[7])
                        {
                            userDataPtr = (EventData*)userData;
                            if (dataRefObj[0] != null)
                            {
                                userDataPtr[refObjPosition[0]].Ptr = (ulong)v0;
                            }
                            if (dataRefObj[1] != null)
                            {
                                userDataPtr[refObjPosition[1]].Ptr = (ulong)v1;
                            }
                            if (dataRefObj[2] != null)
                            {
                                userDataPtr[refObjPosition[2]].Ptr = (ulong)v2;
                            }
                            if (dataRefObj[3] != null)
                            {
                                userDataPtr[refObjPosition[3]].Ptr = (ulong)v3;
                            }
                            if (dataRefObj[4] != null)
                            {
                                userDataPtr[refObjPosition[4]].Ptr = (ulong)v4;
                            }
                            if (dataRefObj[5] != null)
                            {
                                userDataPtr[refObjPosition[5]].Ptr = (ulong)v5;
                            }
                            if (dataRefObj[6] != null)
                            {
                                userDataPtr[refObjPosition[6]].Ptr = (ulong)v6;
                            }
                            if (dataRefObj[7] != null)
                            {
                                userDataPtr[refObjPosition[7]].Ptr = (ulong)v7;
                            }

                            status = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, argCount, userData);
                        }
                    }
                    else
                    {
                        // Slow path: use pinned handles
                        userDataPtr = (EventData*)userData;

                        GCHandle[] rgGCHandle = new GCHandle[refObjIndex];
                        for (int i = 0; i < refObjIndex; ++i)
                        {
                            // below we still use "fixed" to avoid taking dependency on the offset of the first field
                            // in the object (the way we would need to if we used GCHandle.AddrOfPinnedObject)
                            rgGCHandle[i] = GCHandle.Alloc(dataRefObj[i], GCHandleType.Pinned);
                            if (dataRefObj[i] is string)
                            {
                                fixed (char* p = (string?)dataRefObj[i])
                                    userDataPtr[refObjPosition[i]].Ptr = (ulong)p;
                            }
                            else
                            {
                                fixed (byte* p = (byte[]?)dataRefObj[i])
                                    userDataPtr[refObjPosition[i]].Ptr = (ulong)p;
                            }
                        }

                        status = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, argCount, userData);

                        for (int i = 0; i < refObjIndex; ++i)
                        {
                            rgGCHandle[i].Free();
                        }
                    }
                }
            }

            if (status != WriteEventErrorCode.NoError)
            {
                SetLastError(status);
                return false;
            }

            return true;
        }

        /// <summary>
        /// WriteEvent, method to be used by generated code on a derived class
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event. 
        /// </param>
        /// <param name="activityID">
        /// A pointer to the activity ID to log 
        /// </param>
        /// <param name="childActivityID">
        /// If this event is generating a child activity (WriteEventTransfer related activity) this is child activity
        /// This can be null for events that do not generate a child activity.  
        /// </param>
        /// <param name="dataCount">
        /// number of event descriptors 
        /// </param>
        /// <param name="data">
        /// pointer  do the event data
        /// </param>
        // <SecurityKernel Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="Interop.Advapi32.EventWrite(System.Int64,EventDescriptor&,System.UInt32,System.Void*):System.UInt32" />
        // </SecurityKernel>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        internal protected unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, int dataCount, IntPtr data)
        {
            if (childActivityID != null)
            {
                // activity transfers are supported only for events that specify the Send or Receive opcode
                Debug.Assert((EventOpcode)eventDescriptor.Opcode == EventOpcode.Send ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Receive ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Start ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Stop);
            }

            WriteEventErrorCode status = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, dataCount, (EventData*)data);

            if (status != 0)
            {
                SetLastError(status);
                return false;
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        internal unsafe bool WriteEventRaw(
            ref EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityID,
            Guid* relatedActivityID,
            int dataCount,
            IntPtr data)
        {
            WriteEventErrorCode status;

            status = m_eventProvider.EventWriteTransfer(
                m_regHandle,
                in eventDescriptor,
                eventHandle,
                activityID,
                relatedActivityID,
                dataCount,
                (EventData*)data);

            if (status != WriteEventErrorCode.NoError)
            {
                SetLastError(status);
                return false;
            }

            return true;
        }


        // These are look-alikes to the Manifest based ETW OS APIs that have been shimmed to work
        // either with Manifest ETW or Classic ETW (if Manifest based ETW is not available).  
        private unsafe uint EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback)
        {
            m_providerName = eventSource.Name;
            m_providerId = eventSource.Guid;
            m_etwCallback = enableCallback;
            return m_eventProvider.EventRegister(eventSource, enableCallback, null, ref m_regHandle);
        }

        private uint EventUnregister(long registrationHandle)
        {
            return m_eventProvider.EventUnregister(registrationHandle);
        }

#if PLATFORM_WINDOWS
        private static bool m_setInformationMissing;

        internal unsafe int SetInformation(
            Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass,
            IntPtr data,
            uint dataSize)
        {
            int status = Interop.Errors.ERROR_NOT_SUPPORTED;

            if (!m_setInformationMissing)
            {
                try
                {
                    status = Interop.Advapi32.EventSetInformation(
                        m_regHandle,
                        eventInfoClass,
                        (void*)data,
                        (int)dataSize);
                }
                catch (TypeLoadException)
                {
                    m_setInformationMissing = true;
                }
            }

            return status;
        }
#endif
    }

#if PLATFORM_WINDOWS

    // A wrapper around the ETW-specific API calls.
    internal sealed class EtwEventProvider : IEventProvider
    {
        // Register an event provider.
        unsafe uint IEventProvider.EventRegister(
            EventSource eventSource,
            Interop.Advapi32.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle)
        {
            Guid providerId = eventSource.Guid;
            return Interop.Advapi32.EventRegister(
                in providerId,
                enableCallback,
                callbackContext,
                ref registrationHandle);
        }

        // Unregister an event provider.
        uint IEventProvider.EventUnregister(long registrationHandle)
        {
            return Interop.Advapi32.EventUnregister(registrationHandle);
        }

        // Write an event.
        unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            int error = Interop.Advapi32.EventWriteTransfer(
                registrationHandle,
                in eventDescriptor,
                activityId,
                relatedActivityId,
                userDataCount,
                userData);

            switch (error)
            {
                case Interop.Errors.ERROR_ARITHMETIC_OVERFLOW:
                case Interop.Errors.ERROR_MORE_DATA:
                    return EventProvider.WriteEventErrorCode.EventTooBig;
                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    return EventProvider.WriteEventErrorCode.NoFreeBuffers;
            }

            return EventProvider.WriteEventErrorCode.NoError;
        }

        // Get or set the per-thread activity ID.
        int IEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return Interop.Advapi32.EventActivityIdControl(
                ControlCode,
                ref ActivityId);
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
        {
            throw new System.NotSupportedException();
        }
    }

#endif
    internal sealed class NoOpEventProvider : IEventProvider
    {
        unsafe uint IEventProvider.EventRegister(
            EventSource eventSource,
            Interop.Advapi32.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle)
        {
            return 0;
        }

        uint IEventProvider.EventUnregister(long registrationHandle)
        {
            return 0;
        }

        unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            return EventProvider.WriteEventErrorCode.NoError;
        }

        int IEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return 0;
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
        {
            return IntPtr.Zero;
        }
    }
}
