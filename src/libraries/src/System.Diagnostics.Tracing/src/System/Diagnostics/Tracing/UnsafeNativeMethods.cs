// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32
{
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class UnsafeNativeMethods
    {
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurityAttribute()]
        internal static unsafe class ManifestEtw
        {
            //
            // Constants error coded returned by ETW APIs
            //

            // The event size is larger than the allowed maximum (64k - header).
            internal const int ERROR_ARITHMETIC_OVERFLOW = 534;

            // Occurs when filled buffers are trying to flush to disk, 
            // but disk IOs are not happening fast enough. 
            // This happens when the disk is slow and event traffic is heavy. 
            // Eventually, there are no more free (empty) buffers and the event is dropped.
            internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_NOT_SUPPORTED = 50;
            internal const int ERROR_INVALID_PARAMETER = 0x57;

            //
            // ETW Methods
            //

            internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
            internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
            internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;

            //
            // Callback
            //
            [SecurityCritical]
            internal unsafe delegate void EtwEnableCallback(
                [In] ref Guid sourceId,
                [In] int isEnabled,
                [In] byte level,
                [In] long matchAnyKeywords,
                [In] long matchAllKeywords,
                [In] EVENT_FILTER_DESCRIPTOR* filterData,
                [In] void* callbackContext
                );

            //
            // Registration APIs
            //
            [SecurityCritical]
            internal static unsafe uint EventRegister(
                        [In] ref Guid providerId,
                        [In]EtwEnableCallback enableCallback,
                        [In]void* callbackContext,
                        [In][Out]ref long registrationHandle
                        )
            {
                Interop.Kernel32.EtwEnableCallback indirection = delegate(ref Guid sourceId,
                                                                         int isEnabled,
                                                                         byte level,
                                                                         ulong matchAnyKeywords,
                                                                         ulong matchAllKeywords,
                                                                         Interop.Kernel32.EVENT_FILTER_DESCRIPTOR* filterData,
                                                                         IntPtr cbContext)
                {
                    enableCallback(ref sourceId,
                                   isEnabled,
                                   level,
                                   (long)matchAnyKeywords,
                                   (long)matchAllKeywords,
                                   (EVENT_FILTER_DESCRIPTOR*)filterData,
                                   (void*)cbContext);
                };
                ulong temp;
                uint status = Interop.Kernel32.EventRegister(ref providerId, indirection, (IntPtr)callbackContext, out temp);
                registrationHandle = (long)temp;
                
                return status;
            }


            [SecurityCritical]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            internal static uint EventUnregister([In] long registrationHandle)
            {
                return Interop.Kernel32.EventUnregister((ulong)registrationHandle);
            }


            [StructLayout(LayoutKind.Sequential)]
            unsafe internal struct EVENT_FILTER_DESCRIPTOR
            {
                public long Ptr;
                public int Size;
                public int Type;
            };

            /// <summary>
            ///  Call the ETW native API EventWriteTransfer and checks for invalid argument error. 
            ///  The implementation of EventWriteTransfer on some older OSes (Windows 2008) does not accept null relatedActivityId.
            ///  So, for these cases we will retry the call with an empty Guid.
            /// </summary>
            internal static int EventWriteTransferWrapper(long registrationHandle,
                                                         ref EventDescriptor eventDescriptor,
                                                         Guid* activityId,
                                                         Guid* relatedActivityId,
                                                         int userDataCount,
                                                         EventProvider.EventData* userData)
            {
                int HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
                if (HResult == ERROR_INVALID_PARAMETER && relatedActivityId == null)
                {
                    Guid emptyGuid = Guid.Empty;
                    HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, &emptyGuid, userDataCount, userData);
                }

                return HResult;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            private static unsafe int EventWriteTransfer(
                    [In] long registrationHandle,
                    [In] ref EventDescriptor eventDescriptor,
                    [In] Guid* activityId,
                    [In] Guid* relatedActivityId,
                    [In] int userDataCount,
                    [In] EventProvider.EventData* userData
                    )
            {
                IntPtr descripPtr = Marshal.AllocHGlobal(Marshal.SizeOf(eventDescriptor));
                Marshal.StructureToPtr(eventDescriptor, descripPtr, false);
                int status = Interop.Kernel32.EventWriteTransfer((ulong)registrationHandle, 
                                                        (void*)descripPtr, 
                                                        activityId, 
                                                        relatedActivityId, 
                                                        userDataCount, 
                                                        (void*)userData);
                Marshal.FreeHGlobal(descripPtr);
                return status;
            }

            internal enum ActivityControl : uint
            {
                EVENT_ACTIVITY_CTRL_GET_ID = 1,
                EVENT_ACTIVITY_CTRL_SET_ID = 2,
                EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
                EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
                EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
            };

            internal enum EVENT_INFO_CLASS
            {
                BinaryTrackInfo,
                SetEnableAllKeywords, // obsolete
                SetTraits,
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            internal static int EventSetInformation(
                [In] long registrationHandle,
                [In] EVENT_INFO_CLASS informationClass,
                [In] void* eventInformation,
                [In] int informationLength)
            {
                return Interop.Kernel32.EventSetInformation((ulong)registrationHandle, (int)informationClass, (IntPtr)eventInformation, informationLength);
            }

            // We want to not use this API for the Nuget package, as it is not an allowed API in the Store.  
#if ES_SESSION_INFO || FEATURE_ACTIVITYSAMPLING

            // Support for EnumerateTraceGuidsEx
            internal enum TRACE_QUERY_INFO_CLASS
            {
                TraceGuidQueryList,
                TraceGuidQueryInfo,
                TraceGuidQueryProcess,
                TraceStackTracingInfo,
                MaxTraceSetInfoClass
            };

            internal struct TRACE_GUID_INFO
            {
                public int InstanceCount;
                public int Reserved;
            };

            internal struct TRACE_PROVIDER_INSTANCE_INFO
            {
                public int NextOffset;
                public int EnableCount;
                public int Pid;
                public int Flags;
            };

#pragma warning disable 0649
            internal struct TRACE_ENABLE_INFO
            {
                public int IsEnabled;
                public byte Level;
                public byte Reserved1;
                public ushort LoggerId;
                public int EnableProperty;
                public int Reserved2;
                public long MatchAnyKeyword;
                public long MatchAllKeyword;
            };
#pragma warning restore 0649

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            internal static int EnumerateTraceGuidsEx(
                TRACE_QUERY_INFO_CLASS TraceQueryInfoClass,
                void* InBuffer,
                int InBufferSize,
                void* OutBuffer,
                int OutBufferSize,
                ref int ReturnLength)
            {
                return Interop.Kernel32.EnumerateTraceGuidsEx(TraceQueryInfoClass,
                                                             InBuffer,
                                                             InBufferSize,
                                                             OutBuffer,
                                                             OutBufferSize,
                                                             ReturnLength);
                )
            }
#endif
        }
    }

    internal static class Win32Native
    {
#if ES_BUILD_PCL
        private const string CoreProcessThreadsApiSet = "api-ms-win-core-processthreads-l1-1-0";
        private const string CoreLocalizationApiSet = "api-ms-win-core-localization-l1-2-0";
#else
        private const string CoreProcessThreadsApiSet = "kernel32.dll";
        private const string CoreLocalizationApiSet = "kernel32.dll";
#endif

        [System.Security.SecuritySafeCritical]
        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode)
        {
            return Interop.Kernel32.GetMessage(errorCode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [System.Security.SecurityCritical]
        internal static uint GetCurrentProcessId()
        {
            return Interop.Kernel32.GetCurrentProcessId();
        }


        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }
}
