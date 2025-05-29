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

#ifndef __gmsx86_h__
#define __gmsx86_h__

#define __gmsx86_h__

// A MachState indicates the register state of the processor at some point in time (usually
// just before or after a call is made).  It can be made one of two ways.  Either explicitly
// (when you for some reason know the values of all the registers), or implicitly using the
// GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState {

    MachState()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        INDEBUG(memset(this, 0xCC, sizeof(MachState));)
    }

    bool   isValid()     { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pRetAddr) != INVALID_POINTER_CC); return(_pRetAddr != 0); }
    TADDR* pEdi()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEdi) != INVALID_POINTER_CC); return(_pEdi); }
    TADDR* pEsi()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEsi) != INVALID_POINTER_CC); return(_pEsi); }
    TADDR* pEbx()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEbx) != INVALID_POINTER_CC); return(_pEbx); }
    TADDR* pEbp()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEbp) != INVALID_POINTER_CC); return(_pEbp); }
    TADDR  esp()         { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_esp); }
    PTR_TADDR pRetAddr() { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_pRetAddr); }
    TADDR  GetRetAddr()  { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return *_pRetAddr; }
#ifndef DACCESS_COMPILE
    void SetRetAddr(TADDR* addr) { LIMITED_METHOD_CONTRACT; _ASSERTE(isValid()); _pRetAddr = addr; }
#endif

    friend class CheckAsmOffsets;

protected:
    // Note the fields are laid out to make generating a
    // MachState structure from assembly code very easy

    // The state of all the callee saved registers.
    // If the register has been spill to the stack p<REG>
    // points at this location, otherwise it points
    // at the field <REG> field itself
    PTR_TADDR _pEdi;
    TADDR     _edi;
    PTR_TADDR _pEsi;
    TADDR     _esi;
    PTR_TADDR _pEbx;
    TADDR     _ebx;
    PTR_TADDR _pEbp;
    TADDR     _ebp;

    TADDR     _esp;       // stack pointer after the function returns
    PTR_TADDR _pRetAddr;  // The address of the stored IP address (points into the stack)
};

#endif
