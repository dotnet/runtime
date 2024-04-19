// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        /// <summary>
        ///  Call the ETW native API EventWriteTransfer and checks for invalid argument error.
        ///  The implementation of EventWriteTransfer on some older OSes (Windows 2008) does not accept null relatedActivityId.
        ///  So, for these cases we will retry the call with an empty Guid.
        /// </summary>
        internal static unsafe int EventWriteTransfer(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData)
        {
            int HResult = EventWriteTransfer_PInvoke(registrationHandle, in eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
            if (HResult == Errors.ERROR_INVALID_PARAMETER && relatedActivityId == null)
            {
                Guid emptyGuid = Guid.Empty;
                HResult = EventWriteTransfer_PInvoke(registrationHandle, in eventDescriptor, activityId, &emptyGuid, userDataCount, userData);
            }

            return HResult;
        }

        [LibraryImport(Libraries.Advapi32, EntryPoint = "EventWriteTransfer")]
        private static unsafe partial int EventWriteTransfer_PInvoke(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData);
    }
}
