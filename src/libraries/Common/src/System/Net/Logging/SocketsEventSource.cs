// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    [EventSource(Name = "Microsoft-System-Net-Sockets",
        Guid = "e03c0352-f9c9-56ff-0ea7-b94ba8cabc6b",
        LocalizationResources = "FxResources.System.Net.Sockets.SR")]
    internal sealed class SocketsEventSource : EventSource
    {
        private const int ACCEPTED_ID = 1;
        private const int CONNECTED_ID = 2;
        private const int CONNECTED_ASYNC_DNS_ID = 3;
        private const int NOT_LOGGED_FILE_ID = 4;
        private const int DUMP_ARRAY_ID = 5;
        private const int DUMP_ARRAY_ASYNC_ID = 6;

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

        [Event(ACCEPTED_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void Accepted(string remoteEp, string localEp, int socketHash)
        {
            fixed (char* arg1Ptr = remoteEp, arg2Ptr = localEp)
            {
                const int SIZEDATA = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (remoteEp.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (localEp.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(ACCEPTED_ID, SIZEDATA, dataDesc);
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

        [Event(CONNECTED_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void Connected(string localEp, string remoteEp, int socketHash)
        {
            fixed (char* arg1Ptr = localEp, arg2Ptr = remoteEp)
            {
                const int SIZEDATA = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (localEp.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (remoteEp.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(CONNECTED_ID, SIZEDATA, dataDesc);
            }
        }

        [Event(CONNECTED_ASYNC_DNS_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void ConnectedAsyncDns(int socketHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(CONNECTED_ASYNC_DNS_ID, socketHash);
        }

        [Event(NOT_LOGGED_FILE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void NotLoggedFile(string filePath, int socketHash, SocketAsyncOperation completedOperation)
        {
            fixed (char* arg1Ptr = filePath)
            {
                const int SIZEDATA = 3;
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (filePath.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&completedOperation);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&socketHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(NOT_LOGGED_FILE_ID, SIZEDATA, dataDesc);
            }
        }

        [NonEvent]
        internal static void Dump(object method, IntPtr bufferPtr, int length)
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

            if (method is MethodType)
            {
                s_log.DebugDumpArray((MethodType)method, buffer);
            }
            else if (method is SocketAsyncOperation)
            {
                s_log.DumpArrayAsync((SocketAsyncOperation)method, buffer);
            }
        }

        [NonEvent]
        internal static void Dump(object method, byte[] buffer, int offset, int length)
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

            if (method is MethodType)
            {
                s_log.DebugDumpArray((MethodType)method, partialBuffer);
            }
            else if (method is SocketAsyncOperation)
            {
                s_log.DumpArrayAsync((SocketAsyncOperation)method, partialBuffer);
            }
        }

        [Event(DUMP_ARRAY_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void DebugDumpArray(MethodType method, byte[] buffer)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            const int SIZEDATA = 3;
            EventSource.EventData* descrs = stackalloc EventSource.EventData[SIZEDATA];
            descrs[0].DataPointer = (IntPtr)(&method);
            descrs[0].Size = sizeof(int);
            if (buffer == null || buffer.Length == 0)
            {
                int blobSize = 0;
                descrs[1].DataPointer = (IntPtr)(&blobSize);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&blobSize); // Valid address instead of empty contents.
                descrs[2].Size = 0;
                WriteEventCore(DUMP_ARRAY_ID, SIZEDATA, descrs);
            }
            else
            {
                int blobSize = buffer.Length;
                fixed (byte* blob = &buffer[0])
                {
                    descrs[1].DataPointer = (IntPtr)(&blobSize);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)blob;
                    descrs[2].Size = blobSize;
                    WriteEventCore(DUMP_ARRAY_ID, SIZEDATA, descrs);
                }
            }
        }

        [Event(DUMP_ARRAY_ASYNC_ID, Keywords = Keywords.Default,
    Level = EventLevel.Informational)]
        internal unsafe void DumpArrayAsync(SocketAsyncOperation completedOperation, byte[] buffer)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            const int SIZEDATA = 3;
            EventSource.EventData* descrs = stackalloc EventSource.EventData[SIZEDATA];
            descrs[0].DataPointer = (IntPtr)(&completedOperation);
            descrs[0].Size = sizeof(int);
            if (buffer == null || buffer.Length == 0)
            {
                int blobSize = 0;
                descrs[1].DataPointer = (IntPtr)(&blobSize);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&blobSize); // Valid address instead of empty contents.
                descrs[2].Size = 0;
                WriteEventCore(DUMP_ARRAY_ASYNC_ID, SIZEDATA, descrs);
            }
            else
            {
                int blobSize = buffer.Length;
                fixed (byte* blob = &buffer[0])
                {
                    descrs[1].DataPointer = (IntPtr)(&blobSize);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)blob;
                    descrs[2].Size = blobSize;
                    WriteEventCore(DUMP_ARRAY_ASYNC_ID, SIZEDATA, descrs);
                }
            }
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }

        public enum MethodType
        {
            Send,
            SendTo,
            Receive,
            ReceiveFrom,
            FinishOperation,
            PostCompletion
        }
    }
}
