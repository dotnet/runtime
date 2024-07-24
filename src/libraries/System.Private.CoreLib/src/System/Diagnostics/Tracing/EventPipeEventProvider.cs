// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    internal sealed class EventPipeEventProvider : EventProviderImpl
    {
        private readonly WeakReference<EventProvider> _eventProvider;
        private IntPtr _provHandle;
        private GCHandle _gcHandle;

        internal EventPipeEventProvider(EventProvider eventProvider)
        {
            _eventProvider = new WeakReference<EventProvider>(eventProvider);
        }

        protected override unsafe void HandleEnableNotification(
                                    EventProvider target,
                                    byte* additionalData,
                                    byte level,
                                    long matchAnyKeywords,
                                    long matchAllKeywords,
                                    Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
        {
            ulong id = 0;
            if (additionalData != null)
            {
                id = BitConverter.ToUInt64(new ReadOnlySpan<byte>(additionalData, sizeof(ulong)));
            }

            // EventPipe issues Interop.Advapi32.EVENT_CONTROL_CODE_ENABLE_PROVIDER if a session
            // is stopping as long as some other session is still enabled. If the session is stopping
            // the session ID will be null, if it is a session starting it will be a non-zero value
            bool bEnabling = id != 0;

            IDictionary<string, string?>? args = null;
            ControllerCommand command = ControllerCommand.Update;

            if (bEnabling)
            {
                byte[]? filterDataBytes = null;
                if (filterData != null)
                {
                    MarshalFilterData(filterData, out command, out filterDataBytes);
                }

                args = ParseFilterData(filterDataBytes);
            }

            // Since we are sharing logic across ETW and EventPipe in OnControllerCommand we have to set up data to
            // mimic ETW to get the right commands sent to EventSources. perEventSourceSessionId has special meaning,
            // if it is -1 the this command will be translated to a Disable command in EventSource.OnEventCommand. If
            // it is 0-3 it indicates an ETW session with activities, and SessionMask.MAX (4) means legacy ETW session.
            // We send SessionMask.MAX just to conform.
            target.OnControllerCommand(command, args, bEnabling ? (int)SessionMask.MAX : -1);
        }

        [UnmanagedCallersOnly]
        private static unsafe void Callback(byte* sourceId, int isEnabled, byte level,
            long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
        {
            System.Threading.Thread.Sleep(10);

            EventPipeEventProvider _this = (EventPipeEventProvider)GCHandle.FromIntPtr((IntPtr)callbackContext).Target!;
            if (_this._eventProvider.TryGetTarget(out EventProvider? target))
            {
                _this.ProviderCallback(target, sourceId, isEnabled, level, matchAnyKeywords, matchAllKeywords, filterData);
            }
        }

        // Register an event provider.
        internal override unsafe void Register(EventSource eventSource)
        {
            Debug.Assert(!_gcHandle.IsAllocated);
            _gcHandle = GCHandle.Alloc(this);

            _provHandle = EventPipeInternal.CreateProvider(eventSource.Name, &Callback, (void*)GCHandle.ToIntPtr(_gcHandle));
            if (_provHandle == 0)
            {
                // Unable to create the provider.
                _gcHandle.Free();
                throw new OutOfMemoryException();
            }
        }

        // Unregister an event provider.
        internal override void Unregister()
        {
            if (_provHandle != 0)
            {
                EventPipeInternal.DeleteProvider(_provHandle);
                _provHandle = 0;
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
            if (eventHandle != IntPtr.Zero)
            {
                if (userDataCount == 0)
                {
                    EventPipeInternal.WriteEventData(eventHandle, null, 0, activityId, relatedActivityId);
                    return EventProvider.WriteEventErrorCode.NoError;
                }

                // If Channel == 11, this is a TraceLogging event.
                // The first 3 descriptors contain event metadata that is emitted for ETW and should be discarded on EventPipe.
                // EventPipe metadata is provided via the EventPipeEventProvider.DefineEventHandle.
                if (eventDescriptor.Channel == 11)
                {
                    userData += 3;
                    userDataCount -= 3;
                    Debug.Assert(userDataCount >= 0);
                }
                EventPipeInternal.WriteEventData(eventHandle, userData, (uint)userDataCount, activityId, relatedActivityId);
            }

            return EventProvider.WriteEventErrorCode.NoError;
        }

        // Get or set the per-thread activity ID.
        internal override int ActivityIdControl(Interop.Advapi32.ActivityControl controlCode, ref Guid activityId)
        {
            return EventActivityIdControl(controlCode, ref activityId);
        }

        // Define an EventPipeEvent handle.
        internal override unsafe IntPtr DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level,
            byte* pMetadata, uint metadataLength)
        {
            return EventPipeInternal.DefineEvent(_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
        }

        // Get or set the per-thread activity ID.
        internal static int EventActivityIdControl(Interop.Advapi32.ActivityControl controlCode, ref Guid activityId)
        {
            return EventPipeInternal.EventActivityIdControl((uint)controlCode, ref activityId);
        }
    }
}
