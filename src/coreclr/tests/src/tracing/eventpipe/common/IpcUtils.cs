// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

// modified version of same code in dotnet/diagnostics for testing
namespace Tracing.Tests.Common
{
    public class IpcHeader
    {
        IpcHeader() { }

        public IpcHeader(byte commandSet, byte commandId)
        {
            CommandSet = (byte)commandSet;
            CommandId = commandId;
        }

        // the number of bytes for the DiagnosticsIpc::IpcHeader type in native code
        public static readonly UInt16 HeaderSizeInBytes = 20;
        private static readonly UInt16 MagicSizeInBytes = 14;

        public byte[] Magic = DotnetIpcV1; // byte[14] in native code
        public UInt16 Size = HeaderSizeInBytes;
        public byte CommandSet;
        public byte CommandId;
        public UInt16 Reserved = 0x0000;


        // Helper expression to quickly get V1 magic string for comparison
        // should be 14 bytes long
        public static byte[] DotnetIpcV1 => Encoding.ASCII.GetBytes("DOTNET_IPC_V1" + '\0');

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                Debug.Assert(Magic.Length == MagicSizeInBytes);
                writer.Write(Size);
                writer.Write(CommandSet);
                writer.Write(CommandId);
                writer.Write((UInt16)0x0000);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static IpcHeader TryParse(BinaryReader reader)
        {
            IpcHeader header = new IpcHeader
            {
                Magic = reader.ReadBytes(14),
                Size = reader.ReadUInt16(),
                CommandSet = reader.ReadByte(),
                CommandId = reader.ReadByte(),
                Reserved = reader.ReadUInt16()
            };

            return header;
        }

        override public string ToString()
        {
            return $"{{ Magic={Magic}; Size={Size}; CommandSet={CommandSet}; CommandId={CommandId}; Reserved={Reserved} }}";
        }
    }

    public class IpcMessage
    {
        public IpcMessage()
        { }

        public IpcMessage(IpcHeader header, byte[] payload)
        {
            Payload = payload;
            Header = header;
        }

        public IpcMessage(byte commandSet, byte commandId, byte[] payload = null)
        : this(new IpcHeader(commandSet, commandId), payload)
        {
        }

        public byte[] Payload { get; private set; } = null;
        public IpcHeader Header { get; private set; } = default;

        public byte[] Serialize()
        { 
            byte[] serializedData = null;
            // Verify things will fit in the size capacity
            Header.Size = checked((UInt16)(IpcHeader.HeaderSizeInBytes + (Payload?.Length ?? 0))); ;
            byte[] headerBytes = Header.Serialize();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(headerBytes);
                if (Payload != null)
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
                message.Header = IpcHeader.TryParse(reader);
                message.Payload = reader.ReadBytes(message.Header.Size - IpcHeader.HeaderSizeInBytes);
                return message;
            }
        }

        override public string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{{ Header={Header.ToString()}; ");
            if (Payload != null)
            {
                sb.Append("Payload=[ ");
                foreach (byte b in Payload)
                    sb.Append($"'{b:X4}' ");
                sb.Append(" ]");
            }
            sb.Append("}");

            return sb.ToString();
        }
    }

    public class IpcClient
    {
        public static IpcMessage SendMessage(Stream stream, IpcMessage message)
        {
            using (stream)
            {
                Write(stream, message);
                return Read(stream);
            }
        }

        public static Stream SendMessage(Stream stream, IpcMessage message, out IpcMessage response)
        {
            Write(stream, message);
            response = Read(stream);
            return stream;
        }

        private static void Write(Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        private static void Write(Stream stream, IpcMessage message)
        {
            Write(stream, message.Serialize());
        }

        private static IpcMessage Read(Stream stream)
        {
            return IpcMessage.Parse(stream);
        }
    }
}
