// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#if FEATURE_PERFTRACING

namespace System.Diagnostics.Tracing
{
    internal static partial class EventPipeInternal
    {
        //
        // These PInvokes are used by the configuration APIs to interact with EventPipe.
        //
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_Enable")]
        private static unsafe partial ulong Enable(
            char* outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfigurationNative* providers,
            uint numProviders);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_Disable")]
        internal static partial void Disable(ulong sessionID);

        //
        // These PInvokes are used by EventSource to interact with the EventPipe.
        //
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_CreateProvider", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr CreateProvider(string providerName, Interop.Advapi32.EtwEnableCallback callbackFunc);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_DefineEvent")]
        internal static unsafe partial IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, void *pMetadata, uint metadataLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetProvider", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr GetProvider(string providerName);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_DeleteProvider")]
        internal static partial void DeleteProvider(IntPtr provHandle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_EventActivityIdControl")]
        internal static partial int EventActivityIdControl(uint controlCode, ref Guid activityId);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WriteEventData")]
        internal static unsafe partial void WriteEventData(IntPtr eventHandle, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId);

        //
        // These PInvokes are used as part of the EventPipeEventDispatcher.
        //
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetSessionInfo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetNextEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_SignalSession")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SignalSession(ulong sessionID);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WaitForSessionSignal")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool WaitForSessionSignal(ulong sessionID, int timeoutMs);
    }
}

#endif // FEATURE_PERFTRACING
