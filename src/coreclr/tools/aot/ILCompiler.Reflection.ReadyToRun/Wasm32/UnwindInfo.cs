// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.Wasm32
{
    /// Parses the WASM unwind info blob used by ReadyToRun.
    /// The blob format is two unsigned LEB128 values in order:
    /// <c>BytesUnwind</c> (frame size), followed by <c>VirtualIPCount</c> (virtual IP span / 2).
    /// <see cref="BaseUnwindInfo.Size"/> is the number of bytes consumed while decoding those fields.
    public class UnwindInfo : BaseUnwindInfo
    {
        public uint BytesUnwind { get; set; }
        public uint VirtualIPCount { get; set; }
        public uint FunctionLength => VirtualIPCount; // The length is VirtualIP is considered the FunctionLength. (Duplicate the property so that other common code in R2RDump makes more sense)

        public UnwindInfo() { }

        public UnwindInfo(NativeReader imageReader, int offset)
        {
            uint startOffset = (uint)offset;
            BytesUnwind = imageReader.ReadULEB128(ref offset);
            VirtualIPCount = imageReader.ReadULEB128(ref offset) * 2;
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
