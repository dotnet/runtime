// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.x86
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_INFO
    /// </summary>
    public class UnwindInfo : BaseUnwindInfo
    {
        public uint FunctionLength { get; set; }

        public UnwindInfo() { }

        public UnwindInfo(NativeReader imageReader, int offset)
            : this(imageReader, offset, hasFuncletUnwindInfo: false)
        {
        }

        /// <param name="hasFuncletUnwindInfo">
        /// True for R2R images that use funclets on x86 (major version 14 and later), where the unwind data is
        /// an UNWIND_INFO structure holding the funclet length and the GC info follows it. Before that the
        /// unwind data is the GC info itself, which begins with the encoded method size.
        /// </param>
        public UnwindInfo(NativeReader imageReader, int offset, bool hasFuncletUnwindInfo)
        {
            int startOffset = offset;
            FunctionLength = hasFuncletUnwindInfo ? imageReader.ReadUInt32(ref offset) : imageReader.DecodeUnsignedGc(ref offset);
            Size = offset - startOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"    FunctionLength: {FunctionLength}");
            return sb.ToString();
        }
    }
}
