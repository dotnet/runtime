// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
{
    internal sealed class EventPipeEventProvider : IEventProvider
    {
        // The EventPipeProvider handle.
        private IntPtr m_provHandle = IntPtr.Zero;

        // Register an event provider.
        unsafe uint IEventProvider.EventRegister(
            EventSource eventSource,
            Interop.Advapi32.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle)
        {
            uint returnStatus = 0;
            m_provHandle = EventPipeInternal.CreateProvider(eventSource.Name, enableCallback);
            if (m_provHandle != IntPtr.Zero)
            {
                // Fixed registration handle because a new EventPipeEventProvider
                // will be created for each new EventSource.
                registrationHandle = 1;
            }
            else
            {
                // Unable to create the provider.
                returnStatus = 1;
            }

            return returnStatus;
        }

        // Unregister an event provider.
        uint IEventProvider.EventUnregister(long registrationHandle)
        {
            EventPipeInternal.DeleteProvider(m_provHandle);
            return 0;
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
        int IEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return EventActivityIdControl(ControlCode, ref ActivityId);
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level,
            byte *pMetadata, uint metadataLength)
        {
            IntPtr eventHandlePtr = EventPipeInternal.DefineEvent(m_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
            return eventHandlePtr;
        }

        // Get or set the per-thread activity ID.
        internal static int EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return EventPipeInternal.EventActivityIdControl((uint)ControlCode, ref ActivityId);
        }
    }
}
