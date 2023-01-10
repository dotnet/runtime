// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    internal sealed class EventPipeEventProvider : IEventProvider
    {
        private EventEnableCallback? _enableCallback;
        private IntPtr _provHandle;
        private GCHandle _gcHandle;

        [UnmanagedCallersOnly]
        private static unsafe void Callback(byte* sourceId, int isEnabled, byte level,
            long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
        {
            ((EventPipeEventProvider)GCHandle.FromIntPtr((IntPtr)callbackContext).Target!)._enableCallback!(
                isEnabled, level, matchAnyKeywords, matchAllKeywords, filterData);
        }

        // Register an event provider.
        unsafe void IEventProvider.EventRegister(
            EventSource eventSource,
            EventEnableCallback enableCallback)
        {
            _enableCallback = enableCallback;

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
        void IEventProvider.EventUnregister()
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
        unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(
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
        int IEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl controlCode, ref Guid activityId)
        {
            return EventActivityIdControl(controlCode, ref activityId);
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level,
            byte *pMetadata, uint metadataLength)
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
