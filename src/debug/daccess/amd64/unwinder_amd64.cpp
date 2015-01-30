//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// 

#include "stdafx.h"
#include "unwinder_amd64.h"

//---------------------------------------------------------------------------------------
//
// Given the target address of an UNWIND_INFO structure, this function retrieves all the memory used for
// the UNWIND_INFO, including the variable size array of UNWIND_CODE.  The function returns a host copy
// of the UNWIND_INFO.
//
// Arguments:
//    taUnwindInfo - the target address of an UNWIND_INFO
//
// Return Value:
//    Return a host copy of the UNWIND_INFO, including the array of UNWIND_CODE.
//
// Notes:
//    The host copy of UNWIND_INFO is created from DAC memory, which will be flushed when the DAC cache
//    is flushed (i.e. when the debugee is continued).  Thus, the caller doesn't need to worry about freeing
//    this memory.
//

UNWIND_INFO * DacGetUnwindInfo(TADDR taUnwindInfo)
{
    PTR_UNWIND_INFO pUnwindInfo = PTR_UNWIND_INFO(taUnwindInfo);
    DWORD cbUnwindInfo = offsetof(UNWIND_INFO, UnwindCode) + 
                         pUnwindInfo->CountOfUnwindCodes * sizeof(UNWIND_CODE);

    // Check if there is a chained unwind info.  If so, it has an extra RUNTIME_FUNCTION tagged to the end.
    if ((pUnwindInfo->Flags & UNW_FLAG_CHAININFO) != 0)
    {
        // If there is an odd number of UNWIND_CODE, we need to adjust for alignment.
        if ((pUnwindInfo->CountOfUnwindCodes & 1) != 0)
        {
            cbUnwindInfo += sizeof(UNWIND_CODE);
        }
        cbUnwindInfo += sizeof(RUNTIME_FUNCTION);
    }
    return reinterpret_cast<UNWIND_INFO *>(DacInstantiateTypeByAddress(taUnwindInfo, cbUnwindInfo, true));
}

//---------------------------------------------------------------------------------------
//
// This function is just a wrapper over OOPStackUnwinder.  The runtime can call this function to 
// virtually unwind a CONTEXT out-of-process.
//
// Arguments:
//    pContext - This is an in-out parameter.  On entry, this is the CONTEXT to be unwound.  
//               On exit, this is the caller CONTEXT.
//
// Return Value:
//    TRUE if the unwinding is successful
//
// Notes:
//    This function overwrites the specified CONTEXT to store the caller CONTEXT.
//

BOOL DacUnwindStackFrame(CONTEXT * pContext, KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    OOPStackUnwinderAMD64 unwinder;
    BOOL res = unwinder.Unwind(pContext);

    if (res && pContextPointers)
    {
        for (int i = 0; i < 16; i++)
        {
            *(&pContextPointers->Rax + i) = &pContext->Rax + i;
        }
    }
    
    return res;
}

//---------------------------------------------------------------------------------------
//
// Unwind the given CONTEXT to the caller CONTEXT.  The given CONTEXT will be overwritten.
//
// Arguments:
//    pContext - in-out parameter storing the specified CONTEXT on entry and the unwound CONTEXT on exit
//
// Return Value:
//    TRUE if the unwinding is successful
//

BOOL OOPStackUnwinderAMD64::Unwind(CONTEXT * pContext)
{
    HRESULT hr = E_FAIL;

    ULONG64 uControlPC = (DWORD64)dac_cast<PCODE>(::GetIP(pContext));

    // get the module base
    ULONG64 uImageBase;
    hr = GetModuleBase(uControlPC, &uImageBase);
    if (FAILED(hr))
    {
        return FALSE;
    }

    // get the function entry
    IMAGE_RUNTIME_FUNCTION_ENTRY functionEntry;
    hr = GetFunctionEntry(uControlPC, &functionEntry, sizeof(functionEntry));
    if (FAILED(hr))
    {
        return FALSE;
    }

    // call VirtualUnwind() to do the real work
    ULONG64 EstablisherFrame;
    hr = VirtualUnwind(uImageBase, uControlPC, &functionEntry, pContext, &EstablisherFrame);

    return (hr == S_OK);
}


//
//
// <NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE>
//
// Everything below is borrowed from dbghelp.dll
//
// <NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE>
//
//


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
OOPStackUnwinderAMD64::s_UnwindOpSlotTable[] =
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
OOPStackUnwinderAMD64::UnwindPrologue(
    __in ULONG64 ImageBase,
    __in ULONG64 ControlPc,
    __in ULONG64 FrameBase,
    __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord
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
    M128A* FloatingRegister;
    ULONG FrameOffset;
    ULONG Index;
    ULONG64 IntegerAddress;
    PULONG64 IntegerRegister;
    BOOLEAN MachineFrame;
    ULONG OpInfo;
    ULONG PrologOffset;
    ULONG64 ReturnAddress;
    ULONG64 StackAddress;
    PUNWIND_INFO UnwindInfo;
    ULONG UnwindOp;

    //
    // Process the unwind codes.
    //

    FloatingRegister = &ContextRecord->Xmm0;
    IntegerRegister = &ContextRecord->Rax;
    Index = 0;
    MachineFrame = FALSE;
    PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));

    UnwindInfo = DacGetUnwindInfo(ImageBase + FunctionEntry->UnwindInfoAddress);
    if (UnwindInfo == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
    }

    while (Index < UnwindInfo->CountOfUnwindCodes) {

        //
        // If the prologue offset is greater than the next unwind code offset,
        // then simulate the effect of the unwind code.
        //

        UnwindOp = UnwindInfo->UnwindCode[Index].UnwindOp;
        if (UnwindOp > UWOP_PUSH_MACHFRAME) {
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

            case UWOP_PUSH_NONVOL:
                IntegerAddress = ContextRecord->Rsp;
                if ((Status = 
                     ReadAllMemory(IntegerAddress,
                                   &IntegerRegister[OpInfo],
                                   sizeof(ULONG64))) != S_OK) {
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

            case UWOP_ALLOC_LARGE:
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

            case UWOP_ALLOC_SMALL:
                ContextRecord->Rsp += (OpInfo * 8) + 8;
                break;

                //
                // Establish the the frame pointer register.
                //
                // The operation information is not used.
                //

            case UWOP_SET_FPREG:
                ContextRecord->Rsp = IntegerRegister[UnwindInfo->FrameRegister];
                ContextRecord->Rsp -= UnwindInfo->FrameOffset * 16;
                break;

                //
                // Save nonvolatile integer register on the stack using a
                // 16-bit displacment.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_NONVOL:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 8;
                IntegerAddress = FrameBase + FrameOffset;
                if ((Status = 
                    ReadAllMemory(IntegerAddress,
                                  &IntegerRegister[OpInfo],
                                  sizeof(ULONG64))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Save nonvolatile integer register on the stack using a
                // 32-bit displacment.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_NONVOL_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                IntegerAddress = FrameBase + FrameOffset;
                if ((Status = 
                     ReadAllMemory(IntegerAddress,
                                   &IntegerRegister[OpInfo],
                                   sizeof(ULONG64))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(64) register on the stack using a
                // 16-bit displacement.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_XMM:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 8;
                FloatingAddress = FrameBase + FrameOffset;
                FloatingRegister[OpInfo].High = 0;
                if ((Status = 
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo].Low,
                                   sizeof(ULONG64))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(64) register on the stack using a
                // 32-bit displacement.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_XMM_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                FloatingAddress = FrameBase + FrameOffset;
                FloatingRegister[OpInfo].High = 0;
                if ((Status = 
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo].Low,
                                   sizeof(ULONG64))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(128) register on the stack using a
                // 16-bit displacement.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_XMM128:
                Index += 1;
                FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 16;
                FloatingAddress = FrameBase + FrameOffset;
                if ((Status = 
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo],
                                   sizeof(M128A))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Save a nonvolatile XMM(128) register on the stack using a
                // 32-bit displacement.
                //
                // The operation information is the register number.
                //

            case UWOP_SAVE_XMM128_FAR:
                Index += 2;
                FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                FloatingAddress = FrameBase + FrameOffset;
                if ((Status = 
                     ReadAllMemory(FloatingAddress,
                                   &FloatingRegister[OpInfo],
                                   sizeof(M128A))) != S_OK) {
                    goto Fail;
                }
                break;

                //
                // Push a machine frame on the stack.
                //
                // The operation information determines whether the machine
                // frame contains an error code or not.
                //

            case UWOP_PUSH_MACHFRAME:
                MachineFrame = TRUE;
                ReturnAddress = ContextRecord->Rsp;
                StackAddress = ContextRecord->Rsp + (3 * 8);
                if (OpInfo != 0) {
                    ReturnAddress += 8;
                    StackAddress +=  8;
                }

                if ((Status = 
                     ReadAllMemory(ReturnAddress,
                                   &ContextRecord->Rip,
                                   sizeof(ULONG64))) != S_OK) {
                    goto Fail;
                }
                if ((Status = 
                     ReadAllMemory(StackAddress,
                                   &ContextRecord->Rsp,
                                   sizeof(ULONG64))) != S_OK) {
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

            case UWOP_ALLOC_LARGE:
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

    if ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) != 0) {

        _PIMAGE_RUNTIME_FUNCTION_ENTRY ChainEntry;
        
        Index = UnwindInfo->CountOfUnwindCodes;
        if ((Index & 1) != 0) {
            Index += 1;
        }

        // GetUnwindInfo looks for CHAININFO and reads
        // the trailing RUNTIME_FUNCTION so we can just
        // directly use the data sitting in UnwindInfo.
        ChainEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)
            &UnwindInfo->UnwindCode[Index];

        Status = UnwindPrologue(ImageBase,
                                ControlPc,
                                FrameBase,
                                ChainEntry,
                                ContextRecord);

        return Status;

    } else {

        if (MachineFrame == FALSE) {
            if ((Status = 
                 ReadAllMemory(ContextRecord->Rsp,
                               &ContextRecord->Rip,
                               sizeof(ULONG64))) != S_OK) {
                return Status;
            }
            ContextRecord->Rsp += 8;
        }
        
        return S_OK;
    }

 Fail:
    return Status;
}

HRESULT
OOPStackUnwinderAMD64::VirtualUnwind(
    __in ULONG64 ImageBase,
    __in ULONG64 ControlPc,
    __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    __out PULONG64 EstablisherFrame
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
    ULONG FrameRegister = 0;
    ULONG Index;
    BOOL InEpilogue;
    PULONG64 IntegerRegister;
    PUCHAR NextByte;
    _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryFunctionEntry;
    ULONG PrologOffset;
    ULONG RegisterNumber;
    PUNWIND_INFO UnwindInfo;
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

    UnwindInfo = DacGetUnwindInfo(ImageBase + FunctionEntry->UnwindInfoAddress);
    if (UnwindInfo == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
    }

    PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));
    UnwindFrameReg = UnwindInfo->FrameRegister;
    if (UnwindFrameReg == 0) {
        *EstablisherFrame = ContextRecord->Rsp;

    } else if ((PrologOffset >= UnwindInfo->SizeOfProlog) ||
               ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) != 0)) {
        *EstablisherFrame = (&ContextRecord->Rax)[UnwindFrameReg];
        *EstablisherFrame -= UnwindInfo->FrameOffset * 16;

    } else {

        // Read all the data.
        UnwindInfo = DacGetUnwindInfo(ImageBase + FunctionEntry->UnwindInfoAddress);
        if (UnwindInfo == NULL)
        {
            return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
        }

        Index = 0;
        while (Index < UnwindInfo->CountOfUnwindCodes) {
            if (UnwindInfo->UnwindCode[Index].UnwindOp == UWOP_SET_FPREG) {
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
    }

    if ((Status = 
         ReadMemory(ControlPc, InstrBuffer, sizeof(InstrBuffer),
                    &InstrBytes)) != S_OK) {

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
                   ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0)) {
            
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
                if ((Status = 
                    ReadAllMemory(ContextRecord->Rsp,
                                  &IntegerRegister[RegisterNumber],
                                  sizeof(ULONG64))) != S_OK) {
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
                if ((Status = 
                     ReadAllMemory(ContextRecord->Rsp,
                                   &IntegerRegister[RegisterNumber],
                                   sizeof(ULONG64))) != S_OK) {
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
            
        if ((Status = 
            ReadAllMemory(ContextRecord->Rsp,
                          &ContextRecord->Rip,
                          sizeof(ULONG64))) != S_OK) {
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
OOPStackUnwinderAMD64::LookupPrimaryUnwindInfo(
    __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __in ULONG64 ImageBase,
    __out _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryEntry
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
    PUNWIND_INFO UnwindInfo;
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
        UnwindInfo = DacGetUnwindInfo(ImageBase + UnwindRel);
        if ((UnwindInfo == NULL) || ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0))
        {
            break;
        }

        Index = UnwindInfo->CountOfUnwindCodes;
        if ((Index & 1) != 0) {
            Index += 1;
        }

        FunctionEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)
            &UnwindInfo->UnwindCode[Index];
        UnwindRel = FunctionEntry->UnwindInfoAddress;

        // Copy the function entry before it becomes invalid.
        *PrimaryEntry = *FunctionEntry;

    } while (TRUE);

    return UnwindAbs;
}

_PIMAGE_RUNTIME_FUNCTION_ENTRY
OOPStackUnwinderAMD64::SameFunction(
    __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __in ULONG64 ImageBase,
    __in ULONG64 ControlPc,
    __out _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionReturnBuffer
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

    if (GetModuleBase(ControlPc, &TargetImageBase) != S_OK ||
        GetFunctionEntry(ControlPc,
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
