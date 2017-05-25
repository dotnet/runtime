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
            ref Guid providerId,
            UnsafeNativeMethods.ManifestEtw.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle)
        {
            uint returnStatus = 0;
            m_provHandle = EventPipeInternal.CreateProvider(providerId, enableCallback);
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
                    EventPipeInternal.WriteEvent(eventHandle, eventID, null, 0, activityId, relatedActivityId);
                    return 0;
                }

                uint length = 0;
                for (int i = 0; i < userDataCount; i++)
                {
                    length += userData[i].Size;
                }

                byte[] data = new byte[length];
                fixed (byte *pData = data)
                {
                    uint offset = 0;
                    for (int i = 0; i < userDataCount; i++)
                    {
                        byte * singleUserDataPtr = (byte *)(userData[i].Ptr);
                        uint singleUserDataSize = userData[i].Size;
                        WriteToBuffer(pData, length, ref offset, singleUserDataPtr, singleUserDataSize);
                    }
                    EventPipeInternal.WriteEvent(eventHandle, eventID, pData, length, activityId, relatedActivityId);
                }
            }
            return 0;
        }

        // Get or set the per-thread activity ID.
        int IEventProvider.EventActivityIdControl(UnsafeNativeMethods.ManifestEtw.ActivityControl ControlCode, ref Guid ActivityId)
        {
            return 0;
        }

        // Define an EventPipeEvent handle.
        unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, Int64 keywords, uint eventVersion, uint level, byte *pMetadata, uint metadataLength)
        {
            IntPtr eventHandlePtr = EventPipeInternal.DefineEvent(m_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
            return eventHandlePtr;
        }

        // Copy src to buffer and modify the offset.
        // Note: We know the buffer size ahead of time to make sure no buffer overflow.
        private static unsafe void WriteToBuffer(byte *buffer, uint bufferLength, ref uint offset, byte *src, uint srcLength)
        {
            Debug.Assert(bufferLength >= (offset + srcLength));
            for (int i = 0; i < srcLength; i++)
            {
                *(byte *)(buffer + offset + i) = *(byte *)(src + i);
            }
            offset += srcLength;
        }
    }
}
