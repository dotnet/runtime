// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.Security;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private const int EnumerateSecurityPackagesId = NextAvailableEventId;
        private const int SspiPackageNotFoundId = EnumerateSecurityPackagesId + 1;
        private const int AcquireDefaultCredentialId = SspiPackageNotFoundId + 1;
        private const int AcquireCredentialsHandleId = AcquireDefaultCredentialId + 1;
        private const int InitializeSecurityContextId = AcquireCredentialsHandleId + 1;
        private const int SecurityContextInputBufferId = InitializeSecurityContextId + 1;
        private const int SecurityContextInputBuffersId = SecurityContextInputBufferId + 1;
        private const int AcceptSecuritContextId = SecurityContextInputBuffersId + 1;
        private const int OperationReturnedSomethingId = AcceptSecuritContextId + 1;
        // Make sure to update the event IDs in NetEventSource.Security.cs if you add more events here

        [Event(EnumerateSecurityPackagesId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void EnumerateSecurityPackages(string? securityPackage) =>
            WriteEvent(EnumerateSecurityPackagesId, securityPackage);

        [Event(SspiPackageNotFoundId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SspiPackageNotFound(string packageName) =>
            WriteEvent(SspiPackageNotFoundId, packageName);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "parameter intent is an enum and is trimmer safe")]
        [Event(AcquireDefaultCredentialId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void AcquireDefaultCredential(string packageName, Interop.SspiCli.CredentialUse intent) =>
            WriteEvent(AcquireDefaultCredentialId, packageName, intent);

        [NonEvent]
        public void AcquireCredentialsHandle(string packageName, Interop.SspiCli.CredentialUse intent, object authdata) =>
            AcquireCredentialsHandle(packageName, intent, IdOf(authdata));

        [Event(AcquireCredentialsHandleId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void AcquireCredentialsHandle(string packageName, Interop.SspiCli.CredentialUse intent, string authdata) =>
            WriteEvent(AcquireCredentialsHandleId, packageName, (int)intent, authdata);

        [NonEvent]
        public void InitializeSecurityContext(SafeFreeCredentials? credential, SafeDeleteContext? context, string? targetName, Interop.SspiCli.ContextFlags inFlags) =>
            InitializeSecurityContext(IdOf(credential), IdOf(context), targetName, inFlags);

        [Event(InitializeSecurityContextId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void InitializeSecurityContext(string credential, string context, string? targetName, Interop.SspiCli.ContextFlags inFlags) =>
            WriteEvent(InitializeSecurityContextId, credential, context, targetName, (int)inFlags);

        [NonEvent]
        public void AcceptSecurityContext(SafeFreeCredentials? credential, SafeDeleteContext? context, Interop.SspiCli.ContextFlags inFlags) =>
            AcceptSecurityContext(IdOf(credential), IdOf(context), inFlags);

        [Event(AcceptSecuritContextId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void AcceptSecurityContext(string credential, string context, Interop.SspiCli.ContextFlags inFlags) =>
            WriteEvent(AcceptSecuritContextId, credential, context, (int)inFlags);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "parameter errorCode is an enum and is trimmer safe")]
        [Event(OperationReturnedSomethingId, Keywords = Keywords.Default, Level = EventLevel.Informational, Message = "{0} returned {1}.")]
        public void OperationReturnedSomething(string operation, Interop.SECURITY_STATUS errorCode) =>
            WriteEvent(OperationReturnedSomethingId, operation, errorCode);

        [Event(SecurityContextInputBufferId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SecurityContextInputBuffer(string context, int inputBufferSize, int outputBufferSize, Interop.SECURITY_STATUS errorCode) =>
            WriteEvent(SecurityContextInputBufferId, context, inputBufferSize, outputBufferSize, (int)errorCode);

        [Event(SecurityContextInputBuffersId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SecurityContextInputBuffers(string context, int inputBuffersSize, int outputBufferSize, Interop.SECURITY_STATUS errorCode) =>
            WriteEvent(SecurityContextInputBuffersId, context, inputBuffersSize, outputBufferSize, (int)errorCode);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, int arg2, int arg3, int arg4)
        {
            arg1 ??= "";

            fixed (char* arg1Ptr = arg1)
            {
                const int NumEventDatas = 4;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(&arg2),
                    Size = sizeof(int)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&arg3),
                    Size = sizeof(int)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)(&arg4),
                    Size = sizeof(int)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, int arg2, string? arg3)
        {
            arg1 ??= "";
            arg3 ??= "";

            fixed (char* arg1Ptr = arg1)
            fixed (char* arg3Ptr = arg3)
            {
                const int NumEventDatas = 3;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(&arg2),
                    Size = sizeof(int)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(arg3Ptr),
                    Size = (arg3.Length + 1) * sizeof(char)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, int arg4)
        {
            arg1 ??= "";
            arg2 ??= "";
            arg3 ??= "";

            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            fixed (char* arg3Ptr = arg3)
            {
                const int NumEventDatas = 4;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(arg2Ptr),
                    Size = (arg2.Length + 1) * sizeof(char)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(arg3Ptr),
                    Size = (arg3.Length + 1) * sizeof(char)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)(&arg4),
                    Size = sizeof(int)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
