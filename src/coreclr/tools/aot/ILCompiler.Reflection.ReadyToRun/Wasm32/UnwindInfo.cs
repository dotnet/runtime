// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.Wasm32
{
    /// Parses the WASM unwind info blob used by ReadyToRun.
    /// The blob format consumed by this type is two unsigned LEB128 values in order:
    /// <c>BytesUnwind</c>, followed by <c>VirtualIPCount</c>. <see cref="BaseUnwindInfo.Size"/>
    /// is the number of bytes consumed while decoding those fields.
    public class UnwindInfo : BaseUnwindInfo
    {
        public uint BytesUnwind { get; set; }
        public uint VirtualIPCount { get; set; }

        public UnwindInfo() { }

        
        private static uint ReadLebU32(NativeReader imageReader, ref int offset)
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = imageReader.ReadByte(ref offset);
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        public UnwindInfo(NativeReader imageReader, int offset)
        {
            uint startOffset = (uint)offset;
            BytesUnwind = ReadLebU32(imageReader, ref offset);
            VirtualIPCount = ReadLebU32(imageReader, ref offset);
            Size = offset - (int)startOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"    BytesUnwind: {BytesUnwind}");
            sb.AppendLine($"    VirtualIPCount: {VirtualIPCount}");
            return sb.ToString();
        }
    }
}
