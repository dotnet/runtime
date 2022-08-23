// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------------------------
//

//
// Stack unwinding implementation for x64.
//

//
//----------------------------------------------------------------------------

#include "pch.cpp"
#pragma hdrstop

//----------------------------------------------------------------------------
//
// Copied OS code.
//
// This must be kept in sync with the system unwinder.
// base\ntos\rtl\amd64\exdsptch.c
//
//----------------------------------------------------------------------------

//
// Lookup table providing the number of slots used by each unwind code.
//

UCHAR
DbsX64StackUnwinder::s_UnwindOpSlotTable[] =
{
    1,          // UWOP_PUSH_NONVOL
    2,          // UWOP_ALLOC_LARGE (or 3, special cased in lookup code)
    1,          // UWOP_ALLOC_SMALL
    1,          // UWOP_SET_FPREG
    2,          // UWOP_SAVE_NONVOL
    3,          // UWOP_SAVE_NONVOL_FAR
    2,          // UWOP_SAVE_XMM
    3,          // UWOP_SAVE_XMM_FAR
    2,          // UWOP_SAVE_XMM128
    3,          // UWOP_SAVE_XMM128_FAR
    1           // UWOP_PUSH_MACHFRAME
};

//
// ****** temp - defin elsewhere ******
//

#define SIZE64_PREFIX 0x48
#define ADD_IMM8_OP 0x83
#define ADD_IMM32_OP 0x81
#define JMP_IMM8_OP 0xeb
#define JMP_IMM32_OP 0xe9
#define JMP_IND_OP 0xff
#define LEA_OP 0x8d
#define REP_PREFIX 0xf3
#define POP_OP 0x58
#define RET_OP 0xc3
#define RET_OP_2 0xc2

#define IS_REX_PREFIX(x) (((x) & 0xf0) == 0x40)

HRESULT
DbsX64StackUnwinder::UnwindPrologue(
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ ULONG64 FrameBase,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PAMD64_CONTEXT ContextRecord
    )

/*++

Routine Description:

    This function processes unwind codes and reverses the state change
    effects of a prologue. If the specified unwind information contains
    chained unwind information, then that prologue is unwound recursively.
    As the prologue is unwound state changes are recorded in the specified
    context structure and optionally in the specified context pointers
    structures.

Arguments:

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified
        function.

    FrameBase - Supplies the base of the stack frame subject function stack
         frame.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function.

    ContextRecord - Supplies the address of a context record.

--*/

{

    HRESULT Status = E_UNEXPECTED;
    ULONG64 FloatingAddress;
    PAMD64_M128 FloatingRegister;
    ULONG FrameOffset;
    ULONG Index;
    ULONG64 IntegerAddress;
    PULONG64 IntegerRegister;
    BOOLEAN MachineFrame;
    ULONG OpInfo;
    ULONG PrologOffset;
    ULONG64 ReturnAddress;
    ULONG64 StackAddress;
    ULONG64 UnwindInfoBuffer[32];
    PAMD64_UNWIND_INFO UnwindInfo;
    ULONG UnwindOp;

    //
    // Process the unwind codes.
    //

    FloatingRegister = &ContextRecord->Xmm0;
    IntegerRegister = &ContextRecord->Rax;
    Index = 0;
    MachineFrame = FALSE;
    PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));

    m_Services->Status(1, "Prol: RIP %I64X, 0x%X bytes in function at %I64X\n",
                       ControlPc, PrologOffset,
                       FunctionEntry->BeginAddress + ImageBase);
    m_Services->Status(1, "Prol: Read unwind info at %I64X\n",
                       FunctionEntry->UnwindInfoAddress + ImageBase);

    if ((Status =
         GetUnwindInfo(ImageBase, FunctionEntry->UnwindInfoAddress,
                       false,
                       UnwindInfoBuffer, sizeof(UnwindInfoBuffer),
                       (PVOID*)&UnwindInfo)) != S_OK) {
        m_Services->Status(1, "Prol: Unable to read unwind info\n");
        return Status;
    }

    m_Services->Status(1, "  Unwind info has 0x%X codes\n",
                       UnwindInfo->CountOfCodes);

    while (Index < UnwindInfo->CountOfCodes) {

        m_Services->Status(1, "  %02X: Code %X offs %03X, RSP %I64X\n",
             Index, UnwindInfo->UnwindCode[Index].UnwindOp,
             UnwindInfo->UnwindCode[Index].CodeOffset,
             ContextRecord->Rsp);

        //
        // If the prologue offset is greater than the next unwind code offset,
        // then simulate the effect of the unwind code.
        //

        UnwindOp = UnwindInfo->UnwindCode[Index].UnwindOp;
        if (UnwindOp > AMD64_UWOP_PUSH_MACHFRAME) {
            m_Services->Status(1, "Prol: Invalid unwind op %X at index %X\n",
                 UnwindOp, Index);
            goto Fail;
        }

        OpInfo = UnwindInfo->UnwindCode[Index].OpInfo;
        if (PrologOffset >= UnwindInfo->UnwindCode[Index].CodeOffset) {
            switch (UnwindOp) {

                //
                // Push nonvolatile integer register.
                //
                // The operation information is the register number of the
                // register than was pushed.
                //

            case AMD64_UWOP_PUSH_NONVOL:
                IntegerAddress = ContextRecord->Rsp;
                if ((Status = m_Services->
                     ReadAllMemory(IntegerAddress,
                                   &IntegerRegister[OpInfo],
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory "
                                       "read failed at %I64X\n",
                                       UnwindOp, IntegerAddress);
                    goto Fail;
                }

                ContextRecord->Rsp += 8;
                break;

                //
                // Allocate a large sized area on the stack.
                //
                // The operation information determines if the size is
                // 16- or 32-bits.
                //

            case AMD64_UWOP_ALLOC_LARGE:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset;
                if (OpInfo != 0) {
                    Index += 1;
                    FrameOffset += (UnwindInfo->UnwindCode[Index].FrameOffset << 16);
                } else {
                    // The 16-bit form is scaled.
                    FrameOffset *= 8;
                }

                ContextRecord->Rsp += FrameOffset;
                break;

                //
                // Allocate a small sized area on the stack.
                //
                // The operation information is the size of the unscaled
                // allocation size (8 is the scale factor) minus 8.
                //

            case AMD64_UWOP_ALLOC_SMALL:
                ContextRecord->Rsp += (OpInfo * 8) + 8;
                break;

                //
                // Establish the frame pointer register.
                //
                // The operation information is not used.
                //

            case AMD64_UWOP_SET_FPREG:
                ContextRecord->Rsp = IntegerRegister[UnwindInfo->FrameRegister];
                ContextRecord->Rsp -= UnwindInfo->FrameOffset * 16;
                break;

                //
                // Save nonvolatile integer register on the stack using a
                // 16-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_NONVOL:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 8;
                IntegerAddress = FrameBase + FrameOffset;
                if ((Status = m_Services->
                    ReadAllMemory(IntegerAddress,
                                  &IntegerRegister[OpInfo],
                                  sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, IntegerAddress);
                    goto Fail;
                }
                break;

                //
                // Save nonvolatile integer register on the stack using a
                // 32-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_NONVOL_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                IntegerAddress = FrameBase + FrameOffset;
                if ((Status = m_Services->
                     ReadAllMemory(IntegerAddress,
                                   &IntegerRegister[OpInfo],
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, IntegerAddress);
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(64) register on the stack using a
                // 16-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_XMM:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 8;
                FloatingAddress = FrameBase + FrameOffset;
                FloatingRegister[OpInfo].High = 0;
                if ((Status = m_Services->
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo].Low,
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, FloatingAddress);
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(64) register on the stack using a
                // 32-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_XMM_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                FloatingAddress = FrameBase + FrameOffset;
                FloatingRegister[OpInfo].High = 0;
                if ((Status = m_Services->
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo].Low,
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, FloatingAddress);
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(128) register on the stack using a
                // 16-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_XMM128:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 16;
                FloatingAddress = FrameBase + FrameOffset;
                if ((Status = m_Services->
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo],
                                   sizeof(AMD64_M128))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, FloatingAddress);
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(128) register on the stack using a
                // 32-bit displacement.
                //
                // The operation information is the register number.
                //

            case AMD64_UWOP_SAVE_XMM128_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                FloatingAddress = FrameBase + FrameOffset;
                if ((Status = m_Services->
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo],
                                   sizeof(AMD64_M128))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory read "
                                       "failed at %I64X\n",
                                       UnwindOp, FloatingAddress);
                    goto Fail;
                }
                break;

                //
                // Push a machine frame on the stack.
                //
                // The operation information determines whether the machine
                // frame contains an error code or not.
                //

            case AMD64_UWOP_PUSH_MACHFRAME:
                MachineFrame = TRUE;
                ReturnAddress = ContextRecord->Rsp;
                StackAddress = ContextRecord->Rsp + (3 * 8);
                if (OpInfo != 0) {
                    ReturnAddress += 8;
                    StackAddress +=  8;
                }

                m_RestartFrame = true;
                m_TrapAddr = ReturnAddress -
                    offsetof(AMD64_KTRAP_FRAME, Rip);

                if ((Status = m_Services->
                     ReadAllMemory(ReturnAddress,
                                   &ContextRecord->Rip,
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory "
                                       "read 1 failed at %I64X\n",
                                       UnwindOp, ReturnAddress);
                    goto Fail;
                }
                if ((Status = m_Services->
                     ReadAllMemory(StackAddress,
                                   &ContextRecord->Rsp,
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Prol: Op %X memory "
                                       "read 2 failed at %I64X\n",
                                       UnwindOp, StackAddress);
                    goto Fail;
                }
                break;

                //
                // Unused codes.
                //

            default:
                break;
            }

            Index += 1;

        } else {

            //
            // Skip this unwind operation by advancing the slot index by the
            // number of slots consumed by this operation.
            //

            Index += s_UnwindOpSlotTable[UnwindOp];

            //
            // Special case any unwind operations that can consume a variable
            // number of slots.
            //

            switch (UnwindOp) {

                //
                // A non-zero operation information indicates that an
                // additional slot is consumed.
                //

            case AMD64_UWOP_ALLOC_LARGE:
                if (OpInfo != 0) {
                    Index += 1;
                }

                break;

                //
                // No other special cases.
                //

            default:
                break;
            }
        }
    }

    //
    // If chained unwind information is specified, then recursively unwind
    // the chained information. Otherwise, determine the return address if
    // a machine frame was not encountered during the scan of the unwind
    // codes.
    //

    if ((UnwindInfo->Flags & AMD64_UNW_FLAG_CHAININFO) != 0) {

        _PIMAGE_RUNTIME_FUNCTION_ENTRY ChainEntry;

        Index = UnwindInfo->CountOfCodes;
        if ((Index & 1) != 0) {
            Index += 1;
        }

        // GetUnwindInfo looks for CHAININFO and reads
        // the trailing RUNTIME_FUNCTION so we can just
        // directly use the data sitting in UnwindInfo.
        ChainEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)
            &UnwindInfo->UnwindCode[Index];

        m_Services->Status(1, "  Chain with entry at %I64X\n",
             FunctionEntry->UnwindInfoAddress + ImageBase +
             (ULONG64)((PUCHAR)&UnwindInfo->UnwindCode[Index] -
                       (PUCHAR)UnwindInfo));

        Status = UnwindPrologue(ImageBase,
                                ControlPc,
                                FrameBase,
                                ChainEntry,
                                ContextRecord);

        FreeUnwindInfo(UnwindInfo, UnwindInfoBuffer);
        return Status;

    } else {
        FreeUnwindInfo(UnwindInfo, UnwindInfoBuffer);

        if (MachineFrame == FALSE) {
            if ((Status = m_Services->
                 ReadAllMemory(ContextRecord->Rsp,
                               &ContextRecord->Rip,
                               sizeof(ULONG64))) != S_OK) {
                return Status;
            }
            ContextRecord->Rsp += 8;
        }

        m_Services->Status(1, "Prol: Returning with RIP %I64X, RSP %I64X\n",
                           ContextRecord->Rip, ContextRecord->Rsp);
        return S_OK;
    }

 Fail:
    FreeUnwindInfo(UnwindInfo, UnwindInfoBuffer);
    m_Services->Status(1, "Prol: Unwind failed, 0x%08X\n", Status);
    return Status;
}

HRESULT
DbsX64StackUnwinder::VirtualUnwind(
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PAMD64_CONTEXT ContextRecord,
    _Out_ PULONG64 EstablisherFrame
    )

/*++

Routine Description:

    This function virtually unwinds the specified function by executing its
    prologue code backward or its epilogue code forward.

    If a context pointers record is specified, then the address where each
    nonvolatile registers is restored from is recorded in the appropriate
    element of the context pointers record.

Arguments:

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified
        function.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function.

    ContextRecord - Supplies the address of a context record.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

--*/

{

    HRESULT Status;
    ULONG64 BranchTarget;
    LONG Displacement;
    ULONG FrameRegister;
    ULONG Index;
    LOGICAL InEpilogue;
    PULONG64 IntegerRegister;
    PUCHAR NextByte;
    _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryFunctionEntry;
    ULONG PrologOffset;
    ULONG RegisterNumber;
    PAMD64_UNWIND_INFO UnwindInfo;
    ULONG64 UnwindInfoBuffer[8];
    ULONG Done;
    UCHAR InstrBuffer[32];
    ULONG InstrBytes;
    ULONG Bytes;
    ULONG UnwindFrameReg;

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
    // code must be looked up in the unwind codes to detetermine if the
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

    if ((Status = GetUnwindInfo(ImageBase, FunctionEntry->UnwindInfoAddress,
                                true,
                                UnwindInfoBuffer, sizeof(UnwindInfoBuffer),
                                (PVOID*)&UnwindInfo)) != S_OK) {
        return Status;
    }

    PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));
    UnwindFrameReg = UnwindInfo->FrameRegister;
    if (UnwindFrameReg == 0) {
        *EstablisherFrame = ContextRecord->Rsp;

    } else if ((PrologOffset >= UnwindInfo->SizeOfProlog) ||
               ((UnwindInfo->Flags & AMD64_UNW_FLAG_CHAININFO) != 0)) {
        *EstablisherFrame = (&ContextRecord->Rax)[UnwindFrameReg];
        *EstablisherFrame -= UnwindInfo->FrameOffset * 16;

    } else {

        // Read all the data.
        if ((Status = GetUnwindInfo(ImageBase,
                                    FunctionEntry->UnwindInfoAddress,
                                    false,
                                    UnwindInfoBuffer,
                                    sizeof(UnwindInfoBuffer),
                                    (PVOID*)&UnwindInfo)) != S_OK) {
            return Status;
        }

        Index = 0;
        while (Index < UnwindInfo->CountOfCodes) {
            if (UnwindInfo->UnwindCode[Index].UnwindOp == AMD64_UWOP_SET_FPREG) {
                break;
            }

            Index += 1;
        }

        if (PrologOffset >= UnwindInfo->UnwindCode[Index].CodeOffset) {
            *EstablisherFrame = (&ContextRecord->Rax)[UnwindFrameReg];
            *EstablisherFrame -= UnwindInfo->FrameOffset * 16;

        } else {
            *EstablisherFrame = ContextRecord->Rsp;
        }

        FreeUnwindInfo(UnwindInfo, UnwindInfoBuffer);
    }

    if ((Status = m_Services->
         ReadMemory(ControlPc, InstrBuffer, sizeof(InstrBuffer),
                    &InstrBytes)) != S_OK) {
        m_Services->Status(1, "Unable to read instruction stream at %I64X\n",
                           ControlPc);

        // We need the code to look for epilogue ops.
        // It's very rare to be stopped in an epilogue when
        // getting a stack trace, so if we can't read the
        // code just assume we aren't in an epilogue.
        InstrBytes = 0;
    }

    //
    // If the point at which control left the specified function is in an
    // epilogue, then emulate the execution of the epilogue forward and
    // return no exception handler.
    //

    IntegerRegister = &ContextRecord->Rax;
    NextByte = InstrBuffer;
    Bytes = InstrBytes;

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

    if (Bytes >= 4 &&
        (NextByte[0] == SIZE64_PREFIX) &&
        (NextByte[1] == ADD_IMM8_OP) &&
        (NextByte[2] == 0xc4)) {

        //
        // add rsp, imm8.
        //

        NextByte += 4;
        Bytes -= 4;

    } else if (Bytes >= 7 &&
               (NextByte[0] == SIZE64_PREFIX) &&
               (NextByte[1] == ADD_IMM32_OP) &&
               (NextByte[2] == 0xc4)) {

        //
        // add rsp, imm32.
        //

        NextByte += 7;
        Bytes -= 7;

    } else if (Bytes >= 4 &&
               ((NextByte[0] & 0xf8) == SIZE64_PREFIX) &&
               (NextByte[1] == LEA_OP)) {

        FrameRegister = ((NextByte[0] & 0x7) << 3) | (NextByte[2] & 0x7);
        if ((FrameRegister != 0) &&
            (FrameRegister == UnwindFrameReg)) {
            if ((NextByte[2] & 0xf8) == 0x60) {

                //
                // lea rsp, disp8[fp].
                //

                NextByte += 4;
                Bytes -= 4;

            } else if (Bytes >= 7 &&
                       (NextByte[2] &0xf8) == 0xa0) {

                //
                // lea rsp, disp32[fp].
                //

                NextByte += 7;
                Bytes -= 7;
            }
        }
    }

    //
    // Check for any number of:
    //
    //   pop nonvolatile-integer-register[0..15].
    //

    while (TRUE) {
        if (Bytes >= 1 &&
            (NextByte[0] & 0xf8) == POP_OP) {
            NextByte += 1;
            Bytes -= 1;

        } else if (Bytes >= 2 &&
                   IS_REX_PREFIX(NextByte[0]) &&
                   ((NextByte[1] & 0xf8) == POP_OP)) {

            NextByte += 2;
            Bytes -= 2;

        } else {
            break;
        }
    }

    //
    // If the next instruction is a return or an appropriate jump, then
    // control is currently in an epilogue and execution of the epilogue
    // should be emulated. Otherwise, execution is not in an epilogue and
    // the prologue should be unwound.
    //

    InEpilogue = FALSE;
    if ((Bytes >= 1 &&
         ((NextByte[0] == RET_OP) ||
          (NextByte[0] == RET_OP_2))) ||
        (Bytes >= 2 &&
         ((NextByte[0] == REP_PREFIX) && (NextByte[1] == RET_OP)))) {

        //
        // A return is an unambiguous indication of an epilogue.
        //

        InEpilogue = TRUE;

    } else if ((Bytes >= 2 && NextByte[0] == JMP_IMM8_OP) ||
               (Bytes >= 5 && NextByte[0] == JMP_IMM32_OP)) {

        //
        // An unconditional branch to a target that is equal to the start of
        // or outside of this routine is logically a call to another function.
        //

        BranchTarget = (ULONG64)(NextByte - InstrBuffer) + ControlPc - ImageBase;
        if (NextByte[0] == JMP_IMM8_OP) {
            BranchTarget += 2 + (CHAR)NextByte[1];
        } else {
            BranchTarget += 5 + *((LONG UNALIGNED *)&NextByte[1]);
        }

        //
        // Determine whether the branch target refers to code within this
        // function. If not, then it is an epilogue indicator.
        //
        // A branch to the start of self implies a recursive call, so
        // is treated as an epilogue.
        //

        if (BranchTarget < FunctionEntry->BeginAddress ||
            BranchTarget >= FunctionEntry->EndAddress) {

            _IMAGE_RUNTIME_FUNCTION_ENTRY PrimaryEntryBuffer;

            //
            // The branch target is outside of the region described by
            // this function entry.  See whether it is contained within
            // an indirect function entry associated with this same
            // function.
            //
            // If not, then the branch target really is outside of
            // this function.
            //

            PrimaryFunctionEntry =
                SameFunction(FunctionEntry,
                             ImageBase,
                             BranchTarget + ImageBase,
                             &PrimaryEntryBuffer);

            if ((PrimaryFunctionEntry == NULL) ||
                (BranchTarget == PrimaryFunctionEntry->BeginAddress)) {

                InEpilogue = TRUE;
            }

        } else if ((BranchTarget == FunctionEntry->BeginAddress) &&
                   ((UnwindInfo->Flags & AMD64_UNW_FLAG_CHAININFO) == 0)) {

            InEpilogue = TRUE;
        }

    } else if (Bytes >= 2 &&
               (NextByte[0] == JMP_IND_OP) && (NextByte[1] == 0x25)) {

        //
        // An unconditional jump indirect.
        //
        // This is a jmp outside of the function, probably a tail call
        // to an import function.
        //

        InEpilogue = TRUE;

    } else if (Bytes >= 3 &&
               ((NextByte[0] & 0xf8) == SIZE64_PREFIX) &&
               (NextByte[1] == 0xff) &&
               (NextByte[2] & 0x38) == 0x20) {

        //
        // This is an indirect jump opcode: 0x48 0xff /4.  The 64-bit
        // flag (REX.W) is always redundant here, so its presence is
        // overloaded to indicate a branch out of the function - a tail
        // call.
        //
        // Such an opcode is an unambiguous epilogue indication.
        //

        InEpilogue = TRUE;
    }

    if (InEpilogue != FALSE) {
        NextByte = InstrBuffer;
        Bytes = InstrBytes;

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

        if (Bytes >= 1 &&
            (NextByte[0] & 0xf8) == SIZE64_PREFIX) {

            if (Bytes >= 4 &&
                NextByte[1] == ADD_IMM8_OP) {

                //
                // add rsp, imm8.
                //

                ContextRecord->Rsp += (CHAR)NextByte[3];
                NextByte += 4;
                Bytes -= 4;

            } else if (Bytes >= 7 &&
                       NextByte[1] == ADD_IMM32_OP) {

                //
                // add rsp, imm32.
                //

                Displacement = NextByte[3] | (NextByte[4] << 8);
                Displacement |= (NextByte[5] << 16) | (NextByte[6] << 24);
                ContextRecord->Rsp += Displacement;
                NextByte += 7;
                Bytes -= 7;

            } else if (Bytes >= 4 &&
                       NextByte[1] == LEA_OP) {
                if ((NextByte[2] & 0xf8) == 0x60) {

                    //
                    // lea rsp, disp8[frame-register].
                    //

                    ContextRecord->Rsp = IntegerRegister[FrameRegister];
                    ContextRecord->Rsp += (CHAR)NextByte[3];
                    NextByte += 4;
                    Bytes -= 4;

                } else if (Bytes >= 7 &&
                           (NextByte[2] & 0xf8) == 0xa0) {

                    //
                    // lea rsp, disp32[frame-register].
                    //

                    Displacement = NextByte[3] | (NextByte[4] << 8);
                    Displacement |= (NextByte[5] << 16) | (NextByte[6] << 24);
                    ContextRecord->Rsp = IntegerRegister[FrameRegister];
                    ContextRecord->Rsp += Displacement;
                    NextByte += 7;
                    Bytes -= 7;
                }
            }
        }

        //
        // Emulate any number of (if any):
        //
        //   pop nonvolatile-integer-register.
        //

        while (TRUE) {
            if (Bytes >= 1 &&
                (NextByte[0] & 0xf8) == POP_OP) {

                //
                // pop nonvolatile-integer-register[0..7]
                //

                RegisterNumber = NextByte[0] & 0x7;
                if ((Status = m_Services->
                    ReadAllMemory(ContextRecord->Rsp,
                                  &IntegerRegister[RegisterNumber],
                                  sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Unable to read stack at %I64X\n",
                         ContextRecord->Rsp);
                    return Status;
                }
                ContextRecord->Rsp += 8;
                NextByte += 1;
                Bytes -= 1;

            } else if (Bytes >= 2 &&
                       IS_REX_PREFIX(NextByte[0]) &&
                       (NextByte[1] & 0xf8) == POP_OP) {

                //
                // pop nonvolatile-integer-register[8..15]
                //

                RegisterNumber = ((NextByte[0] & 1) << 3) | (NextByte[1] & 0x7);
                if ((Status = m_Services->
                     ReadAllMemory(ContextRecord->Rsp,
                                   &IntegerRegister[RegisterNumber],
                                   sizeof(ULONG64))) != S_OK) {
                    m_Services->Status(1, "Unable to read stack at %I64X\n",
                         ContextRecord->Rsp);
                    return Status;
                }
                ContextRecord->Rsp += 8;
                NextByte += 2;
                Bytes -= 2;

            } else {
                break;
            }
        }

        //
        // Emulate return and return null exception handler.
        //
        // Note: this instruction might in fact be a jmp, however
        //       we want to emulate a return regardless.
        //

        if ((Status = m_Services->
            ReadAllMemory(ContextRecord->Rsp,
                          &ContextRecord->Rip,
                          sizeof(ULONG64))) != S_OK) {
            m_Services->Status(1, "Unable to read stack at %I64X\n",
                 ContextRecord->Rsp);
            return Status;
        }
        ContextRecord->Rsp += 8;
        return S_OK;
    }

    //
    // Control left the specified function outside an epilogue. Unwind the
    // subject function and any chained unwind information.
    //

    return UnwindPrologue(ImageBase,
                          ControlPc,
                          *EstablisherFrame,
                          FunctionEntry,
                          ContextRecord);
}

ULONG64
DbsX64StackUnwinder::LookupPrimaryUnwindInfo(
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    _In_ ULONG64 ImageBase,
    _Out_ _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryEntry
    )

/*++

Routine Description:

    This function determines whether the supplied function entry is a primary
    function entry or a chained function entry. If it is a chained function
    entry, the unwind information associated with the primary function entry
    is returned.

Arguments:

    FunctionEntry - Supplies a pointer to the function entry for which the
        associated primary function entry will be located.

    ImageBase - Supplies the base address of the image containing the
        supplied function entry.

    PrimaryEntry - Supplies the address of a variable that receives a pointer
        to the primary function entry.

Return Value:

    A pointer to the unwind information for the primary function entry is
    returned as the function value.

--*/

{

    ULONG Index;
    ULONG64 UnwindInfoBuffer[32];
    PAMD64_UNWIND_INFO UnwindInfo;
    ULONG UnwindRel;
    ULONG64 UnwindAbs;

    //
    // Locate the unwind information and determine whether it is chained.
    // If the unwind information is chained, then locate the parent function
    // entry and loop again.
    //

    UnwindRel = FunctionEntry->UnwindInfoAddress;
    // Copy the function entry before it becomes invalid.
    *PrimaryEntry = *FunctionEntry;

    do {
        UnwindAbs = ImageBase + UnwindRel;
        if (GetUnwindInfo(ImageBase, UnwindRel,
                          false,
                          UnwindInfoBuffer, sizeof(UnwindInfoBuffer),
                          (PVOID*)&UnwindInfo) != S_OK ||
            (UnwindInfo->Flags & AMD64_UNW_FLAG_CHAININFO) == 0) {
            break;
        }

        Index = UnwindInfo->CountOfCodes;
        if ((Index & 1) != 0) {
            Index += 1;
        }

        FunctionEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)
            &UnwindInfo->UnwindCode[Index];
        UnwindRel = FunctionEntry->UnwindInfoAddress;

        // Copy the function entry before it becomes invalid.
        *PrimaryEntry = *FunctionEntry;

        FreeUnwindInfo(UnwindInfo, UnwindInfoBuffer);

    } while (TRUE);

    return UnwindAbs;
}

_PIMAGE_RUNTIME_FUNCTION_ENTRY
DbsX64StackUnwinder::SameFunction(
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _Out_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionReturnBuffer
    )

/*++

Routine Description:

    This function determines whether the address supplied by ControlPc lies
    anywhere within the function associated with FunctionEntry.

Arguments:

    FunctionEntry - Supplies a pointer to a function entry (primary or chained)
        associated with the function.

    ImageBase - Supplies the base address of the image containing the supplied
        function entry.

    ControlPc - Supplies the address that will be tested for inclusion within
        the function associated with FunctionEntry.

Return Value:

    If the address of the unwind information for the specified function is
    equal to the address of the unwind information for the control PC, then
    a pointer to a function table entry that describes the primary function
    table entry is returned as the function value. Otherwise, NULL is returned.

--*/

{

    _IMAGE_RUNTIME_FUNCTION_ENTRY TargetFunctionEntry;
    ULONG64 TargetImageBase;
    ULONG64 UnwindInfo1;
    ULONG64 UnwindInfo2;

    //
    // Find the unwind information referenced by the primary function entry
    // associated with the specified function entry.
    //

    UnwindInfo1 = LookupPrimaryUnwindInfo(FunctionEntry, ImageBase,
                                          FunctionReturnBuffer);

    //
    // Determine the function entry containing the control Pc and similarly
    // resolve it's primary function entry.
    //

    if (m_Services->GetModuleBase(ControlPc, &TargetImageBase) != S_OK ||
        m_Services->GetFunctionEntry(ControlPc,
                                     &TargetFunctionEntry,
                                     sizeof(TargetFunctionEntry)) != S_OK) {
        return NULL;
    }

    UnwindInfo2 = LookupPrimaryUnwindInfo(&TargetFunctionEntry,
                                          TargetImageBase,
                                          FunctionReturnBuffer);

    //
    // If the address of the two sets of unwind information are equal, then
    // return the address of the primary function entry. Otherwise, return
    // NULL.
    //

    if (UnwindInfo1 == UnwindInfo2) {
        return FunctionReturnBuffer;

    } else {
        return NULL;
    }
}

//----------------------------------------------------------------------------
//
// DbsX64StackUnwinder.
//
//----------------------------------------------------------------------------

#define DBHX64_SAVE_TRAP(_DbhFrame) ((_DbhFrame)->Reserved[0])

//
// Flags word.
//

#define DBHX64_IS_RESTART_FLAG    (0x1UI64)

#define DBHX64_GET_IS_RESTART(_DbhFrame) \
    ((((_DbhFrame)->Reserved[2]) & DBHX64_IS_RESTART_FLAG) != 0)
#define DBHX64_SET_IS_RESTART(_DbhFrame, _IsRestart) \
    ((_DbhFrame)->Reserved[2] = \
     (((_DbhFrame)->Reserved[2]) & ~DBHX64_IS_RESTART_FLAG) | \
      ((_IsRestart) ? DBHX64_IS_RESTART_FLAG : 0))

DbsX64StackUnwinder::DbsX64StackUnwinder(_In_opt_ DbsStackServices* Services)
    : DbsStackUnwinder(Services, "x64", IMAGE_FILE_MACHINE_AMD64,
                       sizeof(m_Context),
                       sizeof(_IMAGE_RUNTIME_FUNCTION_ENTRY),
                       sizeof(AMD64_UNWIND_INFO), 16, 8, 1)
{
    m_ContextBuffer = &m_Context;
}

HRESULT
DbsX64StackUnwinder::Unwind(void)
{
    HRESULT Status;

    ClearUnwindDerived();

    if (SUCCEEDED(Status = BaseUnwind()))
    {
        return Status;
    }

    //
    // Unable to do a normal unwind, so check for
    // alternate transitions like kernel/user boundaries.
    // If this fails just return the original error
    // as that's more likely to be interesting.
    //

    DWORD64 ImageBase;
    _IMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry;
    DWORD64 StackPointer;

    if (UnwindNtKernelCallback(&ImageBase,
                               &FunctionEntry,
                               sizeof(FunctionEntry),
                               &StackPointer) != S_OK)
    {
        return Status;
    }

    m_InstructionPointer = ImageBase + FunctionEntry.BeginAddress;
    m_CallPointer = m_InstructionPointer;
    m_Context.Rip = m_InstructionPointer;
    m_StackPointer = StackPointer;
    m_Context.Rsp = m_StackPointer;
    m_RestartFrame = true;

    return S_OK;
}

DWORD
DbsX64StackUnwinder::
GetFullUnwindInfoSize(_In_ PVOID InfoHeader)
{
    PAMD64_UNWIND_INFO UnwindInfo = (PAMD64_UNWIND_INFO)InfoHeader;

    DWORD UnwindInfoSize = offsetof(AMD64_UNWIND_INFO, UnwindCode) +
        UnwindInfo->CountOfCodes * sizeof(AMD64_UNWIND_CODE);

    // An extra alignment code and function entry may be added on to handle
    // the chained info case where the chain function entry is just
    // beyond the end of the normal code array.
    if ((UnwindInfo->Flags & AMD64_UNW_FLAG_CHAININFO) != 0)
    {
        if ((UnwindInfo->CountOfCodes & 1) != 0)
        {
            UnwindInfoSize += sizeof(AMD64_UNWIND_CODE);
        }
        UnwindInfoSize += sizeof(_IMAGE_RUNTIME_FUNCTION_ENTRY);
    }

    return UnwindInfoSize;
}

HRESULT
DbsX64StackUnwinder::DbhStart(__inout LPSTACKFRAME64 StackFrame,
                              _In_ DWORD DbhVersion,
                              _In_reads_bytes_(DbhStorageBytes) PVOID DbhStorage,
                              _In_ DWORD DbhStorageBytes,
                              __inout PVOID Context)
{
    HRESULT Status;

    if ((StackFrame->AddrPC.Offset &&
         StackFrame->AddrPC.Mode != AddrModeFlat) ||
        (StackFrame->AddrStack.Offset &&
         StackFrame->AddrStack.Mode != AddrModeFlat) ||
        (StackFrame->AddrFrame.Offset &&
         StackFrame->AddrFrame.Mode != AddrModeFlat))
    {
        return E_INVALIDARG;
    }

    if ((Status = DbsStackUnwinder::
         DbhStart(StackFrame, DbhVersion, DbhStorage, DbhStorageBytes,
                  Context)) != S_OK)
    {
        return Status;
    }

    // dbghelp doesn't give a context size so we
    // have to assume the buffer is large enough.
    memcpy(&m_Context, Context, sizeof(m_Context));

    //
    // Override context values from the stack frame if necessary.
    //

    if (StackFrame->AddrPC.Offset)
    {
        m_Context.Rip = StackFrame->AddrPC.Offset;
    }
    if (StackFrame->AddrStack.Offset)
    {
        m_Context.Rsp = StackFrame->AddrStack.Offset;
    }
    if (StackFrame->AddrFrame.Offset)
    {
        m_Context.Rbp = StackFrame->AddrFrame.Offset;
    }
    UpdateAbstractPointers();
    m_CallPointer = m_InstructionPointer;

    SetRestart();
    return S_OK;
}

HRESULT
DbsX64StackUnwinder::
DbhContinue(__inout LPSTACKFRAME64 StackFrame,
            _In_ DWORD DbhVersion,
            _In_reads_bytes_(DbhStorageBytes) PVOID DbhStorage,
            _In_ DWORD DbhStorageBytes,
            __inout PVOID Context)
{
    HRESULT Status;

    if ((Status = DbsStackUnwinder::
         DbhContinue(StackFrame, DbhVersion,
                     DbhStorage, DbhStorageBytes,
                     Context)) != S_OK)
    {
        return Status;
    }

    if (DBHX64_GET_IS_RESTART(StackFrame))
    {
        m_RestartFrame = true;
        // The base DbhContinue always assumes it
        // isn't a restart frame, so override it.
        m_CallPointer = m_InstructionPointer;
    }

    return Status;
}

HRESULT
DbsX64StackUnwinder::DbhUpdatePreUnwind(__inout LPSTACKFRAME64 StackFrame)
{
    HRESULT Status;

    if ((Status = DbsStackUnwinder::DbhUpdatePreUnwind(StackFrame)) != S_OK)
    {
        return Status;
    }

    DBHX64_SET_IS_RESTART(StackFrame, m_RestartFrame);
    return S_OK;
}

HRESULT
DbsX64StackUnwinder::DbhUpdatePostUnwind(__inout LPSTACKFRAME64 StackFrame,
                                         _In_ HRESULT UnwindStatus)
{
    HRESULT Status;

    if ((Status = DbsStackUnwinder::DbhUpdatePostUnwind(StackFrame,
                                                        UnwindStatus)) != S_OK)
    {
        return Status;
    }

    // The frame pointer is an artificial value set
    // to a pointer below the return address.  This
    // matches an RBP-chain style of frame while
    // also allowing easy access to the return
    // address and homed arguments above it.
    StackFrame->AddrFrame.Offset = m_FramePointer;

    DBHX64_SAVE_TRAP(StackFrame) = m_TrapAddr;
    return S_OK;
}

void
DbsX64StackUnwinder::UpdateAbstractPointers(void)
{
    m_InstructionPointer = m_Context.Rip;
    m_StackPointer = m_Context.Rsp;
    m_FramePointer = m_Context.Rbp;
}

HRESULT
DbsX64StackUnwinder::BaseUnwind(void)
{
    HRESULT Status;
    _IMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry;

    Status = m_Services->GetFunctionEntry(m_CallPointer,
                                          &FunctionEntry,
                                          sizeof(FunctionEntry));
    if (Status == S_OK)
    {
        DWORD64 ImageBase;
        DWORD64 EstablisherFrame;

        //
        // The return value coming out of mainCRTStartup is set by some
        // run-time routine to be 0; this serves to cause an error if someone
        // actually does a return from the mainCRTStartup frame.
        //

        if ((Status = m_Services->
             GetModuleBase(m_Context.Rip, &ImageBase)) != S_OK ||
            (Status = VirtualUnwind(ImageBase,
                                    m_Context.Rip,
                                    &FunctionEntry,
                                    &m_Context,
                                    &EstablisherFrame)) != S_OK)
        {
            return Status;
        }

        DWORD64 OldIp = m_InstructionPointer;

        UpdateAbstractPointers();
        UpdateCallPointer();
        m_FramePointer = m_StackPointer - 2 * sizeof(DWORD64);

        // Check for end frame.
        if (m_Context.Rip == 0 ||
            (m_Context.Rip == OldIp &&
             EstablisherFrame == m_Context.Rsp))
        {
            return S_FALSE;
        }
    }
    else if (Status == E_NOINTERFACE)
    {
        //
        // If there's no function entry for a function
        // we assume that it's a leaf and that RSP points
        // directly to the return address.
        //

        m_Services->Status(1, "Leaf %I64X RSP %I64X\n",
                           m_Context.Rip, m_Context.Rsp);

        if ((Status = m_Services->
             ReadAllMemory(m_Context.Rsp, &m_Context.Rip,
                           sizeof(m_Context.Rip))) != S_OK)
        {
            return Status;
        }

        // Update the context values to what they should be in
        // the caller.
        m_Context.Rsp += sizeof(m_Context.Rip);
        UpdateAbstractPointers();
        m_CallPointer = m_InstructionPointer - 1;
        m_FramePointer = m_StackPointer - 2 * sizeof(DWORD64);
    }
    else
    {
        return Status;
    }

    m_FrameIndex++;
    if (!m_RestartFrame)
    {
        AdjustForNoReturn(&m_InstructionPointer);
    }
    return S_OK;
}
