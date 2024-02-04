// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.IO;

namespace ILCompiler.Reflection.ReadyToRun
{
    public abstract class TransitionBlock
    {
        public ReadyToRunReader _reader;

        public static TransitionBlock FromReader(ReadyToRunReader reader)
        {
            switch (reader.Machine)
            {
                case Machine.I386:
                    return X86TransitionBlock.Instance;

                case Machine.Amd64:
                    return reader.OperatingSystem == OperatingSystem.Windows ? X64WindowsTransitionBlock.Instance : X64UnixTransitionBlock.Instance;

                case Machine.Arm:
                case Machine.Thumb:
                case Machine.ArmThumb2:
                    return ArmTransitionBlock.Instance;

                case Machine.Arm64:
                    return Arm64TransitionBlock.Instance;

                case Machine.LoongArch64:
                    return LoongArch64TransitionBlock.Instance;

                case (Machine)0x5064: /* TODO: update with RiscV64 */
                    return RiscV64TransitionBlock.Instance;

                default:
                    throw new NotImplementedException();
            }
        }

        public abstract int PointerSize { get; }

        public abstract int NumArgumentRegisters { get; }

        public int SizeOfArgumentRegisters => NumArgumentRegisters * PointerSize;

        public abstract int NumCalleeSavedRegisters { get; }

        public int SizeOfCalleeSavedRegisters => NumCalleeSavedRegisters * PointerSize;

        public abstract int SizeOfTransitionBlock { get; }

        public abstract int OffsetOfArgumentRegisters { get; }

        /// <summary>
        /// The offset of the first slot in a GC ref map. Overridden on ARM64 to return the offset of the X8 register.
        /// </summary>
        public virtual int OffsetOfFirstGCRefMapSlot => OffsetOfArgumentRegisters;

        /// <summary>
        /// Recalculate pos in GC ref map to actual offset. This is the default implementation for all architectures
        /// except for X86 where it's overridden to supply a more complex algorithm.
        /// </summary>
        public virtual int OffsetFromGCRefMapPos(int pos)
        {
            return OffsetOfFirstGCRefMapSlot + pos * PointerSize;
        }

        /// <summary>
        /// The transition block should define everything pushed by callee. The code assumes in number of places that
        /// end of the transition block is caller's stack pointer.
        /// </summary>
        public int OffsetOfArgs => SizeOfTransitionBlock;

        private sealed class X86TransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new X86TransitionBlock();

            public override int PointerSize => 4;
            public override int NumArgumentRegisters => 2;
            public override int NumCalleeSavedRegisters => 4;
            // Argument registers, callee-save registers, return address
            public override int SizeOfTransitionBlock => SizeOfArgumentRegisters + SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => 0;

            public override int OffsetFromGCRefMapPos(int pos)
            {
                if (pos < NumArgumentRegisters)
                {
                    return OffsetOfArgumentRegisters + SizeOfArgumentRegisters - (pos + 1) * PointerSize;
                }
                else
                {
                    return OffsetOfArgs + (pos - NumArgumentRegisters) * PointerSize;
                }
            }
        }

        private sealed class X64WindowsTransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new X64WindowsTransitionBlock();

            public override int PointerSize => 8;
            // RCX, RDX, R8, R9
            public override int NumArgumentRegisters => 4;
            // RDI, RSI, RBX, RBP, R12, R13, R14, R15
            public override int NumCalleeSavedRegisters => 8;
            // Callee-saved registers, return address
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => SizeOfTransitionBlock;
        }

        private sealed class X64UnixTransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new X64UnixTransitionBlock();

            public override int PointerSize => 8;
            // RDI, RSI, RDX, RCX, R8, R9
            public override int NumArgumentRegisters => 6;
            // R12, R13, R14, R15, RBX, RBP
            public override int NumCalleeSavedRegisters => 6;
            // Argument registers, callee-saved registers, return address
            public override int SizeOfTransitionBlock => SizeOfArgumentRegisters + SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => 0;
        }

        private sealed class ArmTransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new ArmTransitionBlock();

            public override int PointerSize => 4;
            // R0, R1, R2, R3
            public override int NumArgumentRegisters => 4;
            // R4, R5, R6, R7, R8, R9, R10, R11, R14
            public override int NumCalleeSavedRegisters => 9;
            // Callee-saves, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + SizeOfArgumentRegisters;
            public override int OffsetOfArgumentRegisters => SizeOfCalleeSavedRegisters;
        }

        private sealed class Arm64TransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new Arm64TransitionBlock();

            public override int PointerSize => 8;
            // X0 .. X7
            public override int NumArgumentRegisters => 8;
            // X29, X30, X19, X20, X21, X22, X23, X24, X25, X26, X27, X28
            public override int NumCalleeSavedRegisters => 12;
            // Callee-saves, padding, m_x8RetBuffReg, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + 2 * PointerSize + SizeOfArgumentRegisters;
            public override int OffsetOfArgumentRegisters => SizeOfCalleeSavedRegisters + 2 * PointerSize;
            private int OffsetOfX8Register => OffsetOfArgumentRegisters - PointerSize;
            public override int OffsetOfFirstGCRefMapSlot => OffsetOfX8Register;
        }

        private sealed class LoongArch64TransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new LoongArch64TransitionBlock();

            public override int PointerSize => 8;
            // R4 .. R11
            public override int NumArgumentRegisters => 8;
            // fp=R22,ra=R1,s0-s8(R23-R31),tp=R2
            public override int NumCalleeSavedRegisters => 12;
            // Callee-saves, padding, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + SizeOfArgumentRegisters;
            public override int OffsetOfFirstGCRefMapSlot => SizeOfCalleeSavedRegisters;
            public override int OffsetOfArgumentRegisters => OffsetOfFirstGCRefMapSlot;
        }

        private sealed class RiscV64TransitionBlock : TransitionBlock
        {
            public static readonly TransitionBlock Instance = new RiscV64TransitionBlock();

            public override int PointerSize => 8;
            // a0 .. a7
            public override int NumArgumentRegisters => 8;
            // fp=x8, ra=x1, s1-s11(R9,R18-R27), tp=x3, gp=x4
            public override int NumCalleeSavedRegisters => 15;
            // Callee-saves, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + SizeOfArgumentRegisters;
            public override int OffsetOfFirstGCRefMapSlot => SizeOfCalleeSavedRegisters;
            public override int OffsetOfArgumentRegisters => OffsetOfFirstGCRefMapSlot;
        }
        
    }
}
