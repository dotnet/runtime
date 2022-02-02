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
        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_Enable")]
        private static unsafe partial ulong Enable(
            char* outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfigurationNative* providers,
            uint numProviders);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_Disable")]
        internal static partial void Disable(ulong sessionID);

        //
        // These PInvokes are used by EventSource to interact with the EventPipe.
        //
        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_CreateProvider", CharSet = CharSet.Unicode)]
        internal static partial IntPtr CreateProvider(string providerName, Interop.Advapi32.EtwEnableCallback callbackFunc);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_DefineEvent")]
        internal static unsafe partial IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, void *pMetadata, uint metadataLength);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetProvider", CharSet = CharSet.Unicode)]
        internal static partial IntPtr GetProvider(string providerName);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_DeleteProvider")]
        internal static partial void DeleteProvider(IntPtr provHandle);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we annotate blittable types used in interop in CoreLib (like Guid)
        [DllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_EventActivityIdControl")]
        internal static extern int EventActivityIdControl(uint controlCode, ref Guid activityId);
#pragma warning restore DLLIMPORTGENANALYZER015

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WriteEventData")]
        internal static unsafe partial void WriteEventData(IntPtr eventHandle, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId);

        //
        // These PInvokes are used as part of the EventPipeEventDispatcher.
        //
        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetSessionInfo")]
        internal static unsafe partial bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetNextEvent")]
        internal static unsafe partial bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_GetWaitHandle")]
        internal static unsafe partial IntPtr GetWaitHandle(ulong sessionID);
    }
}

#endif // FEATURE_PERFTRACING
