// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    [EventSource(Name = "Microsoft-System-Net-Sockets",
        Guid = "e03c0352-f9c9-56ff-0ea7-b94ba8cabc6b",
        LocalizationResources = "FxResources.System.Net.Sockets.SR")]
    internal sealed class SocketsEventSource : EventSource
    {
        private const int AcceptedId = 1;
        private const int ConnectedId = 2;
        private const int ConnectedAsyncDnsId = 3;
        private const int NotLoggedFileId = 4;
        private const int DumpArrayId = 5;

        private const int DefaultMaxDumpSize = 1024;

        private readonly static SocketsEventSource s_log = new SocketsEventSource();
        private SocketsEventSource() { }
        public static SocketsEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [NonEvent]
        internal static void Accepted(Socket socket, object remoteEp, object localEp)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            s_log.Accepted(LoggingHash.GetObjectName(remoteEp), LoggingHash.GetObjectName(localEp), LoggingHash.HashInt(socket));
        }

        [Event(AcceptedId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void Accepted(string remoteEp, string localEp, int socketHash)
        {
            fixed (char* arg1Ptr = remoteEp, arg2Ptr = localEp)
            {
                const int SizeData = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (remoteEp.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (localEp.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(AcceptedId, SizeData, dataDesc);
            }
        }

        [NonEvent]
        internal static void Connected(Socket socket, object localEp, object remoteEp)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            s_log.Connected(LoggingHash.GetObjectName(localEp), LoggingHash.GetObjectName(remoteEp), LoggingHash.HashInt(socket));
        }

        [Event(ConnectedId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void Connected(string localEp, string remoteEp, int socketHash)
        {
            fixed (char* arg1Ptr = localEp, arg2Ptr = remoteEp)
            {
                const int SizeData = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (localEp.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (remoteEp.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(ConnectedId, SizeData, dataDesc);
            }
        }

        [Event(ConnectedAsyncDnsId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void ConnectedAsyncDns(int socketHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(ConnectedAsyncDnsId, socketHash);
        }

        [Event(NotLoggedFileId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void NotLoggedFile(string filePath, int socketHash, SocketAsyncOperation completedOperation)
        {
            fixed (char* arg1Ptr = filePath)
            {
                const int SizeData = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (filePath.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&completedOperation);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(NotLoggedFileId, SizeData, dataDesc);
            }
        }

        [NonEvent]
        internal static void Dump(IntPtr bufferPtr, int length, [CallerMemberName] string callerName = "")
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            if (length > DefaultMaxDumpSize)
            {
                length = DefaultMaxDumpSize;
            }

            byte[] buffer = new byte[length];
            Marshal.Copy(bufferPtr, buffer, 0, length);
            s_log.DebugDumpArray(buffer, callerName);
        }

        [NonEvent]
        internal static void Dump(byte[] buffer, int offset, int length, [CallerMemberName] string callerName = "")
        {
            if (!s_log.IsEnabled() || offset > buffer.Length)
            {
                return;
            }

            if (length > DefaultMaxDumpSize)
            {
                length = DefaultMaxDumpSize;
            }

            if ((length < 0) || (length > buffer.Length - offset))
            {
                length = buffer.Length - offset;
            }

            byte[] partialBuffer = new byte[length];
            Buffer.BlockCopy(buffer, offset, partialBuffer, 0, length);
            s_log.DebugDumpArray(partialBuffer, callerName);
        }

        [Event(DumpArrayId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void DebugDumpArray(byte[] buffer, string callerMemberName)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            const int SizeData = 3;
            EventSource.EventData* descrs = stackalloc EventSource.EventData[SizeData];
            if (buffer == null || buffer.Length == 0)
            {
                int blobSize = 0;
                fixed (char* arg1Ptr = callerMemberName)
                {

                    descrs[0].DataPointer = (IntPtr)(&blobSize);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)(&blobSize); // Valid address instead of empty contents.
                    descrs[1].Size = 0;
                    descrs[2].DataPointer = (IntPtr)(arg1Ptr);
                    descrs[2].Size = (callerMemberName.Length + 1) * sizeof(char);
                    WriteEventCore(DumpArrayId, SizeData, descrs);
                }
            }
            else
            {
                int blobSize = buffer.Length;
                fixed (byte* blob = &buffer[0])
                fixed (char* arg1Ptr = callerMemberName)
                {

                    descrs[0].DataPointer = (IntPtr)(&blobSize);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)blob;
                    descrs[1].Size = blobSize;
                    descrs[2].DataPointer = (IntPtr)(arg1Ptr);
                    descrs[2].Size = (callerMemberName.Length + 1) * sizeof(char);
                    WriteEventCore(DumpArrayId, SizeData, descrs);
                }
            }
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
