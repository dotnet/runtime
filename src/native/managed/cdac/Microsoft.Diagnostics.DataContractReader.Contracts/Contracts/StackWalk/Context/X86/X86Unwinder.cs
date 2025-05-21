// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public class X86Unwinder(Target target)
{
    private readonly Target _target = target;

    private static readonly RegMask[] registerOrder =
    [
        RegMask.EBP, // last register to be pushed
        RegMask.EBX,
        RegMask.ESI,
        RegMask.EDI, // first register to be pushed
    ];

    #region Entrypoint

    // UnwindStackFrameX86 in src/coreclr/vm/gc_unwind_x86.inl
    public bool Unwind(ref X86Context context)
    {
        IExecutionManager eman = _target.Contracts.ExecutionManager;

        if (eman.GetCodeBlockHandle(context.InstructionPointer.Value) is not CodeBlockHandle cbh)
        {
            throw new InvalidOperationException("Unwind failed, unable to find code block for the instruction pointer.");
        }

        eman.GetGCInfo(cbh, out TargetPointer gcInfoAddress, out uint _);
        uint relOffset = (uint)eman.GetRelativeOffset(cbh).Value;
        TargetCodePointer methodStart = eman.GetStartAddress(cbh);
        // TargetCodePointer funcletStart = eman.GetFuncletStartAddress(cbh);
        // bool isFunclet = eman.IsFunclet(cbh);

        GCInfo gcInfo = new(_target, gcInfoAddress, relOffset);

        if (gcInfo.IsInEpilog)
        {
            /* First, handle the epilog */
            TargetPointer epilogBase = methodStart + (gcInfo.RelativeOffset - gcInfo.EpilogOffset);
            UnwindEpilog(ref context, gcInfo, epilogBase);
        }
        else if (!gcInfo.Header.EbpFrame && !gcInfo.Header.DoubleAlign)
        {
            /* Handle ESP frames */
            UnwindEspFrame(ref context, gcInfo, methodStart);
            return true;
        }
        else
        {
            // /* Now we know that we have an EBP frame */
            // if (!UnwindEbpDoubleAlignFrameEpilog(
            //     ref context,
            //     gcInfo,
            //     methodStart,
            //     funcletStart,
            //     isFunclet))
            // {
            //     return false;
            // }
        }

        return true;
    }

    #endregion
    #region Unwind Logic

    private void UnwindEpilog(ref X86Context context, GCInfo gcInfo, TargetPointer epilogBase)
    {
        Debug.Assert(gcInfo.IsInEpilog);
        Debug.Assert(gcInfo.EpilogOffset > 0);

        if (gcInfo.Header.EbpFrame || gcInfo.Header.DoubleAlign)
        {
            UnwindEbpDoubleAlignFrameEpilog(ref context, gcInfo, epilogBase);
        }
        else
        {
            UnwindEspFrameEpilog(ref context, gcInfo, epilogBase);
        }

        /* Now adjust stack pointer */
        context.Esp += ESPIncrementOnReturn(gcInfo);
    }

    private void UnwindEbpDoubleAlignFrameEpilog(ref X86Context context, GCInfo gcInfo, TargetPointer epilogBase)
    {
        /* See how many instructions we have executed in the
            epilog to determine which callee-saved registers
            have already been popped */
        uint offset = 0;

        uint esp = context.Esp;

        bool needMovEspEbp = false;

        if (gcInfo.Header.DoubleAlign)
        {
            // add esp, RawStackSize

            if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
            {
                esp += gcInfo.RawStackSize;
            }
            Debug.Assert(gcInfo.RawStackSize != 0);
            offset = SKIP_ARITH_REG((int)gcInfo.RawStackSize, epilogBase, offset);

            // We also need "mov esp, ebp" after popping the callee-saved registers
            needMovEspEbp = true;
        }
        else
        {
            bool needLea = false;

            if (gcInfo.Header.LocalAlloc)
            {
                // ESP may be variable if a localloc was actually executed. We will reset it.
                //    lea esp, [ebp-calleeSavedRegs]
                needLea = true;
            }
            else if (gcInfo.SavedRegsCountExclFP == 0)
            {
                // We will just generate "mov esp, ebp" and be done with it.
                if (gcInfo.RawStackSize != 0)
                {
                    needMovEspEbp = true;
                }
            }
            else if (gcInfo.RawStackSize == 0)
            {
                // do nothing before popping the callee-saved registers
            }
            else if (gcInfo.RawStackSize == _target.PointerSize)
            {
                // "pop ecx" will make ESP point to the callee-saved registers
                if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
                {
                    esp += (uint)_target.PointerSize;
                }
                offset = SKIP_POP_REG(epilogBase, offset);
            }
            else
            {
                // We need to make ESP point to the callee-saved registers
                //    lea esp, [ebp-calleeSavedRegs]

                needLea = true;
            }

            if (needLea)
            {
                // lea esp, [ebp-calleeSavedRegs]

                uint calleeSavedRegsSize = gcInfo.SavedRegsCountExclFP * (uint)_target.PointerSize;

                if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
                {
                    esp = context.Ebp - calleeSavedRegsSize;
                }

                offset = SKIP_LEA_ESP_EBP(-(int)calleeSavedRegsSize, epilogBase, offset);
            }
        }

        foreach (RegMask regMask in registerOrder)
        {
            if (regMask == RegMask.EBP)
            {
                continue; // EBP is handled separately
            }

            if (!gcInfo.SavedRegsMask.HasFlag(regMask))
            {
                continue;
            }

            if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
            {
                // TODO(cdacX86): UpdateAllRegs set location??
                esp += (uint)_target.PointerSize;
            }

            offset = SKIP_POP_REG(epilogBase, offset);
        }

        if (needMovEspEbp)
        {
            if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
                esp = context.Ebp;

            offset = SKIP_MOV_REG_REG(epilogBase, offset);
        }

        // Have we executed the pop EBP?
        if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
        {
            // TODO(cdacx86): are these equivalent?
            // pContext->SetEbpLocation(PTR_DWORD(TADDR(ESP)));
            // context.Ebp = _target.Read<uint>(esp);
            esp += (uint)_target.PointerSize;
        }
        _ = SKIP_POP_REG(epilogBase, offset);


        // TODO(cdacx86): are these equivalent?
        // SetRegdisplayPCTAddr(pContext, (TADDR)ESP);
        context.Eip = _target.Read<uint>(esp);

        context.Esp = esp;
    }

    private void UnwindEspFrameEpilog(ref X86Context context, GCInfo gcInfo, TargetPointer epilogBase)
    {
        Debug.Assert(gcInfo.IsInEpilog);
        Debug.Assert(!gcInfo.Header.EbpFrame && !gcInfo.Header.DoubleAlign);
        Debug.Assert(gcInfo.EpilogOffset > 0);

        uint offset = 0;
        uint esp = context.Esp;

        if (gcInfo.RawStackSize != 0)
        {
            if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
            {
                /* We have NOT executed the "ADD ESP, FrameSize",
                   so manually adjust stack pointer */
                esp += gcInfo.RawStackSize;
            }

            // We have already popped off the frame (excluding the callee-saved registers)
            if (ReadByteAt(epilogBase) == X86_INSTR_POP_ECX)
            {
                // We may use "POP ecx" for doing "ADD ESP, 4",
                // or we may not (in the case of JMP epilogs)
                Debug.Assert(gcInfo.RawStackSize == _target.PointerSize);
                offset = SKIP_POP_REG(epilogBase, offset);
            }
            else
            {
                // "add esp, rawStkSize"
                offset = SKIP_ARITH_REG((int)gcInfo.RawStackSize, epilogBase, offset);
            }
        }

        /* Remaining callee-saved regs are at ESP. Need to update
           regsMask as well to exclude registers which have already been popped. */
        foreach (RegMask regMask in registerOrder)
        {
            if (!gcInfo.SavedRegsMask.HasFlag(regMask))
                continue;

            if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
            {
                /* We have NOT yet popped off the register.
                   Get the value from the stack if needed */
                // TODO(cdacX86): UpdateAllRegs set location??
                esp += (uint)_target.PointerSize;
            }

            offset = SKIP_POP_REG(epilogBase, offset);
        }

        //CEE_JMP generates an epilog similar to a normal CEE_RET epilog except for the last instruction
        Debug.Assert(
            CheckInstrBytePattern((byte)(ReadByteAt(epilogBase + offset) & X86_INSTR_RET), X86_INSTR_RET, ReadByteAt(epilogBase + offset)) //ret
            || CheckInstrBytePattern(ReadByteAt(epilogBase + offset), X86_INSTR_JMP_NEAR_REL32, ReadByteAt(epilogBase + offset)) //jmp ret32
            || CheckInstrWord(ReadShortAt(epilogBase + offset), X86_INSTR_w_JMP_FAR_IND_IMM)); //jmp [addr32]

        /* Finally we can set pPC */
        // TODO(cdacx86): are these equivalent?
        // SetRegdisplayPCTAddr(pContext, (TADDR)ESP);
        context.Eip = _target.Read<uint>(esp);

        context.Esp = esp;
    }

    private void UnwindEspFrame(ref X86Context context, GCInfo gcInfo, TargetCodePointer methodStart)
    {
        Debug.Assert(!gcInfo.Header.EbpFrame && !gcInfo.Header.DoubleAlign);
        Debug.Assert(!gcInfo.IsInEpilog);

        Console.WriteLine(methodStart);

        uint esp = context.Esp;

        if (gcInfo.IsInProlog)
        {
            if (gcInfo.PrologOffset != 0) // Do nothing for the very start of the method
            {
                // TODO(cdacx86):
                // UnwindEspFrameProlog(pContext, info, methodStart, flags);
                esp = context.Esp;
            }
        }
        else
        {
            /* We are past the prolog, ESP has been set above */

            // Handle arguments pushed to the stack

            // TODO(cdacx86): calculate pushed arg size. This involves reading the ArgRegTable from the GCInfo.

            esp += gcInfo.RawStackSize;

            foreach (RegMask regMask in registerOrder)
            {
                if (!gcInfo.SavedRegsMask.HasFlag(regMask))
                    continue;

                // TODO(cdacx86): UpdateAllRegs set location??
                // SetLocation(pContext, i - 1, PTR_DWORD((TADDR)ESP));

                // Pop the callee-saved registers
                esp += (uint)_target.PointerSize;
            }
        }

        /* we can now set the (address of the) return address */
        // TODO(cdacx86): are these equivalent?
        // SetRegdisplayPCTAddr(pContext, (TADDR)ESP);
        context.Eip = _target.Read<uint>(esp);

        context.Esp = esp + ESPIncrementOnReturn(gcInfo);
    }

    // private bool UnwindEbpDoubleAlignFrameEpilog(
    //     ref X86Context context,
    //     GCInfo gcInfo,
    //     TargetCodePointer methodStart,
    //     TargetCodePointer funcletStart,
    //     bool isFunclet)
    // {
    //     Debug.Assert(gcInfo.Header.EbpFrame || gcInfo.Header.DoubleAlign);

    //     uint curEsp = context.Esp;
    //     uint curEbp = context.Ebp;

    //     /* First check if we are in a filter (which is obviously after the prolog) */
    //     if (gcInfo.Header.Handlers && !gcInfo.IsInProlog)
    //     {
    //         TargetPointer baseSP;

    //         if (isFunclet)
    //         {
    //             baseSP = curEsp;
    //             // Set baseSP as initial SP

    //             // baseSP += GetPushedArgSize(info, table, curOffs);
    //         }
    //         else
    //         {
    //             baseSP = methodStart;
    //         }
    //     }

    //     return false;
    // }

    #endregion
    #region Helper Methods

    // Use this to check if the instruction at offset "walkOffset" has already
    // been executed
    // "actualHaltOffset" is the offset when the code was suspended
    // It is assumed that there is linear control flow from offset 0 to "actualHaltOffset".
    //
    // This has been factored out just so that the intent of the comparison
    // is clear (compared to the opposite intent)
    private static bool InstructionAlreadyExecuted(uint walkOffset, uint actualHaltOffset)
    {
        return walkOffset < actualHaltOffset;
    }

    private uint ESPIncrementOnReturn(GCInfo gcInfo)
    {

        uint stackParameterSize = gcInfo.Header.VarArgs ? 0 // varargs are caller-popped
            : gcInfo.Header.ArgCount * (uint)_target.PointerSize;
        return (uint)_target.PointerSize /* return address size */ + stackParameterSize;
    }

    // skips past a "arith REG, IMM"
    private uint SKIP_ARITH_REG(int val, TargetPointer baseAddress, uint offset)
    {
        uint delta = 0;
        if (val != 0)
        {
            // Confirm that arith instruction is at the correct place
            Debug.Assert(CheckInstrBytePattern((byte)(ReadByteAt(baseAddress + offset) & 0xFD), 0x81, ReadByteAt(baseAddress + offset)));
            Debug.Assert(CheckInstrBytePattern((byte)(ReadByteAt(baseAddress + offset + 1) & 0xC0), 0xC0, ReadByteAt(baseAddress + offset + 1)));

            // only use DWORD form if needed
            Debug.Assert(((ReadByteAt(baseAddress + offset) & 2) != 0 == CAN_COMPRESS(val)) || IsMarkerInstr(ReadByteAt(baseAddress + offset)));

            delta = 2u + (CAN_COMPRESS(val) ? 1u : 4u);
        }
        return offset + delta;
    }

    private uint SKIP_POP_REG(TargetPointer baseAddress, uint offset)
    {
        // Confirm it is a pop instruction
        Debug.Assert(CheckInstrBytePattern((byte)(ReadByteAt(baseAddress + offset) & 0xF8), 0x58, ReadByteAt(baseAddress + offset)));

        return offset + 1;
    }

    private uint SKIP_LEA_ESP_EBP(int val, TargetPointer baseAddress, uint offset)
    {
        // Confirm it is the right instruction
        // Note that only the first byte may have been stomped on by IsMarkerInstr()
        // So we can check the second byte directly
        Debug.Assert(
            (CheckInstrWord(ReadShortAt(baseAddress), X86_INSTR_w_LEA_ESP_EBP_BYTE_OFFSET) &&
            (val == ReadSByteAt(baseAddress + 2)) &&
            CAN_COMPRESS(val))
            ||
            (CheckInstrWord(ReadShortAt(baseAddress), X86_INSTR_w_LEA_ESP_EBP_DWORD_OFFSET) &&
            (val == ReadIntAt(baseAddress + 2)) &&
            !CAN_COMPRESS(val))
        );

        uint delta = 2u + (CAN_COMPRESS(val) ? 1u : 4u);
        return offset + delta;
    }

    private uint SKIP_MOV_REG_REG(TargetPointer baseAddress, uint offset)
    {
        // Confirm it is a move instruction
        // Note that only the first byte may have been stomped on by IsMarkerInstr()
        // So we can check the second byte directly
        Debug.Assert(
            CheckInstrBytePattern((byte)(ReadByteAt(baseAddress + offset) & 0xFD), 0x89, ReadByteAt(baseAddress + offset))
            &&
            (ReadByteAt(baseAddress + offset) & 0xC0) == 0xC0
        );
        return offset + 2;
    }

    private static bool CAN_COMPRESS(int val)
    {
        return ((byte)val) == val;
    }

    #endregion

    #region Verification Helpers

    private const byte X86_INSTR_INT3 = 0xCC; // int3
    private const byte X86_INSTR_POP_ECX = 0x59; // pop ecx
    private const byte X86_INSTR_RET = 0xC2; // ret imm16
    private const byte X86_INSTR_JMP_NEAR_REL32 = 0xE9; // near jmp rel32
    private const ushort X86_INSTR_w_JMP_FAR_IND_IMM = 0x25FF; // far jmp [addr32]

    private const ushort X86_INSTR_w_LEA_ESP_EBP_BYTE_OFFSET = 0x658d; // lea esp, [ebp-bOffset]
    private const ushort X86_INSTR_w_LEA_ESP_EBP_DWORD_OFFSET = 0xa58d; // lea esp, [ebp-dwOffset]


    /* Similar to CheckInstrByte(). Use this to check a masked opcode (ignoring
       optional bits in the opcode encoding).
       valPattern is the masked out value.
       expectedPattern is the mask value we expect.
       val is the actual instruction opcode */
    private static bool CheckInstrBytePattern(byte valPattern, byte expectedPattern, byte val)
    {
        Debug.Assert((valPattern & val) == valPattern);

        return (valPattern == expectedPattern) || IsMarkerInstr(val);
    }

    /* Similar to CheckInstrByte() */
    private static bool CheckInstrWord(ushort val, ushort expectedValue)
    {
        return (val == expectedValue) || IsMarkerInstr((byte)(val & 0xFF));
    }

    private static bool IsMarkerInstr(byte val)
    {
        return val == X86_INSTR_INT3;
    }

    private sbyte ReadSByteAt(TargetPointer address)
    {
        return _target.Read<sbyte>(address);
    }

    private byte ReadByteAt(TargetPointer address)
    {
        return _target.Read<byte>(address);
    }

    private ushort ReadShortAt(TargetPointer address)
    {
        return _target.Read<ushort>(address);
    }

    private int ReadIntAt(TargetPointer address)
    {
        return _target.Read<int>(address);
    }

    #endregion
}
