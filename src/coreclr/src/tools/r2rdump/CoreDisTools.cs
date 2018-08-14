// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace R2RDump
{
    public class CoreDisTools
    {
        private const string _dll = "coredistools.dll";

        public enum TargetArch
        {
            Target_Host, // Target is the same as host architecture
            Target_X86,
            Target_X64,
            Target_Thumb,
            Target_Arm64
        };

        [DllImport(_dll)]
        public static extern IntPtr InitBufferedDisasm(TargetArch Target);

        [DllImport(_dll)]
        public static extern void DumpCodeBlock(IntPtr Disasm, ulong Address, IntPtr Bytes, int Size);

        [DllImport(_dll)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int DumpInstruction(IntPtr Disasm, ulong Address, IntPtr Bytes, int Size);

        [DllImport(_dll)]
        public static extern IntPtr GetOutputBuffer();

        [DllImport(_dll)]
        public static extern void ClearOutputBuffer();

        [DllImport(_dll)]
        public static extern void FinishDisasm(IntPtr Disasm);

        public unsafe static int GetInstruction(IntPtr Disasm, RuntimeFunction rtf, int imageOffset, int rtfOffset, byte[] image, out string instr)
        {
            int instrSize = 1;
            fixed (byte* p = image)
            {
                IntPtr ptr = (IntPtr)(p + imageOffset + rtfOffset);
                instrSize = DumpInstruction(Disasm, (ulong)(rtf.StartAddress + rtfOffset), ptr, rtf.Size);
            }
            IntPtr pBuffer = GetOutputBuffer();
            instr = Marshal.PtrToStringAnsi(pBuffer);
            return instrSize;
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

    public class Disassembler : IDisposable
    {
        private readonly IntPtr _disasm;

        private readonly byte[] _image;

        private readonly Machine _machine;

        public Disassembler(byte[] image, Machine machine)
        {
            _disasm = CoreDisTools.GetDisasm(machine);
            _image = image;
            _machine = machine;
        }

        public void Dispose()
        {
            if (_disasm != IntPtr.Zero)
            {
                CoreDisTools.FinishDisasm(_disasm);
            }
        }

        public int GetInstruction(RuntimeFunction rtf, int imageOffset, int rtfOffset, out string instruction)
        {
            int instrSize = CoreDisTools.GetInstruction(_disasm, rtf, imageOffset, rtfOffset, _image, out instruction);

            switch (_machine)
            {
                case Machine.Amd64:
                case Machine.IA64:
                    ProbeX64Quirks(rtf, imageOffset, rtfOffset, instrSize, ref instruction);
                    break;

                case Machine.I386:
                    break;

                case Machine.ArmThumb2:
                case Machine.Thumb:
                    break;

                case Machine.Arm64:
                    break;

                default:
                    throw new NotImplementedException();
            }

            return instrSize;
        }

        const string RelIPTag = "[rip ";

        private void ProbeX64Quirks(RuntimeFunction rtf, int imageOffset, int rtfOffset, int instrSize, ref string instruction)
        {
            int relip = instruction.IndexOf(RelIPTag);
            if (relip >= 0 && instruction.Length >= relip + RelIPTag.Length + 3)
            {
                int start = relip;
                relip += RelIPTag.Length;
                char sign = instruction[relip];
                if (sign == '+' || sign == '-' &&
                    instruction[relip + 1] == ' ' &&
                    Char.IsDigit(instruction[relip + 2]))
                {
                    relip += 2;
                    int offset = 0;
                    do
                    {
                        offset = 10 * offset + (int)(instruction[relip] - '0');
                    }
                    while (++relip < instruction.Length && Char.IsDigit(instruction[relip]));
                    if (relip < instruction.Length && instruction[relip] == ']')
                    {
                        relip++;
                        if (sign == '-')
                        {
                            offset = -offset;
                        }
                        int target = rtf.StartAddress + rtfOffset + instrSize + offset;
                        instruction = instruction.Substring(0, start) + $@"[0x{target:x4}]" + instruction.Substring(relip);
                    }
                }
            }
        }
    }
}
