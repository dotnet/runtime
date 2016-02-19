// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// This class is meant to be inherited by a user eventSource (which provides specific events and then
    /// calls code:EventSource.WriteEvent to log them).
    /// 
    /// sealed class MinimalEventSource : EventSource
    /// {
    ///     * public void Load(long ImageBase, string Name) { WriteEvent(1, ImageBase, Name); }
    ///     * public void Unload(long ImageBase) { WriteEvent(2, ImageBase); }
    ///     * private MinimalEventSource() {}
    /// }
    /// 
    /// This functionaity is sufficient for many users.   When more control is needed over the ETW manifest
    /// that is created, that can be done by adding [Event] attributes on the  methods.
    /// 
    /// Finally for very advanced EventSources, it is possible to intercept the commands being given to the
    /// eventSource and change what filtering is done (or cause actions to be performed by the eventSource (eg
    /// dumping a data structure).  
    /// 
    /// The eventSources can be turned on with Window ETW controllers (eg logman), immediately.  It is also
    /// possible to control and intercept the data dispatcher programatically.  We code:EventListener for
    /// more.      
    /// </summary>
    public class EventSource : IDisposable
    {
        /// <summary>
        /// The human-friendly name of the eventSource.  It defaults to the simple name of the class
        /// </summary>
        public string Name { get { return m_name; } }
        /// <summary>
        /// Every eventSource is assigned a GUID to uniquely identify it to the system. 
        /// </summary>
        public Guid Guid { get { return m_guid; } }

        /// <summary>
        /// Returns true if the eventSource has been enabled at all.
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        public bool IsEnabled()
        {
            return m_eventSourceEnabled;
        }
        /// <summary>
        /// Returns true if events with >= 'level' and have one of 'keywords' set are enabled. 
        /// 
        /// Note that the result of this function only an approximiation on whether a particular
        /// event is active or not. It is only meant to be used as way of avoiding expensive
        /// computation for logging when logging is not on, therefore it sometimes returns false
        //  positives (but is always accurate when returning false).  EventSources are free to 
        /// have additional filtering.    
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        public bool IsEnabled(EventLevel level, EventKeywords keywords)
        {
            if (!m_eventSourceEnabled)
                return false;
            if (m_level != 0 && m_level < level)
                return false;
            if (m_matchAnyKeyword != 0 && (keywords & m_matchAnyKeyword) == 0)
                return false;
            return true;
        }

        // Manifest support 
        /// <summary>
        /// Returns the GUID that uniquely identifies the eventSource defined by 'eventSourceType'.  
        /// This API allows you to compute this without actually creating an instance of the EventSource.   
        /// It only needs to reflect over the type.  
        /// </summary>
        public static Guid GetGuid(Type eventSourceType)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Returns the official ETW Provider name for the eventSource defined by 'eventSourceType'.  
        /// This API allows you to compute this without actually creating an instance of the EventSource.   
        /// It only needs to reflect over the type.  
        /// </summary>
        public static string GetName(Type eventSourceType)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Returns a string of the XML manifest associated with the curEventSource. The scheme for this XML is
        /// documented at in EventManifest Schema http://msdn.microsoft.com/en-us/library/aa384043(VS.85).aspx
        /// </summary>
        /// <param name="assemblyPathForManifest">The manifest XML fragment contains the string name of the DLL name in
        /// which it is embeded.  This parameter spcifies what name will be used</param>
        /// <returns>The XML data string</returns>
        public static string GenerateManifest(Type eventSourceType, string assemblyPathToIncludeInManifest)
        {
            throw new PlatformNotSupportedException();
        }

        // EventListener support
        /// <summary>
        /// returns a list (IEnumerable) of all sources in the appdomain).  EventListners typically need this.  
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventSource> GetSources()
        {
            var ret = new List<EventSource>();
            lock (EventListener.EventListenersLock)
            {
                foreach (WeakReference eventSourceRef in EventListener.s_EventSources)
                {
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource != null)
                        ret.Add(eventSource);
                }
            }
            return ret;
        }

        /// <summary>
        /// Send a command to a particular EventSource identified by 'eventSource'
        /// 
        /// Calling this routine simply forwards the command to the EventSource.OnEventCommand
        /// callback.  What the EventSource does with the command and its arguments are from that point
        /// EventSource-specific.  
        /// 
        /// The eventSource is passed the EventListener that issued the command along with the command and
        /// arguments.  The contract is that to the extent possible the eventSource should not affect other
        /// EventListeners (eg filtering events), however sometimes this simply is not possible (if the
        /// command was to provoke a GC, or a System flush etc).   
        /// </summary>
        public static void SendCommand(EventSource eventSource, EventCommand command, IDictionary<string, string> commandArguments)
        {
            if (eventSource == null)
                throw new ArgumentNullException(nameof(eventSource));

            // User-defined EventCommands should not conflict with the reserved commands.
            if ((int)command <= (int)EventCommand.Update && (int)command != (int)EventCommand.SendManifest)
                throw new ArgumentException(SR.ArgumentOutOfRange_NeedPosNum, nameof(command));

            eventSource.SendCommand(null, command, true, EventLevel.LogAlways, EventKeywords.None, commandArguments);
        }

        // ActivityID support (see also WriteEventWithRelatedActivityIdCore)
        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another 
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. This API
        /// should be used when the caller knows the thread's current activity (the one being
        /// overwritten) has completed. Otherwise, callers should prefer the overload that
        /// return the oldActivityThatWillContinue (below).
        /// 
        /// All events created with the EventSource on this thread are also tagged with the 
        /// activity ID of the thread. 
        /// 
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity 
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark 
        /// the current thread</param>
        [System.Security.SecuritySafeCritical]
        public static void SetCurrentThreadActivityId(Guid activityId)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another 
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. It returns 
        /// whatever activity the thread was previously marked with. There is a  convention that
        /// callers can assume that callees restore this activity mark before the callee returns. 
        /// To encourage this this API returns the old activity, so that it can be restored later.
        /// 
        /// All events created with the EventSource on this thread are also tagged with the 
        /// activity ID of the thread. 
        /// 
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity 
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark 
        /// the current thread</param>
        /// <param name="oldActivityThatWillContinue">The Guid that represents the current activity  
        /// which will continue at some point in the future, on the current thread</param>
        [System.Security.SecuritySafeCritical]
        public static void SetCurrentThreadActivityId(Guid activityId, out Guid oldActivityThatWillContinue)
        {
            throw new PlatformNotSupportedException();
        }

        public static Guid CurrentThreadActivityId
        {
            [System.Security.SecurityCritical]
            get
            {
                throw new PlatformNotSupportedException();
            }
        }


        // Error APIs.  (We don't throw by default, but you can probe for status)
        /// <summary>
        /// Because
        /// 
        ///     1) Logging is often optional and thus should not generate fatal errors (exceptions)
        ///     2) EventSources are often initialized in class constructors (which propagate exceptions poorly)
        ///     
        /// The event source constructor does not throw exceptions.  Instead we remember any exception that 
        /// was generated (it is also logged to Trace.WriteLine).
        /// </summary>
        public Exception ConstructionException { get { return m_constructionException; } }

        /// <summary>
        /// Displays thew name and GUID for the eventSoruce for debugging purposes.  
        /// </summary>
        public override string ToString() { return SR.Format(SR.EventSource_ToString, Name, Guid); }

        #region protected
        /// <summary>
        /// This is the consturctor that most users will use to create their eventSource.   It takes 
        /// no parameters.  The ETW provider name and GUID of the EventSource are determined by the EventSource 
        /// custom attribute (so you can determine these things declaratively).   If the GUID for the eventSource
        /// is not specified in the EventSourceAttribute (recommended), it is Generated by hashing the name.
        /// If the ETW provider name of the EventSource is not given, the name of the EventSource class is used as
        /// the ETW provider name.
        /// </summary>
        protected EventSource()
            : this(false)
        {
        }

        /// <summary>
        /// By default calling the 'WriteEvent' methods do NOT throw on errors (they silently discard the event).  
        /// This is because in most cases users assume logging is not 'precious' and do NOT wish to have logging falures
        /// crash the program.   However for those applications where logging is 'precious' and if it fails the caller
        /// wishes to react, setting 'throwOnEventWriteErrors' will cause an exception to be thrown if WriteEvent
        /// fails.   Note the fact that EventWrite succeeds does not necessarily mean that the event reached its destination
        /// only that operation of writing it did not fail.   
        /// </summary>
        protected EventSource(bool throwOnEventWriteErrors)
        {
            try
            {
                m_throwOnEventWriteErrors = throwOnEventWriteErrors;
                Initialize();
                m_constructionException = m_lastCommandException;
            }
            catch (Exception e)
            {
                Contract.Assert(m_eventData == null && m_eventSourceEnabled == false);
                ReportOutOfBandMessage("ERROR: Exception during construction of EventSource " + Name + ": "
                                + e.Message, false);
                m_eventSourceEnabled = false;       // This is insurance, it should still be off.    
                if (m_lastCommandException != null)
                    m_constructionException = m_lastCommandException;
                else
                    m_constructionException = e;
            }
        }

        protected virtual void GetMetadata(out Guid eventSourceGuid, out string eventSourceName, out EventDescriptor[] eventDescriptors, out byte[] manifestBytes)
        {
            //
            // Subclasses need to override this method, and return the data from their EventSourceAttribute and EventAttribute annotations.
            //
            // eventDescriptors needs to contain one EventDescriptor for each event; the event's ID should be the same as its index in this array.
            // manifestBytes is a UTF-8 encoding of the ETW manifest for the type.
            //
            // This will be implemented by an IL rewriter, so we can't make this method abstract or the initial build of the subclass would fail.
            //
            throw new InvalidOperationException(SR.EventSource_ImplementGetMetadata);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "guid")]
        [SecuritySafeCritical]
        private void Initialize()
        {
            Guid eventSourceGuid;
            string eventSourceName;
            EventDescriptor[] eventDescriptors;
            byte[] manifest;
            GetMetadata(out eventSourceGuid, out eventSourceName, out eventDescriptors, out manifest);

            if (eventSourceGuid.Equals(Guid.Empty))
                throw new ArgumentException(SR.EventSource_NeedGuid);
            if (eventSourceName == null)
                throw new ArgumentException(SR.EventSource_NeedName);
            if (eventDescriptors == null)
                throw new ArgumentException(SR.EventSource_NeedDescriptors);
            if (manifest == null)
                throw new ArgumentException(SR.EventSource_NeedManifest);

            m_name = eventSourceName;
            m_guid = eventSourceGuid;
            m_eventData = eventDescriptors;
            m_rawManifest = manifest;
            m_provider = new OverideEventProvider(this);

            try
            {
                m_provider.Register(eventSourceGuid);
            }
            catch (ArgumentException)
            {
                // Failed to register.  Don't crash the app, just don't write events to ETW.
                m_provider = null;
            }

            if (m_eventSourceEnabled && !m_ETWManifestSent)
            {
                SendManifest(m_rawManifest);
                m_ETWManifestSent = true;
            }


            // Add the curEventSource to the global (weak) list.  This also sets m_id, which is the
            // index in the list. 
            EventListener.AddEventSource(this);

            // We are logically completely initialized at this point.  
            m_completelyInited = true;
        }

        /// <summary>
        /// This method is called when the eventSource is updated by the controller.  
        /// </summary>
        protected virtual void OnEventCommand(EventCommandEventArgs command) { }

        // optimized for common signatures (no args)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId)
        {
            WriteEventCore(eventId, 0, null);
        }

        // optimized for common signatures (ints)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, int arg1)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, int arg1, int arg2)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        // optimized for common signatures (longs)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, long arg1)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, long arg1, long arg2)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, long arg1, long arg2, long arg3)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 8;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        // optimized for common signatures (strings)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                fixed (char* string1Bytes = arg1)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[1];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    WriteEventCore(eventId, 1, descrs);
                }
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1, string arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
                if (arg3 == null) arg3 = "";
                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                fixed (char* string3Bytes = arg3)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        // optimized for common signatures (string and ints)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1, int arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                fixed (char* string1Bytes = arg1)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                fixed (char* string1Bytes = arg1)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 4;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        // optimized for common signatures (string and longs)
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, string arg1, long arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null) arg1 = "";
                fixed (char* string1Bytes = arg1)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 8;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

#pragma warning disable 649   // Disable warning about m_Reserved never being assigned.
        protected internal struct EventData
        {
            public IntPtr DataPointer { get { return (IntPtr)m_Ptr; } set { m_Ptr = (long)value; } }
            public int Size { get { return m_Size; } set { m_Size = value; } }

            #region private
            //Important, we pass this structure directly to the Win32 EventWrite API, so this structure must be layed out exactly
            // the way EventWrite wants it.  
            private long m_Ptr;
            private int m_Size;
            internal int m_Reserved;	// Used to pad the size to match the Win32 API
            #endregion
        }
#pragma warning restore 649

        /// <summary>
        /// This routine allows you to create efficient WriteEvent helpers, however the code that you use to
        /// do this while straightfoward is unsafe.  See the bodies of the WriteEvent helpers above for its use.     
        /// </summary>
        [SecurityCritical]
        [CLSCompliant(false)]
        protected unsafe void WriteEventCore(int eventId, int eventDataCount, EventSource.EventData* data)
        {
            if (m_eventData != null)
            {
                if (m_eventData[eventId].EnabledForETW)
                {
                    EventProvider.EventData* dataDescrs = stackalloc EventProvider.EventData[eventDataCount];
                    if (eventDataCount <= 0)
                    {
                        // EventWrite expects NULL if dataDescrs is zero.
                        // You cannot pass a ptr to an empty array.
                        dataDescrs = (EventProvider.EventData*)IntPtr.Zero;
                    }

                    for (int i = 0; i < eventDataCount; i++)
                    {
                        dataDescrs[i].Size = (uint)data[i].Size;
                        dataDescrs[i].Ptr = (ulong)(data[i].DataPointer.ToInt64());
                        dataDescrs[i].Reserved = 0;
                    }

                    if (!m_provider.WriteEvent(ref m_eventData[eventId].Descriptor, eventDataCount, (IntPtr)dataDescrs) && m_throwOnEventWriteErrors)
                        throw new EventSourceException();
                }

                if (m_Dispatchers != null && m_eventData[eventId].EnabledForAnyListener)
                {
                    object[] args = new object[eventDataCount];

                    for (int i = 0; i < eventDataCount; i++)
                    {
                        args[i] = DecodeObject(eventId, i, data[i].Size, data[i].DataPointer);
                    }

                    WriteToAllListeners(eventId, args);
                }
            }
        }

        /// <summary>
        /// This routine allows you to create efficient WriteEventCreatingChildActivity helpers, however the code 
        /// that you use to do this while straightfoward is unsafe.  See the bodies of the WriteEvent helpers above 
        /// for its use.   The only difference is that you pass the ChildAcivityID from caller through to this API
        /// </summary>
        [CLSCompliant(false)]
        [SecurityCritical] // required to match contract
        protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, Guid* childActivityID, int eventDataCount, EventData* data)
        {
            throw new PlatformNotSupportedException();
        }

        // fallback varags helpers. 
        /// <summary>
        /// This is the varargs helper for writing an event.   It does create an array and box all the arguments so it is
        /// relatively inefficient and should only be used for relatively rare events (e.g. less than 100 / sec).  If you
        /// rates are fast than that you should be used WriteEventCore to create fast helpers for your particular method
        /// signature.   Even if you use this for rare evnets, this call should be guarded by a 'IsEnabled()' check so that 
        /// the varargs call is not made when the EventSource is not active.  
        /// </summary>
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        protected unsafe void WriteEvent(int eventId, params object[] args)
        {
            Contract.Assert(m_eventData == null || m_eventData[eventId].Descriptor.Id != 0);
            if (m_eventData != null)
            {
                if (m_eventData[eventId].EnabledForETW)
                    if (!m_provider.WriteEvent(ref m_eventData[eventId].Descriptor, args) && m_throwOnEventWriteErrors)
                        throw new EventSourceException();

                if (m_Dispatchers != null && m_eventData[eventId].EnabledForAnyListener) WriteToAllListeners(eventId, args);
            }
        }

        /// <summary>
        /// This is the varargs helper for writing an event which also creates a child activity.  It is completely analygous
        /// to cooresponding WriteEvent (they share implementation).   It does create an array and box all the arguments so it is
        /// relatively inefficient and should only be used for relatively rare events (e.g. less than 100 / sec).  If you
        /// rates are fast than that you should be used WriteEventCore to create fast helpers for your particular method
        /// signature.   Even if you use this for rare evnets, this call should be guarded by a 'IsEnabled()' check so that 
        /// the varargs call is not made when the EventSource is not active. 
        /// </summary>
        protected void WriteEventWithRelatedActivityId(int eventId, Guid childActivityID, params object[] args)
        {
            throw new PlatformNotSupportedException();
        }

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Disposes of an EventSource.
        /// </summary>
        /// <remarks>
        /// Called from Dispose() with disposing=true, and from the finalizer (~MeasurementBlock) with disposing=false.
        /// Guidelines:
        /// 1. We may be called more than once: do nothing after the first call.
        /// 2. Avoid throwing exceptions if disposing is false, i.e. if we're being finalized.
        /// </remarks>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_provider != null)
                {
                    m_provider.Dispose();
                    m_provider = null;
                }
            }
        }
        ~EventSource()
        {
            this.Dispose(false);
        }
        #endregion

        #region private

        [SecurityCritical]
        private unsafe object DecodeObject(int eventId, int parameterId, int dataBytes, IntPtr dataPointer)
        {
            switch (m_eventData[eventId].ParameterTypes[parameterId])
            {
                case EventParameterType.IntPtr:
                    return *((IntPtr*)dataPointer);
                case EventParameterType.Int32:
                    return *((int*)dataPointer);
                case EventParameterType.UInt32:
                    return *((uint*)dataPointer);
                case EventParameterType.Int64:
                    return *((long*)dataPointer);
                case EventParameterType.UInt64:
                    return *((ulong*)dataPointer);
                case EventParameterType.Byte:
                    return *((byte*)dataPointer);
                case EventParameterType.SByte:
                    return *((sbyte*)dataPointer);
                case EventParameterType.Int16:
                    return *((short*)dataPointer);
                case EventParameterType.UInt16:
                    return *((ushort*)dataPointer);
                case EventParameterType.Single:
                    return *((float*)dataPointer);
                case EventParameterType.Double:
                    return *((double*)dataPointer);
                case EventParameterType.Decimal:
                    return *((decimal*)dataPointer);
                case EventParameterType.Boolean:
                    // The manifest defines a bool as a 32bit type (WIN32 BOOL), not 1 bit as CLR Does.
                    if (*((int*)dataPointer) == 1)
                        return true;
                    else
                        return false;
                case EventParameterType.Guid:
                    return *((Guid*)dataPointer);
                case EventParameterType.Char:
                    return *((char*)dataPointer);
                case EventParameterType.String:
                    // ETW strings are NULL-terminated, so marshal everything up to the first
                    // null in the string.
                    return new String((char*)dataPointer);
                default:
                    throw new InvalidOperationException("EventSource_InvalidEventParameterType");
            }
        }

        // Finds the Dispatcher (which holds the filtering state), for a given dispatcher for the current
        // eventSource).  
        private EventDispatcher GetDispatcher(EventListener listener)
        {
            EventDispatcher dispatcher = m_Dispatchers;
            while (dispatcher != null)
            {
                if (dispatcher.m_Listener == listener)
                    return dispatcher;
                dispatcher = dispatcher.m_Next;
            }
            return dispatcher;
        }

        // helper for writing to all EventListeners attached the current curEventSource.  
        private void WriteToAllListeners(int eventId, params object[] args)
        {
            EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this);
            eventCallbackArgs.EventId = eventId;
            eventCallbackArgs.Payload = new ReadOnlyCollection<object>(args);

            Exception lastThrownException = null;

            for (EventDispatcher dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
            {
                if (dispatcher.m_EventEnabled[eventId])
                {
                    try
                    {
                        dispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
                    }
                    catch (Exception e)
                    {
                        lastThrownException = e;
                    }
                }
            }

            if (lastThrownException != null && m_throwOnEventWriteErrors)
            {
                throw new EventSourceException(lastThrownException);
            }
        }

        /// <summary>
        /// Returns true if 'eventNum' is enabled if you only consider the level and matchAnyKeyword filters.
        /// It is possible that eventSources turn off the event based on additional filtering criteria.  
        /// </summary>
        private bool IsEnabledByDefault(int eventNum, bool enable, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword)
        {
            if (!enable)
                return false;

            EventLevel eventLevel = (EventLevel)m_eventData[eventNum].Descriptor.Level;
            EventKeywords eventKeywords = (EventKeywords)m_eventData[eventNum].Descriptor.Keyword;

            if ((eventLevel <= currentLevel) || (currentLevel == 0))
            {
                if ((eventKeywords == 0) || ((eventKeywords & currentMatchAnyKeyword) != 0))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// This class lets us hook the 'OnEventCommand' from the eventSource.  
        /// </summary>
        private class OverideEventProvider : EventProvider
        {
            public OverideEventProvider(EventSource eventSource)
            {
                this.m_eventSource = eventSource;
            }
            protected override void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments)
            {
                // We use null to represent the ETW EventListener.  
                EventListener listener = null;
                m_eventSource.SendCommand(listener,
                                          (EventCommand)command, IsEnabled(), Level, MatchAnyKeyword, arguments);
            }
            private EventSource m_eventSource;
        }

        protected internal enum EventParameterType
        {
            Boolean,
            Byte,
            SByte,
            Char,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            IntPtr,
            Single,
            Double,
            Decimal,
            Guid,
            String
        }

        /// <summary>
        /// Used to hold all the static information about an event.  This includes everything in the event
        /// descriptor as well as some stuff we added specifically for EventSource. see the
        /// code:m_eventData for where we use this.  
        /// </summary>
        protected internal struct EventDescriptor
        {
            [CLSCompliant(false)]
            public EventDescriptor(ushort id, byte version, byte channel, byte level, byte opcode, ushort task, ulong keyword, string message, EventParameterType[] parameterTypes)
                : this()
            {
                Descriptor.Id = id;
                Descriptor.Version = version;
                Descriptor.Channel = channel;
                Descriptor.Level = level;
                Descriptor.Opcode = opcode;
                Descriptor.Task = task;
                Descriptor.Keyword = keyword;

                Message = message;
                ParameterTypes = parameterTypes;
            }
            internal Interop.mincore._EVENT_DESCRIPTOR Descriptor;
            internal bool EnabledForAnyListener;      // true if any dispatcher has this event turned on
            internal bool EnabledForETW;              // is this event on for the OS ETW data dispatcher?
            public string Message { get; set; }
            public EventParameterType[] ParameterTypes { get; set; }
            public int EventId
            {
                get { return Descriptor.Id; }
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("id", SR.ArgumentOutOfRange_NeedNonNegNum);
                    if (value > ushort.MaxValue)
                        throw new ArgumentOutOfRangeException("id", SR.Format(SR.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
                    Descriptor.Id = (ushort)value;
                }
            }
            public EventLevel Level
            {
                get { return (EventLevel)Descriptor.Level; }
                set { Descriptor.Level = (byte)value; }
            }
            public EventKeywords Keywords
            {
                get { return (EventKeywords)Descriptor.Keyword; }
                set { Descriptor.Keyword = (ulong)value; }
            }
            public EventOpcode Opcode
            {
                get { return (EventOpcode)Descriptor.Opcode; }
                set { Descriptor.Opcode = (byte)value; }
            }
            public EventTask Task
            {
                get { return (EventTask)Descriptor.Task; }
                set { Descriptor.Task = (ushort)value; }
            }
#if FEATURE_MANAGED_ETW_CHANNELS
            public EventChannel Channel
            {
                get { return (EventChannel)Descriptor.Channel; }
                set { Descriptor.Channel = (byte)value; }
            }
#endif
            public byte Version
            {
                get { return Descriptor.Version; }
                set { Descriptor.Version = value; }
            }
        };

        // This is the internal entry point that code:EventListeners call when wanting to send a command to a
        // eventSource. The logic is as follows
        // 
        // * if Command == Update
        //     * enabled, level, matchAnyKeywords are used to set a default for all events for the
        //         curEventSource.  In particular, if 'enabled' is false, 'level' and
        //         'matchAnyKeywords' are not used.  
        //     * OnEventCommand is invoked, which may cause calls to
        //         code:EventSource.EnableEventForDispatcher which may cause changes in the filtering
        //         depending on the logic in that routine.
        // * else (command != Update)
        //     * Simply call OnEventCommand. The expectation is that filtering is NOT changed.
        //     * The 'enabled' 'level', matchAnyKeyword' arguments are ignored (must be true, 0, 0).  
        // 
        // dispatcher == null has special meaning. It is the 'ETW' dispatcher.
        internal void SendCommand(EventListener listener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> commandArguments)
        {
            m_lastCommandException = null;
            try
            {
                InsureInitialized();

                // Find the per-EventSource dispatcher cooresponding to registered dispatcher
                EventDispatcher eventSourceDispatcher = GetDispatcher(listener);
                if (eventSourceDispatcher == null && listener != null)     // dispatcher == null means ETW dispatcher
                    throw new ArgumentException(SR.EventSource_ListenerNotFound);

                if (commandArguments == null)
                    commandArguments = new Dictionary<string, string>();

                if (command == EventCommand.Update)
                {
                    // Set it up using the 'standard' filtering bitfields
                    for (int i = 0; i < m_eventData.Length; i++)
                        EnableEventForDispatcher(eventSourceDispatcher, i, IsEnabledByDefault(i, enable, level, matchAnyKeyword));

                    command = EventCommand.Disable;
                    if (enable)
                    {
                        command = EventCommand.Enable;
                        if (!m_eventSourceEnabled)
                        {
                            // EventSource turned on for the first time, simply copy the bits.  
                            m_level = level;
                            m_matchAnyKeyword = matchAnyKeyword;
                        }
                        else
                        {
                            // Already enabled, make it the most verbose of the existing and new filter
                            if (level > m_level)
                                m_level = level;
                            if (matchAnyKeyword == 0)
                                m_matchAnyKeyword = 0;
                            else if (m_matchAnyKeyword != 0)
                                m_matchAnyKeyword |= matchAnyKeyword;
                        }

                        // Send the manifest if we are writing to ETW 
                        if (eventSourceDispatcher == null)
                        {
                            // eventSourceDispatcher == null means this is the ETW manifest
                            // If we are not completely initalized we can't send the manifest because WriteEvent
                            // will fail (handle was not returned from OS API).  We will try again after the
                            // constuctor completes.  
                            if (!m_ETWManifestSent && m_completelyInited)
                            {
                                m_ETWManifestSent = true;
                                SendManifest(m_rawManifest);
                            }
                        }
                    }

                    this.OnEventCommand(new EventCommandEventArgs(command, commandArguments, this, eventSourceDispatcher));

                    if (enable)
                    {
                        m_eventSourceEnabled = true;
                    }
                    else
                    {
                        // If we are disabling, maybe we can turn so 'quick checks' to filter
                        // quickly.  These are all just optimizations (since later checks will still filter)

                        // reset the 'manifestSent' bit for the listener.   
                        if (eventSourceDispatcher == null)
                            m_ETWManifestSent = false;          // Null dispatcher means ETW dispatcher.  

                        // There is a good chance EnabledForAnyListener are not as accurate as
                        // they could be, go ahead and get a better estimate.  
                        for (int i = 0; i < m_eventData.Length; i++)
                        {
                            m_eventData[i].EnabledForAnyListener = false;
                            for (EventDispatcher dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
                            {
                                if (dispatcher.m_EventEnabled[i])
                                {
                                    m_eventData[i].EnabledForAnyListener = true;
                                    break;
                                }
                            }
                        }

                        // If no events are enabled, disable the global enabled bit.
                        if (!AnyEventEnabled())
                        {
                            m_level = 0;
                            m_matchAnyKeyword = 0;
                            m_eventSourceEnabled = false;
                        }
                    }
                }
                else
                {
                    if (command == EventCommand.SendManifest)
                        SendManifest(m_rawManifest);

                    // These are not used for non-update commands and thus should always be 'default' values
                    Contract.Assert(enable == true);
                    Contract.Assert(m_level == EventLevel.LogAlways);
                    Contract.Assert(m_matchAnyKeyword == EventKeywords.None);

                    this.OnEventCommand(new EventCommandEventArgs(command, commandArguments, null, null));
                }
            }
            catch (Exception e)
            {
                // Remember any exception and rethrow.  
                m_lastCommandException = e;
                throw;
            }
        }

        /// <summary>
        /// If 'value is 'true' then set the eventSource so that 'dispatcher' will recieve event with the eventId
        /// of 'eventId.  If value is 'false' disable the event for that dispatcher.   If 'eventId' is out of
        /// range return false, otherwise true.  
        /// </summary>
        internal bool EnableEventForDispatcher(EventDispatcher dispatcher, int eventId, bool value)
        {
            if (dispatcher == null)
            {
                if (eventId >= m_eventData.Length)
                    return false;

                if (m_provider != null)
                    m_eventData[eventId].EnabledForETW = value;
            }
            else
            {
                if (eventId >= dispatcher.m_EventEnabled.Length)
                    return false;
                dispatcher.m_EventEnabled[eventId] = value;
                if (value)
                    m_eventData[eventId].EnabledForAnyListener = true;
            }
            return true;
        }

        /// <summary>
        /// Returns true if any event at all is on.  
        /// </summary>
        private bool AnyEventEnabled()
        {
            for (int i = 0; i < m_eventData.Length; i++)
                if (m_eventData[i].EnabledForETW || m_eventData[i].EnabledForAnyListener)
                    return true;
            return false;
        }

        private void InsureInitialized()
        {
            lock (EventListener.EventListenersLock)
            {
                // TODO Enforce singleton pattern 
                foreach (WeakReference eventSourceRef in EventListener.s_EventSources)
                {
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource != null && eventSource.Guid.Equals(m_guid))
                    {
                        if (eventSource != this)
                            throw new ArgumentException(SR.Format(SR.EventSource_EventSourceGuidInUse, m_guid));
                    }
                }

                // Make certain all dispatchers are also have their array's initialized
                EventDispatcher dispatcher = m_Dispatchers;
                while (dispatcher != null)
                {
                    if (dispatcher.m_EventEnabled == null)
                        dispatcher.m_EventEnabled = new bool[m_eventData.Length];
                    dispatcher = dispatcher.m_Next;
                }
            }
        }

        // Send out the ETW manifest XML out to ETW
        // Today, we only send the manifest to ETW, custom listeners don't get it. 
        [SecuritySafeCritical]
        private unsafe bool SendManifest(byte[] rawManifest)
        {
            bool success = true;

            if (rawManifest.Length == 1)
                throw new ArgumentException(SR.EventSource_NoManifest);  // EventSourceTransformer detected an illegal event method and inserted a 1-length manifest as a sentinel.

            fixed (byte* dataPtr = rawManifest)
            {
                Interop.mincore._EVENT_DESCRIPTOR manifestDescr = new Interop.mincore._EVENT_DESCRIPTOR()
                {
                    Id = 0xFFFE,
                    Version = 1,
                    Opcode = 0xFE,
                    Task = 0xFFFE,
                    Keyword = 0xFFFFFFFFFFFFFFFF
                };
                ManifestEnvelope envelope = new ManifestEnvelope();

                envelope.Format = ManifestEnvelope.ManifestFormats.SimpleXmlFormat;
                envelope.MajorVersion = 1;
                envelope.MinorVersion = 0;
                envelope.Magic = 0x5B;              // An unusual number that can be checked for consistancy. 
                int dataLeft = rawManifest.Length;
                envelope.TotalChunks = (ushort)((dataLeft + (ManifestEnvelope.MaxChunkSize - 1)) / ManifestEnvelope.MaxChunkSize);
                envelope.ChunkNumber = 0;

                EventProvider.EventData* dataDescrs = stackalloc EventProvider.EventData[2];
                dataDescrs[0].Ptr = (ulong)&envelope;
                dataDescrs[0].Size = (uint)sizeof(ManifestEnvelope);
                dataDescrs[0].Reserved = 0;

                dataDescrs[1].Ptr = (ulong)dataPtr;
                dataDescrs[1].Reserved = 0;

                while (dataLeft > 0)
                {
                    dataDescrs[1].Size = (uint)Math.Min(dataLeft, ManifestEnvelope.MaxChunkSize);
                    if (m_provider != null)
                    {
                        if (!m_provider.WriteEvent(ref manifestDescr, 2, (IntPtr)dataDescrs))
                        {
                            success = false;
                            if (m_throwOnEventWriteErrors)
                            {
                                throw new EventSourceException();
                            }
                        }
                    }
                    dataLeft -= ManifestEnvelope.MaxChunkSize;
                    dataDescrs[1].Ptr += ManifestEnvelope.MaxChunkSize;
                    envelope.ChunkNumber++;
                }
            }

            return success;
        }

        // Helper used by code:EventListener.AddEventSource and code:EventListener.EventListener
        // when a listener gets attached to a eventSource
        internal void AddListener(EventListener listener)
        {
            lock (EventListener.EventListenersLock)
            {
                bool[] enabledArray = null;
                if (m_eventData != null)
                    enabledArray = new bool[m_eventData.Length];
                m_Dispatchers = new EventDispatcher(m_Dispatchers, enabledArray, listener);
                listener.OnEventSourceCreated(this);
            }
        }


        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        internal void ReportOutOfBandMessage(string msg, bool flush)
        {
            // msg == null is a signal to flush what's accumulated in the buffer
            if (msg == null && flush)
            {
                if (!string.IsNullOrEmpty(m_deferredErrorInfo))
                {
                    //WriteString(m_deferredErrorInfo);
                    m_deferredErrorInfo = String.Empty;
                }
                return;
            }

            if (!msg.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                msg = msg + Environment.NewLine;

            // send message to debugger without delay
            Debug.WriteLine(msg);

            m_deferredErrorInfo = m_deferredErrorInfo + msg;
            if (flush)
            {
                // send message to the ETW listener if available
                //WriteString(m_deferredErrorInfo);
                m_deferredErrorInfo = String.Empty;
            }
        }


        // private instance state 
        private string m_name;                          // My friendly name (privided in ctor)
        internal int m_id;                              // A small integer that is unique to this instance.  
        private Guid m_guid;                            // GUID representing the ETW eventSource to the OS.  
        internal volatile EventDescriptor[] m_eventData;               // None per-event data
        private volatile byte[] m_rawManifest;                   // Bytes to send out representing the event schema
        private readonly bool m_throwOnEventWriteErrors;              // If a listener throws and error, should we catch it or not

        // Enabling bits
        private bool m_eventSourceEnabled;                 // am I enabled (any of my events are enabled for any dispatcher)
        internal EventLevel m_level;                    // higest level enabled by any output dispatcher
        internal EventKeywords m_matchAnyKeyword;       // the logical OR of all levels enabled by any output dispatcher (zero is a special case) meaning 'all keywords'

        // Dispatching state 
        internal volatile EventDispatcher m_Dispatchers; // Linked list of code:EventDispatchers we write the data to (we also do ETW specially)
        private volatile OverideEventProvider m_provider;        // This hooks up ETW commands to our 'OnEventCommand' callback

        private bool m_completelyInited;                // The EventSource constructor has returned without exception.  
        private bool m_ETWManifestSent;                 // we sent the ETW manifest as an event in the callback

        private Exception m_lastCommandException;       // If there was an exception during a command, this is it.  
        private Exception m_constructionException;      // If there was an exception construction, this is it 
        private string m_deferredErrorInfo;             // non-fatal error info accumulated during construction
        #endregion
    }

    /// <summary>
    /// An code:EventListener represents the target for all events generated by EventSources (that is
    /// subclasses of code:EventSource), in the currnet appdomain. When a new EventListener is created
    /// it is logically attached to all eventSources in that appdomain. When the EventListener is Disposed, then
    /// it is disconnected from the event eventSources. Note that there is a internal list of STRONG references
    /// to EventListeners, which means that relying on the lack of references ot EventListeners to clean up
    /// EventListeners will NOT work. You must call EventListener.Dispose explicitly when a dispatcher is no
    /// longer needed.
    /// 
    /// Once created, EventListeners can enable or disable on a per-eventSource basis using verbosity levels
    /// (code:EventLevel) and bitfields code:EventKeywords to further restrict the set of events to be sent
    /// to the dispatcher. The dispatcher can also send arbitrary commands to a particular eventSource using the
    /// 'SendCommand' method. The meaning of the commands are eventSource specific.
    /// 
    /// The Null Guid (that is (new Guid()) has special meaning as a wildcard for 'all current eventSources in
    /// the appdomain'. Thus it is relatively easy to turn on all events in the appdomain if desired.
    /// 
    /// It is possible for there to be many EventListener's defined in a single appdomain. Each dispatcher is
    /// logically independent of the other listeners. Thus when one dispatcher enables or disables events, it
    /// affects only that dispatcher (other listeners get the events they asked for). It is possible that
    /// commands sent with 'SendCommand' would do a semantic operation that would affect the other listeners
    /// (like doing a GC, or flushing data ...), but this is the exception rather than the rule.
    /// 
    /// Thus the model is that each EventSource keeps a list of EventListeners that it is sending events
    /// to. Associated with each EventSource-dispatcher pair is a set of filtering criteria that determine for
    /// that eventSource what events that dispatcher will recieve.
    /// 
    /// Listeners receive the events on their 'OnEventWritten' method. Thus subclasses of EventListener must
    /// override this method to do something useful with the data.
    /// 
    /// In addition, when new eventSources are created, the 'OnEventSourceCreate' method is called. The
    /// invariant associated with this callback is that every eventSource gets exactly one
    /// 'OnEventSourceCreate' call for ever eventSource that can potentially send it log messages. In
    /// particular when a EventListener is created, typically a series of OnEventSourceCreate' calls are
    /// made to notify the new dispatcher of all the eventSources that existed before the EventListener was
    /// created.
    /// 
    /// </summary>
    public abstract class EventListener : IDisposable
    {
        /// <summary>
        /// Create a new EventListener in which all events start off truned off (use EnableEvents to turn
        /// them on).  
        /// </summary>
        protected EventListener()
        {
            lock (EventListenersLock)
            {
                // Find all existing eventSources call OnEventSourceCreated to 'catchup'
                foreach (WeakReference eventSourceRef in s_EventSources)
                {
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource != null)
                        eventSource.AddListener(this);
                }

                // Add to list of listeners in the system 
                this.m_Next = s_Listeners;
                s_Listeners = this;

                Validate();
            }
        }
        /// <summary>
        /// Dispose should be called when the EventListener no longer desires 'OnEvent*' callbacks. Because
        /// there is an internal list of strong references to all EventListeners, calling 'Displose' directly
        /// is the only way to actually make the listen die. Thus it is important that users of EventListener
        /// call Dispose when they are done with their logging.
        /// </summary>
        public virtual void Dispose()
        {
            lock (EventListenersLock)
            {
                Contract.Assert(s_Listeners != null);
                if (s_Listeners != null)
                {
                    if (this == s_Listeners)
                    {
                        EventListener cur = s_Listeners;
                        s_Listeners = this.m_Next;
                        RemoveReferencesToListenerInEventSources(cur);
                    }
                    else
                    {
                        // Find 'this' from the s_Listeners linked list.  
                        EventListener prev = s_Listeners;
                        for (; ;)
                        {
                            EventListener cur = prev.m_Next;
                            if (cur == null)
                                break;
                            if (cur == this)
                            {
                                // Found our Listener, remove references to to it in the eventSources
                                prev.m_Next = cur.m_Next;       // Remove entry. 
                                RemoveReferencesToListenerInEventSources(cur);
                                break;
                            }
                            prev = cur;
                        }
                    }
                }
                Validate();
            }
        }
        // We don't expose a Dispose(bool), because the contract is that you don't have any non-syncronous
        // 'cleanup' associated with this object

        /// Enable all events from the curEventSource identified by 'eventSource' to the current dispatcher that have a
        /// verbosity level of 'level' or lower.
        ///   
        /// This call can have the effect of REDUCING the number of events sent to the dispatcher if 'level'
        /// indicates a less verbose level than was previously enabled.
        /// 
        /// This call never has an effect on other EventListeners.
        ///
        /// Returns 'true' if any curEventSource could be found that matches 'eventSourceGuid'
        /// </summary>
        public void EnableEvents(EventSource eventSource, EventLevel level)
        {
            EnableEvents(eventSource, level, EventKeywords.None);
        }
        /// <summary>
        /// Enable all events from the eventSource identified by 'eventSourceGuid' to the current dispatcher that have a
        /// verbosity level of 'level' or lower and have a event keyword matching any of the bits in
        /// 'machAnyKeyword'.
        /// 
        /// This call can have the effect of REDUCING the number of events sent to the dispatcher if 'level'
        /// indicates a less verbose level than was previously enabled or if 'machAnyKeyword' has fewer
        /// keywords set than where previously set.
        /// 
        /// If eventSourceGuid is Guid.Empty, then the affects all eventSources in the appdomain
        /// 
        /// If eventSourceGuid is not Guid.Empty, this call has no effect on any other eventSources in the appdomain.
        /// 
        /// This call never has an effect on other EventListeners.
        /// 
        /// Returns 'true' if any eventSource could be found that matches 'eventSourceGuid'        
        /// </summary>
        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
        {
            EnableEvents(eventSource, level, matchAnyKeyword, null);
        }
        /// <summary>
        /// Enable all events from the eventSource identified by 'eventSource' to the current dispatcher that have a
        /// verbosity level of 'level' or lower and have a event keyword matching any of the bits in
        /// 'machAnyKeyword' as well as any (curEventSource specific) effect passing addingional 'key-value' arguments
        /// 'arguments' might have.  
        /// 
        /// This call can have the effect of REDUCING the number of events sent to the dispatcher if 'level'
        /// indicates a less verbose level than was previously enabled or if 'machAnyKeyword' has fewer
        /// keywords set than where previously set.
        /// 
        /// This call never has an effect on other EventListeners.
        /// </summary>       
        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> arguments)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            Contract.EndContractBlock();

            eventSource.SendCommand(this, EventCommand.Update, true, level, matchAnyKeyword, arguments);
        }
        /// <summary>
        /// Disables all events coming from eventSource identified by 'eventSource'.  
        /// 
        /// If eventSourceGuid is Guid.Empty, then the affects all eventSources in the appdomain
        /// 
        /// This call never has an effect on other EventListeners.      
        /// </summary>
        public void DisableEvents(EventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            Contract.EndContractBlock();

            eventSource.SendCommand(this, EventCommand.Update, false, EventLevel.LogAlways, EventKeywords.None, null);
        }

        /// <summary>
        /// This method is called whenever a new eventSource is 'attached' to the dispatcher.
        /// This can happen for all existing EventSources when the EventListener is created
        /// as well as for any EventSources that come into existance after the EventListener
        /// has been created.
        /// 
        /// These 'catch up' events are called during the construction of the EventListener.
        /// Subclasses need to be prepared for that.
        /// 
        /// In a multi-threaded environment, it is possible that 'OnEventWritten' callbacks
        /// for a paritcular eventSource to occur BEFORE the OnEventSourceCreated is issued.
        /// </summary>
        /// <param name="eventSource"></param>
        internal protected virtual void OnEventSourceCreated(EventSource eventSource) { }
        /// <summary>
        /// This method is called whenever an event has been written by a EventSource for which the EventListener
        /// has enabled events.  
        /// </summary>
        internal protected abstract void OnEventWritten(EventWrittenEventArgs eventData);
        /// <summary>
        /// EventSourceIndex is small non-negative integer (suitable for indexing in an array)
        /// identifying EventSource. It is unique per-appdomain. Some EventListeners might find
        /// it useful to store addditional information about each eventSource connected to it,
        /// and EventSourceIndex allows this extra infomation to be efficiently stored in a
        /// (growable) array (eg List(T)).
        /// </summary>
        static protected int EventSourceIndex(EventSource eventSource) { return eventSource.m_id; }

        #region private
        /// <summary>
        /// This routine adds newEventSource to the global list of eventSources, it also assigns the
        /// ID to the eventSource (which is simply the oridinal in the global list).
        /// 
        /// EventSources currently do not pro-actively remove themselves from this list. Instead
        /// when eventSources's are GCed, the weak handle in this list naturally gets nulled, and
        /// we will reuse the slot. Today this list never shrinks (but we do reuse entries
        /// that are in the list). This seems OK since the expectation is that EventSources
        /// tend to live for the lifetime of the appdomain anyway (they tend to be used in
        /// global variables).
        /// </summary>
        /// <param name="newEventSource"></param>
        internal static void AddEventSource(EventSource newEventSource)
        {
            lock (EventListenersLock)
            {
                if (s_EventSources == null)
                    s_EventSources = new List<WeakReference>(2);

                // Periodically search the list for existing entries to reuse, this avoids
                // unbounded memory use if we keep recycling eventSources (an unlikely thing). 
                int newIndex = -1;
                if (s_EventSources.Count % 64 == 63)   // on every block of 64, fill up the block before continuing
                {
                    int i = s_EventSources.Count;      // Work from the top down. 
                    while (0 < i)
                    {
                        --i;
                        WeakReference weakRef = s_EventSources[i];
                        if (!weakRef.IsAlive)
                        {
                            newIndex = i;
                            weakRef.Target = newEventSource;
                            break;
                        }
                    }
                }
                if (newIndex < 0)
                {
                    newIndex = s_EventSources.Count;
                    s_EventSources.Add(new WeakReference(newEventSource));
                }
                newEventSource.m_id = newIndex;

                // Add every existing dispatcher to the new EventSource
                for (EventListener listener = s_Listeners; listener != null; listener = listener.m_Next)
                    newEventSource.AddListener(listener);

                Validate();
            }
        }

        /// <summary>
        /// Helper used in code:Dispose that removes any references to 'listenerToRemove' in any of the
        /// eventSources in the appdomain.  
        /// 
        /// The EventListenersLock must be held before calling this routine. 
        /// </summary>
        private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
        {
            // Foreach existing EventSource in the appdomain
            foreach (WeakReference eventSourceRef in s_EventSources)
            {
                EventSource eventSource = eventSourceRef.Target as EventSource;
                if (eventSource != null)
                {
                    // Is the first output dispatcher the dispatcher we are removing?
                    if (eventSource.m_Dispatchers.m_Listener == listenerToRemove)
                        eventSource.m_Dispatchers = eventSource.m_Dispatchers.m_Next;
                    else
                    {
                        // Remove 'listenerToRemove' from the eventSource.m_Dispatchers linked list.  
                        EventDispatcher prev = eventSource.m_Dispatchers;
                        for (; ;)
                        {
                            EventDispatcher cur = prev.m_Next;
                            if (cur == null)
                            {
                                Contract.Assert(false, "EventSource did not have a registered EventListener!");
                                break;
                            }
                            if (cur.m_Listener == listenerToRemove)
                            {
                                prev.m_Next = cur.m_Next;       // Remove entry. 
                                break;
                            }
                            prev = cur;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks internal consistancy of EventSources/Listeners. 
        /// </summary>
        [Conditional("DEBUG")]
        internal static void Validate()
        {
            lock (EventListenersLock)
            {
                // Get all listeners 
                Dictionary<EventListener, bool> allListeners = new Dictionary<EventListener, bool>();
                EventListener cur = s_Listeners;
                while (cur != null)
                {
                    allListeners.Add(cur, true);
                    cur = cur.m_Next;
                }

                // For all eventSources 
                int id = -1;
                foreach (WeakReference eventSourceRef in s_EventSources)
                {
                    id++;
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource == null)
                        continue;
                    Contract.Assert(eventSource.m_id == id, "Unexpected event source ID.");

                    // None listeners on eventSources exist in the dispatcher list.   
                    EventDispatcher dispatcher = eventSource.m_Dispatchers;
                    while (dispatcher != null)
                    {
                        Contract.Assert(allListeners.ContainsKey(dispatcher.m_Listener), "EventSource has a listener not on the global list.");
                        dispatcher = dispatcher.m_Next;
                    }

                    // Every dispatcher is on Dispatcher List of every eventSource. 
                    foreach (EventListener listener in allListeners.Keys)
                    {
                        dispatcher = eventSource.m_Dispatchers;
                        for (; ;)
                        {
                            Contract.Assert(dispatcher != null, "Listener is not on all eventSources.");
                            if (dispatcher.m_Listener == listener)
                                break;
                            dispatcher = dispatcher.m_Next;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a global lock that is intended to protect the code:s_Listeners linked list and the
        /// code:s_EventSources WeakReference list.  (We happen to use the s_EventSources list as
        /// the lock object)
        /// </summary>
        internal static object EventListenersLock
        {
            get
            {
                if (s_EventSources == null)
                    Interlocked.CompareExchange(ref s_EventSources, new List<WeakReference>(2), null);
                return s_EventSources;
            }
        }

        // Instance fields
        internal volatile EventListener m_Next;                         // These form a linked list in s_Listeners

        // static fields
        internal static EventListener s_Listeners;             // list of all EventListeners in the appdomain
        internal static List<WeakReference> s_EventSources;     // all EventSources in the appdomain
        #endregion
    }

    /// <summary>
    /// Passed to the code:EventSource.OnEventCommand callback
    /// </summary>
    public class EventCommandEventArgs : EventArgs
    {
        public EventCommand Command { get; private set; }
        public IDictionary<String, String> Arguments { get; private set; }

        public bool EnableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventId, true);
        }
        public bool DisableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventId, false);
        }

        #region private

        internal EventCommandEventArgs(EventCommand command, IDictionary<string, string> arguments, EventSource eventSource, EventDispatcher dispatcher)
        {
            this.Command = command;
            this.Arguments = arguments;
            this.eventSource = eventSource;
            this.dispatcher = dispatcher;
        }

        internal EventSource eventSource;
        internal EventDispatcher dispatcher;
        #endregion
    }

    /// <summary>
    /// code:EventWrittenEventArgs is passed when the callback given in code:EventListener.OnEventWritten is
    /// fired.
    /// </summary>
    public class EventWrittenEventArgs : EventArgs
    {
        public int EventId { get; internal set; }
        public ReadOnlyCollection<Object> Payload { get; internal set; }
        public EventSource EventSource { get { return m_eventSource; } }
        public EventKeywords Keywords { get { return (EventKeywords)m_eventSource.m_eventData[EventId].Descriptor.Keyword; } }
        public EventOpcode Opcode { get { return (EventOpcode)m_eventSource.m_eventData[EventId].Descriptor.Opcode; } }
        public EventTask Task { get { return (EventTask)m_eventSource.m_eventData[EventId].Descriptor.Task; } }
        public string Message { get { return m_eventSource.m_eventData[EventId].Message; } }

#if FEATURE_MANAGED_ETW_CHANNELS
        public EventChannel Channel { get { return (EventChannel)m_eventSource.m_eventData[EventId].Descriptor.Channel; } }
#endif
        public byte Version { get { return m_eventSource.m_eventData[EventId].Descriptor.Version; } }
        public EventLevel Level
        {
            get
            {
                if ((uint)EventId >= (uint)m_eventSource.m_eventData.Length)
                    return EventLevel.LogAlways;
                return (EventLevel)m_eventSource.m_eventData[EventId].Descriptor.Level;
            }
        }

        public Guid ActivityId
        {
            [SecurityCritical] // required to match contract
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Guid RelatedActivityId
        {
            [SecurityCritical] // required to match contract
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        #region private
        internal EventWrittenEventArgs(EventSource eventSource)
        {
            m_eventSource = eventSource;
        }
        private EventSource m_eventSource;
        #endregion
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EventSourceAttribute : Attribute
    {
        public string Name { get; set; }
        public string Guid { get; set; }

        /// <summary>
        /// EventSources support localization of events. The names used for events, opcodes, tasks, keyworks and maps 
        /// can be localized to several languages if desired. This works by creating a ResX style string table 
        /// (by simply adding a 'Resource File' to your project). This resource file is given a name e.g. 
        /// 'DefaultNameSpace.ResourceFileName' which can be passed to the ResourceManager constructor to read the 
        /// resoruces. This name is the value of the LocalizationResources property. 
        /// 
        /// LocalizationResources property is non-null, then EventSource will look up the localized strings for events by 
        /// using the following resource naming scheme
        /// 
        ///     event_EVENTNAME
        ///     task_TASKNAME
        ///     keyword_KEYWORDNAME
        ///     map_MAPNAME
        ///     
        /// where the capitpalized name is the name of the event, task, keywork, or map value that should be localized.   
        /// Note that the localized string for an event corresponds to the Messsage string, and can have {0} values 
        /// which represent the payload values.  
        /// </summary>
        public string LocalizationResources { get; set; }
    }

    /// <summary>
    /// None instance methods in a class that subclasses code:EventSource that and return void are
    /// assumed by default to be methods that generate an event. Enough information can be deduced from the
    /// name of the method and its signature to generate basic schema information for the event. The
    /// code:EventAttribute allows you to specify additional event schema information for an event if
    /// desired.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventAttribute : Attribute
    {
        public EventAttribute(int eventId) { this.EventId = eventId; Level = EventLevel.Informational; }
        public int EventId { get; private set; }
        public EventLevel Level { get; set; }
        public EventKeywords Keywords { get; set; }
        public EventOpcode Opcode { get; set; }
        public EventTask Task { get; set; }
#if FEATURE_MANAGED_ETW_CHANNELS
        public EventChannel Channel { get; set; }
#endif
        public byte Version { get; set; }

        /// <summary>
        /// This is also used for TraceSource compatabilty.  If code:EventSource.TraceSourceSupport is
        /// on events will also be logged a tracesource with the same name as the eventSource.  If this
        /// property is set then the payload will go to code:TraceSource.TraceEvent, and this string
        /// will be used as the message.  If this property is not set not set it goes to
        /// code:TraceSource.TraceData.   You can use standard .NET substitution operators (eg {1}) in 
        /// the string and they will be replaced with the 'ToString()' of the cooresponding part of the
        /// event payload.   
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// By default all instance methods in a class that subclasses code:EventSource that and return
    /// void are assumed to be methods that generate an event. This default can be overriden by specifying
    /// the code:NonEventAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NonEventAttribute : Attribute
    {
        public NonEventAttribute() { }
    }


    // FUTURE we may want to expose this at some point once we have a partner that can help us validate the design.
#if FEATURE_MANAGED_ETW_CHANNELS
    [AttributeUsage(AttributeTargets.Field)]
    public class ChannelAttribute : Attribute
    {
        public bool Enabled { get; set; }
        public string Isolation { get; set; }
        /// <summary>
        /// Legal values are in ChannelTypes
        /// </summary>
        public string Type { get; set; }

        // FUTURE should we include?  public string Access { get; set; } 
        public string ImportChannel { get; set; }
        // FUTURE there is a convention that the name is the Provider/Type   Should we provide an override?
        // public string Name { get; set; }
    }

    // FUTURE add to spec
    // FUTURE should we bother with this class?. 
    public static class ChannelTypes
    {
        public const string Admin = "Admin";
        public const string Operational = "Operational";
        public const string Analytic = "Analytic";
        public const string Debug = "Debug";
    }
#endif

    public enum EventCommand
    {
        Update = 0,
        SendManifest = -1,
        Enable = -2,
        Disable = -3
    };

    #region private classes

    /// <summary>
    /// code:EventDispatchers are a simple 'helper' structure that holds the filtering state
    /// (m_EventEnabled) for a particular EventSource X EventListener tuple
    /// 
    /// Thus a single EventListener may have many EventDispatchers (one for every EventSource 
    /// that that EventListener has activate) and a Single EventSource may also have many
    /// event Dispatchers (one for every EventListener that has activated it). 
    /// 
    /// Logically a particular EventDispatcher belongs to exactly one EventSource and exactly  
    /// one EventListener (alhtough EventDispatcher does not 'remember' the EventSource it is
    /// associated with. 
    /// </summary>
    internal class EventDispatcher
    {
        internal EventDispatcher(EventDispatcher next, bool[] eventEnabled, EventListener listener)
        {
            m_Next = next;
            m_EventEnabled = eventEnabled;
            m_Listener = listener;
        }

        // Instance fields
        readonly internal EventListener m_Listener;   // The dispatcher this entry is for
        internal bool[] m_EventEnabled;               // For every event in a the eventSource, is it enabled?
        // Only guarenteed to exist after a InsureInit()
        internal EventDispatcher m_Next;              // These form a linked list in code:EventSource.m_Dispatchers
        // Of all listeners for that eventSource.  
    }

    /// <summary>
    /// Used to send the m_rawManifest into the event dispatcher as a series of events.  
    /// </summary>
    internal struct ManifestEnvelope
    {
        public const int MaxChunkSize = 0xFF00;
        public enum ManifestFormats : byte
        {
            SimpleXmlFormat = 1,          // simply dump the XML manifest as UTF8
        }

        public ManifestFormats Format;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte Magic;
        public ushort TotalChunks;
        public ushort ChunkNumber;
    };
    #endregion
}

