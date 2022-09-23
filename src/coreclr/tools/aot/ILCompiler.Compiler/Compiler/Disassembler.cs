// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Disassembler based on CoreDisTools. Only available in Debug builds.
    /// </summary>
    public class Disassembler
    {
        public static string Disassemble(TargetArchitecture arch, byte[] bytes, Relocation[] relocs)
        {
            var sb = new StringBuilder();

            // The coredistools library is not available in release builds because
            // we don't want to ship yet another huge LLVM-based DLL.
#if DEBUG
            SortedList<int, Relocation> sortedRelocs = new SortedList<int, Relocation>(relocs.Length);
            foreach (Relocation reloc in relocs)
                sortedRelocs.Add(reloc.Offset, reloc);

            int relocIndex = 0;

            using (var disasm = new CoreDisassembler(arch))
            {
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int size = disasm.Disassemble(bytes, offset, out string dis);
                    if (size == 0)
                        break;
                    offset += size;

                    // Drop the annoying `\n`
                    dis = dis.Substring(0, dis.Length - 1);

                    sb.Append(dis);

                    if (relocIndex < sortedRelocs.Count)
                    {
                        Relocation currentReloc = sortedRelocs.Values[relocIndex];
                        if (currentReloc.Offset < offset)
                        {
                            sb.Append(" // ");
                            sb.Append(currentReloc.Target.ToString());
                            relocIndex++;
                        }
                    }

                    sb.AppendLine();
                }
            }
#else
            sb.AppendLine("// CoreDisTools not available in release builds");
#endif
            return sb.ToString();
        }

        private sealed class CoreDisassembler : IDisposable
        {
            private IntPtr _handle;

            private const string Library = "coredistools";

            private enum TargetArch
            {
                Target_X86 = 1,
                Target_X64,
                Target_Thumb,
                Target_Arm64
            };

            [DllImport(Library)]
            private static extern IntPtr InitBufferedDisasm(TargetArch Target);

            public CoreDisassembler(TargetArchitecture arch)
            {
                _handle = InitBufferedDisasm(arch switch
                {
                    TargetArchitecture.X86 => TargetArch.Target_X86,
                    TargetArchitecture.X64 => TargetArch.Target_X64,
                    TargetArchitecture.ARM => TargetArch.Target_Thumb,
                    TargetArchitecture.ARM64 => TargetArch.Target_Arm64,
                    _ => throw new NotSupportedException()
                });

                if (_handle == IntPtr.Zero)
                    throw new OutOfMemoryException();
            }

            [DllImport(Library)]
            private static extern int DumpInstruction(IntPtr handle, ulong address, IntPtr bytes, int size);

            public unsafe int Disassemble(byte[] bytes, int offset, out string instruction)
            {
                int size;
                fixed (byte* pByte = &bytes[offset])
                {
                    size = DumpInstruction(_handle, (ulong)offset, (IntPtr)pByte, bytes.Length - offset);
                }

                instruction = Marshal.PtrToStringUTF8(GetOutputBuffer());
                ClearOutputBuffer();
                return size;
            }

            [DllImport(Library)]
            private static extern IntPtr GetOutputBuffer();

            [DllImport(Library)]
            private static extern void ClearOutputBuffer();

            [DllImport(Library)]
            private static extern void FinishDisasm(IntPtr handle);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                FinishDisasm(_handle);
                _handle = IntPtr.Zero;
            }

            ~CoreDisassembler()
            {
                Dispose(false);
            }
        }
    }
}
