// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

#if TARGET_WINDOWS
using Internal.Win32;
#endif

namespace System.Diagnostics.Tracing
{
    internal enum EventProviderType
    {
        None = 0,
        ETW,
        EventPipe
    }

    internal enum ControllerCommand
    {
        // Strictly Positive numbers are for provider-specific commands, negative number are for 'shared' commands. 256
        // The first 256 negative numbers are reserved for the framework.
        Update = 0, // Not used by EventProviderBase.
        SendManifest = -1,
        Enable = -2,
        Disable = -3,
    }

    /// <summary>
    /// Only here because System.Diagnostics.EventProvider needs one more extensibility hook (when it gets a
    /// controller callback)
    ///
    /// As of Feb 2023 the current factoring of this type remains a work in progress. Ideally all the ETW specific functionality
    /// would be moved to EtwEventProvider and all the common functionality would be moved to EventProviderImpl. At that point this
    /// type would no longer need to exist, EventSource would have a direct reference to EventProviderImpl, and EventProviderImpl's
    /// WeakReference would point back to EventSource. However for now we still have this intermediate layer:
    ///     EventSource -- EventProvider -- EventProviderImpl.
    ///
    /// Be careful interpreting code that uses 'ETW' naming.Some of it really is only used for ETW scenarios whereas in other places
    /// ETW behavior was adopted as the standard that EventPipe also implements. Ideally the former would be moved to EtwEventProvider
    /// and the latter would remove ETW from the naming.
    /// </summary>
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

        internal EventProviderImpl _eventProvider;      // The implementation of the specific logging mechanism functions.
        private string? _providerName;                  // Control name
        private Guid _providerId;                       // Control Guid
        internal bool _disposed;                        // when true provider has unregistered

        [ThreadStatic]
        private static WriteEventErrorCode s_returnCode; // The last return code

        private const int BasicTypeAllocationBufferSize = 16;
        private const int EtwMaxNumberArguments = 128;
        private const int EtwAPIMaxRefObjCount = 8;
        private const int TraceEventMaximumSize = 65482;

        public enum WriteEventErrorCode : int
        {
            // check mapping to runtime codes
            NoError = 0,
            NoFreeBuffers = 1,
            EventTooBig = 2,
            NullInput = 3,
            TooManyArgs = 4,
            Other = 5,
        }

        // Because callbacks happen on registration, and we need the callbacks for those setup
        // we can't call Register in the constructor.
        //
        // Note that EventProvider should ONLY be used by EventSource.  In particular because
        // it registers a callback from native code you MUST dispose it BEFORE shutdown, otherwise
        // you may get native callbacks during shutdown when we have destroyed the delegate.
        // EventSource has special logic to do this, no one else should be calling EventProvider.
        internal EventProvider(EventProviderType providerType)
        {
            _eventProvider = providerType switch
            {
#if TARGET_WINDOWS
                EventProviderType.ETW => new EtwEventProvider(this),
#endif
#if FEATURE_PERFTRACING
                EventProviderType.EventPipe => new EventPipeEventProvider(this),
#endif
                _ => new EventProviderImpl(),
            };
        }

        /// <summary>
        /// This method registers the controlGuid of this class with ETW. We need to be running on
        /// Vista or above. If not a PlatformNotSupported exception will be thrown. If for some
        /// reason the ETW Register call failed a NotSupported exception will be thrown.
        /// </summary>
        internal unsafe void Register(EventSource eventSource)
        {
            _providerName = eventSource.Name;
            _providerId = eventSource.Guid;

            _eventProvider.Register(eventSource);
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
            if (_disposed)
                return;

            // Disable the provider.
            _eventProvider.Disable();

            // Do most of the work under a lock to avoid shutdown race.

            lock (EventListener.EventListenersLock)
            {
                // Double check
                if (_disposed)
                    return;

                _disposed = true;
            }

            // We do the Unregistration outside the EventListenerLock because there is a lock
            // inside the ETW routines.   This lock is taken before ETW issues commands
            // Thus the ETW lock gets taken first and then our EventListenersLock gets taken
            // in SendCommand(), and also here.  If we called EventUnregister after taking
            // the EventListenersLock then the take-lock order is reversed and we can have
            // deadlocks in race conditions (dispose racing with an ETW command).
            //
            // We solve by Unregistering after releasing the EventListenerLock.
            Debug.Assert(!Monitor.IsEntered(EventListener.EventListenersLock));
            _eventProvider.Unregister();
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

        internal virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string?>? arguments, int sessionId) { }

        protected EventLevel Level
        {
            get => _eventProvider.Level;
            set => _eventProvider.Level = value;
        }

        protected EventKeywords MatchAnyKeyword
        {
            get => _eventProvider.MatchAnyKeyword;
            set => _eventProvider.MatchAnyKeyword = value;
        }

        protected EventKeywords MatchAllKeyword
        {
            get => _eventProvider.MatchAllKeyword;
            set => _eventProvider.MatchAllKeyword = value;
        }

        /// <summary>
        /// IsEnabled, method used to test if provider is enabled
        /// </summary>
        public bool IsEnabled()
        {
            return _eventProvider.IsEnabled();
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
            return _eventProvider.IsEnabled(level, keywords);
        }

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

        private static unsafe object? EncodeObject(ref object? data, ref EventData* dataDescriptor, ref byte* dataBuffer, ref uint totalEventSize)
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
                dataBuffer += BasicTypeAllocationBufferSize;
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
                if ((bool)data)
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
                if (data is Enum)
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
                    catch { }   // On weird cases (e.g. enums of type double), give up and for compat simply tostring.
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
            dataBuffer += BasicTypeAllocationBufferSize;

            return (object?)sRet ?? (object?)blobRet;
        }

        /// <summary>
        /// WriteEvent, method to write a parameters with event schema properties
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="eventHandle">
        /// Event handle for this event.
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
        internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, object?[] eventPayload)
        {
            WriteEventErrorCode status = WriteEventErrorCode.NoError;

            if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
            {
                int argCount = eventPayload.Length;

                if (argCount > EtwMaxNumberArguments)
                {
                    s_returnCode = WriteEventErrorCode.TooManyArgs;
                    return false;
                }

                uint totalEventSize = 0;
                int index;
                int refObjIndex = 0;

                Debug.Assert(EtwAPIMaxRefObjCount == 8, $"{nameof(EtwAPIMaxRefObjCount)} must equal the number of fields in {nameof(EightObjects)}");
                EightObjects eightObjectStack = default;
                Span<int> refObjPosition = stackalloc int[EtwAPIMaxRefObjCount];
                Span<object?> dataRefObj = new Span<object?>(ref eightObjectStack._arg0, EtwAPIMaxRefObjCount);

                EventData* userData = stackalloc EventData[2 * argCount];
                for (int i = 0; i < 2 * argCount; i++)
                    userData[i] = default;

                EventData* userDataPtr = userData;
                byte* dataBuffer = stackalloc byte[BasicTypeAllocationBufferSize * 2 * argCount]; // Assume 16 chars for non-string argument
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
                                if (eventPayload.Length + idx + 1 - index > EtwMaxNumberArguments)
                                {
                                    s_returnCode = WriteEventErrorCode.TooManyArgs;
                                    return false;
                                }
                                hasNonStringRefArgs = true;
                            }

                            if (refObjIndex >= dataRefObj.Length)
                            {
                                Span<object?> newDataRefObj = new object?[dataRefObj.Length * 2];
                                dataRefObj.CopyTo(newDataRefObj);
                                dataRefObj = newDataRefObj;

                                Span<int> newRefObjPosition = new int[refObjPosition.Length * 2];
                                refObjPosition.CopyTo(newRefObjPosition);
                                refObjPosition = newRefObjPosition;
                            }

                            dataRefObj[refObjIndex] = supportedRefObj;
                            refObjPosition[refObjIndex] = idx;
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

                if (totalEventSize > TraceEventMaximumSize)
                {
                    s_returnCode = WriteEventErrorCode.EventTooBig;
                    return false;
                }

                // the optimized path (using "fixed" instead of allocating pinned GCHandles
                if (!hasNonStringRefArgs && (refObjIndex <= EtwAPIMaxRefObjCount))
                {
                    // Fast path: at most 8 string arguments

                    // ensure we have at least s_etwAPIMaxStringCount in dataString, so that
                    // the "fixed" statement below works
                    while (refObjIndex < EtwAPIMaxRefObjCount)
                    {
                        dataRefObj[refObjIndex] = null;
                        refObjPosition[refObjIndex] = -1;
                        ++refObjIndex;
                    }

                    //
                    // now fix any string arguments and set the pointer on the data descriptor
                    //
                    fixed (char* v0 = (string?)dataRefObj[0], v1 = (string?)dataRefObj[1], v2 = (string?)dataRefObj[2], v3 = (string?)dataRefObj[3],
                            v4 = (string?)dataRefObj[4], v5 = (string?)dataRefObj[5], v6 = (string?)dataRefObj[6], v7 = (string?)dataRefObj[7])
                    {
                        userDataPtr = userData;
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

                        status = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, argCount, userData);
                    }
                }
                else
                {
                    // Slow path: use pinned handles
                    userDataPtr = userData;

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

                    status = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, argCount, userData);

                    for (int i = 0; i < refObjIndex; ++i)
                    {
                        rgGCHandle[i].Free();
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

        /// <summary>Workaround for inability to stackalloc object[EtwAPIMaxRefObjCount == 8].</summary>
        private struct EightObjects
        {
            internal object? _arg0;
#pragma warning disable CA1823, CS0169, IDE0051, IDE0044
            private object? _arg1;
            private object? _arg2;
            private object? _arg3;
            private object? _arg4;
            private object? _arg5;
            private object? _arg6;
            private object? _arg7;
#pragma warning restore CA1823, CS0169, IDE0051, IDE0044
        }

        /// <summary>
        /// WriteEvent, method to be used by generated code on a derived class
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="eventHandle">
        /// Event handle for this event.
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
        protected internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, int dataCount, IntPtr data)
        {
            if (childActivityID != null)
            {
                // activity transfers are supported only for events that specify the Send or Receive opcode
                Debug.Assert((EventOpcode)eventDescriptor.Opcode == EventOpcode.Send ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Receive ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Start ||
                                (EventOpcode)eventDescriptor.Opcode == EventOpcode.Stop);
            }

            WriteEventErrorCode status = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, dataCount, (EventData*)data);

            if (status != 0)
            {
                SetLastError(status);
                return false;
            }
            return true;
        }

        internal unsafe bool WriteEventRaw(
            ref EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityID,
            Guid* relatedActivityID,
            int dataCount,
            IntPtr data)
        {
            WriteEventErrorCode status = _eventProvider.EventWriteTransfer(
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

#if TARGET_WINDOWS
        internal unsafe int SetInformation(
            Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass,
            void* data,
            uint dataSize)
        {
            return ((EtwEventProvider)_eventProvider).SetInformation(eventInfoClass, data, dataSize);
        }
#endif
    }

#if TARGET_WINDOWS
    // A wrapper around the ETW-specific API calls.
    internal sealed class EtwEventProvider : EventProviderImpl
    {
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

        private readonly WeakReference<EventProvider> _eventProvider;
        private long _registrationHandle;
        private GCHandle _gcHandle;
        private List<SessionInfo>? _liveSessions;       // current live sessions (KeyValuePair<sessionIdBit, etwSessionId>)
        private Guid _providerId;

        internal EtwEventProvider(EventProvider eventProvider)
        {
            _eventProvider = new WeakReference<EventProvider>(eventProvider);
        }

        internal override void Disable()
        {
            base.Disable();
            _liveSessions = null;
        }

        protected override unsafe void HandleEnableNotification(
                                    EventProvider target,
                                    byte *additionalData,
                                    byte level,
                                    long matchAnyKeywords,
                                    long matchAllKeywords,
                                    Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
        {
            Debug.Assert(additionalData == null);

            // The GetSessions() logic was here to support the idea that different ETW sessions
            // could have different user-defined filters. (I believe it is currently broken.)
            //
            // All this session based logic should be reviewed and likely removed, but that is a larger
            // change that needs more careful staging.
            List<KeyValuePair<SessionInfo, bool>> sessionsChanged = GetChangedSessions();

            foreach (KeyValuePair<SessionInfo, bool> session in sessionsChanged)
            {
                int sessionChanged = session.Key.sessionIdBit;
                bool bEnabling = session.Value;

                // reinitialize args for every session...
                IDictionary<string, string?>? args = null;
                ControllerCommand command = ControllerCommand.Update;

                // read filter data only when a session is being *added*
                if (bEnabling)
                {
                    byte[]? filterDataBytes;
                    // if we get more than one session changed we have no way
                    // of knowing which one "filterData" belongs to
                    if (sessionsChanged.Count > 1 || filterData == null)
                    {
                        TryReadRegistryFilterData(session.Key.etwSessionId, out command, out filterDataBytes);
                    }
                    else
                    {
                        MarshalFilterData(filterData, out command, out filterDataBytes);
                    }
                    args = ParseFilterData(filterDataBytes);
                }

                // execute OnControllerCommand once for every session that has changed.
                // If the sessionId argument is positive it will be sent to the EventSource as an Enable,
                // and if it is negative it will be sent as a disable. See EventSource.DoCommand()
                target.OnControllerCommand(command, args, bEnabling ? sessionChanged : -sessionChanged);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void Callback(Guid* sourceId, int isEnabled, byte level,
            long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
        {
            EtwEventProvider _this = (EtwEventProvider)GCHandle.FromIntPtr((IntPtr)callbackContext).Target!;

            if (_this._eventProvider.TryGetTarget(out EventProvider? target))
            {
                _this.ProviderCallback(target, null, isEnabled, level, matchAnyKeywords, matchAllKeywords, filterData);
            }
        }


        // Register an event provider.
        internal override unsafe void Register(EventSource eventSource)
        {
            Debug.Assert(!_gcHandle.IsAllocated);
            _gcHandle = GCHandle.Alloc(this);

            long registrationHandle = 0;
            _providerId = eventSource.Guid;
            Guid providerId = _providerId;
            uint status = Interop.Advapi32.EventRegister(
                &providerId,
                &Callback,
                (void*)GCHandle.ToIntPtr(_gcHandle),
                &registrationHandle);
            if (status != 0)
            {
                _gcHandle.Free();
                throw new ArgumentException(Interop.Kernel32.GetMessage((int)status));
            }

            Debug.Assert(_registrationHandle == 0);
            _registrationHandle = registrationHandle;
        }

        // Unregister an event provider.
        internal override void Unregister()
        {
            if (_registrationHandle != 0)
            {
                Interop.Advapi32.EventUnregister(_registrationHandle);
                _registrationHandle = 0;
            }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
        }

        // Write an event.
        internal override unsafe EventProvider.WriteEventErrorCode EventWriteTransfer(
            in EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            int error = Interop.Advapi32.EventWriteTransfer(
                _registrationHandle,
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
        internal override int ActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return Interop.Advapi32.EventActivityIdControl(
                ControlCode,
                ref ActivityId);
        }

        // Define an EventPipeEvent handle.
        internal override unsafe IntPtr DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion,
            uint level, byte* pMetadata, uint metadataLength)
        {
            throw new NotSupportedException();
        }


        private static bool s_setInformationMissing;

        internal unsafe int SetInformation(
            Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass,
            void* data,
            uint dataSize)
        {
            int status = Interop.Errors.ERROR_NOT_SUPPORTED;

            if (!s_setInformationMissing)
            {
                try
                {
                    status = Interop.Advapi32.EventSetInformation(
                        _registrationHandle,
                        eventInfoClass,
                        data,
                        dataSize);
                }
                catch (TypeLoadException)
                {
                    s_setInformationMissing = true;
                }
            }

            return status;
        }

        /// <summary>
        /// Callback data for ETW is only present when the provider is active at the time the controller issues the command.
        /// To allow for providers to activate after the controller issued a command, we also check the registry and use that
        /// to get the data. The function returns an array of bytes representing the data, the index into that byte array
        /// where the data starts, and the command being issued associated with that data.
        /// </summary>
        private unsafe bool TryReadRegistryFilterData(int etwSessionId, out ControllerCommand command, out byte[]? data)
        {
            command = ControllerCommand.Update;
            data = null;

            string regKey = @"\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + _providerId + "}";
            if (IntPtr.Size == 8)
            {
                regKey = @"Software\Wow6432Node" + regKey;
            }
            else
            {
                regKey = "Software" + regKey;
            }

            string valueName = "ControllerData_Session_" + etwSessionId.ToString(CultureInfo.InvariantCulture);

            // we need to assert this permission for partial trust scenarios
            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(regKey))
            {
                data = key?.GetValue(valueName, null) as byte[];
                if (data != null)
                {
                    // We only used the persisted data from the registry for updates.
                    return true;
                }
            }

            return false;
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
        private List<KeyValuePair<SessionInfo, bool>> GetChangedSessions()
        {
            List<SessionInfo>? liveSessionList = null;

            GetSessionInfo(
                GetSessionInfoCallback,
                ref liveSessionList);

            List<KeyValuePair<SessionInfo, bool>> changedSessionList = new List<KeyValuePair<SessionInfo, bool>>();

            // first look for sessions that have gone away (or have changed)
            // (present in the _liveSessions but not in the new liveSessionList)
            if (_liveSessions != null)
            {
                foreach (SessionInfo s in _liveSessions)
                {
                    int idx;
                    if ((idx = IndexOfSessionInList(liveSessionList, s.etwSessionId)) < 0 ||
                        (liveSessionList![idx].sessionIdBit != s.sessionIdBit))
                        changedSessionList.Add(new KeyValuePair<SessionInfo, bool>(s, false));
                }
            }
            // next look for sessions that were created since the last callback  (or have changed)
            // (present in the new liveSessionList but not in _liveSessions)
            if (liveSessionList != null)
            {
                foreach (SessionInfo s in liveSessionList)
                {
                    int idx;
                    if ((idx = IndexOfSessionInList(_liveSessions, s.etwSessionId)) < 0 ||
                        (_liveSessions![idx].sessionIdBit != s.sessionIdBit))
                        changedSessionList.Add(new KeyValuePair<SessionInfo, bool>(s, true));
                }
            }

            _liveSessions = liveSessionList;
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

            sessionList ??= new List<SessionInfo>(8);

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
            int buffSize = 256;     // An initial guess that probably works most of the time.
            byte* stackSpace = stackalloc byte[buffSize];
            byte* buffer = stackSpace;
            try
            {
                while (true)
                {
                    int hr = 0;

                    fixed (Guid* provider = &_providerId)
                    {
                        hr = Interop.Advapi32.EnumerateTraceGuidsEx(Interop.Advapi32.TRACE_QUERY_INFO_CLASS.TraceGuidQueryInfo,
                            provider, sizeof(Guid), buffer, buffSize, out buffSize);
                    }
                    if (hr == 0)
                        break;
                    if (hr != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                        return;

                    if (buffer != stackSpace)
                    {
                        byte* toFree = buffer;
                        buffer = null;
                        Marshal.FreeHGlobal((IntPtr)toFree);
                    }
                    buffer = (byte*)Marshal.AllocHGlobal(buffSize);
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
                    byte* structBase = (byte*)providerInstance;
                    providerInstance = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)&structBase[providerInstance->NextOffset];
                }
            }
            finally
            {
                if (buffer != null && buffer != stackSpace)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }
            }
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

    }
#endif

#pragma warning disable CA1852 // EventProviderImpl is not derived from in all targets
    internal class EventProviderImpl
    {
        protected byte _level;                            // Tracing Level
        protected long _anyKeywordMask;                   // Trace Enable Flags
        protected long _allKeywordMask;                   // Match all keyword
        protected bool _enabled;                          // Enabled flag from Trace callback

        internal EventLevel Level
        {
            get => (EventLevel)_level;
            set => _level = (byte)value;
        }

        internal EventKeywords MatchAnyKeyword
        {
            get => (EventKeywords)_anyKeywordMask;
            set => _anyKeywordMask = unchecked((long)value);
        }

        internal EventKeywords MatchAllKeyword
        {
            get => (EventKeywords)_allKeywordMask;
            set => _allKeywordMask = unchecked((long)value);
        }

        protected virtual unsafe void HandleEnableNotification(
                                    EventProvider target,
                                    byte *additionalData,
                                    byte level,
                                    long matchAnyKeywords,
                                    long matchAllKeywords,
                                    Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
        {
        }

        /// <summary>
        /// IsEnabled, method used to test if provider is enabled
        /// </summary>
        public bool IsEnabled()
        {
            return _enabled;
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
            if (!_enabled)
            {
                return false;
            }

            // This also covers the case of Level == 0.
            if ((level <= _level) ||
                (_level == 0))
            {
                //
                // Check if Keyword is enabled
                //

                if ((keywords == 0) ||
                    (((keywords & _anyKeywordMask) != 0) &&
                     ((keywords & _allKeywordMask) == _allKeywordMask)))
                {
                    return true;
                }
            }

            return false;
        }

        internal void Enable(byte level, long anyKeyword, long allKeyword)
        {
            _enabled = true;
            _level = level;
            _anyKeywordMask = anyKeyword;
            _allKeywordMask = allKeyword;
        }

        internal virtual void Disable()
        {
            _enabled = false;
            _level = 0;
            _anyKeywordMask = 0;
            _allKeywordMask = 0;
        }

        internal virtual void Register(EventSource eventSource)
        {
        }

        internal virtual void Unregister()
        {
        }

        internal virtual unsafe EventProvider.WriteEventErrorCode EventWriteTransfer(
            in EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            return EventProvider.WriteEventErrorCode.NoError;
        }

        internal virtual int ActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return 0;
        }

        // Define an EventPipeEvent handle.
        internal virtual unsafe IntPtr DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion,
            uint level, byte* pMetadata, uint metadataLength)
        {
            return IntPtr.Zero;
        }

        protected unsafe void ProviderCallback(
                        EventProvider target,
                        byte *additionalData,
                        int controlCode,
                        byte level,
                        long matchAnyKeywords,
                        long matchAllKeywords,
                        Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
        {
            // This is an optional callback API. We will therefore ignore any failures that happen as a
            // result of turning on this provider as to not crash the app.
            // EventSource has code to validate whether initialization it expected to occur actually occurred
            try
            {
                if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_ENABLE_PROVIDER)
                {
                    Enable(level, matchAnyKeywords, matchAllKeywords);
                    HandleEnableNotification(target, additionalData, level, matchAnyKeywords, matchAllKeywords, filterData);
                    return;
                }

                ControllerCommand command = ControllerCommand.Update;
                if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_DISABLE_PROVIDER)
                {
                    Disable();
                }
                else if (controlCode == Interop.Advapi32.EVENT_CONTROL_CODE_CAPTURE_STATE)
                {
                    command = ControllerCommand.SendManifest;
                }
                else
                {
                    return;     // per spec you ignore commands you don't recognize.
                }

                target.OnControllerCommand(command, null, 0);
            }
            catch
            {
                // We want to ignore any failures that happen as a result of turning on this provider as to
                // not crash the app.
            }
        }

        private static int FindNull(byte[] buffer, int idx)
        {
            while (idx < buffer.Length && buffer[idx] != 0)
            {
                idx++;
            }

            return idx;
        }

        protected static unsafe IDictionary<string, string?>? ParseFilterData(byte[]? data)
        {
            Dictionary<string, string?>? args = null;

            // data can be null if the filterArgs had a very large size which failed our sanity check
            if (data != null)
            {
                args = new Dictionary<string, string?>(4);
                int dataStart = 0;
                while (dataStart < data.Length)
                {
                    int keyEnd = FindNull(data, dataStart);
                    int valueIdx = keyEnd + 1;
                    int valueEnd = FindNull(data, valueIdx);
                    if (valueEnd < data.Length)
                    {
                        string key = Text.Encoding.UTF8.GetString(data, dataStart, keyEnd - dataStart);
                        string value = Text.Encoding.UTF8.GetString(data, valueIdx, valueEnd - valueIdx);
                        args[key] = value;
                    }
                    dataStart = valueEnd + 1;
                }
            }

            return args;
        }

        protected unsafe bool MarshalFilterData(Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, out ControllerCommand command, out byte[]? data)
        {
            Debug.Assert(filterData != null);

            data = null;
            // ETW limited filter data to 1024 bytes but EventPipe doesn't. DiagnosticSourceEventSource
            // can legitimately use large filter data buffers to encode a large set of events and properties
            // that should be gathered so I am bumping the limit from 1K -> 100K.
            if (filterData->Ptr != 0 && 0 < filterData->Size && filterData->Size <= 100 * 1024)
            {
                data = new byte[filterData->Size];
                Marshal.Copy((IntPtr)(void*)filterData->Ptr, data, 0, data.Length);
            }
            command = (ControllerCommand)filterData->Type;
            return true;
        }
    }
#pragma warning restore CA1852
}
