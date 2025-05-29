// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/*                       gmscpu.h                             */
/**************************************************************/
/* HelperFrame is defines 'GET_STATE(machState)' macro, which
   figures out what the state of the machine will be when the
   current method returns.  It then stores the state in the
   JIT_machState structure.  */

/**************************************************************/

#ifndef __gmsAMD64_h__
#define __gmsAMD64_h__

// A MachState indicates the register state of the processor at some point in time (usually
// just before or after a call is made).  It can be made one of two ways.  Either explicitly
// (when you for some reason know the values of all the registers), or implicitly using the
// GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState
{
    MachState()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        INDEBUG(memset((void*)this, 0xCC, sizeof(MachState));)
    }

    bool   isValid()    { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pRetAddr) != INVALID_POINTER_CC); return(_pRetAddr != nullptr); }
    TADDR* pRetAddr()   { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_pRetAddr); }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return *_pRetAddr; }
#ifndef DACCESS_COMPILE
    void SetRetAddr(TADDR* addr) { _ASSERTE(isValid()); _pRetAddr = addr; }
#endif

    friend class CheckAsmOffsets;

protected:
    PCODE m_Rip;
    TADDR m_Rsp;

    // context pointers for preserved registers
    CalleeSavedRegistersPointers m_Ptrs;

    PTR_TADDR _pRetAddr;

#ifdef TARGET_UNIX
    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, preserve
    // the unwound values.
    CalleeSavedRegisters m_Unwound;
#endif
};

#endif // __gmsAMD64_h__
