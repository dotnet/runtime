// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

#if FEATURE_PERFTRACING

namespace System.Diagnostics.Tracing
{
    internal static partial class EventPipeInternal
    {
        // These ICalls are used by the configuration APIs to interact with EventPipe.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe ulong Enable(char* outputFile, EventPipeSerializationFormat format, uint circularBufferSizeInMB, EventPipeProviderConfigurationNative* providers, uint numProviders);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void Disable(ulong sessionID);

        //
        // These ICalls are used by EventSource to interact with the EventPipe.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr CreateProvider(string providerName, Interop.Advapi32.EtwEnableCallback callbackFunc);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr GetProvider(char* providerName);

        internal static unsafe IntPtr GetProvider(string providerName)
        {
            fixed (char* p = providerName)
            {
                return GetProvider(p);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void DeleteProvider(IntPtr provHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int EventActivityIdControl(uint controlCode, ref Guid activityId);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void WriteEventData(IntPtr eventHandle, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId);


        //
        // These ICalls are used as part of the EventPipeEventDispatcher.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr GetWaitHandle(ulong sessionID);
    }
}

#endif // FEATURE_PERFTRACING
