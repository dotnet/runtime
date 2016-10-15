// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Net
{
    internal sealed partial class SecurityEventSource : EventSource
    {
        [Event(AcquireDefaultCredentialId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void AcquireDefaultCredential(string packageName, Interop.SspiCli.CredentialUse intent)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            string arg1Str = "";
            if (packageName != null)
            {
                arg1Str = packageName;
            }

            s_log.WriteEvent(AcquireDefaultCredentialId, arg1Str, intent);
        }

        [NonEvent]
        internal static void AcquireCredentialsHandle(string packageName, Interop.SspiCli.CredentialUse intent, object authdata)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            string arg1Str = "";
            if (packageName != null)
            {
                arg1Str = packageName;
            }

            s_log.AcquireCredentialsHandle(arg1Str, intent, LoggingHash.GetObjectName(authdata));
        }

        [Event(AcquireCredentialsHandleId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void AcquireCredentialsHandle(string packageName, Interop.SspiCli.CredentialUse intent, string authdata)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            const int SizeData = 3;
            fixed (char* arg1Ptr = packageName, arg2Ptr = authdata)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (packageName.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&intent);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (authdata.Length + 1) * sizeof(char);

                WriteEventCore(AcquireCredentialsHandleId, SizeData, dataDesc);
            }
        }

        [Event(InitializeSecurityContextId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void InitializeSecurityContext(string credential, string context, string targetName, Interop.SspiCli.ContextFlags inFlags)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = credential, arg2Ptr = context, arg3Ptr = targetName)
            {
                const int SizeData = 4;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (credential.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (context.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (targetName.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&inFlags);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(InitializeSecurityContextId, SizeData, dataDesc);
            }
        }

        [Event(AcceptSecuritContextId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void AcceptSecurityContext(string credential, string context, Interop.SspiCli.ContextFlags inFlags)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = credential, arg2Ptr = context)
            {
                const int SizeData = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (credential.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (context.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&inFlags);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(AcceptSecuritContextId, SizeData, dataDesc);
            }
        }

        [Event(OperationReturnedSomethingId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void OperationReturnedSomething(string operation, Interop.SECURITY_STATUS errorCode)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(OperationReturnedSomethingId, operation, errorCode);
        }

        [Event(SecurityContextInputBufferId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SecurityContextInputBuffer(string context, int inputBufferSize, int outputBufferSize, Interop.SECURITY_STATUS errorCode)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = context)
            {
                const int SizeData = 4;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (context.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&inputBufferSize);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&outputBufferSize);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&errorCode);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(SecurityContextInputBufferId, SizeData, dataDesc);
            }
        }

        [Event(SecurityContextInputBuffersId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SecurityContextInputBuffers(string context, int inputBuffersSize, int outputBufferSize, Interop.SECURITY_STATUS errorCode)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = context)
            {
                const int SizeData = 4;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (context.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&inputBuffersSize);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&outputBufferSize);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&errorCode);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(SecurityContextInputBuffersId, SizeData, dataDesc);
            }
        }
    }
}
