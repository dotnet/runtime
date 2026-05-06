// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal class IpcHeader
    {
        IpcHeader() { }

        public IpcHeader(DiagnosticsServerCommandSet commandSet, byte commandId)
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

        public static IpcHeader Parse(BinaryReader reader)
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

        public static async Task<IpcHeader> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = await stream.ReadBytesAsync(HeaderSizeInBytes, cancellationToken).ConfigureAwait(false);
            using MemoryStream bufferStream = new MemoryStream(buffer);
            using BinaryReader bufferReader = new BinaryReader(bufferStream);
            IpcHeader header = Parse(bufferReader);
            Debug.Assert(bufferStream.Position == bufferStream.Length);
            return header;
        }

        override public string ToString()
        {
            return $"{{ Magic={Magic}; Size={Size}; CommandSet={CommandSet}; CommandId={CommandId}; Reserved={Reserved} }}";
        }
    }
}
