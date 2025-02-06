// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    internal static partial class EventPipeInternal
    {
#if FEATURE_PERFTRACING
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
        internal static unsafe partial IntPtr CreateProvider(string providerName,
            delegate* unmanaged<byte*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void> callbackFunc,
            void* callbackContext);

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
#else
#pragma warning disable IDE0060
        private static unsafe ulong Enable(
            char* outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfigurationNative* providers,
            uint numProviders)
        {
            return 0;
        }

        internal static void Disable(ulong sessionID)
        {
        }

        internal static unsafe IntPtr CreateProvider(string providerName,
            delegate* unmanaged<byte*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void> callbackFunc,
            void* callbackContext)
        {
            return IntPtr.Zero;
        }

        internal static unsafe IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, void *pMetadata, uint metadataLength)
        {
            return IntPtr.Zero;
        }

        internal static IntPtr GetProvider(string providerName)
        {
            return IntPtr.Zero;
        }

        internal static void DeleteProvider(IntPtr provHandle)
        {
        }

        internal static int EventActivityIdControl(uint controlCode, ref Guid activityId)
        {
            return 0;
        }

        internal static unsafe void WriteEventData(IntPtr eventHandle, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId)
        {
        }

        internal static unsafe bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo)
        {
            return false;
        }

        internal static unsafe bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance)
        {
            return false;
        }

        internal static unsafe bool SignalSession(ulong sessionID)
        {
            return false;
        }

        internal static unsafe bool WaitForSessionSignal(ulong sessionID, int timeoutMs)
        {
            return false;
        }
#pragma warning restore IDE0060
#endif //FEATURE_PERFTRACING
    }
}
