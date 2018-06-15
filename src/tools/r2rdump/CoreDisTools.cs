// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace R2RDump
{
    class CoreDisTools
    {
        public enum TargetArch
        {
            Target_Host, // Target is the same as host architecture
            Target_X86,
            Target_X64,
            Target_Thumb,
            Target_Arm64
        };

        [DllImport("coredistools.dll")]
        public static extern IntPtr InitBufferedDisasm(TargetArch Target);

        [DllImport("coredistools.dll")]
        public static extern void DumpCodeBlock(IntPtr Disasm, ulong Address, IntPtr Bytes, int Size);

        [DllImport("coredistools.dll")]
        public static extern IntPtr GetOutputBuffer();

        [DllImport("coredistools.dll")]
        public static extern void ClearOutputBuffer();

        [DllImport("coredistools.dll")]
        public static extern void FinishDisasm(IntPtr Disasm);

        public unsafe static string GetCodeBlock(IntPtr Disasm, int Address, int Offset, byte[] image, int Size)
        {
            fixed (byte* p = image)
            {
                IntPtr ptr = (IntPtr)(p + Offset);
                DumpCodeBlock(Disasm, (ulong)Address, ptr, Size);
            }
            IntPtr pBuffer = GetOutputBuffer();
            string buffer = Marshal.PtrToStringAnsi(pBuffer);
            ClearOutputBuffer();
            return buffer;
        }

        public static IntPtr GetDisasm(Machine machine)
        {
            TargetArch target = TargetArch.Target_Host;
            switch (machine)
            {
                case Machine.Amd64:
                    target = TargetArch.Target_X64;
                    break;
                case Machine.I386:
                    target = TargetArch.Target_X86;
                    break;
                case Machine.Arm64:
                    target = TargetArch.Target_Arm64;
                    break;
                case Machine.ArmThumb2:
                    target = TargetArch.Target_Thumb;
                    break;
            }
            return InitBufferedDisasm(target);
        }
    }
}
