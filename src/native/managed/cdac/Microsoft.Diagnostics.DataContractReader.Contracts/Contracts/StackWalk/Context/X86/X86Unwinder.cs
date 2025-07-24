// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

public class X86Unwinder(Target target)
{
    private const byte X86_INSTR_INT3 = 0xCC; // int3
    private const byte X86_INSTR_POP_ECX = 0x59; // pop ecx
    private const byte X86_INSTR_RET = 0xC2; // ret imm16
    private const byte X86_INSTR_JMP_NEAR_REL32 = 0xE9; // near jmp rel32
    private const byte X86_INSTR_PUSH_EAX = 0x50; // push eax
    private const byte X86_INSTR_XOR = 0x33; // xor
    private const byte X86_INSTR_NOP = 0x90; // nop
    private const byte X86_INSTR_RETN = 0xC3; // ret
    private const byte X86_INSTR_PUSH_EBP = 0x55; // push ebp
    private const ushort X86_INSTR_W_MOV_EBP_ESP = 0xEC8B; // mov ebp, esp
    private const byte X86_INSTR_CALL_REL32 = 0xE8; // call rel32
    private const ushort X86_INSTR_W_CALL_IND_IMM = 0x15FF; // call [addr32]
    private const ushort X86_INSTR_w_JMP_FAR_IND_IMM = 0x25FF; // far jmp [addr32]
    private const ushort X86_INSTR_w_TEST_ESP_EAX = 0x0485; // test [esp], eax
    private const ushort X86_INSTR_w_LEA_ESP_EBP_BYTE_OFFSET = 0x658d; // lea esp, [ebp-bOffset]
    private const ushort X86_INSTR_w_LEA_ESP_EBP_DWORD_OFFSET = 0xa58d; // lea esp, [ebp-dwOffset]
    private const ushort X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX = 0x8485; // test [esp-dwOffset], eax
    private const ushort X86_INSTR_w_LEA_EAX_ESP_BYTE_OFFSET = 0x448d; // lea eax, [esp-bOffset]
    private const ushort X86_INSTR_w_LEA_EAX_ESP_DWORD_OFFSET = 0x848d; // lea eax, [esp-dwOffset]

    private readonly Target _target = target;
    private readonly uint _pointerSize = (uint)target.PointerSize;
    private readonly bool _updateAllRegs = true;
    private readonly bool _unixX86ABI = target.Contracts.RuntimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Unix;

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

        eman.GetGCInfo(cbh, out TargetPointer gcInfoAddress, out uint gcInfoVersion);
        uint relOffset = (uint)eman.GetRelativeOffset(cbh).Value;
        TargetPointer methodStart = eman.GetStartAddress(cbh).AsTargetPointer;
        TargetPointer funcletStart = eman.GetFuncletStartAddress(cbh).AsTargetPointer;
        bool isFunclet = eman.IsFunclet(cbh);

        GCInfo gcInfo = new(_target, gcInfoAddress, gcInfoVersion, relOffset);

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
        }
        else
        {
            /* Now we know that we have an EBP frame */
            if (!UnwindEbpDoubleAlignFrame(
                ref context,
                gcInfo,
                methodStart,
                funcletStart,
                isFunclet))
            {
                return false;
            }
        }

        context.ContextFlags |= (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
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
            else if (gcInfo.RawStackSize == _target.PointerSize && ReadByteAt(epilogBase) == X86_INSTR_POP_ECX)
            {
                // We may use "POP ecx" for doing "ADD ESP, 4",
                // or we may not (in the case of JMP epilogs)

                // "pop ecx" will make ESP point to the callee-saved registers
                if (!InstructionAlreadyExecuted(offset, gcInfo.EpilogOffset))
                {
                    esp += _pointerSize;
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

                uint calleeSavedRegsSize = gcInfo.SavedRegsCountExclFP * _pointerSize;

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
                if (_updateAllRegs)
                {
                    TargetPointer regValueFromStack = _target.ReadPointer(esp);
                    SetRegValue(ref context, regMask, regValueFromStack);
                }
                esp += _pointerSize;
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
            context.Ebp = _target.Read<uint>(esp);
            esp += _pointerSize;
        }
        _ = SKIP_POP_REG(epilogBase, offset);

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
                if (_updateAllRegs || regMask == RegMask.EBP)
                {
                    TargetPointer regValueFromStack = _target.ReadPointer(esp);
                    SetRegValue(ref context, regMask, regValueFromStack);
                }
                esp += _pointerSize;
            }

            offset = SKIP_POP_REG(epilogBase, offset);
        }

        //CEE_JMP generates an epilog similar to a normal CEE_RET epilog except for the last instruction
        Debug.Assert(
            CheckInstrBytePattern((byte)(ReadByteAt(epilogBase + offset) & X86_INSTR_RET), X86_INSTR_RET, ReadByteAt(epilogBase + offset)) //ret
            || CheckInstrBytePattern(ReadByteAt(epilogBase + offset), X86_INSTR_JMP_NEAR_REL32, ReadByteAt(epilogBase + offset)) //jmp ret32
            || CheckInstrWord(ReadShortAt(epilogBase + offset), X86_INSTR_w_JMP_FAR_IND_IMM)); //jmp [addr32]

        /* Finally we can set pPC */
        context.Eip = _target.Read<uint>(esp);
        context.Esp = esp;
    }

    private void UnwindEspFrame(ref X86Context context, GCInfo gcInfo, TargetPointer methodStart)
    {
        Debug.Assert(!gcInfo.Header.EbpFrame && !gcInfo.Header.DoubleAlign);
        Debug.Assert(!gcInfo.IsInEpilog);

        Console.WriteLine(methodStart);

        uint esp = context.Esp;

        if (gcInfo.IsInProlog)
        {
            if (gcInfo.PrologOffset != 0) // Do nothing for the very start of the method
            {
                UnwindEspFrameProlog(ref context, gcInfo, methodStart);
                esp = context.Esp;
            }
        }
        else
        {
            /* We are past the prolog, ESP has been set above */

            esp += gcInfo.PushedArgSize;
            esp += gcInfo.RawStackSize;

            foreach (RegMask regMask in registerOrder)
            {
                if (!gcInfo.SavedRegsMask.HasFlag(regMask))
                    continue;

                TargetPointer regValueFromStack = _target.ReadPointer(esp);
                SetRegValue(ref context, regMask, regValueFromStack);

                // Pop the callee-saved registers
                esp += _pointerSize;
            }
        }

        /* we can now set the (address of the) return address */
        context.Eip = _target.Read<uint>(esp);
        context.Esp = esp + ESPIncrementOnReturn(gcInfo);
    }

    private void UnwindEspFrameProlog(ref X86Context context, GCInfo gcInfo, TargetPointer methodStart)
    {
        Debug.Assert(gcInfo.IsInProlog);
        Debug.Assert(!gcInfo.Header.EbpFrame && !gcInfo.Header.DoubleAlign);

        uint offset = 0;

        // If the first two instructions are 'nop, int3', then  we will
        // assume that is from a JitHalt operation and skip past it
        if (ReadByteAt(methodStart) == X86_INSTR_NOP && ReadByteAt(methodStart + 1) == X86_INSTR_INT3)
        {
            offset += 2;
        }

        uint curOffs = gcInfo.PrologOffset;
        uint esp = context.Esp;

        RegMask regsMask = RegMask.NONE;
        TargetPointer savedRegPtr = esp;

        // Find out how many callee-saved regs have already been pushed
        foreach (RegMask regMask in registerOrder)
        {
            if (!gcInfo.SavedRegsMask.HasFlag(regMask))
                continue;

            if (InstructionAlreadyExecuted(offset, curOffs))
            {
                esp += _pointerSize;
                regsMask |= regMask;
            }

            offset = SKIP_PUSH_REG(methodStart.Value, offset);
        }

        if (gcInfo.RawStackSize != 0)
        {
            offset = SKIP_ALLOC_FRAME((int)gcInfo.RawStackSize, methodStart.Value, offset);

            // Note that this assumes that only the last instruction in SKIP_ALLOC_FRAME
            // actually updates ESP
            if (InstructionAlreadyExecuted(offset, curOffs + 1))
            {
                savedRegPtr += gcInfo.RawStackSize;
                esp += gcInfo.RawStackSize;
            }
        }


        // Always restore EBP
        if (regsMask.HasFlag(RegMask.EBP))
        {
            context.Ebp = _target.Read<uint>(savedRegPtr);
            savedRegPtr += _pointerSize;
        }

        if (_updateAllRegs)
        {
            if (regsMask.HasFlag(RegMask.EBX))
            {
                context.Ebx = _target.Read<uint>(savedRegPtr);
                savedRegPtr += _pointerSize;
            }
            if (regsMask.HasFlag(RegMask.ESI))
            {
                context.Esi = _target.Read<uint>(savedRegPtr);
                savedRegPtr += _pointerSize;
            }
            if (regsMask.HasFlag(RegMask.EDI))
            {
                context.Edi = _target.Read<uint>(savedRegPtr);
            }
        }

        context.Esp = esp;
    }

    private bool UnwindEbpDoubleAlignFrame(
        ref X86Context context,
        GCInfo gcInfo,
        TargetPointer methodStart,
        TargetPointer funcletStart,
        bool isFunclet)
    {
        Debug.Assert(gcInfo.Header.EbpFrame || gcInfo.Header.DoubleAlign);

        uint curEsp = context.Esp;
        uint curEbp = context.Ebp;

        /* First check if we are in a filter (which is obviously after the prolog) */
        if (gcInfo.Header.Handlers && !gcInfo.IsInProlog)
        {
            TargetPointer baseSP;

            if (isFunclet)
            {
                baseSP = curEsp;
                // Set baseSP as initial SP
                baseSP += gcInfo.PushedArgSize;

                if (_unixX86ABI)
                {
                    // 16-byte stack alignment padding (allocated in genFuncletProlog)
                    // Current funclet frame layout (see CodeGen::genFuncletProlog() and genFuncletEpilog()):
                    //   prolog: sub esp, 12
                    //   epilog: add esp, 12
                    //           ret
                    // SP alignment padding should be added for all instructions except the first one and the last one.
                    // Epilog may not exist (unreachable), so we need to check the instruction code.
                    if (funcletStart != methodStart + gcInfo.RelativeOffset && ReadByteAt(methodStart + gcInfo.RelativeOffset) != X86_INSTR_RETN)
                        baseSP += 12;
                }

                context.Eip = (uint)_target.ReadPointer(baseSP);
                context.Esp = (uint)baseSP + _pointerSize;
                return true;
            }

            /* The cDAC only supports FEATURE_EH_FUNCLETS and therefore does not
               support unwinding filters without funclets. */
        }

        //
        // Prolog of an EBP method
        //

        if (gcInfo.IsInProlog)
        {
            UnwindEbpDoubleAlignFrameProlog(ref context, gcInfo, methodStart.Value);

            /* Now adjust stack pointer. */

            context.Esp += ESPIncrementOnReturn(gcInfo);
            return true;
        }

        if (_updateAllRegs)
        {
            // Get to the first callee-saved register
            TargetPointer pSavedRegs = curEbp;
            if (gcInfo.Header.DoubleAlign && (curEbp & 0x04) != 0)
                pSavedRegs -= _pointerSize;

            foreach (RegMask regMask in registerOrder.Reverse())
            {
                if (regMask == RegMask.EBP) continue;

                if (!gcInfo.SavedRegsMask.HasFlag(regMask)) continue;

                pSavedRegs -= _pointerSize;
                TargetPointer regValueFromStack = _target.ReadPointer(pSavedRegs);
                SetRegValue(ref context, regMask, regValueFromStack);
            }
        }

        /* The caller's ESP will be equal to EBP + retAddrSize + argSize. */
        context.Esp = curEbp + _pointerSize + ESPIncrementOnReturn(gcInfo);

        /* The caller's saved EIP is right after our EBP */
        context.Eip = (uint)_target.ReadPointer(curEbp + _pointerSize);

        /* The caller's saved EBP is pointed to by our EBP */
        context.Ebp = (uint)_target.ReadPointer(curEbp);
        return true;
    }

    private void UnwindEbpDoubleAlignFrameProlog(ref X86Context context, GCInfo gcInfo, TargetPointer methodStart)
    {
        Debug.Assert(gcInfo.IsInProlog);
        Debug.Assert(gcInfo.Header.EbpFrame || gcInfo.Header.DoubleAlign);

        uint offset = 0;

        // If the first two instructions are 'nop, int3', then  we will
        // assume that is from a JitHalt operation and skip past it
        if (ReadByteAt(methodStart) == X86_INSTR_NOP && ReadByteAt(methodStart + 1) == X86_INSTR_INT3)
        {
            offset += 2;
        }

        /* Check for the case where EBP has not been updated yet. */
        uint curOffs = gcInfo.PrologOffset;

        // If we have still not executed "push ebp; mov ebp, esp", then we need to
        // report the frame relative to ESP

        if (!InstructionAlreadyExecuted(offset + 1, curOffs))
        {
            Debug.Assert(CheckInstrByte(ReadByteAt(methodStart + offset), X86_INSTR_PUSH_EBP) ||
                    CheckInstrWord(ReadShortAt(methodStart + offset), X86_INSTR_W_MOV_EBP_ESP) ||
                    CheckInstrByte(ReadByteAt(methodStart + offset), X86_INSTR_JMP_NEAR_REL32));   // a rejit jmp-stamp

            /* If we're past the "push ebp", adjust ESP to pop EBP off */
            if (curOffs == (offset + 1))
                context.Esp += _pointerSize;

            /* Stack pointer points to return address */
            context.Eip = (uint)_target.ReadPointer(context.Esp);

            /* EBP and callee-saved registers still have the correct value */
            return;
        }

        // We are atleast after the "push ebp; mov ebp, esp"
        offset = SKIP_MOV_REG_REG(methodStart, SKIP_PUSH_REG(methodStart, offset));

        /* At this point, EBP has been set up. The caller's ESP and the return value
           can be determined using EBP. Since we are still in the prolog,
           we need to know our exact location to determine the callee-saved registers */
        uint curEBP = context.Ebp;

        if (_updateAllRegs)
        {
            TargetPointer pSavedRegs = curEBP;

            /* make sure that we align ESP just like the method's prolog did */
            if (gcInfo.Header.DoubleAlign)
            {
                // "and esp,-8"
                offset = SKIP_ARITH_REG(-8, methodStart, offset);
                if ((curEBP & 0x04) != 0) pSavedRegs--;
            }

            /* Increment "offset" in steps to see which callee-saved
               registers have been pushed already */

            foreach (RegMask regMask in registerOrder.Reverse())
            {
                if (regMask == RegMask.EBP) continue;

                if (!gcInfo.SavedRegsMask.HasFlag(regMask)) continue;

                if (InstructionAlreadyExecuted(offset, curOffs))
                {
                    pSavedRegs -= _pointerSize;
                    TargetPointer regValueFromStack = _target.ReadPointer(pSavedRegs);
                    SetRegValue(ref context, regMask, regValueFromStack);
                }

                offset = SKIP_PUSH_REG(methodStart, offset);
            }
        }

        /* The caller's saved EBP is pointed to by our EBP */
        context.Ebp = (uint)_target.ReadPointer(curEBP);
        context.Esp = (uint)_target.ReadPointer(curEBP + _pointerSize);

        /* Stack pointer points to return address */
        context.Eip = (uint)_target.ReadPointer(context.Esp);
    }

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
            : gcInfo.Header.ArgCount * _pointerSize;
        return _pointerSize /* return address size */ + stackParameterSize;
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

    private uint SKIP_PUSH_REG(TargetPointer baseAddress, uint offset)
    {
        // Confirm it is a push instruction
        Debug.Assert(CheckInstrBytePattern((byte)(ReadByteAt(baseAddress + offset) & 0xF8), 0x50, ReadByteAt(baseAddress + offset)));
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

    private uint SKIP_ALLOC_FRAME(int size, TargetPointer baseAddress, uint offset)
    {
        Debug.Assert(size != 0);

        if (size == _target.PointerSize)
        {
            // JIT emits "push eax" instead of "sub esp,4"
            return SKIP_PUSH_REG(baseAddress, offset);
        }

        const int STACK_PROBE_PAGE_SIZE_BYTES = 4096;
        const int STACK_PROBE_BOUNDARY_THRESHOLD_BYTES = 1024;

        int lastProbedLocToFinalSp = size;

        if (size < STACK_PROBE_PAGE_SIZE_BYTES)
        {
            // sub esp, size
            offset = SKIP_ARITH_REG(size, baseAddress, offset);
        }
        else
        {
            ushort wOpcode = ReadShortAt(baseAddress + offset);

            if (CheckInstrWord(wOpcode, X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX))
            {
                // In .NET 5.0 and earlier for frames that have size smaller than 0x3000 bytes
                // JIT emits one or two 'test eax, [esp-dwOffset]' instructions before adjusting the stack pointer.
                Debug.Assert(size < 0x3000);

                // test eax, [esp-0x1000]
                offset += 7;
                lastProbedLocToFinalSp -= 0x1000;

                if (size >= 0x2000)
                {
                    Debug.Assert(CheckInstrWord(ReadShortAt(baseAddress + offset), X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX));

                    //test eax, [esp-0x2000]
                    offset += 7;
                    lastProbedLocToFinalSp -= 0x1000;
                }

                // sub esp, size
                offset = SKIP_ARITH_REG(size, baseAddress, offset);
            }
            else
            {
                bool pushedStubParam = false;

                if (CheckInstrByte(ReadByteAt(baseAddress + offset), X86_INSTR_PUSH_EAX))
                {
                    // push eax
                    offset = SKIP_PUSH_REG(baseAddress, offset);
                    pushedStubParam = true;
                }

                if (CheckInstrByte(ReadByteAt(baseAddress + offset), X86_INSTR_XOR))
                {
                    // In .NET Core 3.1 and earlier for frames that have size greater than or equal to 0x3000 bytes
                    // JIT emits the following loop.
                    Debug.Assert(size >= 0x3000);

                    offset += 2;
                    //      xor eax, eax                2
                    //      [nop]                       0-3
                    // loop:
                    //      test [esp + eax], eax       3
                    //      sub eax, 0x1000             5
                    //      cmp eax, -size              5
                    //      jge loop                    2

                    // R2R images that support ReJIT may have extra nops we need to skip over.
                    while (offset < 5)
                    {
                        if (CheckInstrByte(ReadByteAt(baseAddress + offset), X86_INSTR_NOP))
                        {
                            offset++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    offset += 15;

                    if (pushedStubParam)
                    {
                        // pop eax
                        offset = SKIP_POP_REG(baseAddress, offset);
                    }

                    // sub esp, size
                    return SKIP_ARITH_REG(size, baseAddress, offset);
                }
                else
                {
                    // In .NET 5.0 and later JIT emits a call to JIT_StackProbe helper.

                    if (pushedStubParam)
                    {
                        // lea eax, [esp-size+4]
                        offset = SKIP_LEA_EAX_ESP(-size + 4, baseAddress, offset);
                        // call JIT_StackProbe
                        offset = SKIP_HELPER_CALL(baseAddress, offset);
                        // pop eax
                        offset = SKIP_POP_REG(baseAddress, offset);
                        // sub esp, size
                        return SKIP_ARITH_REG(size, baseAddress, offset);
                    }
                    else
                    {
                        // lea eax, [esp-size]
                        offset = SKIP_LEA_EAX_ESP(-size, baseAddress, offset);
                        // call JIT_StackProbe
                        offset = SKIP_HELPER_CALL(baseAddress, offset);
                        // mov esp, eax
                        return SKIP_MOV_REG_REG(baseAddress, offset);
                    }
                }
            }
        }

        if (lastProbedLocToFinalSp + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > STACK_PROBE_PAGE_SIZE_BYTES)
        {
            Debug.Assert(CheckInstrWord(_target.Read<ushort>(baseAddress + offset), X86_INSTR_w_TEST_ESP_EAX));

            // test [esp], eax
            offset += 3;
        }

        return offset;
    }

    private uint SKIP_LEA_EAX_ESP(int val, TargetPointer baseAddress, uint offset)
    {
        ushort wOpcode = ReadShortAt(baseAddress + offset);
        if (CheckInstrWord(wOpcode, X86_INSTR_w_LEA_EAX_ESP_BYTE_OFFSET))
        {
            Debug.Assert(val == _target.Read<sbyte>(baseAddress + offset + 3));
            Debug.Assert(CAN_COMPRESS(val));
        }
        else
        {
            Debug.Assert(CheckInstrWord(wOpcode, X86_INSTR_w_LEA_EAX_ESP_DWORD_OFFSET));
            Debug.Assert(val == _target.Read<int>(baseAddress + offset + 3));
            Debug.Assert(!CAN_COMPRESS(val));
        }

        uint delta = 3u + (CAN_COMPRESS(-val) ? 1u : 4u);
        return offset + delta;
    }

    private uint SKIP_HELPER_CALL(TargetPointer baseAddress, uint offset)
    {
        uint delta;

        if (CheckInstrByte(ReadByteAt(baseAddress + offset), X86_INSTR_CALL_REL32))
        {
            delta = 5;
        }
        else
        {
            Debug.Assert(CheckInstrWord(_target.Read<ushort>(baseAddress + offset), X86_INSTR_W_CALL_IND_IMM));
            delta = 6;
        }

        return offset + delta;
    }

    private static bool CAN_COMPRESS(int val)
    {
        return ((byte)val) == val;
    }

    private static void SetRegValue(ref X86Context context, RegMask regMask, TargetPointer value)
    {
        uint regValue = (uint)value;
        switch (regMask)
        {
            case RegMask.EAX:
                context.Eax = regValue;
                break;
            case RegMask.EBX:
                context.Ebx = regValue;
                break;
            case RegMask.ECX:
                context.Ecx = regValue;
                break;
            case RegMask.EDX:
                context.Edx = regValue;
                break;
            case RegMask.EBP:
                context.Ebp = regValue;
                break;
            case RegMask.ESI:
                context.Esi = regValue;
                break;
            case RegMask.EDI:
                context.Edi = regValue;
                break;
            default:
                throw new ArgumentException($"Unsupported register mask: {regMask}");
        }
    }

    #endregion

    #region Verification Helpers

    /* Check if the given instruction opcode is the one we expect.
    This is a "necessary" but not "sufficient" check as it ignores the check
    if the instruction is one of our special markers (for debugging and GcStress) */
    private static bool CheckInstrByte(byte val, byte expectedValue)
    {
        return (val == expectedValue) || IsMarkerInstr(val);
    }

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
