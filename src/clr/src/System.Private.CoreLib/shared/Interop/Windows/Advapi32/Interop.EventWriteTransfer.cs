// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;
#if ES_BUILD_STANDALONE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

internal partial class Interop
{
    internal partial class Advapi32
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

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, EntryPoint = "EventWriteTransfer")]
        private static unsafe extern int EventWriteTransfer_PInvoke(
            long registrationHandle,
            in EventDescriptor eventDescriptor,
            Guid* activityId,
            Guid* relatedActivityId,
            int userDataCount,
            EventProvider.EventData* userData);
    }
}
