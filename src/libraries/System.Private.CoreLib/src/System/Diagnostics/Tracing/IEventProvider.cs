// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Win32;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    // Represents the interface between EventProvider and an external logging mechanism.
    internal interface IEventProvider
    {
        // Register an event provider.
        unsafe uint EventRegister(
            EventSource eventSource,
            Interop.Advapi32.EtwEnableCallback enableCallback,
            void* callbackContext,
            ref long registrationHandle);

        // Unregister an event provider.
        uint EventUnregister(long registrationHandle);

        // Write an event.
        unsafe EventProvider.WriteEventErrorCode EventWriteTransfer(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData);

        // Get or set the per-thread activity ID.
        int EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId);

        // Define an EventPipeEvent handle.
        unsafe IntPtr DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte *pMetadata, uint metadataLength);
    }
}
