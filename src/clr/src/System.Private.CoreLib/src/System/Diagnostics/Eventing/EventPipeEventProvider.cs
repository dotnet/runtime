// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
    internal sealed class EventPipeEventProvider : IEventProvider
    {
        // The EventPipeProvider handle.
        private IntPtr m_provHandle = IntPtr.Zero;

        // Register an event provider.
        unsafe uint IEventProvider.EventRegister(
            EventSource eventSource,
            UnsafeNativeMethods.ManifestEtw.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle)
        {
            uint returnStatus = 0;
            m_provHandle = EventPipeInternal.CreateProvider(eventSource.Name, enableCallback);
            if(m_provHandle != IntPtr.Zero)
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
        unsafe int IEventProvider.EventWriteTransferWrapper(
            long registrationHandle,
            ref EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            uint eventID = (uint)eventDescriptor.EventId;
            if(eventID != 0 && eventHandle != IntPtr.Zero)
            {
                if (userDataCount == 0)
                {
                    EventPipeInternal.WriteEventData(eventHandle, eventID, null, 0, activityId, relatedActivityId);
                    return 0;
                }

                // If Channel == 11, this is a TraceLogging event.
                // The first 3 descriptors contain event metadata that is emitted for ETW and should be discarded on EventPipe.
                // EventPipe metadata is provided via the EventPipeEventProvider.DefineEventHandle.
                if (eventDescriptor.Channel == 11)
                {
                    userData = userData + 3;
                    userDataCount = userDataCount - 3;
                    Debug.Assert(userDataCount >= 0);
                }
                EventPipeInternal.WriteEventData(eventHandle, eventID, userData, (uint) userDataCount, activityId, relatedActivityId);
            }
            return 0;
        }

        // Get or set the per-thread activity ID.
        int IEventProvider.EventActivityIdControl(UnsafeNativeMethods.ManifestEtw.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return EventPipeInternal.EventActivityIdControl((uint)ControlCode, ref ActivityId);
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte *pMetadata, uint metadataLength)
        {
            IntPtr eventHandlePtr = EventPipeInternal.DefineEvent(m_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
            return eventHandlePtr;
        }
    }
}
