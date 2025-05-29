// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __gmscpu_h__
#define __gmscpu_h__

#define __gmscpu_h__

    // A MachState indicates the register state of the processor at some point in time (usually
    // just before or after a call is made).  It can be made one of two ways.  Either explicitly
    // (when you for some reason know the values of all the registers), or implicitly using the
    // GET_STATE macros.

    typedef DPTR(struct MachState) PTR_MachState;
    struct MachState {
    
        BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
        TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return 0; }
    
        friend class CheckAsmOffsets;
    };

    #endif
