// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        /// <summary>
        /// Make sure to keep in sync with EventProvider.EventData.
        /// </summary>
        public struct EventData
        {
            internal unsafe ulong Ptr;
            internal uint Size;
            internal uint Reserved;
        }

        [DllImport(Interop.Libraries.Eventing)]
        internal unsafe static extern int EventWriteTransfer(
                ulong registrationHandle,
                void* eventDescriptor,
                Guid* activityId,
                Guid* relatedActivityId,
                int userDataCount,
                EventData* userData
                );
    }
}
