// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "unwinder.h"

typedef DPTR(M128A)  PTR_M128A;

//---------------------------------------------------------------------------------------
//
// Read 64 bit unsigned value from the specified address. When the unwinder is built
// for jitted code unwinding on non-Windows systems, this is just a plain memory read.
// When the unwinder is built for DAC though, this reads data from the target debugged
// process.
//
// Arguments:
//    addr - address to read from
//
// Return Value:
//    The value that was read
//
// Notes:
//    If the memory read fails in the DAC mode, the failure is reported as an exception
//    via the DacError function.
//
static ULONG64 MemoryRead64(PULONG64 addr)
{
    return *dac_cast<PTR_ULONG64>((TADDR)addr);
}

//---------------------------------------------------------------------------------------
//
// Read 128 bit value from the specified address. When the unwinder is built
// for jitted code unwinding on non-Windows systems, this is just a plain memory read.
// When the unwinder is built for DAC though, this reads data from the target debugged
// process.
//
// Arguments:
//    addr - address to read from
//
// Return Value:
//    The value that was read
//
// Notes:
//    If the memory read fails in the DAC mode, the failure is reported as an exception
//    via the DacError function.
//
static M128A MemoryRead128(PM128A addr)
{
    return *dac_cast<PTR_M128A>((TADDR)addr);
}

#ifdef DACCESS_COMPILE

// Report failure in the unwinder if the condition is FALSE
#define UNWINDER_ASSERT(Condition) if (!(Condition)) DacError(CORDBG_E_TARGET_INCONSISTENT)

//---------------------------------------------------------------------------------------
//
// The InstructionBuffer class abstracts accessing assembler instructions in the function
// being unwound. It behaves as a memory byte pointer, but it reads the instruction codes
// from the target process being debugged and removes all changes that the debugger
// may have made to the code, e.g. breakpoint instructions.
//
class InstructionBuffer
{
    UINT m_offset;
    SIZE_T m_address;
    UCHAR m_buffer[32];

    // Load the instructions from the target process being debugged
    HRESULT Load()
    {
        HRESULT hr = DacReadAll(TO_TADDR(m_address), m_buffer, sizeof(m_buffer), false);
        if (SUCCEEDED(hr))
        {
            // On X64, we need to replace any patches which are within the requested memory range.
            // This is because the X64 unwinder needs to disassemble the native instructions in order to determine
            // whether the IP is in an epilog.
            MemoryRange range(dac_cast<PTR_VOID>((TADDR)m_address), sizeof(m_buffer));
            hr = DacReplacePatchesInHostMemory(range, m_buffer);
        }

        return hr;
    }

public:

    // Construct the InstructionBuffer for the given address in the target process
    InstructionBuffer(SIZE_T address)
      : m_offset(0),
        m_address(address)
    {
        HRESULT hr = Load();
        if (FAILED(hr))
        {
            // If we have failed to read from the target process, just pretend
            // we've read zeros.
            // The InstructionBuffer is used in code driven epilogue unwinding
            // when we read processor instructions and simulate them.
            // It's very rare to be stopped in an epilogue when
            // getting a stack trace, so if we can't read the
            // code just assume we aren't in an epilogue instead of failing
            // the unwind.
            memset(m_buffer, 0, sizeof(m_buffer));
        }
    }

    // Move to the next byte in the buffer
    InstructionBuffer& operator++()
    {
        m_offset++;
        return *this;
    }

    // Skip delta bytes in the buffer
    InstructionBuffer& operator+=(INT delta)
    {
        m_offset += delta;
        return *this;
    }

    // Return address of the current byte in the buffer
    explicit operator ULONG64()
    {
        return m_address + m_offset;
    }

    // Get the byte at the given index from the current position
    // Invoke DacError if the index is out of the buffer
    UCHAR operator[](int index)
    {
        int realIndex = m_offset + index;
        UNWINDER_ASSERT(realIndex < (int)sizeof(m_buffer));
        return m_buffer[realIndex];
    }
};

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
        cbUnwindInfo += sizeof(T_RUNTIME_FUNCTION);
    }
    return reinterpret_cast<UNWIND_INFO *>(DacInstantiateTypeByAddress(taUnwindInfo, cbUnwindInfo, true));
}

//---------------------------------------------------------------------------------------
//
// This function just wraps the DacGetUnwindInfo.
// The DacGetUnwindInfo is called from other places outside of the unwinder, so it
// cannot be merged into the body of this method.
//
UNWIND_INFO * OOPStackUnwinderAMD64::GetUnwindInfo(TADDR taUnwindInfo)
{
    return DacGetUnwindInfo(taUnwindInfo);
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
    BOOL res = OOPStackUnwinderAMD64::Unwind(pContext);

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
    hr = VirtualUnwind(0, uImageBase, uControlPC, &functionEntry, pContext, NULL, &EstablisherFrame, NULL, NULL);

    return (hr == S_OK);
}

#else // DACCESS_COMPILE

// Report failure in the unwinder if the condition is FALSE
#define UNWINDER_ASSERT _ASSERTE

// For unwinding of the jitted code on non-Windows platforms, the Instruction buffer is
// just a plain pointer to the instruction data.
typedef UCHAR * InstructionBuffer;

//---------------------------------------------------------------------------------------
//
// Return UNWIND_INFO pointer for the given address.
//
UNWIND_INFO * OOPStackUnwinderAMD64::GetUnwindInfo(TADDR taUnwindInfo)
{
    return (UNWIND_INFO *)taUnwindInfo;
}

//---------------------------------------------------------------------------------------
//
// This function behaves like the RtlVirtualUnwind in Windows.
// It virtually unwinds the specified function by executing its
// prologue code backward or its epilogue code forward.
//
// If a context pointers record is specified, then the address where each
// nonvolatile registers is restored from is recorded in the appropriate
// element of the context pointers record.
//
// Arguments:
//
//     HandlerType - Supplies the handler type expected for the virtual unwind.
//         This may be either an exception or an unwind handler. A flag may
//         optionally be supplied to avoid epilogue detection if it is known
//         the specified control PC is not located inside a function epilogue.
//
//     ImageBase - Supplies the base address of the image that contains the
//         function being unwound.
//
//     ControlPc - Supplies the address where control left the specified
//         function.
//
//     FunctionEntry - Supplies the address of the function table entry for the
//         specified function.
//
//     ContextRecord - Supplies the address of a context record.
//
//     HandlerData - Supplies a pointer to a variable that receives a pointer
//         the language handler data.
//
//     EstablisherFrame - Supplies a pointer to a variable that receives the
//         the establisher frame pointer value.
//
//     ContextPointers - Supplies an optional pointer to a context pointers
//         record.
//
// Return value:
//
//     The handler routine address.  If control did not leave the specified
//     function in either the prologue or an epilogue and a handler of the
//     proper type is associated with the function, then the address of the
//     language specific exception handler is returned. Otherwise, NULL is
//     returned.
//
PEXCEPTION_ROUTINE RtlVirtualUnwind_Unsafe(
    _In_ ULONG HandlerType,
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ OUT PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PULONG64 EstablisherFrame,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers
    )
{
    PEXCEPTION_ROUTINE handlerRoutine;

    HRESULT res = OOPStackUnwinderAMD64::VirtualUnwind(
        HandlerType,
        ImageBase,
        ControlPc,
        (_PIMAGE_RUNTIME_FUNCTION_ENTRY)FunctionEntry,
        ContextRecord,
        HandlerData,
        EstablisherFrame,
        ContextPointers,
        &handlerRoutine);

    _ASSERTE(SUCCEEDED(res));

    return handlerRoutine;
}


#endif // DACCESS_COMPILE

//
//
// <NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE>
//
// Everything below is borrowed from minkernel\ntos\rtl\amd64\exdsptch.c file from Windows
//
// <NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE>
//
//


//----------------------------------------------------------------------------
//
// Copied OS code.
//
// This must be kept in sync with the system unwinder.
// minkernel\ntos\rtl\amd64\exdsptch.c
//
//----------------------------------------------------------------------------

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
#define REPNE_PREFIX 0xf2
#define REP_PREFIX 0xf3
#define POP_OP 0x58
#define RET_OP 0xc3
#define RET_OP_2 0xc2

#define IS_REX_PREFIX(x) (((x) & 0xf0) == 0x40)

#define UNWIND_CHAIN_LIMIT 32

HRESULT
OOPStackUnwinderAMD64::UnwindEpilogue(
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ ULONG EpilogueOffset,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers
)

/*++

Routine Description:

    This function emulates the state change associated with a function
    epilogue by using the corresponding prologue unwind codes of the
    primary function entry corresponding to the specified function.

    The prologue unwind codes can be used to reverse the epilogue since
    the epilogue operations are structured as a mirror-image of the initial
    prologue instructions prior to the establishment of the frame.

Arguments:

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified function.

    EpilogueOffset - Supplies the offset within an epilogue of the specified
        instruction pointer address.

    FunctionEntry - Supplies a pointer to the function table entry for the
        specified function. If appropriate, this has already been probed.

    ContextRecord - Supplies a pointer to a context record.

    ContextPointers - Supplies an optional pointer to a context pointers record.


Return Value:

HRESULT.

--*/

{

    ULONG ChainCount;
    ULONG CountOfCodes;
    ULONG CurrentOffset;
    ULONG FirstPushIndex;
    ULONG Index;
    PULONG64 IntegerAddress;
    PULONG64 IntegerRegister;
    ULONG OpInfo = 0;
    PULONG64 ReturnAddress;
    PULONG64 StackAddress;
    PUNWIND_INFO UnwindInfo;
    UNWIND_CODE UnwindOp = {};

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

    ChainCount = 0;
    for (;;) {
        UnwindInfo = GetUnwindInfo(FunctionEntry->UnwindInfoAddress + ImageBase);
        if (UnwindInfo == NULL)
        {
            return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
        }
        CountOfCodes = UnwindInfo->CountOfUnwindCodes;
        FirstPushIndex = 0;
        while (FirstPushIndex < CountOfCodes) {
            UnwindOp = UnwindInfo->UnwindCode[FirstPushIndex];
            if ((UnwindOp.UnwindOp == UWOP_PUSH_NONVOL) ||
                (UnwindOp.UnwindOp == UWOP_PUSH_MACHFRAME)) {

                break;
            }

            FirstPushIndex += UnwindOpSlots(UnwindOp);
        }

        if (FirstPushIndex < CountOfCodes) {
            break;
        }

        //
        // If a chained parent function entry exists, continue looking for
        // push opcodes in the parent.
        //

        if ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0) {
            break;
        }

        ChainCount += 1;
        if (ChainCount > UNWIND_CHAIN_LIMIT) {
            return E_FAIL;
        }

        Index = CountOfCodes;
        if (Index % 2 != 0) {
            Index += 1;
        }

        FunctionEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)&UnwindInfo->UnwindCode[Index];
    }

    //
    // Unwind any push codes that have not already been reversed by the
    // epilogue.
    //

    CurrentOffset = 0;
    IntegerRegister = &ContextRecord->Rax;
    for (Index = FirstPushIndex; Index < CountOfCodes; Index += 1) {
        UnwindOp = UnwindInfo->UnwindCode[Index];
        OpInfo = UnwindOp.OpInfo;

        if (UnwindOp.UnwindOp != UWOP_PUSH_NONVOL) {
            break;
        }

        if (CurrentOffset >= EpilogueOffset) {
            IntegerAddress = (PULONG64)(ContextRecord->Rsp);

            ContextRecord->Rsp += 8;
            IntegerRegister[OpInfo] = MemoryRead64(IntegerAddress);
            if (ARGUMENT_PRESENT(ContextPointers)) {
                ContextPointers->IntegerContext[OpInfo] = IntegerAddress;
            }
        }

        //
        // POP r64 is encoded as (58h + r64) for the lower 8 general-purpose
        // registers and REX.R, (58h + r64) for r8 - r15.
        //

        CurrentOffset += 1;
        if (OpInfo >= 8) {
            CurrentOffset += 1;
        }
    }

    //
    // Check for an UWOP_ALLOC_SMALL 8 directive, which corresponds to a push
    // of the FLAGS register.
    //

    if ((Index < CountOfCodes) &&
        (UnwindOp.UnwindOp == UWOP_ALLOC_SMALL) && (OpInfo == 0)) {

        if (CurrentOffset >= EpilogueOffset) {
            ContextRecord->Rsp += 8;
        }

        CurrentOffset += 1;
        Index += 1;
    }

    //
    // Check for a machine frame.
    //

    if (Index < CountOfCodes) {
        UnwindOp = UnwindInfo->UnwindCode[Index];
        if (UnwindOp.UnwindOp == UWOP_PUSH_MACHFRAME) {
            ReturnAddress = (PULONG64)(ContextRecord->Rsp);
            StackAddress = (PULONG64)(ContextRecord->Rsp + (3 * 8));

            ContextRecord->Rip = MemoryRead64(ReturnAddress);
            ContextRecord->Rsp = MemoryRead64(StackAddress);
            return S_OK;
        }

        //
        // Any remaining operation must be a machine frame.
        //

        UNWINDER_ASSERT(FALSE);
    }

    //
    // Emulate a return operation.
    //

    IntegerAddress = (PULONG64)(ContextRecord->Rsp);

    ContextRecord->Rip = MemoryRead64(IntegerAddress);
    ContextRecord->Rsp += 8;
    return S_OK;
}

HRESULT
OOPStackUnwinderAMD64::UnwindPrologue(
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ ULONG64 FrameBase,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
    _Outptr_ _PIMAGE_RUNTIME_FUNCTION_ENTRY *FinalFunctionEntry
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

    ContextPointers - Supplies an optional pointer to a context pointers
        record.

    FinalFunctionEntry - Supplies a pointer to a variable that receives the
        final function entry after the specified function entry and all
        descendent chained entries have been unwound. This will have been
        probed as appropriate.

Return Value:

    HRESULT.

--*/

{

    ULONG ChainCount;
    PM128A FloatingAddress;
    PM128A FloatingRegister;
    ULONG FrameOffset;
    ULONG Index;
    PULONG64 IntegerAddress;
    PULONG64 IntegerRegister;
    BOOLEAN MachineFrame;
    ULONG OpInfo;
    ULONG PrologOffset;
    PULONG64 ReturnAddress;
    PULONG64 StackAddress;
    PUNWIND_INFO UnwindInfo;
    ULONG UnwindOp;

    //
    // Process the unwind codes for the specified function entry and all its
    // descendent chained function entries.
    //

    ChainCount = 0;
    FloatingRegister = &ContextRecord->Xmm0;
    IntegerRegister = &ContextRecord->Rax;
    do {
        Index = 0;
        MachineFrame = FALSE;
        PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));

        UnwindInfo = GetUnwindInfo(ImageBase + FunctionEntry->UnwindInfoAddress);
        if (UnwindInfo == NULL)
        {
            return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
        }

        while (Index < UnwindInfo->CountOfUnwindCodes) {

            //
            // If the prologue offset is greater than the next unwind code
            // offset, then simulate the effect of the unwind code.
            //

            UnwindOp = UnwindInfo->UnwindCode[Index].UnwindOp;
#ifdef TARGET_UNIX
            if (UnwindOp > UWOP_SET_FPREG_LARGE) {
                return E_UNEXPECTED;
            }
#else // !TARGET_UNIX
            if (UnwindOp > UWOP_PUSH_MACHFRAME) {
                return E_UNEXPECTED;
            }
#endif // !TARGET_UNIX

            OpInfo = UnwindInfo->UnwindCode[Index].OpInfo;
            if (PrologOffset >= UnwindInfo->UnwindCode[Index].CodeOffset) {
                switch (UnwindOp) {

                    //
                    // Push nonvolatile integer register.
                    //
                    // The operation information is the register number of
                    // the register than was pushed.
                    //

                case UWOP_PUSH_NONVOL:
                    IntegerAddress = (PULONG64)ContextRecord->Rsp;
                    IntegerRegister[OpInfo] = MemoryRead64(IntegerAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->IntegerContext[OpInfo] = IntegerAddress;
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
                    // Establish the frame pointer register.
                    //
                    // The operation information is not used.
                    //

                case UWOP_SET_FPREG:
                    ContextRecord->Rsp = IntegerRegister[UnwindInfo->FrameRegister];
                    ContextRecord->Rsp -= UnwindInfo->FrameOffset * 16;
                    break;

#ifdef TARGET_UNIX

                    //
                    // Establish the frame pointer register using a large size displacement.
                    // UNWIND_INFO.FrameOffset must be 15 (the maximum value, corresponding to a scaled
                    // offset of 15 * 16 == 240). The next two codes contain a 32-bit offset, which
                    // is also scaled by 16, since the stack must remain 16-bit aligned.
                    //

                case UWOP_SET_FPREG_LARGE:
                    UNWINDER_ASSERT(UnwindInfo->FrameOffset == 15);
                    Index += 2;
                    FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                    FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                    UNWINDER_ASSERT((FrameOffset & 0xF0000000) == 0);
                    ContextRecord->Rsp = IntegerRegister[UnwindInfo->FrameRegister];
                    ContextRecord->Rsp -= FrameOffset * 16;
                    break;

#endif // TARGET_UNIX

                    //
                    // Save nonvolatile integer register on the stack using a
                    // 16-bit displacement.
                    //
                    // The operation information is the register number.
                    //

                case UWOP_SAVE_NONVOL:
                    Index += 1;
                    FrameOffset = UnwindInfo->UnwindCode[Index].FrameOffset * 8;
                    IntegerAddress = (PULONG64)(FrameBase + FrameOffset);
                    IntegerRegister[OpInfo] = MemoryRead64(IntegerAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->IntegerContext[OpInfo] = IntegerAddress;
                    }

                    break;

                    //
                    // Save nonvolatile integer register on the stack using a
                    // 32-bit displacement.
                    //
                    // The operation information is the register number.
                    //

                case UWOP_SAVE_NONVOL_FAR:
                    Index += 2;
                    FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                    FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                    IntegerAddress = (PULONG64)(FrameBase + FrameOffset);
                    IntegerRegister[OpInfo] = MemoryRead64(IntegerAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->IntegerContext[OpInfo] = IntegerAddress;
                    }

                    break;

                    //
                    // Function epilog marker (ignored for prologue unwind).
                    //

               case UWOP_EPILOG:
                    Index += 1;
                    break;

                    //
                    // Spare unused codes.
                    //


                case UWOP_SPARE_CODE:

                    UNWINDER_ASSERT(FALSE);

                    Index += 2;
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
                    FloatingAddress = (PM128A)(FrameBase + FrameOffset);
                    FloatingRegister[OpInfo] = MemoryRead128(FloatingAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->FloatingContext[OpInfo] = FloatingAddress;
                    }

                    break;

                    //
                    // Save a nonvolatile XMM(128) register on the stack using
                    // a 32-bit displacement.
                    //
                    // The operation information is the register number.
                    //

                case UWOP_SAVE_XMM128_FAR:
                    Index += 2;
                    FrameOffset = UnwindInfo->UnwindCode[Index - 1].FrameOffset;
                    FrameOffset += UnwindInfo->UnwindCode[Index].FrameOffset << 16;
                    FloatingAddress = (PM128A)(FrameBase + FrameOffset);
                    FloatingRegister[OpInfo] = MemoryRead128(FloatingAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->FloatingContext[OpInfo] = FloatingAddress;
                    }

                    break;

                    //
                    // Push a machine frame on the stack.
                    //
                    // The operation information determines whether the
                    // machine frame contains an error code or not.
                    //

                case UWOP_PUSH_MACHFRAME:
                    MachineFrame = TRUE;
                    ReturnAddress = (PULONG64)ContextRecord->Rsp;
                    StackAddress = (PULONG64)(ContextRecord->Rsp + (3 * 8));
                    if (OpInfo != 0) {
                        ReturnAddress += 1;
                        StackAddress +=  1;
                    }

                    ContextRecord->Rip = MemoryRead64(ReturnAddress);
                    ContextRecord->Rsp = MemoryRead64(StackAddress);

                    break;

                    //
                    // Unused codes.
                    //

                default:
                    //RtlRaiseStatus(STATUS_BAD_FUNCTION_TABLE);
                    break;
                }

                Index += 1;

            } else {

                //
                // Skip this unwind operation by advancing the slot index
                // by the number of slots consumed by this operation.
                //

                Index += UnwindOpSlots(UnwindInfo->UnwindCode[Index]);
            }
        }

        //
        // If chained unwind information is specified, then set the function
        // entry address to the chained function entry and continue the scan.
        // Otherwise, determine the return address if a machine frame was not
        // encountered during the scan of the unwind codes and terminate the
        // scan.
        //

        if ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) != 0) {

            Index = UnwindInfo->CountOfUnwindCodes;
            if ((Index & 1) != 0) {
                Index += 1;
            }

            // GetUnwindInfo looks for CHAININFO and reads
            // the trailing RUNTIME_FUNCTION so we can just
            // directly use the data sitting in UnwindInfo.
            FunctionEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)
                &UnwindInfo->UnwindCode[Index];
        } else {

            if (MachineFrame == FALSE) {
                ContextRecord->Rip = MemoryRead64((PULONG64)ContextRecord->Rsp);
                ContextRecord->Rsp += 8;
            }

            break;
        }

        //
        // Limit the number of iterations possible for chained function table
        // entries.
        //

        ChainCount += 1;
        UNWINDER_ASSERT(ChainCount <= UNWIND_CHAIN_LIMIT);

    } while (TRUE);

    *FinalFunctionEntry = FunctionEntry;
    return S_OK;
}

HRESULT
OOPStackUnwinderAMD64::VirtualUnwind(
    _In_ DWORD HandlerType,
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PULONG64 EstablisherFrame,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine
    )

/*++

Routine Description:

    This function virtually unwinds the specified function by executing its
    prologue code backward or its epilogue code forward.

    If a context pointers record is specified, then the address where each
    nonvolatile registers is restored from is recorded in the appropriate
    element of the context pointers record.

Arguments:

    HandlerType - Supplies the handler type expected for the virtual unwind.
        This may be either an exception or an unwind handler. A flag may
        optionally be supplied to avoid epilogue detection if it is known
        the specified control PC is not located inside a function epilogue.

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified
        function.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function.

    ContextRecord - Supplies the address of a context record.


    HandlerData - Supplies a pointer to a variable that receives a pointer
        the language handler data.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

    ContextPointers - Supplies an optional pointer to a context pointers
        record.

    HandlerRoutine - Supplies an optional pointer to a variable that receives
        the handler routine address.  If control did not leave the specified
        function in either the prologue or an epilogue and a handler of the
        proper type is associated with the function, then the address of the
        language specific exception handler is returned. Otherwise, NULL is
        returned.
--*/

{

    ULONG64 BranchTarget;
    LONG Displacement;
    ULONG EpilogueOffset = 0;
    ULONG EpilogueSize;
    PEXCEPTION_ROUTINE FoundHandler;
    ULONG FrameRegister = 0;
    ULONG FrameOffset;
    ULONG Index;
    BOOL InEpilogue;
    PULONG64 IntegerAddress;
    PULONG64 IntegerRegister;
    _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryFunctionEntry;
    ULONG PrologOffset;
    ULONG RegisterNumber;
    ULONG RelativePc;
    HRESULT Status;
    PUNWIND_INFO UnwindInfo;
    ULONG UnwindVersion;
    UNWIND_CODE UnwindOp;

    FoundHandler = NULL;
    UnwindInfo = GetUnwindInfo(ImageBase + FunctionEntry->UnwindInfoAddress);
    if (UnwindInfo == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
    }

    UnwindVersion = UnwindInfo->Version;

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

    PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));
    if (UnwindInfo->FrameRegister == 0) {
        *EstablisherFrame = ContextRecord->Rsp;

    } else if ((PrologOffset >= UnwindInfo->SizeOfProlog) ||
               ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) != 0)) {

        FrameOffset = UnwindInfo->FrameOffset;

#ifdef TARGET_UNIX
        // If UnwindInfo->FrameOffset == 15 (the maximum value), then there might be a UWOP_SET_FPREG_LARGE.
        // However, it is still legal for a UWOP_SET_FPREG to set UnwindInfo->FrameOffset == 15 (since this
        // was always part of the specification), so we need to look through the UnwindCode array to determine
        // if there is indeed a UWOP_SET_FPREG_LARGE. If we don't find UWOP_SET_FPREG_LARGE, then just use
        // (scaled) FrameOffset of 240, as before. (We don't verify there is a UWOP_SET_FPREG code, but we could.)
        if (FrameOffset == 15) {
            Index = 0;
            while (Index < UnwindInfo->CountOfUnwindCodes) {
                UnwindOp = UnwindInfo->UnwindCode[Index];
                if (UnwindOp.UnwindOp == UWOP_SET_FPREG_LARGE) {
                    FrameOffset = UnwindInfo->UnwindCode[Index + 1].FrameOffset;
                    FrameOffset += UnwindInfo->UnwindCode[Index + 2].FrameOffset << 16;
                    break;
                }

                Index += UnwindOpSlots(UnwindOp);
            }
        }
#endif // TARGET_UNIX

        *EstablisherFrame = (&ContextRecord->Rax)[UnwindInfo->FrameRegister];
        *EstablisherFrame -= FrameOffset * 16;

    } else {
        FrameOffset = UnwindInfo->FrameOffset;
        Index = 0;
        while (Index < UnwindInfo->CountOfUnwindCodes) {
            UnwindOp = UnwindInfo->UnwindCode[Index];
            if (UnwindOp.UnwindOp == UWOP_SET_FPREG) {
                break;
            }
#ifdef TARGET_UNIX
            else if (UnwindOp.UnwindOp == UWOP_SET_FPREG_LARGE) {
                UNWINDER_ASSERT(UnwindInfo->FrameOffset == 15);
                FrameOffset = UnwindInfo->UnwindCode[Index + 1].FrameOffset;
                FrameOffset += UnwindInfo->UnwindCode[Index + 2].FrameOffset << 16;
                break;
            }
#endif // TARGET_UNIX

            Index += UnwindOpSlots(UnwindOp);
        }

        if (PrologOffset >= UnwindInfo->UnwindCode[Index].CodeOffset) {
            *EstablisherFrame = (&ContextRecord->Rax)[UnwindInfo->FrameRegister];
            *EstablisherFrame -= FrameOffset * 16;

        } else {
            *EstablisherFrame = ContextRecord->Rsp;
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

    InEpilogue = FALSE;
    if (UnwindVersion < 2) {
        InstructionBuffer InstrBuffer = (InstructionBuffer)ControlPc;
        InstructionBuffer NextByte = InstrBuffer;

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

        if ((NextByte[0] == SIZE64_PREFIX) &&
            (NextByte[1] == ADD_IMM8_OP) &&
            (NextByte[2] == 0xc4)) {

            //
            // add rsp, imm8.
            //

            NextByte += 4;

        } else if ((NextByte[0] == SIZE64_PREFIX) &&
                   (NextByte[1] == ADD_IMM32_OP) &&
                   (NextByte[2] == 0xc4)) {

            //
            // add rsp, imm32.
            //

            NextByte += 7;

        } else if (((NextByte[0] & 0xfe) == SIZE64_PREFIX) &&
                (NextByte[1] == LEA_OP)) {

            FrameRegister = ((NextByte[0] & 0x1) << 3) | (NextByte[2] & 0x7);
            if ((FrameRegister != 0) &&
                (FrameRegister == UnwindInfo->FrameRegister)) {

                if ((NextByte[2] & 0xf8) == 0x60) {

                    //
                    // lea rsp, disp8[fp].
                    //

                    NextByte += 4;

                } else if ((NextByte[2] &0xf8) == 0xa0) {

                    //
                    // lea rsp, disp32[fp].
                    //

                    NextByte += 7;
                }
            }
        }

        //
        // Check for any number of:
        //
        //   pop nonvolatile-integer-register[0..15].
        //

        while (TRUE) {
            if ((NextByte[0] & 0xf8) == POP_OP) {
                NextByte += 1;

            } else if (IS_REX_PREFIX(NextByte[0]) &&
                       ((NextByte[1] & 0xf8) == POP_OP)) {

                NextByte += 2;

            } else {
                break;
            }
        }

        //
        // A REPNE prefix may optionally precede a control transfer
        // instruction with no effect on unwinding.
        //

        if (NextByte[0] == REPNE_PREFIX) {
            NextByte += 1;
        }

        //
        // If the next instruction is a return or an appropriate jump, then
        // control is currently in an epilogue and execution of the epilogue
        // should be emulated. Otherwise, execution is not in an epilogue and
        // the prologue should be unwound.
        //

        InEpilogue = FALSE;
        if ( ((NextByte[0] == RET_OP) ||
              (NextByte[0] == RET_OP_2)) ||
            (((NextByte[0] == REP_PREFIX) && (NextByte[1] == RET_OP)))) {

            //
            // A return is an unambiguous indication of an epilogue.
            //

            InEpilogue = TRUE;

        } else if ((NextByte[0] == JMP_IMM8_OP) ||
                   (NextByte[0] == JMP_IMM32_OP)) {

            //
            // An unconditional branch to a target that is equal to the start of
            // or outside of this routine is logically a call to another function.
            //

            BranchTarget = (ULONG64)NextByte - ImageBase;
            if (NextByte[0] == JMP_IMM8_OP) {
                BranchTarget += 2 + (CHAR)NextByte[1];

            } else {
                LONG32 delta = NextByte[1] | (NextByte[2] << 8) |
                               (NextByte[3] << 16) | (NextByte[4] << 24);
                BranchTarget += 5 + delta;

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
                                 BranchTarget + ImageBase);

                if ((PrimaryFunctionEntry == NULL) ||
                    (BranchTarget == PrimaryFunctionEntry->BeginAddress)) {

                    InEpilogue = TRUE;
                }

            } else if ((BranchTarget == FunctionEntry->BeginAddress) &&
                       ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0)) {

                InEpilogue = TRUE;
            }

        } else if ((NextByte[0] == JMP_IND_OP) && (NextByte[1] == 0x25)) {

            //
            // An unconditional jump indirect.
            //
            // This is a jmp outside of the function, probably a tail call
            // to an import function.
            //

            InEpilogue = TRUE;

        } else if (((NextByte[0] & 0xf8) == SIZE64_PREFIX) &&
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
            IntegerRegister = &ContextRecord->Rax;
            NextByte = InstrBuffer;

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

            if ((NextByte[0] & 0xf8) == SIZE64_PREFIX) {

                if (NextByte[1] == ADD_IMM8_OP) {

                    //
                    // add rsp, imm8.
                    //

                    ContextRecord->Rsp += (CHAR)NextByte[3];
                    NextByte += 4;

                }
                else if (NextByte[1] == ADD_IMM32_OP) {

                    //
                    // add rsp, imm32.
                    //

                    Displacement = NextByte[3] | (NextByte[4] << 8);
                    Displacement |= (NextByte[5] << 16) | (NextByte[6] << 24);
                    ContextRecord->Rsp += Displacement;
                    NextByte += 7;

                }
                else if (NextByte[1] == LEA_OP) {
                    if ((NextByte[2] & 0xf8) == 0x60) {

                        //
                        // lea rsp, disp8[frame-register].
                        //

                        ContextRecord->Rsp = IntegerRegister[FrameRegister];
                        ContextRecord->Rsp += (CHAR)NextByte[3];
                        NextByte += 4;

                    }
                    else if ((NextByte[2] & 0xf8) == 0xa0) {

                        //
                        // lea rsp, disp32[frame-register].
                        //

                        Displacement = NextByte[3] | (NextByte[4] << 8);
                        Displacement |= (NextByte[5] << 16) | (NextByte[6] << 24);
                        ContextRecord->Rsp = IntegerRegister[FrameRegister];
                        ContextRecord->Rsp += Displacement;
                        NextByte += 7;
                    }
                }
            }

            //
            // Emulate any number of (if any):
            //
            //   pop nonvolatile-integer-register.
            //

            while (TRUE) {
                if ((NextByte[0] & 0xf8) == POP_OP) {

                    //
                    // pop nonvolatile-integer-register[0..7]
                    //

                    RegisterNumber = NextByte[0] & 0x7;
                    IntegerAddress = (PULONG64)ContextRecord->Rsp;
                    IntegerRegister[RegisterNumber] = MemoryRead64(IntegerAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->IntegerContext[RegisterNumber] = IntegerAddress;
                    }

                    ContextRecord->Rsp += 8;
                    NextByte += 1;

                }
                else if (IS_REX_PREFIX(NextByte[0]) &&
                    (NextByte[1] & 0xf8) == POP_OP) {

                    //
                    // pop nonvolatile-integer-register[8..15]
                    //

                    RegisterNumber = ((NextByte[0] & 1) << 3) | (NextByte[1] & 0x7);
                    IntegerAddress = (PULONG64)ContextRecord->Rsp;
                    IntegerRegister[RegisterNumber] = MemoryRead64(IntegerAddress);

                    if (ARGUMENT_PRESENT(ContextPointers)) {
                        ContextPointers->IntegerContext[RegisterNumber] = IntegerAddress;
                    }

                    ContextRecord->Rsp += 8;
                    NextByte += 2;

                }
                else {
                    break;
                }
            }

            //
            // Emulate return and return null exception handler.
            //
            // Note: This instruction might in fact be a jmp, however
            //       we want to emulate a return regardless.
            //

            ContextRecord->Rip = MemoryRead64((PULONG64)ContextRecord->Rsp);
            ContextRecord->Rsp += 8;
            goto ExitSetHandler;
        }

    } else if (UnwindInfo->CountOfUnwindCodes != 0) {

        UNWINDER_ASSERT(UnwindVersion >= 2);

        //
        // Capture the first unwind code and check if it is an epilogue code.
        // If it is not an epilogue code, the current function entry does not
        // contain any epilogues (it could represent a body region of a
        // separated function or it could represent a function which never
        // returns).
        //

        UnwindOp = UnwindInfo->UnwindCode[0];
        if (UnwindOp.UnwindOp == UWOP_EPILOG) {
            EpilogueSize = UnwindOp.CodeOffset;

            UNWINDER_ASSERT(EpilogueSize != 0);
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

            RelativePc = (ULONG)(ControlPc - ImageBase);
            if ((UnwindOp.OpInfo & 1) != 0) {
                EpilogueOffset = FunctionEntry->EndAddress - EpilogueSize;
                if (RelativePc - EpilogueOffset < EpilogueSize) {
                    InEpilogue = TRUE;
                }
            }

            if (InEpilogue == FALSE) {
                for (Index = 1; Index < UnwindInfo->CountOfUnwindCodes; Index += 1) {
                    UnwindOp = UnwindInfo->UnwindCode[Index];

                    if (UnwindOp.UnwindOp == UWOP_EPILOG) {
                        EpilogueOffset = UnwindOp.EpilogueCode.OffsetLow +
                                            UnwindOp.EpilogueCode.OffsetHigh * 256;

                        //
                        // An epilogue offset of 0 indicates that this is
                        // a padding entry (the number of epilogue codes
                        // is a multiple of 2).
                        //

                        if (EpilogueOffset == 0) {
                            break;
                        }

                        EpilogueOffset = FunctionEntry->EndAddress - EpilogueOffset;
                        if (RelativePc - EpilogueOffset < EpilogueSize) {

                            UNWINDER_ASSERT(EpilogueOffset != FunctionEntry->EndAddress);
                            InEpilogue = TRUE;
                            break;
                        }

                    } else {
                        break;
                    }
                }
            }

            if (InEpilogue != FALSE) {
                Status = UnwindEpilogue(ImageBase,
                                        ControlPc,
                                        RelativePc - EpilogueOffset,
                                        FunctionEntry,
                                        ContextRecord,
                                        ContextPointers);

                goto ExitSetHandler;
            }
        }
    }

    //
    // Control left the specified function outside an epilogue. Unwind the
    // subject function and any chained unwind information.
    //

    Status = UnwindPrologue(ImageBase,
                            ControlPc,
                            *EstablisherFrame,
                            FunctionEntry,
                            ContextRecord,
                            ContextPointers,
                            &FunctionEntry);

    if (Status != S_OK) {
        return Status;
    }

    //
    // If control left the specified function outside of the prologue and
    // the function has a handler that matches the specified type, then
    // return the address of the language specific exception handler.
    // Otherwise, return NULL.
    //

    if (HandlerType != 0) {
        PrologOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));
        UnwindInfo = GetUnwindInfo(FunctionEntry->UnwindInfoAddress + ImageBase);
        if (UnwindInfo == NULL)
        {
            return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
        }
        if ((PrologOffset >= UnwindInfo->SizeOfProlog) &&
            ((UnwindInfo->Flags & HandlerType) != 0)) {

            Index = UnwindInfo->CountOfUnwindCodes;
            if ((Index & 1) != 0) {
                Index += 1;
            }

            *HandlerData = &UnwindInfo->UnwindCode[Index + 2];
            FoundHandler = (PEXCEPTION_ROUTINE)(*((PULONG)&UnwindInfo->UnwindCode[Index]) + ImageBase);
        }
    }

ExitSetHandler:
    if (ARGUMENT_PRESENT(HandlerRoutine)) {
        *HandlerRoutine = FoundHandler;
    }

    return S_OK;
}

_PIMAGE_RUNTIME_FUNCTION_ENTRY
OOPStackUnwinderAMD64::LookupPrimaryFunctionEntry(
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    _In_ ULONG64 ImageBase

    )

/*++

Routine Description:

    This function determines whether the supplied function entry is a primary
    function entry or a chained function entry. If it is a chained function
    entry, then the primary function entry is returned.

Arguments:

    FunctionEntry - Supplies a pointer to the function entry for which the
        associated primary function entry will be located.

    ImageBase - Supplies the base address of the image containing the
        supplied function entry.

Return Value:

    A pointer to the primary function entry is returned as the function value.

--*/

{

    ULONG ChainCount;
    ULONG Index;
    PUNWIND_INFO UnwindInfo;

    //
    // Locate the unwind information and determine whether it is chained.
    // If the unwind information is chained, then locate the parent function
    // entry and loop again.
    //

    ChainCount = 0;
    do {
        UnwindInfo = GetUnwindInfo(FunctionEntry->UnwindInfoAddress + ImageBase);
        if ((UnwindInfo == NULL) || ((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0))
        {
            break;
        }

        Index = UnwindInfo->CountOfUnwindCodes;
        if ((Index & 1) != 0) {
            Index += 1;
        }

        FunctionEntry = (_PIMAGE_RUNTIME_FUNCTION_ENTRY)&UnwindInfo->UnwindCode[Index];

        //
        // Limit the number of iterations possible for chained function table
        // entries.
        //

        ChainCount += 1;
        UNWINDER_ASSERT(ChainCount <= UNWIND_CHAIN_LIMIT);

    } while (TRUE);

    return FunctionEntry;
}

_PIMAGE_RUNTIME_FUNCTION_ENTRY
OOPStackUnwinderAMD64::SameFunction(
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc
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
    _PIMAGE_RUNTIME_FUNCTION_ENTRY PrimaryFunctionEntry;
    _IMAGE_RUNTIME_FUNCTION_ENTRY TargetFunctionEntry;
    ULONG64 TargetImageBase;

    //
    // Find the unwind information referenced by the primary function entry
    // associated with the specified function entry.
    //

    PrimaryFunctionEntry = LookupPrimaryFunctionEntry(FunctionEntry,
                                                      ImageBase);

    //
    // Determine the function entry containing the control Pc and similarly
    // resolve its primary function entry.  If no function entry can be
    // found then the control pc resides in a different function.
    //

    if (GetModuleBase(ControlPc, &TargetImageBase) != S_OK ||
        GetFunctionEntry(ControlPc,
                         &TargetFunctionEntry,
                         sizeof(TargetFunctionEntry)) != S_OK) {
        return NULL;
    }

    //
    // Lookup the primary function entry associated with the target function
    // entry.
    //

    TargetFunctionEntry = *LookupPrimaryFunctionEntry(&TargetFunctionEntry,
                                                      TargetImageBase);

    //
    // If the beginning offset of the two function entries are equal, then
    // return the address of the primary function entry. Otherwise, return
    // NULL.
    //

    if (PrimaryFunctionEntry->BeginAddress ==  TargetFunctionEntry.BeginAddress) {
        return PrimaryFunctionEntry;

    } else {
        return NULL;
    }
}

ULONG OOPStackUnwinderAMD64::UnwindOpSlots(_In_ UNWIND_CODE UnwindCode)
/*++

Routine Description:

    This routine determines the number of unwind code slots ultimately
    consumed by an unwind code sequence.

Arguments:

    UnwindCode - Supplies the first unwind code in the sequence.

Return Value:

    Returns the total count of the number of slots consumed by the unwind
    code sequence.

--*/
{

    ULONG Slots;
    ULONG UnwindOp;

    //
    // UWOP_SPARE_CODE may be found in very old x64 images.
    //

    UnwindOp = UnwindCode.UnwindOp;

    UNWINDER_ASSERT(UnwindOp != UWOP_SPARE_CODE);
    UNWINDER_ASSERT(UnwindOp < sizeof(UnwindOpExtraSlotTable));

    Slots = UnwindOpExtraSlotTable[UnwindOp] + 1;
    if ((UnwindOp == UWOP_ALLOC_LARGE) && (UnwindCode.OpInfo != 0)) {
        Slots += 1;
    }

    return Slots;
}

