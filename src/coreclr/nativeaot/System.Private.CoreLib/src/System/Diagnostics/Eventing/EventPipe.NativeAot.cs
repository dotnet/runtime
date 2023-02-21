// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;

#if FEATURE_PERFTRACING

namespace System.Diagnostics.Tracing
{
    //
    // NOTE:
    //
    // The implementation below takes some manual marshaling actions to ensure arguments are in
    // primitive form before they are passed through to the underlying RuntimeImports.Rh*
    // function.
    //
    // These extra steps are necessary only if the RuntimeImports mechanism represents "raw"
    // calls into the native runtime (as has been the case at least in the distant past).
    //
    // If the RuntimeImports mechanism automatically applies rich p/invoke marshaling to all of
    // these calls, then all of the manual steps below are unnecessary and can be removed (by
    // making the RuntimeImports.Rh* function signatures generally match the corresponding
    // EventPipeInternal function signatures; in other words, by making the RuntimeImports.Rh*
    // functions look like the QCalls in EventPipe.CoreCLR.cs).
    //
    internal static partial class EventPipeInternal
    {
        //
        // These PInvokes are used by the configuration APIs to interact with EventPipe.
        //
        private static unsafe ulong Enable(
            char* outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfigurationNative* providers,
            uint numProviders)
        {
            return RuntimeImports.RhEventPipeInternal_Enable(
                outputFile,
                (int)format,
                circularBufferSizeInMB,
                providers,
                numProviders);
        }

        internal static void Disable(ulong sessionID)
        {
            RuntimeImports.RhEventPipeInternal_Disable(sessionID);
        }

        //
        // These PInvokes are used by EventSource to interact with the EventPipe.
        //

//        private static extern unsafe IntPtr CreateProvider(string providerName, IntPtr callbackFunc, IntPtr callbackContext);

        internal static unsafe IntPtr CreateProvider(string providerName,
                    delegate* unmanaged<byte*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void> callbackFunc,
                    void* callbackContext)
                    => CreateProvider(providerName, (IntPtr)callbackFunc, (IntPtr)callbackContext);
        //internal static unsafe IntPtr CreateProvider(string providerName, IntPtr callbackFunc, IntPtr callbackContext);

        internal static unsafe IntPtr CreateProvider(string providerName, IntPtr callbackFunc, IntPtr callbackContext)
        {
            fixed (char* pProviderName = providerName)
            {
                return RuntimeImports.RhEventPipeInternal_CreateProvider(
                    pProviderName,
                    callbackFunc,
                    callbackContext);
            }
        }

        internal static unsafe IntPtr DefineEvent(
            IntPtr provHandle,
            uint eventID,
            long keywords,
            uint eventVersion,
            uint level,
            void *pMetadata,
            uint metadataLength)
        {
            return RuntimeImports.RhEventPipeInternal_DefineEvent(
                provHandle,
                eventID,
                keywords,
                eventVersion,
                level,
                pMetadata,
                metadataLength);
        }

        internal static unsafe IntPtr GetProvider(string providerName)
        {
            fixed (char* pProviderName = providerName)
            {
                return RuntimeImports.RhEventPipeInternal_GetProvider(pProviderName);
            }
        }

        internal static void DeleteProvider(IntPtr provHandle)
        {
            RuntimeImports.RhEventPipeInternal_DeleteProvider(provHandle);
        }

        internal static unsafe int EventActivityIdControl(uint controlCode, ref Guid activityId)
        {
            //
            // Ensure that the address passed to native code is never on the managed heap, while still
            // managing the supplied byref in an in/out manner.
            //
            Guid localActivityId = activityId;
            try { return RuntimeImports.RhEventPipeInternal_EventActivityIdControl(controlCode, &localActivityId); }
            finally { activityId = localActivityId; }
        }

        internal static unsafe void WriteEventData(
            IntPtr eventHandle,
            EventProvider.EventData* pEventData,
            uint dataCount,
            Guid* activityId,
            Guid* relatedActivityId)
        {
            RuntimeImports.RhEventPipeInternal_WriteEventData(
                eventHandle,
                pEventData,
                dataCount,
                activityId,
                relatedActivityId);
        }

        //
        // These PInvokes are used as part of the EventPipeEventDispatcher.
        //
        internal static unsafe bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo)
        {
            uint rawBool = RuntimeImports.RhEventPipeInternal_GetSessionInfo(sessionID, pSessionInfo);
            return (rawBool != 0);
        }

        internal static unsafe bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance)
        {
            uint rawBool = RuntimeImports.RhEventPipeInternal_GetNextEvent(sessionID, pInstance);
            return (rawBool != 0);
        }

        internal static bool SignalSession(ulong sessionID)
        {
            uint rawBool = RuntimeImports.RhEventPipeInternal_SignalSession(sessionID);
            return (rawBool != 0);
        }

        internal static bool WaitForSessionSignal(ulong sessionID, int timeoutMs)
        {
            uint rawBool = RuntimeImports.RhEventPipeInternal_WaitForSessionSignal(sessionID, timeoutMs);
            return (rawBool != 0);
        }
    }
}

#endif // FEATURE_PERFTRACING

