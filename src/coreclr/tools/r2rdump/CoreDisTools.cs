// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using ILCompiler.Reflection.ReadyToRun;

namespace R2RDump
{
    internal sealed class CoreDisTools
    {
        private const string _dll = "coredistools";

        public enum TargetArch
        {
            Target_Host, // Target is the same as host architecture
            Target_X86,
            Target_X64,
            Target_Thumb,
            Target_Arm64,
            Target_LoongArch64
        };

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr InitBufferedDisasm(TargetArch Target);

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DumpCodeBlock(IntPtr Disasm, IntPtr Address, IntPtr Bytes, IntPtr Size);

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DumpInstruction(IntPtr Disasm, IntPtr Address, IntPtr Bytes, IntPtr Size);

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetOutputBuffer();

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearOutputBuffer();

        [DllImport(_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FinishDisasm(IntPtr Disasm);

        public unsafe static int GetInstruction(IntPtr Disasm, RuntimeFunction rtf, int imageOffset, int rtfOffset, byte[] image, out string instr)
        {
            int instrSize;
            fixed (byte* p = image)
            {
                IntPtr ptr = (IntPtr)(p + imageOffset + rtfOffset);
                instrSize = DumpInstruction(Disasm, new IntPtr(rtf.StartAddress + rtfOffset), ptr, new IntPtr(rtf.Size));
            }
            IntPtr pBuffer = GetOutputBuffer();
            instr = Marshal.PtrToStringUTF8(pBuffer);
            ClearOutputBuffer();
            return instrSize;
        }

        public static IntPtr GetDisasm(Machine machine)
        {
            TargetArch target;
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
                case Machine.LoongArch64:
                    target = TargetArch.Target_LoongArch64;
                    break;
                default:
                    Program.WriteWarning($"{machine} not supported on CoreDisTools");
                    return IntPtr.Zero;
            }
            return InitBufferedDisasm(target);
        }
    }

    /// <summary>
    /// Helper class for converting machine instructions to textual representation.
    /// </summary>
    internal sealed class Disassembler : IDisposable
    {
        /// <summary>
        /// Indentation of instruction mnemonics in naked mode with no offsets.
        /// </summary>
        private const int NakedNoOffsetIndentation = 4;

        /// <summary>
        /// Indentation of instruction mnemonics in naked mode with offsets.
        /// </summary>
        private const int NakedWithOffsetIndentation = 11;

        /// <summary>
        /// Indentation of instruction comments.
        /// </summary>
        private const int CommentIndentation = 62;

        /// <summary>
        /// R2R reader is used to access architecture info, the PE image data and symbol table.
        /// </summary>
        private readonly ReadyToRunReader _reader;

        /// <summary>
        /// Dump model
        /// </summary>
        private readonly DumpModel _model;

        /// <summary>
        /// COM interface to the native disassembler in the CoreDisTools.dll library.
        /// </summary>
        private readonly IntPtr _disasm;

        /// <summary>
        /// ARM64: The image offset of the ADD instruction in an ADRP+ADD pair.
        /// </summary>
        private int _addInstructionOffset;

        /// <summary>
        /// ARM64: The target of the ADD instruction in an ADRP+ADD pair.
        /// </summary>
        private int _addInstructionTarget;

        /// <summary>
        /// Indentation of instruction mnemonics.
        /// </summary>
        public int MnemonicIndentation { get; private set; }

        /// <summary>
        /// Indentation of instruction mnemonics.
        /// </summary>
        public int OperandsIndentation { get; private set; }

        /// <summary>
        /// Store the R2R reader and construct the disassembler for the appropriate architecture.
        /// </summary>
        /// <param name="reader"></param>
        public Disassembler(ReadyToRunReader reader, DumpModel model)
        {
            _reader = reader;
            _model = model;
            _disasm = CoreDisTools.GetDisasm(_reader.Machine);
            SetIndentations();
        }

        /// <summary>
        /// Shut down the native disassembler interface.
        /// </summary>
        public void Dispose()
        {
            if (_disasm != IntPtr.Zero)
            {
                CoreDisTools.FinishDisasm(_disasm);
            }
        }

        /// <summary>
        /// Set indentations for mnemonics and operands.
        /// </summary>
        private void SetIndentations()
        {
            if (_model.Naked)
            {
                MnemonicIndentation = _model.HideOffsets ? NakedNoOffsetIndentation : NakedWithOffsetIndentation;
            }
            else
            {
                // The length of the byte dump starting with the first hexadecimal digit and ending with the final space
                int byteDumpLength = _reader.Machine switch
                {
                    // Most instructions are no longer than 7 bytes. CorDisasm::dumpInstruction always pads byte dumps
                    // to 7 * 3 characters; see https://github.com/dotnet/llilc/blob/master/lib/CoreDisTools/coredistools.cpp.
                    Machine.I386 => 7 * 3,
                    Machine.Amd64 => 7 * 3,

                    // Instructions are either 2 or 4 bytes long
                    Machine.ArmThumb2 => 4 * 3,

                    // Instructions are dumped as 4-byte hexadecimal integers
                    Machine.Arm64 => 4 * 2 + 1,

                    // Instructions are dumped as 4-byte hexadecimal integers
                    Machine.LoongArch64 => 4 * 2 + 1,

                    _ => throw new NotImplementedException()
                };

                MnemonicIndentation = NakedWithOffsetIndentation + byteDumpLength;
            }

            // This leaves 7 characters for the mnemonic
            OperandsIndentation = MnemonicIndentation + 8;
        }

        /// <summary>
        /// Append spaces to the string builder to achieve at least the given indentation.
        /// </summary>
        private static void EnsureIndentation(StringBuilder builder, int lineStartIndex, int desiredIndentation)
        {
            int currentIndentation = builder.Length - lineStartIndex;
            int spacesToAppend = Math.Max(desiredIndentation - currentIndentation, 1);
            builder.Append(' ', spacesToAppend);
        }

        /// <summary>
        /// Parse and dump a single instruction and return its size in bytes.
        /// </summary>
        /// <param name="rtf">Runtime function to parse</param>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        /// <param name="rtfOffset">Instruction offset within the runtime function</param>
        /// <param name="instruction">Output text representation of the instruction</param>
        /// <returns>Instruction size in bytes - i.o.w. the next instruction starts at rtfOffset + (the return value)</returns>
        public int GetInstruction(RuntimeFunction rtf, int imageOffset, int rtfOffset, out string instruction)
        {
            if (_disasm == IntPtr.Zero)
            {
                instruction = "";
                return rtf.Size;
            }

            int instrSize = CoreDisTools.GetInstruction(_disasm, rtf, imageOffset, rtfOffset, _reader.Image, out instruction);

            // CoreDisTools dumps instructions in the following format:
            //
            //      address: bytes [padding] \t mnemonic [\t operands] \n
            //
            // However, due to an LLVM issue regarding instruction prefixes (https://bugs.llvm.org/show_bug.cgi?id=7709),
            // multiple lines may be returned for a single x86/x64 instruction.

            var builder = new StringBuilder();
            int lineNum = 0;
            // The start index of the last line in builder
            int lineStartIndex = 0;

            // Remove this foreach wrapper and line* variables after the aforementioned LLVM issue is fixed
            foreach (string line in instruction.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colonIndex = line.IndexOf(':');
                int tab1Index = line.IndexOf('\t');

                if ((0 < colonIndex) && (colonIndex < tab1Index))
                {
                    // First handle the address and the byte dump
                    if (_model.Naked)
                    {
                        if (!_model.HideOffsets)
                        {
                            // All lines but the last one must represent single-byte prefixes, so add lineNum to the offset
                            builder.Append($"{rtf.CodeOffset + rtfOffset + lineNum,8:x4}:");
                        }
                    }
                    else
                    {
                        if ((_reader.Machine == Machine.Arm64) || (_reader.Machine == Machine.LoongArch64))
                        {
                            // Replace " hh hh hh hh " byte dump with " hhhhhhhh ".
                            // CoreDisTools should be fixed to dump bytes this way for ARM64.
                            uint instructionBytes = BitConverter.ToUInt32(_reader.Image, imageOffset + rtfOffset);
                            builder.Append(line, 0, colonIndex + 1);
                            builder.Append(' ');
                            builder.Append(instructionBytes.ToString("x8"));
                        }
                        else
                        {
                            // Copy the offset and the byte dump
                            int byteDumpEndIndex = tab1Index;
                            do
                            {
                                byteDumpEndIndex--;
                            }
                            while (line[byteDumpEndIndex] == ' ');
                            builder.Append(line, 0, byteDumpEndIndex + 1);
                        }
                        builder.Append(' ');
                    }

                    // Now handle the mnemonic and operands. Ensure proper indentation for the mnemonic.
                    EnsureIndentation(builder, lineStartIndex, MnemonicIndentation);

                    int tab2Index = line.IndexOf('\t', tab1Index + 1);
                    if (tab2Index >= 0)
                    {
                        // Copy everything between the first and the second tabs
                        builder.Append(line, tab1Index + 1, tab2Index - tab1Index - 1);
                        // Ensure proper indentation for the operands
                        EnsureIndentation(builder, lineStartIndex, OperandsIndentation);
                        int afterTab2Index = tab2Index + 1;

                        // Work around an LLVM issue causing an extra space to be output before operands;
                        // see https://reviews.llvm.org/D35946.
                        if ((afterTab2Index < line.Length) &&
                            ((line[afterTab2Index] == ' ') || (line[afterTab2Index] == '\t')))
                        {
                            afterTab2Index++;
                        }

                        // Copy everything after the second tab
                        int savedLength = builder.Length;
                        builder.Append(line, afterTab2Index, line.Length - afterTab2Index);
                        // There should be no extra tabs. Should we encounter them, replace them with a single space.
                        if (line.IndexOf('\t', afterTab2Index) >= 0)
                        {
                            builder.Replace('\t', ' ', savedLength, builder.Length - savedLength);
                        }
                    }
                    else
                    {
                        // Copy everything after the first tab
                        builder.Append(line, tab1Index + 1, line.Length - tab1Index - 1);
                    }
                }
                else
                {
                    // Should not happen. Just replace tabs with spaces.
                    builder.Append(line.Replace('\t', ' '));
                }

                string translatedLine = builder.ToString(lineStartIndex, builder.Length - lineStartIndex);
                string fixedTranslatedLine = translatedLine;

                switch (_reader.Machine)
                {
                    case Machine.Amd64:
                        ProbeX64Quirks(rtf, imageOffset, rtfOffset, instrSize, ref fixedTranslatedLine);
                        break;

                    case Machine.I386:
                        ProbeX86Quirks(rtf, imageOffset, rtfOffset, instrSize, ref fixedTranslatedLine);
                        break;

                    case Machine.Arm64:
                        ProbeArm64Quirks(rtf, imageOffset, rtfOffset, ref fixedTranslatedLine);
                        break;

                    case Machine.LoongArch64:
                        //TODO-LoongArch64: maybe should add ProbeLoongArch64Quirks. At least it's unused now.
                        break;

                    case Machine.ArmThumb2:
                        break;

                    default:
                        break;
                }

                // If the translated line has been changed, replace it in the builder
                if (!object.ReferenceEquals(fixedTranslatedLine, translatedLine))
                {
                    builder.Length = lineStartIndex;
                    builder.Append(fixedTranslatedLine);
                }

                builder.Append(Environment.NewLine);
                lineNum++;
                lineStartIndex = builder.Length;
            }

            instruction = builder.ToString();
            return instrSize;
        }

        private bool TryGetImportCellName(int target, out string targetName)
        {
            targetName = null;
            _reader.ImportSignatures.TryGetValue(target, out ReadyToRunSignature targetSignature);
            if (targetSignature != null)
            {
                targetName = targetSignature.ToString(_model.SignatureFormattingOptions);
                return true;
            }
            return false;
        }

        const string RelIPTag = "[rip ";

        /// <summary>
        /// Translate RIP-relative offsets to RVA's and convert cell addresses to symbol names
        /// </summary>
        /// <param name="rtf">Runtime function</param>
        /// <param name="imageOffset">Offset within the image byte array</param>
        /// <param name="rtfOffset">Offset within the runtime function</param>
        /// <param name="instrSize">Instruction size</param>
        /// <param name="instruction">Textual representation of the instruction</param>
        private void ProbeX64Quirks(RuntimeFunction rtf, int imageOffset, int rtfOffset, int instrSize, ref string instruction)
        {
            int leftBracket;
            int rightBracketPlusOne;
            int displacement;
            if (TryParseRipRelative(instruction, out leftBracket, out rightBracketPlusOne, out displacement))
            {
                int target = rtf.StartAddress + rtfOffset + instrSize + displacement;
                StringBuilder translated = new StringBuilder();
                translated.Append(instruction, 0, leftBracket);

                TryGetImportCellName(target, out string targetName);

                if (_model.Naked)
                {
                    if (targetName != null)
                    {
                        translated.Append($"[{targetName}]");
                    }
                    else
                    {
                        translated.Append($"[0x{target:x4}]");
                    }
                    translated.Append(instruction, rightBracketPlusOne, instruction.Length - rightBracketPlusOne);
                }
                else
                {
                    translated.Append($"[0x{target:x4}]");
                    translated.Append(instruction, rightBracketPlusOne, instruction.Length - rightBracketPlusOne);
                    if (targetName != null)
                    {
                        AppendComment(translated, targetName);
                    }
                }

                instruction = translated.ToString();
            }
            else
            {
                ProbeCommonIntelQuirks(rtf, imageOffset, rtfOffset, instrSize, ref instruction);
            }
        }

        /// <summary>
        /// X86 disassembler has a bug in decoding absolute indirections, mistaking them for RIP-relative indirections
        /// </summary>
        /// <param name="rtf">Runtime function</param>
        /// <param name="imageOffset">Offset within the image byte array</param>
        /// <param name="rtfOffset">Offset within the runtime function</param>
        /// <param name="instrSize">Instruction size</param>
        /// <param name="instruction">Textual representation of the instruction</param>
        private void ProbeX86Quirks(RuntimeFunction rtf, int imageOffset, int rtfOffset, int instrSize, ref string instruction)
        {
            int leftBracket;
            int rightBracketPlusOne;
            int absoluteAddress;
            if (TryParseRipRelative(instruction, out leftBracket, out rightBracketPlusOne, out absoluteAddress) ||
                TryParseAbsoluteAddress(instruction, out leftBracket, out rightBracketPlusOne, out absoluteAddress))
            {
                int target = absoluteAddress - (int)_reader.ImageBase;

                StringBuilder translated = new StringBuilder();
                translated.Append(instruction, 0, leftBracket);

                TryGetImportCellName(target, out string targetName);

                if (_model.Naked)
                {
                    if (targetName != null)
                    {
                        translated.Append($"[{targetName}]");
                    }
                    else
                    {
                        translated.Append($"[0x{target:x4}]");
                    }
                    translated.Append(instruction, rightBracketPlusOne, instruction.Length - rightBracketPlusOne);
                }
                else
                {
                    translated.Append($"[0x{target:x4}]");
                    translated.Append(instruction, rightBracketPlusOne, instruction.Length - rightBracketPlusOne);
                    if (targetName != null)
                    {
                        AppendComment(translated, targetName);
                    }
                }

                instruction = translated.ToString();
            }
            else
            {
                ProbeCommonIntelQuirks(rtf, imageOffset, rtfOffset, instrSize, ref instruction);
            }
        }

        /// <summary>
        /// Probe quirks that have the same behavior for X86 and X64.
        /// </summary>
        /// <param name="rtf">Runtime function</param>
        /// <param name="imageOffset">Offset within the image byte array</param>
        /// <param name="rtfOffset">Offset within the runtime function</param>
        /// <param name="instrSize">Instruction size</param>
        /// <param name="instruction">Textual representation of the instruction</param>
        private void ProbeCommonIntelQuirks(RuntimeFunction rtf, int imageOffset, int rtfOffset, int instrSize, ref string instruction)
        {
            int instructionRVA = rtf.StartAddress + rtfOffset;
            int nextInstructionRVA = instructionRVA + instrSize;
            if (instrSize == 2 && IsIntelJumpInstructionWithByteOffset(imageOffset + rtfOffset))
            {
                sbyte offset = (sbyte)_reader.Image[imageOffset + rtfOffset + 1];
                ReplaceRelativeOffset(ref instruction, nextInstructionRVA + offset, rtf);
            }
            else if (instrSize == 5 && IsIntel1ByteJumpInstructionWithIntOffset(imageOffset + rtfOffset))
            {
                int offset = BitConverter.ToInt32(_reader.Image, imageOffset + rtfOffset + 1);
                ReplaceRelativeOffset(ref instruction, nextInstructionRVA + offset, rtf);
            }
            else if (instrSize == 5 && IsIntelCallInstructionWithIntOffset(imageOffset + rtfOffset))
            {
                int offset = BitConverter.ToInt32(_reader.Image, imageOffset + rtfOffset + 1);
                int targetRVA = nextInstructionRVA + offset;
                int targetImageOffset = _reader.GetOffset(targetRVA);
                bool pointsOutsideRuntimeFunction = (targetRVA < rtf.StartAddress || targetRVA >= rtf.StartAddress + rtf.Size);
                if (pointsOutsideRuntimeFunction && IsIntel2ByteIndirectJumpPCRelativeInstruction(targetImageOffset, out int instructionRelativeOffset))
                {
                    int thunkTargetRVA = targetRVA + instructionRelativeOffset;
                    bool haveImportCell = TryGetImportCellName(thunkTargetRVA, out string importCellName);

                    if (_model.Naked && haveImportCell)
                    {
                        ReplaceRelativeOffset(ref instruction, $@"qword ptr [{importCellName}]", rtf);
                    }
                    else
                    {
                        ReplaceRelativeOffset(ref instruction, targetRVA, rtf);
                        if (haveImportCell)
                        {
                            StringBuilder builder = new StringBuilder(instruction, capacity: 256);
                            AppendComment(builder, @$"JMP [0x{thunkTargetRVA:x4}]: {importCellName}");
                            instruction = builder.ToString();
                        }
                    }
                }
                else if (pointsOutsideRuntimeFunction && IsAnotherRuntimeFunctionWithinMethod(targetRVA, rtf, out int runtimeFunctionIndex))
                {
                    string runtimeFunctionName = string.Format("RUNTIME_FUNCTION[{0}]", runtimeFunctionIndex);

                    if (_model.Naked)
                    {
                        ReplaceRelativeOffset(ref instruction, runtimeFunctionName, rtf);
                    }
                    else
                    {
                        ReplaceRelativeOffset(ref instruction, targetRVA, rtf);
                        StringBuilder builder = new StringBuilder(instruction,capacity: 256);
                        AppendComment(builder, runtimeFunctionName);
                        instruction = builder.ToString();
                    }
                }
                else
                {
                    ReplaceRelativeOffset(ref instruction, targetRVA, rtf);
                }
            }
            else if (instrSize == 6 && IsIntel2ByteJumpInstructionWithIntOffset(imageOffset + rtfOffset))
            {
                int offset = BitConverter.ToInt32(_reader.Image, imageOffset + rtfOffset + 2);
                ReplaceRelativeOffset(ref instruction, nextInstructionRVA + offset, rtf);
            }
        }

        /// <summary>
        /// Try to parse the [absoluteAddress] section in a disassembled instruction string.
        /// </summary>
        /// <param name="instruction">Disassembled instruction string</param>
        /// <param name="leftBracket">Index of the left bracket in the instruction</param>
        /// <param name="rightBracketPlusOne">Index of the right bracket in the instruction plus one</param>
        /// <param name="displacement">Value of the absolute address</param>
        /// <returns></returns>
        private bool TryParseAbsoluteAddress(string instruction, out int leftBracket, out int rightBracketPlusOne, out int absoluteAddress)
        {
            int start = instruction.IndexOf('[', StringComparison.Ordinal);
            int current = start + 1;
            absoluteAddress = 0;
            while (current < instruction.Length && IsDigit(instruction[current]))
            {
                absoluteAddress = 10 * absoluteAddress + (int)(instruction[current] - '0');
                current++;
            }

            if (current < instruction.Length && instruction[current] == ']')
            {
                leftBracket = start;
                rightBracketPlusOne = current + 1;
                return true;
            }

            leftBracket = 0;
            rightBracketPlusOne = 0;
            absoluteAddress = 0;
            return false;
        }

        /// <summary>
        /// Try to parse the [rip +- displacement] section in a disassembled instruction string.
        /// </summary>
        /// <param name="instruction">Disassembled instruction string</param>
        /// <param name="leftBracket">Index of the left bracket in the instruction</param>
        /// <param name="rightBracketPlusOne">Index of the right bracket in the instruction plus one</param>
        /// <param name="displacement">Value of the IP-relative delta</param>
        /// <returns></returns>
        private bool TryParseRipRelative(string instruction, out int leftBracket, out int rightBracketPlusOne, out int displacement)
        {
            int relip = instruction.IndexOf(RelIPTag, StringComparison.Ordinal);
            if (relip >= 0 && instruction.Length >= relip + RelIPTag.Length + 3)
            {
                int start = relip;
                relip += RelIPTag.Length;
                char sign = instruction[relip];
                if ((sign == '+' || sign == '-') &&
                    instruction[relip + 1] == ' ' &&
                    IsDigit(instruction[relip + 2]))
                {
                    relip += 2;
                    int offset = 0;
                    do
                    {
                        offset = 10 * offset + (int)(instruction[relip] - '0');
                    }
                    while (++relip < instruction.Length && IsDigit(instruction[relip]));
                    if (relip < instruction.Length && instruction[relip] == ']')
                    {
                        relip++;
                        if (sign == '-')
                        {
                            offset = -offset;
                        }
                        leftBracket = start;
                        rightBracketPlusOne = relip;
                        displacement = offset;
                        return true;
                    }
                }
            }

            leftBracket = 0;
            rightBracketPlusOne = 0;
            displacement = 0;
            return false;
        }

        /// <summary>
        /// Append a given comment to the string builder.
        /// </summary>
        /// <param name="builder">String builder to append comment to</param>
        /// <param name="comment">Comment to append</param>
        private static void AppendComment(StringBuilder builder, string comment)
        {
            EnsureIndentation(builder, 0, CommentIndentation);
            builder.Append("// ").Append(comment);
        }

        /// <summary>
        /// Replace relative offset in the disassembled instruction with the true target RVA.
        /// </summary>
        /// <param name="instruction">Disassembled instruction to modify</param>
        /// <param name="target">Target string to replace offset with</param>
        /// <param name="rtf">Runtime function being disassembled</param>
        private void ReplaceRelativeOffset(ref string instruction, int target, RuntimeFunction rtf)
        {
            int outputOffset = target;
            if (_model.Naked)
            {
                outputOffset = outputOffset - rtf.StartAddress + rtf.CodeOffset;
            }
            ReplaceRelativeOffset(ref instruction, string.Format("0x{0:x4}", outputOffset), rtf);
        }

        /// <summary>
        /// Replace relative offset in the disassembled instruction with an arbitrary string.
        /// </summary>
        /// <param name="instruction">Disassembled instruction to modify</param>
        /// <param name="replacementString">String to replace offset with</param>
        /// <param name="rtf">Runtime function being disassembled</param>
        private static void ReplaceRelativeOffset(ref string instruction, string replacementString, RuntimeFunction rtf)
        {
            int number = instruction.Length;
            while (number > 0)
            {
                char c = instruction[number - 1];
                if (c >= ' ' && !IsDigit(c) && c != '-')
                {
                    break;
                }
                number--;
            }

            StringBuilder translated = new StringBuilder();
            translated.Append(instruction, 0, number);
            translated.Append(replacementString);
            instruction = translated.ToString();
        }

        /// <summary>
        /// Returns true when this is one of the x86 / amd64 opcodes used for branch instructions
        /// with single-byte offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        private bool IsIntelJumpInstructionWithByteOffset(int imageOffset)
        {
            byte opCode = _reader.Image[imageOffset];
            return
                (opCode >= 0x70 && opCode <= 0x7F) // short conditional jumps
                || opCode == 0xE3 // JCXZ
                || opCode == 0xEB // JMP
                ;
        }

        /// <summary>
        /// Returns true for the call relative opcode with signed 4-byte offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        private bool IsIntelCallInstructionWithIntOffset(int imageOffset)
        {
            return _reader.Image[imageOffset] == 0xE8; // CALL rel32
        }

        /// <summary>
        /// Returns true when this is one of the x86 / amd64 near jump / call opcodes
        /// with signed 4-byte offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        private bool IsIntel1ByteJumpInstructionWithIntOffset(int imageOffset)
        {
            return _reader.Image[imageOffset] == 0xE9; // JMP rel32
        }

        /// <summary>
        /// Returns true when this is one of the x86 / amd64 conditional near jump
        /// opcodes with signed 4-byte offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        private bool IsIntel2ByteJumpInstructionWithIntOffset(int imageOffset)
        {
            byte opCode1 = _reader.Image[imageOffset];
            byte opCode2 = _reader.Image[imageOffset + 1];
            return opCode1 == 0x0F &&
                (opCode2 >= 0x80 && opCode2 <= 0x8F); // near conditional jumps
        }

        /// <summary>
        /// Returns true when this is one of the x86 / amd64 conditional near jump
        /// opcodes with signed 4-byte offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        private bool IsIntelCallAbsoluteAddress(int imageOffset)
        {
            byte opCode1 = _reader.Image[imageOffset];
            byte opCode2 = _reader.Image[imageOffset + 1];

            return opCode1 == 0xFF && opCode2 == 0x15;
        }

        /// <summary>
        /// Returns true when this is the 2-byte instruction for indirect jump
        /// with RIP-relative offset.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array</param>
        /// <returns></returns>
        private bool IsIntel2ByteIndirectJumpPCRelativeInstruction(int imageOffset, out int instructionRelativeOffset)
        {
            byte opCode1 = _reader.Image[imageOffset + 0];
            byte opCode2 = _reader.Image[imageOffset + 1];
            int offsetDelta = 6;

            if (opCode1 == 0x48 && opCode2 == 0x8B && _reader.Image[imageOffset + 2] == 0x15) // MOV RDX, [R2R module]
            {
                imageOffset += 7;
                offsetDelta += 7;
                opCode1 = _reader.Image[imageOffset + 0];
                opCode2 = _reader.Image[imageOffset + 1];
            }

            if (opCode1 == 0xFF && opCode2 == 0x25)
            {
                // JMP [RIP + rel32]
                instructionRelativeOffset = offsetDelta + BitConverter.ToInt32(_reader.Image, imageOffset + 2);
                return true;
            }

            instructionRelativeOffset = 0;
            return false;
        }

        /// <summary>
        /// Improves disassembler output for ARM64.
        /// </summary>
        /// <param name="rtf">Runtime function</param>
        /// <param name="imageOffset">Offset within the image byte array</param>
        /// <param name="rtfOffset">Offset within the runtime function</param>
        /// <param name="instruction">Textual representation of the instruction</param>
        private void ProbeArm64Quirks(RuntimeFunction rtf, int imageOffset, int rtfOffset, ref string instruction)
        {
            const int InstructionSize = 4;

            // The list of PC-relative instructions: ADR, ADRP, B.cond, B, BL, CBNZ, CBZ, TBNZ, TBZ.

            // Handle an ADR instruction
            if (IsArm64AdrInstruction(imageOffset + rtfOffset, out int adrOffset))
            {
                ReplaceRelativeOffset(ref instruction, rtf.StartAddress + rtfOffset + adrOffset, rtf);
            }
            // Handle the ADRP instruction of an ADRP+ADD pair
            else if (IsArm64AdrpInstruction(imageOffset + rtfOffset, out uint adrpRegister, out long pageOffset))
            {
                int pc = rtf.StartAddress + rtfOffset;
                long targetPage = (pc & ~0xfff) + pageOffset;

                if ((0 <= targetPage) && (targetPage <= int.MaxValue) &&
                    IsArm64AddImmediate64NoShiftInstruction(imageOffset + rtfOffset + InstructionSize, out uint addSrcRegister, out uint offset) &&
                    (addSrcRegister == adrpRegister))
                {
                    int target = (int)targetPage + (int)offset;
                    _addInstructionOffset = imageOffset + rtfOffset + 4;
                    _addInstructionTarget = target;

                    int hashPos = instruction.LastIndexOf('#');
                    var translated = new StringBuilder();
                    translated.Append(instruction, 0, hashPos);

                    TryGetImportCellName(target, out string targetName);

                    if (_model.Naked && (targetName != null))
                    {
                        translated.Append("import_hi21{").Append(targetName).Append('}');
                    }
                    else
                    {
                        translated.Append($"#0x{targetPage:x4}");
                    }

                    instruction = translated.ToString();
                }
            }
            // Handle the ADD instruction of an ADRP+ADD pair
            else if (imageOffset + rtfOffset == _addInstructionOffset)
            {
                int target = _addInstructionTarget;
                _addInstructionOffset = 0;
                _addInstructionTarget = 0;

                int hashPos = instruction.LastIndexOf('#');
                var translated = new StringBuilder();
                translated.Append(instruction, 0, hashPos);

                TryGetImportCellName(target, out string targetName);

                if (_model.Naked && (targetName != null))
                {
                    translated.Append("import_lo12{").Append(targetName).Append('}');
                }
                else
                {
                    translated.Append($"#0x{target & 0xfff:x}");
                    if (targetName != null)
                    {
                        AppendComment(translated, "import{" + targetName + "}");
                    }
                }

                instruction = translated.ToString();
            }
            // Handle a B.cond, B, CBZ, CBNZ, TBZ, TBNZ instruction
            else if (IsArm64BCondInstruction(imageOffset + rtfOffset, out int branchOffset) ||
                IsArm64BInstruction(imageOffset + rtfOffset, out branchOffset) ||
                IsArm64CbzOrCbnzInstruction(imageOffset + rtfOffset, out branchOffset) ||
                IsArm64TbzOrTbnzInstruction(imageOffset + rtfOffset, out branchOffset))
            {
                ReplaceRelativeOffset(ref instruction, rtf.StartAddress + rtfOffset + branchOffset, rtf);
            }
            // Handle a BL instruction
            else if (IsArm64BLInstruction(imageOffset + rtfOffset, out int blOffset))
            {
                int blTargetImageOffset = imageOffset + rtfOffset + blOffset;
                int blTargetRva = rtf.StartAddress + rtfOffset + blOffset;

                // Search for one of the two patterns below at the BL target:
                //      580000ac  ldr     x12, label
                //      f940018c  ldr     x12, [x12]
                //      d61f0180  br      x12
                // or
                //      580000a1  ldr     x1, label1
                //      f9400021  ldr     x1, [x1]
                //      580000ac  ldr     x12, label2
                //      f940018c  ldr     x12, [x12]
                //      d61f0180  br      x12

                if (IsArm64LdrLiteral64Instruction(blTargetImageOffset, out uint ldr1Register, out int ldr1Offset) &&
                    IsArm64LdrImmediate64ZeroOffsetInstruction(blTargetImageOffset + InstructionSize, out uint ldr2DestRegister, out uint ldr2SrcRegister))
                {
                    int ldr1ImageOffset = blTargetImageOffset;
                    if (IsArm64LdrLiteral64Instruction(ldr1ImageOffset + InstructionSize * 2, out uint ldr3Register, out int ldr3Offset) &&
                        IsArm64LdrImmediate64ZeroOffsetInstruction(ldr1ImageOffset + InstructionSize * 3, out uint ldr4DestRegister, out uint ldr4SrcRegister))
                    {
                        ldr1ImageOffset += InstructionSize * 2;
                        ldr1Register = ldr3Register;
                        ldr1Offset = ldr3Offset;
                        ldr2DestRegister = ldr4DestRegister;
                        ldr2SrcRegister = ldr4SrcRegister;
                    }

                    if (IsArm64BrInstruction(ldr1ImageOffset + InstructionSize * 2, out uint brRegister) &&
                        (ldr2SrcRegister == ldr1Register) &&
                        (brRegister == ldr2DestRegister))
                    {
                        int labelOffset = ldr1ImageOffset + ldr1Offset;
                        int target = checked((int)(BitConverter.ToUInt64(_reader.Image, labelOffset) - _reader.ImageBase));
                        TryGetImportCellName(target, out string targetName);
                        var translated = new StringBuilder();

                        if (_model.Naked && (targetName != null))
                        {
                            int hashPos = instruction.LastIndexOf('#');
                            translated.Append(instruction, 0, hashPos);
                            translated.Append("thunk{").Append(targetName).Append('}');
                        }
                        else
                        {
                            ReplaceRelativeOffset(ref instruction, rtf.StartAddress + rtfOffset + blOffset, rtf);
                            translated.Append(instruction);
                            if (targetName != null)
                            {
                                AppendComment(translated, $"br [0x{target:x4}]: {targetName}");
                            }
                        }

                        instruction = translated.ToString();
                    }
                }
                else if (IsAnotherRuntimeFunctionWithinMethod(blTargetRva, rtf, out int runtimeFunctionIndex))
                {
                    string runtimeFunctionName = string.Format("RUNTIME_FUNCTION[{0}]", runtimeFunctionIndex);
                    var translated = new StringBuilder();

                    if (_model.Naked)
                    {
                        int hashPos = instruction.LastIndexOf('#');
                        translated.Append(instruction, 0, hashPos);
                        translated.Append(runtimeFunctionName);
                    }
                    else
                    {
                        ReplaceRelativeOffset(ref instruction, blTargetRva, rtf);
                        translated.Append(instruction);
                        AppendComment(translated, runtimeFunctionName);
                    }

                    instruction = translated.ToString();
                }
                else
                {
                    Debug.Fail("Is this a new pattern that we need to handle?");
                    ReplaceRelativeOffset(ref instruction, blTargetRva, rtf);
                }
            }
        }

        /// <summary>
        /// Determine whether a given instruction is an ADR.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64AdrInstruction(int imageOffset, out int immediate)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0x9f) != 0x10)
            {
                immediate = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            // imm = SignExtend(immhi:immlo, 64)
            immediate = (((int)instruction & ~0x1f) | ((int)instruction >> 26) & 0x18) << 8 >> 11;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is an ADRP.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64AdrpInstruction(int imageOffset, out uint register, out long immediate)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0x9f) != 0x90)
            {
                register = 0;
                immediate = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            register = instruction & 0x1f;
            // imm = SignExtend(immhi:immlo:Zeros(12), 64)
            immediate = (long)unchecked((int)(((instruction ^ register) | (instruction >> 26) & 0x18) << 8)) << 1;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is an ADD immediate 64-bit with no shift.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64AddImmediate64NoShiftInstruction(int imageOffset, out uint sourceRegister, out uint immediate)
        {
            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            if ((instruction & 0xffc0_0000) != 0x9100_0000)
            {
                sourceRegister = 0;
                immediate = 0;
                return false;
            }

            sourceRegister = (instruction >> 5) & 0x1f;
            // imm = ZeroExtend(imm12, 64)
            immediate = instruction << 10 >> 20;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a B.cond.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64BCondInstruction(int imageOffset, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if (highByte != 0x54)
            {
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            if ((instruction & 0x10) != 0)
            {
                offset = 0;
                return false;
            }

            // offset = SignExtend(imm19:'00', 64)
            offset = ((int)instruction & ~0xf) << 8 >> 11;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a B.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64BInstruction(int imageOffset, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0xfc) != 0x14)
            {
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            // offset = SignExtend(imm26:'00', 64)
            offset = (int)instruction << 6 >> 4;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a CBZ or a CBNZ.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64CbzOrCbnzInstruction(int imageOffset, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0x7e) != 0x34)
            {
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            // offset = SignExtend(imm19:'00', 64)
            offset = ((int)instruction & ~0x1f) << 8 >> 11;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a TBZ or a TBNZ.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64TbzOrTbnzInstruction(int imageOffset, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0x7e) != 0x36)
            {
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            // offset = SignExtend(imm14:'00', 64)
            offset = ((int)instruction & ~0x1f) << 13 >> 16;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a BL.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64BLInstruction(int imageOffset, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if ((highByte & 0xfc) != 0x94)
            {
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            // offset = SignExtend(imm26:'00', 64)
            offset = (int)instruction << 6 >> 4;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is an LDR literal 64-bit.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64LdrLiteral64Instruction(int imageOffset, out uint register, out int offset)
        {
            byte highByte = _reader.Image[imageOffset + 3];
            if (highByte != 0x58)
            {
                register = 0;
                offset = 0;
                return false;
            }

            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            register = instruction & 0x1f;
            // offset = SignExtend(imm19:'00', 64)
            offset = (int)(instruction ^ register) << 8 >> 11;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is an LDR immediate 64-bit with the zero offset, e.g., <c>ldr x12, [x12]</c>.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64LdrImmediate64ZeroOffsetInstruction(int imageOffset, out uint destRegister, out uint sourceRegister)
        {
            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            if ((instruction & 0xffff_fc00) != 0xf940_0000)
            {
                destRegister = 0;
                sourceRegister = 0;
                return false;
            }

            destRegister = instruction & 0x1f;
            sourceRegister = (instruction >> 5) & 0x1f;
            return true;
        }

        /// <summary>
        /// Determine whether a given instruction is a BR.
        /// </summary>
        /// <param name="imageOffset">Offset within the PE image byte array.</param>
        private bool IsArm64BrInstruction(int imageOffset, out uint register)
        {
            uint instruction = BitConverter.ToUInt32(_reader.Image, imageOffset);
            if ((instruction & 0xffff_fc1f) != 0xd61f_0000)
            {
                register = 0;
                return false;
            }

            register = (instruction >> 5) & 0x1f;
            return true;
        }

        /// <summary>
        /// Check whether a given target RVA corresponds to another runtime function within the same method.
        /// </summary>
        /// <param name="rva">Target RVA to analyze</param>
        /// <param name="rtf">Runtime function being disassembled</param>
        /// <param name="runtimeFunctionIndex">Output runtime function index if found, -1 otherwise</param>
        /// <returns>true if target runtime function has been found, false otherwise</returns>
        private static bool IsAnotherRuntimeFunctionWithinMethod(int rva, RuntimeFunction rtf, out int runtimeFunctionIndex)
        {
            for (int rtfIndex = 0; rtfIndex < rtf.Method.RuntimeFunctions.Count; rtfIndex++)
            {
                if (rva == rtf.Method.RuntimeFunctions[rtfIndex].StartAddress)
                {
                    runtimeFunctionIndex = rtfIndex;
                    return true;
                }
            }

            runtimeFunctionIndex = -1;
            return false;
        }

        /// <summary>
        /// Determine whether a given character is an ASCII digit.
        /// </summary>
        private static bool IsDigit(char c) => (uint)(c - '0') <= (uint)('9' - '0');
    }
}
