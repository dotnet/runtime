// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// Errors (HRESULT) returned for DiagnosticsServerCommandId.Error responses.
    /// </summary>
    internal enum DiagnosticsIpcError : uint
    {
        Fail                  = 0x80004005,
        InvalidArgument       = 0x80070057,
        NotSupported          = 0x80131515,
        ProfilerAlreadyActive = 0x8013136A,
        BadEncoding           = 0x80131384,
        UnknownCommand        = 0x80131385,
        UnknownMagic          = 0x80131386,
        UnknownError          = 0x80131387,
    }

    /// <summary>
    /// Different diagnostic message types that are handled by the runtime.
    /// </summary>
    internal enum DiagnosticsMessageType : uint
    {
        /// <summary>
        /// Initiates core dump generation 
        /// </summary>
        GenerateCoreDump = 1,
        /// <summary>
        /// Starts an EventPipe session that writes events to a file when the session is stopped or the application exits.
        /// </summary>
        StartEventPipeTracing = 1024,
        /// <summary>
        /// Stops an EventPipe session.
        /// </summary>
        StopEventPipeTracing = 1025,
        /// <summary>
        /// Starts an EventPipe session that sends events out-of-proc through IPC.
        /// </summary>
        CollectEventPipeTracing = 1026,
        /// <summary>
        /// Attaches a profiler to an existing process
        /// </summary>
        AttachProfiler = 2048,
    }


    /// <summary>
    /// Message header used to send commands to the .NET Core runtime through IPC.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MessageHeader
    {
        /// <summary>
        /// Request type.
        /// </summary>
        public DiagnosticsMessageType RequestType;

        /// <summary>
        /// Remote process Id.
        /// </summary>
        public uint Pid;
    }


    internal class IpcMessage
    {
        public IpcMessage()
        { }

        public IpcMessage(IpcHeader header, byte[] payload = null)
        {
            Payload = payload ?? Array.Empty<byte>();
            Header = header;
        }

        public IpcMessage(DiagnosticsServerCommandSet commandSet, byte commandId, byte[] payload = null)
        : this(new IpcHeader(commandSet, commandId), payload)
        {
        }

        public byte[] Payload { get; private set; } = null;
        public IpcHeader Header { get; private set; } = default;

        public byte[] Serialize()
        { 
            byte[] serializedData = null;
            // Verify things will fit in the size capacity
            Header.Size = checked((UInt16)(IpcHeader.HeaderSizeInBytes + Payload.Length));
            byte[] headerBytes = Header.Serialize();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(headerBytes);
                writer.Write(Payload);
                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static IpcMessage Parse(Stream stream)
        {
            IpcMessage message = new IpcMessage();
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                message.Header = IpcHeader.Parse(reader);
                message.Payload = reader.ReadBytes(message.Header.Size - IpcHeader.HeaderSizeInBytes);
                return message;
            }
        }

        public static async Task<IpcMessage> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            IpcMessage message = new IpcMessage();
            message.Header = await IpcHeader.ParseAsync(stream, cancellationToken).ConfigureAwait(false);
            message.Payload = await stream.ReadBytesAsync(message.Header.Size - IpcHeader.HeaderSizeInBytes, cancellationToken).ConfigureAwait(false);
            return message;
        }
    }
}
