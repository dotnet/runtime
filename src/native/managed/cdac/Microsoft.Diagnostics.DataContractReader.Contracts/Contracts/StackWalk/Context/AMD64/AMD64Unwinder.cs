// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.AMD64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.AMD64;

internal class AMD64Unwinder(Target target)
{
    private const byte SIZE64_PREFIX = 0x48;
    private const byte ADD_IMM8_OP = 0x83;
    private const byte ADD_IMM32_OP = 0x81;
    private const byte JMP_IMM8_OP = 0xeb;
    private const byte JMP_IMM32_OP = 0xe9;
    private const byte JMP_IND_OP = 0xff;
    private const byte LEA_OP = 0x8d;
    private const byte REPNE_PREFIX = 0xf2;
    private const byte REP_PREFIX = 0xf3;
    private const byte POP_OP = 0x58;
    private const byte RET_OP = 0xc3;
    private const byte RET_OP_2 = 0xc2;

    private const uint UNWIND_CHAIN_LIMIT = 32;

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    private readonly bool _unix = target.Contracts.RuntimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Unix;

    public bool Unwind(ref AMD64Context context)
    {
        ulong branchTarget;
        uint epilogueOffset = 0;
        TargetPointer establisherFrame;
        uint frameOffset;
        uint frameRegister = 0;
        uint index;
        bool inEpilogue = false;
        Data.RuntimeFunction? primaryFunctionEntry;
        UnwindCode unwindOp;

        if (_eman.GetCodeBlockHandle(context.InstructionPointer.Value) is not CodeBlockHandle cbh)
            return false;

        TargetPointer controlPC = context.InstructionPointer;

        TargetPointer imageBase = _eman.GetUnwindInfoBaseAddress(cbh);
        Data.RuntimeFunction functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(_eman.GetUnwindInfo(cbh));
        if (functionEntry.EndAddress is null)
            return false;
        if (GetUnwindInfoHeader(imageBase + functionEntry.UnwindData) is not UnwindInfoHeader unwindInfo)
            return false;

        //
        // If the specified function does not use a frame pointer, then the
        // establisher frame is the contents of the stack pointer. This may
        // not actually be the real establisher frame if control left the
        // function from within the prologue. In this case the establisher
        // frame may be not required since control has not actually entered
        // the function and prologue entries cannot refer to the establisher
        // frame before it has been established, i.e., if it has not been
        // established, then no save unwind codes should be encountered during
        // the unwind operation.
        //
        // If the specified function uses a frame pointer and control left the
        // function outside of the prologue or the unwind information contains
        // a chained information structure, then the establisher frame is the
        // contents of the frame pointer.
        //
        // If the specified function uses a frame pointer and control left the
        // function from within the prologue, then the set frame pointer unwind
        // code must be looked up in the unwind codes to determine if the
        // contents of the stack pointer or the contents of the frame pointer
        // should be used for the establisher frame. This may not actually be
        // the real establisher frame. In this case the establisher frame may
        // not be required since control has not actually entered the function
        // and prologue entries cannot refer to the establisher frame before it
        // has been established, i.e., if it has not been established, then no
        // save unwind codes should be encountered during the unwind operation.
        //
        // N.B. The correctness of these assumptions is based on the ordering of
        //      unwind codes.
        //

        uint prologOffset = (uint)(controlPC - (functionEntry.BeginAddress + imageBase));
        if (unwindInfo.FrameRegister == 0)
        {
            establisherFrame = context.StackPointer;
        }
        else if ((prologOffset >= unwindInfo.SizeOfProlog) || unwindInfo.Flags.HasFlag(UnwindInfoHeader.Flag.UNW_FLAG_CHAININFO))
        {
            frameOffset = unwindInfo.FrameOffset;

            if (_unix)
            {
                // If UnwindInfo->FrameOffset == 15 (the maximum value), then there might be a UWOP_SET_FPREG_LARGE.
                // However, it is still legal for a UWOP_SET_FPREG to set UnwindInfo->FrameOffset == 15 (since this
                // was always part of the specification), so we need to look through the UnwindCode array to determine
                // if there is indeed a UWOP_SET_FPREG_LARGE. If we don't find UWOP_SET_FPREG_LARGE, then just use
                // (scaled) FrameOffset of 240, as before. (We don't verify there is a UWOP_SET_FPREG code, but we could.)
                if (frameOffset == 15)
                {
                    index = 0;
                    while (index < unwindInfo.CountOfUnwindCodes)
                    {
                        unwindOp = GetUnwindCode(unwindInfo, index);
                        if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_SET_FPREG_LARGE)
                        {
                            frameOffset = GetUnwindCode(unwindInfo, index + 1).FrameOffset;
                            frameOffset += (uint)(GetUnwindCode(unwindInfo, index + 2).FrameOffset << 16);
                            break;
                        }
                        index += unwindOp.UnwindOpSlots();
                    }
                }
            }

            establisherFrame = GetRegister(context, unwindInfo.FrameRegister);
            establisherFrame -= frameOffset * 16;
        }
        else
        {
            frameOffset = unwindInfo.FrameOffset;
            index = 0;
            while (index < unwindInfo.CountOfUnwindCodes)
            {
                unwindOp = GetUnwindCode(unwindInfo, index);
                if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_SET_FPREG)
                    break;
                if (_unix)
                {
                    if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_SET_FPREG_LARGE)
                    {
                        UnwinderAssert(unwindInfo.FrameOffset == 15, "FrameOffset should be 15 for UWOP_SET_FPREG_LARGE.");
                        frameOffset = GetUnwindCode(unwindInfo, index + 1).FrameOffset;
                        frameOffset += (uint)(GetUnwindCode(unwindInfo, index + 2).FrameOffset << 16);
                        break;
                    }
                }
                index += unwindOp.UnwindOpSlots();
            }

            if (prologOffset >= GetUnwindCode(unwindInfo, index).CodeOffset)
            {
                establisherFrame = GetRegister(context, unwindInfo.FrameRegister);
                establisherFrame -= frameOffset * 16;
            }
            else
            {
                establisherFrame = context.Rsp;
            }
        }

        //
        // Check if control left the specified function during an epilogue
        // sequence and emulate the execution of the epilogue forward and
        // return no exception handler.
        //
        // If the unwind version indicates the absence of epilogue unwind codes
        // this is done by emulating the instruction stream. Otherwise, epilogue
        // detection and emulation is performed using the function unwind codes.
        //

        if (unwindInfo.Version < 2)
        {
            TargetPointer nextByte = controlPC;

            //
            // Check for one of:
            //
            //   add rsp, imm8
            //       or
            //   add rsp, imm32
            //       or
            //   lea rsp, -disp8[fp]
            //       or
            //   lea rsp, -disp32[fp]
            //

            if ((ReadByteAt(nextByte) == SIZE64_PREFIX) &&
                (ReadByteAt(nextByte + 1) == ADD_IMM8_OP) &&
                (ReadByteAt(nextByte + 2) == 0xc4))
            {

                //
                // add rsp, imm8.
                //

                nextByte += 4;

            }
            else if ((ReadByteAt(nextByte) == SIZE64_PREFIX) &&
                     (ReadByteAt(nextByte + 1) == ADD_IMM32_OP) &&
                     (ReadByteAt(nextByte + 2) == 0xc4))
            {

                //
                // add rsp, imm32.
                //

                nextByte += 7;

            }
            else if (((ReadByteAt(nextByte) & 0xfe) == SIZE64_PREFIX) &&
                      (ReadByteAt(nextByte + 1) == LEA_OP))
            {
                frameRegister = (uint)(((ReadByteAt(nextByte) & 0x1) << 3) | (ReadByteAt(nextByte + 2) & 0x7));

                if ((frameRegister != 0) &&
                    (frameRegister == unwindInfo.FrameRegister))
                {

                    if ((ReadByteAt(nextByte + 2) & 0xf8) == 0x60)
                    {

                        //
                        // lea rsp, disp8[fp].
                        //

                        nextByte += 4;

                    }
                    else if ((ReadByteAt(nextByte + 2) & 0xf8) == 0xa0)
                    {

                        //
                        // lea rsp, disp32[fp].
                        //

                        nextByte += 7;
                    }
                }
            }

            //
            // Check for any number of:
            //
            //   pop nonvolatile-integer-register[0..15].
            //

            while (true)
            {
                if ((ReadByteAt(nextByte) & 0xf8) == POP_OP)
                {
                    nextByte += 1;
                }
                else if (IsRexPrefix(ReadByteAt(nextByte)) &&
                       ((ReadByteAt(nextByte + 1) & 0xf8) == POP_OP))
                {
                    nextByte += 2;
                }
                else
                {
                    break;
                }
            }

            //
            // A REPNE prefix may optionally precede a control transfer
            // instruction with no effect on unwinding.
            //

            if (ReadByteAt(nextByte) == REPNE_PREFIX)
            {
                nextByte += 1;
            }

            //
            // If the next instruction is a return or an appropriate jump, then
            // control is currently in an epilogue and execution of the epilogue
            // should be emulated. Otherwise, execution is not in an epilogue and
            // the prologue should be unwound.
            //

            inEpilogue = false;
            if ((ReadByteAt(nextByte) == RET_OP) ||
                (ReadByteAt(nextByte) == RET_OP_2) ||
               ((ReadByteAt(nextByte) == REP_PREFIX) && (ReadByteAt(nextByte + 1) == RET_OP)))
            {

                //
                // A return is an unambiguous indication of an epilogue.
                //

                inEpilogue = true;

            }
            else if ((ReadByteAt(nextByte) == JMP_IMM8_OP) ||
                     (ReadByteAt(nextByte) == JMP_IMM32_OP))
            {

                //
                // An unconditional branch to a target that is equal to the start of
                // or outside of this routine is logically a call to another function.
                //

                branchTarget = nextByte - imageBase;
                if (ReadByteAt(nextByte) == JMP_IMM8_OP)
                {
                    branchTarget += 2u + ReadByteAt(nextByte + 1);
                }
                else
                {
                    int delta = ReadByteAt(nextByte + 1) |
                                (ReadByteAt(nextByte + 2) << 8) |
                                (ReadByteAt(nextByte + 3) << 16) |
                                (ReadByteAt(nextByte + 4) << 24);
                    branchTarget += (uint)(5 + delta);
                }

                //
                // Determine whether the branch target refers to code within this
                // function. If not, then it is an epilogue indicator.
                //
                // A branch to the start of self implies a recursive call, so
                // is treated as an epilogue.
                //

                if (branchTarget < functionEntry.BeginAddress ||
                    branchTarget >= functionEntry.EndAddress)
                {

                    //
                    // The branch target is outside of the region described by
                    // this function entry.  See whether it is contained within
                    // an indirect function entry associated with this same
                    // function.
                    //
                    // If not, then the branch target really is outside of
                    // this function.
                    //

                    primaryFunctionEntry = SameFunction(
                        functionEntry,
                        imageBase,
                        branchTarget + imageBase);

                    if ((primaryFunctionEntry is null) ||
                        (branchTarget == primaryFunctionEntry.BeginAddress))
                    {
                        inEpilogue = true;
                    }

                }
                else if ((branchTarget == functionEntry.BeginAddress) &&
                        (!unwindInfo.Flags.HasFlag(UnwindInfoHeader.Flag.UNW_FLAG_CHAININFO)))
                {
                    inEpilogue = true;
                }

            }
            else if ((ReadByteAt(nextByte) == JMP_IND_OP) && (ReadByteAt(nextByte + 1) == 0x25))
            {
                //
                // An unconditional jump indirect.
                //
                // This is a jmp outside of the function, probably a tail call
                // to an import function.
                //

                inEpilogue = true;
            }
            else if (((ReadByteAt(nextByte) & 0xf8) == SIZE64_PREFIX) &&
                    (ReadByteAt(nextByte + 1) == 0xff) &&
                    (ReadByteAt(nextByte + 2) & 0x38) == 0x20)
            {
                //
                // This is an indirect jump opcode: 0x48 0xff /4.  The 64-bit
                // flag (REX.W) is always redundant here, so its presence is
                // overloaded to indicate a branch out of the function - a tail
                // call.
                //
                // Such an opcode is an unambiguous epilogue indication.
                //

                inEpilogue = true;
            }

            if (inEpilogue)
            {
                nextByte = controlPC;

                //
                // Emulate one of (if any):
                //
                //   add rsp, imm8
                //       or
                //   add rsp, imm32
                //       or
                //   lea rsp, disp8[frame-register]
                //       or
                //   lea rsp, disp32[frame-register]
                //

                if ((ReadByteAt(nextByte) & 0xf8) == SIZE64_PREFIX)
                {

                    if (ReadByteAt(nextByte + 1) == ADD_IMM8_OP)
                    {

                        //
                        // add rsp, imm8.
                        //

                        context.Rsp += ReadByteAt(nextByte + 3);
                        nextByte += 4;

                    }
                    else if (ReadByteAt(nextByte + 1) == ADD_IMM32_OP)
                    {

                        //
                        // add rsp, imm32.
                        //

                        int displacement = ReadByteAt(nextByte + 3) |
                                          (ReadByteAt(nextByte + 4) << 8) |
                                          (ReadByteAt(nextByte + 5) << 16) |
                                          (ReadByteAt(nextByte + 6) << 24);
                        context.Rsp += (uint)displacement;
                        nextByte += 7;

                    }
                    else if (ReadByteAt(nextByte + 1) == LEA_OP)
                    {
                        if ((ReadByteAt(nextByte + 2) & 0xf8) == 0x60)
                        {

                            //
                            // lea rsp, disp8[frame-register].
                            //

                            context.Rsp = GetRegister(context, (byte)frameRegister);
                            context.Rsp += ReadByteAt(nextByte + 3);
                            nextByte += 4;

                        }
                        else if ((ReadByteAt(nextByte + 2) & 0xf8) == 0xa0)
                        {

                            //
                            // lea rsp, disp32[frame-register].
                            //

                            int displacement = ReadByteAt(nextByte + 3) |
                                            (ReadByteAt(nextByte + 4) << 8) |
                                            (ReadByteAt(nextByte + 5) << 16) |
                                            (ReadByteAt(nextByte + 6) << 24);
                            context.Rsp = GetRegister(context, (byte)frameRegister);
                            context.Rsp += (uint)displacement;
                            nextByte += 7;
                        }
                    }
                }

                //
                // Emulate any number of (if any):
                //
                //   pop nonvolatile-integer-register.
                //

                while (true)
                {
                    if ((ReadByteAt(nextByte) & 0xf8) == POP_OP)
                    {

                        //
                        // pop nonvolatile-integer-register[0..7]
                        //

                        byte registerNumber = (byte)(ReadByteAt(nextByte + 2) & 0x7);
                        SetRegister(ref context, registerNumber, _target.Read<ulong>(context.Rsp));

                        context.Rsp += 8;
                        nextByte += 1;
                    }
                    else if (IsRexPrefix(ReadByteAt(nextByte)) &&
                            (ReadByteAt(nextByte + 1) & 0xf8) == POP_OP)
                    {

                        //
                        // pop nonvolatile-integer-register[8..15]
                        //

                        byte registerNumber = (byte)(((ReadByteAt(nextByte) & 1) << 3) | (ReadByteAt(nextByte + 1) & 0x7));
                        SetRegister(ref context, registerNumber, _target.Read<ulong>(context.Rsp));

                        context.Rsp += 8;
                        nextByte += 2;
                    }
                    else
                    {
                        break;
                    }
                }

                //
                // Emulate return and return null exception handler.
                //
                // Note: This instruction might in fact be a jmp, however
                //       we want to emulate a return regardless.
                //

                context.Rip = _target.Read<ulong>(context.Rsp);
                context.Rsp += 8;
                return true;
            }
        }
        else if (unwindInfo.CountOfUnwindCodes != 0)
        {
            UnwinderAssert(unwindInfo.Version >= 2);

            //
            // Capture the first unwind code and check if it is an epilogue code.
            // If it is not an epilogue code, the current function entry does not
            // contain any epilogues (it could represent a body region of a
            // separated function or it could represent a function which never
            // returns).
            //

            unwindOp = GetUnwindCode(unwindInfo, 0);
            if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_EPILOG)
            {
                uint epilogueSize = unwindOp.CodeOffset;

                UnwinderAssert(epilogueSize != 0);

                //
                // If the low bit of the OpInfo field of the first epilogue code
                // is set, the function has a single epilogue at the end of the
                // function. Otherwise, subsequent epilogue unwind codes indicate
                // the offset of the epilogue(s) from the function end and the
                // relative PC must be compared against each epilogue record.
                //
                // N.B. The relative instruction pointer may not be within the
                //      bounds of the runtime function entry if control left the
                //      function in a region described by an indirect function
                //      entry. Such a region cannot contain any epilogues.
                //

                uint relativePC = (uint)(controlPC - imageBase);
                if ((unwindOp.OpInfo & 0x1) != 0)
                {
                    epilogueOffset = functionEntry.EndAddress.Value - epilogueSize;
                    if (relativePC - epilogueOffset < epilogueSize)
                    {
                        inEpilogue = true;
                    }
                }

                if (!inEpilogue)
                {
                    for (uint i = 1; i < unwindInfo.CountOfUnwindCodes; i += 1)
                    {
                        unwindOp = GetUnwindCode(unwindInfo, i);

                        if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_EPILOG)
                        {
                            epilogueOffset = unwindOp.CodeOffset +
                                unwindOp.OpInfo * 256u;

                            //
                            // An epilogue offset of 0 indicates that this is
                            // a padding entry (the number of epilogue codes
                            // is a multiple of 2).
                            //

                            if (epilogueOffset == 0)
                            {
                                break;
                            }

                            epilogueOffset = functionEntry.EndAddress.Value - epilogueOffset;
                            if (relativePC - epilogueOffset < epilogueSize)
                            {
                                UnwinderAssert(epilogueOffset != functionEntry.EndAddress.Value);
                                inEpilogue = true;
                                break;
                            }

                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (inEpilogue)
                {
                    return UnwindEpilogue(
                        ref context,
                        controlPC,
                        imageBase,
                        functionEntry,
                        epilogueOffset);
                }
            }
        }

        //
        // Control left the specified function outside an epilogue. Unwind the
        // subject function and any chained unwind information.
        //

        if (!UnwindPrologue(ref context, controlPC, imageBase, establisherFrame, functionEntry))
            return false;

        //
        // If control left the specified function outside of the prologue and
        // the function has a handler that matches the specified type, then
        // return the address of the language specific exception handler.
        // Otherwise, return NULL.
        //

        // The cDAC doesn't care about handlers and therefore that logic is omitted

        return true;
    }

    private bool UnwindEpilogue(
        ref AMD64Context context,
        TargetPointer controlPC,
        TargetPointer imageBase,
        Data.RuntimeFunction functionEntry,
        uint epilogueOffset)
    {
        //
        // A canonical epilogue sequence consists of the following operations:
        //
        // 1. Optional cleanup of fixed and dynamic stack allocations, which is
        //    considered to be outside of the epilogue region.
        //
        //    add rsp, imm
        //        or
        //    lea rsp, disp[fp]
        //
        // 2. Zero or more pop nonvolatile-integer-register[0..15] instructions,
        //    which are unwound using the corresponding UWOP_PUSH_NONVOL opcodes.
        //
        //    pop r64
        //        or
        //    REX.R pop r64
        //
        // 3. An optional one-byte pop r64 to a volatile register to clean up an
        //    RFLAGS register pushed with pushfq. This is marked with a
        //    UWOP_ALLOC_SMALL 8 opcode.
        //
        //    pop rcx
        //
        // 4. A control transfer instruction (ret or jump). In both cases, there
        //    will be no prologue unwind codes remaining after the previous set of
        //    recognized operations are emulated.
        //
        //    ret 0
        //        or
        //    jmp imm
        //        or
        //    jmp [target]
        //        or
        //    iretq
        //
        // N.B. The correctness of these assumptions is based on the ordering
        //      of unwind codes and the mirroring of epilogue and prologue
        //      regions.
        //
        // Find the function's primary entry, which contains the relevant frame
        // adjustment unwind codes.
        //
        // Locate the first push unwind code. This code requires that all pushes
        // occur within a single function entry, though not necessarily within the
        // root function entry of a chained function.
        //

        uint relativePC = (uint)(controlPC - imageBase);
        uint offsetIntoEpilogue = relativePC - epilogueOffset;

        UnwindInfoHeader? unwindInfo;
        uint chainCount = 0;
        uint firstPushIndex;
        UnwindCode unwindOp = default;
        while (true)
        {
            unwindInfo = GetUnwindInfoHeader(functionEntry.UnwindData + imageBase);
            if (unwindInfo is null)
                return false;

            firstPushIndex = 0;
            while (firstPushIndex < unwindInfo.Value.CountOfUnwindCodes)
            {
                unwindOp = GetUnwindCode(unwindInfo.Value, firstPushIndex);
                if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_PUSH_NONVOL ||
                    unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_PUSH_MACHFRAME)
                {
                    break;
                }

                firstPushIndex += unwindOp.UnwindOpSlots();
            }

            if (firstPushIndex < unwindInfo.Value.CountOfUnwindCodes)
            {
                break;
            }

            //
            // If a chained parent function entry exists, continue looking for
            // push opcodes in the parent.
            //

            if (!unwindInfo.Value.Flags.HasFlag(UnwindInfoHeader.Flag.UNW_FLAG_CHAININFO))
            {
                break;
            }

            chainCount++;
            if (chainCount > UNWIND_CHAIN_LIMIT)
            {
                // Too many chained unwind entries, stop unwinding.
                return false;
            }

            functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(unwindInfo.Value.GetChainedEntryAddress());
        }

        //
        // Unwind any push codes that have not already been reversed by the
        // epilogue.
        //

        uint currentOffset = 0;
        uint index;
        for (index = firstPushIndex; index < unwindInfo.Value.CountOfUnwindCodes; index++)
        {
            unwindOp = GetUnwindCode(unwindInfo.Value, index);
            if (unwindOp.UnwindOp != UnwindCode.OpCodes.UWOP_PUSH_NONVOL)
            {
                break;
            }

            if (currentOffset >= offsetIntoEpilogue)
            {
                SetRegister(ref context, unwindOp.OpInfo, _target.Read<ulong>(context.Rsp));
                context.Rsp += 8;
            }

            //
            // POP r64 is encoded as (58h + r64) for the lower 8 general-purpose
            // registers and REX.R, (58h + r64) for r8 - r15.
            //

            currentOffset += 1;
            if (unwindOp.OpInfo >= 8)
            {
                currentOffset += 1;
            }
        }

        //
        // Check for an UWOP_ALLOC_SMALL 8 directive, which corresponds to a push
        // of the FLAGS register.
        //

        if ((index < unwindInfo.Value.CountOfUnwindCodes) &&
            (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_ALLOC_SMALL) && (unwindOp.OpInfo == 0))
        {

            if (currentOffset >= offsetIntoEpilogue)
            {
                context.Rsp += 8;
            }

            // currentOffset += 1;
            index += 1;
        }

        //
        // Check for a machine frame.
        //

        if (index < unwindInfo.Value.CountOfUnwindCodes)
        {
            unwindOp = GetUnwindCode(unwindInfo.Value, index);
            if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_PUSH_MACHFRAME)
            {
                context.Rip = _target.ReadPointer(context.Rsp);
                context.Rsp = _target.ReadPointer(context.Rsp + (3 * 8));
                return true;
            }

            Debug.Fail("Any remaining operation must be a machine frame");
        }

        //
        // Emulate a return operation.
        //

        context.Rip = _target.ReadPointer(context.Rsp);
        context.Rsp += 8;
        return true;
    }

    private bool UnwindPrologue(
        ref AMD64Context context,
        TargetPointer controlPC,
        TargetPointer imageBase,
        TargetPointer frameBase,
        Data.RuntimeFunction functionEntry)
    {
        uint chainCount = 0;

        while (true)
        {
            uint index = 0;
            bool machineFrame = false;
            uint prologOffset = (uint)(controlPC - (functionEntry.BeginAddress + imageBase));

            if (GetUnwindInfoHeader(imageBase + functionEntry.UnwindData) is not UnwindInfoHeader unwindInfo)
                return false;

            while (index < unwindInfo.CountOfUnwindCodes)
            {
                //
                // If the prologue offset is greater than the next unwind code
                // offset, then simulate the effect of the unwind code.
                //

                UnwindCode unwindOp = GetUnwindCode(unwindInfo, index);

                if (_unix)
                {
                    if (unwindOp.UnwindOp > UnwindCode.OpCodes.UWOP_SET_FPREG_LARGE)
                    {
                        Debug.Fail("Expected unwind code");
                        return false;
                    }
                }
                else
                {
                    if (unwindOp.UnwindOp == UnwindCode.OpCodes.UWOP_SET_FPREG_LARGE)
                    {
                        Debug.Fail("Expected unwind code");
                        return false;
                    }
                }

                if (prologOffset >= unwindOp.CodeOffset)
                {
                    switch (unwindOp.UnwindOp)
                    {
                        //
                        // Push nonvolatile integer register.
                        //
                        // The operation information is the register number of
                        // the register than was pushed.
                        //
                        case UnwindCode.OpCodes.UWOP_PUSH_NONVOL:
                            {
                                SetRegister(ref context, unwindOp.OpInfo, _target.ReadPointer(context.Rsp));
                                context.Rsp += 8;
                                break;
                            }

                        //
                        // Allocate a large sized area on the stack.
                        //
                        // The operation information determines if the size is
                        // 16- or 32-bits.
                        //
                        case UnwindCode.OpCodes.UWOP_ALLOC_LARGE:
                            {
                                index++;
                                UnwindCode nextUnwindOp = GetUnwindCode(unwindInfo, index);
                                uint frameOffset = nextUnwindOp.FrameOffset;
                                if (unwindOp.OpInfo != 0)
                                {
                                    index++;
                                    nextUnwindOp = GetUnwindCode(unwindInfo, index);
                                    frameOffset += (uint)(nextUnwindOp.FrameOffset << 16);
                                }
                                else
                                {
                                    // The 16-bit form is scaled.
                                    frameOffset *= 8;
                                }

                                context.Rsp += frameOffset;
                                break;
                            }

                        //
                        // Allocate a small sized area on the stack.
                        //
                        // The operation information is the size of the unscaled
                        // allocation size (8 is the scale factor) minus 8.
                        //
                        case UnwindCode.OpCodes.UWOP_ALLOC_SMALL:
                            {
                                context.Rsp += (unwindOp.OpInfo * 8u) + 8u;
                                break;
                            }

                        //
                        // Establish the frame pointer register.
                        //
                        // The operation information is not used.
                        //
                        case UnwindCode.OpCodes.UWOP_SET_FPREG:
                            {
                                context.Rsp = GetRegister(context, unwindInfo.FrameRegister);
                                context.Rsp -= unwindInfo.FrameOffset * 16u;
                                break;
                            }

                        //
                        // Establish the frame pointer register using a large size displacement.
                        // UNWIND_INFO.FrameOffset must be 15 (the maximum value, corresponding to a scaled
                        // offset of 15 * 16 == 240). The next two codes contain a 32-bit offset, which
                        // is also scaled by 16, since the stack must remain 16-bit aligned.
                        // Unix only.
                        //
                        case UnwindCode.OpCodes.UWOP_SET_FPREG_LARGE:
                            {
                                UnwinderAssert(_unix);
                                UnwinderAssert(unwindInfo.FrameOffset == 15);
                                uint frameOffset = GetUnwindCode(unwindInfo, index + 1).FrameOffset;
                                frameOffset += (uint)(GetUnwindCode(unwindInfo, index + 2).FrameOffset << 16);
                                UnwinderAssert((frameOffset & 0xF0000000) == 0);

                                context.Rsp = GetRegister(context, unwindInfo.FrameRegister);
                                context.Rsp -= frameOffset * 16;

                                index += 2;
                                break;
                            }

                        //
                        // Save nonvolatile integer register on the stack using a
                        // 16-bit displacement.
                        //
                        // The operation information is the register number.
                        //
                        case UnwindCode.OpCodes.UWOP_SAVE_NONVOL:
                            {
                                uint frameOffset = GetUnwindCode(unwindInfo, index + 1).FrameOffset * 8u;
                                SetRegister(ref context, unwindOp.OpInfo, _target.ReadPointer(frameBase + frameOffset));
                                index += 1;
                                break;
                            }

                        //
                        // Function epilog marker (ignored for prologue unwind).
                        //
                        case UnwindCode.OpCodes.UWOP_EPILOG:
                            index += 1;
                            break;

                        //
                        // Spare unused codes.
                        //
                        case UnwindCode.OpCodes.UWOP_SPARE_CODE:
                            UnwinderAssert(false);
                            index += 2;
                            break;

                        //
                        // Save a nonvolatile XMM(128) register on the stack using a
                        // 16-bit displacement.
                        //
                        // The operation information is the register number.
                        //
                        case UnwindCode.OpCodes.UWOP_SAVE_XMM128:
                            index += 1;
                            // Operation not currently supported by the cDAC.
                            break;

                        //
                        // Save a nonvolatile XMM(128) register on the stack using
                        // a 32-bit displacement.
                        //
                        // The operation information is the register number.
                        //
                        case UnwindCode.OpCodes.UWOP_SAVE_XMM128_FAR:
                            index += 2;
                            // Operation not currently supported by the cDAC.
                            break;

                        //
                        // Push a machine frame on the stack.
                        //
                        // The operation information determines whether the
                        // machine frame contains an error code or not.
                        //
                        case UnwindCode.OpCodes.UWOP_PUSH_MACHFRAME:
                            {
                                machineFrame = false;
                                TargetPointer returnAddressPtr = context.Rsp;
                                TargetPointer stackAddressPtr = context.Rsp + (3 * 8);
                                if (unwindOp.OpInfo != 0)
                                {
                                    returnAddressPtr += (uint)_target.PointerSize;
                                    stackAddressPtr += (uint)_target.PointerSize;
                                }

                                context.Rip = _target.ReadPointer(returnAddressPtr);
                                context.Rsp = _target.ReadPointer(stackAddressPtr);
                                break;
                            }

                        default:
                            Debug.Fail("Unexpected unwind operation code.");
                            break;
                    }

                    index += 1;
                }
                else
                {
                    index += unwindOp.UnwindOpSlots();
                }
            }

            //
            // If chained unwind information is specified, then set the function
            // entry address to the chained function entry and continue the scan.
            // Otherwise, determine the return address if a machine frame was not
            // encountered during the scan of the unwind codes and terminate the
            // scan.
            //

            if (unwindInfo.Flags.HasFlag(UnwindInfoHeader.Flag.UNW_FLAG_CHAININFO))
            {
                functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(unwindInfo.GetChainedEntryAddress());
            }
            else
            {
                if (!machineFrame)
                {
                    context.Rip = _target.ReadPointer(context.Rsp);
                    context.Rsp += (uint)_target.PointerSize;
                }

                break;
            }

            //
            // Limit the number of iterations possible for chained function table
            // entries.
            //
            chainCount += 1;
            UnwinderAssert(chainCount <= UNWIND_CHAIN_LIMIT);
        }

        return true;
    }


    #region Unwind Helpers

    /// <summary>
    /// Well known UnwindInfo header for AMD64.
    /// </summary>
    private struct UnwindInfoHeader(TargetPointer address, uint header)
    {
        [Flags]
        public enum Flag : byte
        {
            UNW_FLAG_NHANDLER = 0x0,
            UNW_FLAG_EHANDLER = 0x1,
            UNW_FLAG_UHANDLER = 0x2,
            UNW_FLAG_CHAININFO = 0x4,
        }

        private TargetPointer _address = address;
        public byte Version = (byte)(header & 0x7);                     // bits 0-2 (3 bits)
        public Flag Flags = (Flag)((header >> 3) & 0x1F);               // bits 3-7 (5 bits)
        public byte SizeOfProlog = (byte)((header >> 8) & 0xFF);        // bits 8-15 (8 bits)
        public byte CountOfUnwindCodes = (byte)((header >> 16) & 0xFF); // bits 16-23 (8 bits)
        public byte FrameRegister = (byte)((header >> 24) & 0xF);       // bits 24-27 (4 bits)
        public byte FrameOffset = (byte)((header >> 28) & 0xF);         // bits 28-31 (4 bits)

        public TargetPointer GetUnwindCodeAddress(uint index)
        {
            if (index >= CountOfUnwindCodes)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range for unwind codes.");

            TargetPointer unwindCodeAddress = _address + sizeof(uint) /* size of header */ + (index * sizeof(ushort) /* size of unwind code */);
            return unwindCodeAddress;
        }

        public TargetPointer GetChainedEntryAddress()
        {
            if (!Flags.HasFlag(Flag.UNW_FLAG_CHAININFO))
                throw new InvalidOperationException("This unwind info does not contain a chained entry.");

            uint index = CountOfUnwindCodes;
            if ((index & 0x1) != 0)
                index++;

            TargetPointer chainedEntryAddress = _address + sizeof(uint) /* size of header */ + (index * sizeof(ushort) /* size of unwind code */);
            return chainedEntryAddress;
        }
    }

    private struct UnwindCode(ushort value)
    {
        public enum OpCodes : byte
        {
            UWOP_PUSH_NONVOL = 0,
            UWOP_ALLOC_LARGE,
            UWOP_ALLOC_SMALL,
            UWOP_SET_FPREG,
            UWOP_SAVE_NONVOL,
            UWOP_SAVE_NONVOL_FAR,
            UWOP_EPILOG,
            UWOP_SPARE_CODE,
            UWOP_SAVE_XMM128,
            UWOP_SAVE_XMM128_FAR,
            UWOP_PUSH_MACHFRAME,

            // UWOP_SET_FPREG_LARGE is a CLR Unix-only extension to the Windows AMD64 unwind codes.
            // It is not part of the standard Windows AMD64 unwind codes specification.
            // UWOP_SET_FPREG allows for a maximum of a 240 byte offset between RSP and the
            // frame pointer, when the frame pointer is established. UWOP_SET_FPREG_LARGE
            // has a 32-bit range scaled by 16. When UWOP_SET_FPREG_LARGE is used,
            // UNWIND_INFO.FrameRegister must be set to the frame pointer register, and
            // UNWIND_INFO.FrameOffset must be set to 15 (its maximum value). UWOP_SET_FPREG_LARGE
            // is followed by two UNWIND_CODEs that are combined to form a 32-bit offset (the same
            // as UWOP_SAVE_NONVOL_FAR). This offset is then scaled by 16. The result must be less
            // than 2^32 (that is, the top 4 bits of the unscaled 32-bit number must be zero). This
            // result is used as the frame pointer register offset from RSP at the time the frame pointer
            // is established. Either UWOP_SET_FPREG or UWOP_SET_FPREG_LARGE can be used, but not both.
            UWOP_SET_FPREG_LARGE,
        }

        public byte CodeOffset = (byte)(value & 0xFF);           // bits 0-8 (8 bits)
        public OpCodes UnwindOp = (OpCodes)((value >> 8) & 0xF); // bits 9-12 (4 bits)
        public byte OpInfo = (byte)((value >> 12) & 0xF);        // bits 13-16 (4 bits)

        public ushort FrameOffset = value;

        public uint UnwindOpSlots()
        {
            UnwinderAssert(UnwindOp != OpCodes.UWOP_SPARE_CODE);
            return UnwindOp switch
            {
                OpCodes.UWOP_PUSH_NONVOL => 1u,
                OpCodes.UWOP_ALLOC_LARGE => OpInfo != 0 ? 3u : 2u,
                OpCodes.UWOP_ALLOC_SMALL => 1u,
                OpCodes.UWOP_SET_FPREG => 1u,
                OpCodes.UWOP_SAVE_NONVOL => 2u,
                OpCodes.UWOP_SAVE_NONVOL_FAR => 3u,
                OpCodes.UWOP_EPILOG => 2u,
                // previously 64-bit UWOP_SAVE_XMM_FAR
                OpCodes.UWOP_SPARE_CODE => 3u,
                OpCodes.UWOP_SAVE_XMM128 => 2u,
                OpCodes.UWOP_SAVE_XMM128_FAR => 3u,
                OpCodes.UWOP_PUSH_MACHFRAME => 1u,
                OpCodes.UWOP_SET_FPREG_LARGE => 3u,
                _ => throw new InvalidOperationException($"Unsupported unwind operation: {UnwindOp}"),
            };
        }
    }

    private UnwindInfoHeader? GetUnwindInfoHeader(TargetPointer unwindInfoAddress)
    {
        try
        {
            Data.UnwindInfo unwindInfoData = _target.ProcessedData.GetOrAdd<Data.UnwindInfo>(unwindInfoAddress);

            if (unwindInfoData.Header is not uint headerValue)
                return null;

            return new UnwindInfoHeader(unwindInfoAddress, headerValue);
        }
        catch (InvalidOperationException)
        {
            // InvalidOperationException thrown if failed to read memory
            return null;
        }
    }

    private UnwindCode GetUnwindCode(UnwindInfoHeader unwindInfo, uint index) =>
        new UnwindCode(_target.Read<ushort>(unwindInfo.GetUnwindCodeAddress(index)));

    private Data.RuntimeFunction LookupPrimaryFunctionEntry(Data.RuntimeFunction functionEntry, TargetPointer imageBase)
    {
        uint chainCount = 0;

        while (true)
        {
            UnwindInfoHeader? unwindInfo = GetUnwindInfoHeader(imageBase + functionEntry.UnwindData);

            if (unwindInfo is null || !unwindInfo.Value.Flags.HasFlag(UnwindInfoHeader.Flag.UNW_FLAG_CHAININFO))
            {
                break;
            }

            functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(unwindInfo.Value.GetChainedEntryAddress());

            //
            // Limit the number of iterations possible for chained function table
            // entries.
            //

            chainCount += 1;
            UnwinderAssert(chainCount <= UNWIND_CHAIN_LIMIT, "Unwind chain limit exceeded.");
        }

        return functionEntry;
    }

    private Data.RuntimeFunction? SameFunction(Data.RuntimeFunction functionEntry, TargetPointer imageBase, TargetPointer controlPC)
    {
        Data.RuntimeFunction primaryFunctionEntry = LookupPrimaryFunctionEntry(functionEntry, imageBase);

        if (_eman.GetCodeBlockHandle(controlPC.Value) is not CodeBlockHandle cbh)
            return null;

        TargetPointer targetImageBase = _eman.GetUnwindInfoBaseAddress(cbh);
        Data.RuntimeFunction targetFunctionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(_eman.GetUnwindInfo(cbh));

        targetFunctionEntry = LookupPrimaryFunctionEntry(targetFunctionEntry, targetImageBase);

        if (primaryFunctionEntry.BeginAddress == targetFunctionEntry.BeginAddress)
        {
            return primaryFunctionEntry;
        }
        else
        {
            return null;
        }
    }

    #endregion
    #region Helpers

    private byte ReadByteAt(TargetPointer address) => _target.Read<byte>(address);

    private static bool IsRexPrefix(byte b) => (b & 0xf0) == 0x40;

    private static TargetPointer GetRegister(AMD64Context context, byte register) => register switch
    {
        0 => context.Rax,
        1 => context.Rcx,
        2 => context.Rdx,
        3 => context.Rbx,
        4 => context.Rsp,
        5 => context.Rbp,
        6 => context.Rsi,
        7 => context.Rdi,
        8 => context.R8,
        9 => context.R9,
        10 => context.R10,
        11 => context.R11,
        12 => context.R12,
        13 => context.R13,
        14 => context.R14,
        15 => context.R15,
        _ => throw new ArgumentOutOfRangeException(nameof(register), "Invalid register number for AMD64 context.")
    };

    private static void SetRegister(ref AMD64Context context, byte register, TargetPointer value)
    {
        switch (register)
        {
            case 0: context.Rax = value; break;
            case 1: context.Rcx = value; break;
            case 2: context.Rdx = value; break;
            case 3: context.Rbx = value; break;
            case 4: context.Rsp = value; break;
            case 5: context.Rbp = value; break;
            case 6: context.Rsi = value; break;
            case 7: context.Rdi = value; break;
            case 8: context.R8 = value; break;
            case 9: context.R9 = value; break;
            case 10: context.R10 = value; break;
            case 11: context.R11 = value; break;
            case 12: context.R12 = value; break;
            case 13: context.R13 = value; break;
            case 14: context.R14 = value; break;
            case 15: context.R15 = value; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(register), "Invalid register number for AMD64 context.");
        }
    }

    private static void UnwinderAssert(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    #endregion
}
