// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __regdisplay_h__
#define __regdisplay_h__

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#include "PalRedhawkCommon.h" // Fp128

struct REGDISPLAY
{
    PTR_UIntNative pRax;
    PTR_UIntNative pRcx;
    PTR_UIntNative pRdx;
    PTR_UIntNative pRbx;
    //           pEsp;
    PTR_UIntNative pRbp;
    PTR_UIntNative pRsi;
    PTR_UIntNative pRdi;
#ifdef TARGET_AMD64
    PTR_UIntNative pR8;
    PTR_UIntNative pR9;
    PTR_UIntNative pR10;
    PTR_UIntNative pR11;
    PTR_UIntNative pR12;
    PTR_UIntNative pR13;
    PTR_UIntNative pR14;
    PTR_UIntNative pR15;
#endif // TARGET_AMD64

    uintptr_t   SP;
    PCODE        IP;

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    Fp128          Xmm[16-6]; // preserved xmm6..xmm15 regs for EH stackwalk
                              // these need to be unwound during a stack walk
                              // for EH, but not adjusted, so we only need
                              // their values, not their addresses
#endif // TARGET_AMD64 && !UNIX_AMD64_ABI

    inline PCODE GetIP() { return IP; }
    inline uintptr_t GetSP() { return SP; }
    inline uintptr_t GetFP() { return *pRbp; }
    inline uintptr_t GetPP() { return *pRbx; }

    inline void SetIP(PCODE IP) { this->IP = IP; }
    inline void SetSP(uintptr_t SP) { this->SP = SP; }
};

#elif defined(TARGET_ARM)

struct REGDISPLAY
{
    PTR_UIntNative pR0;
    PTR_UIntNative pR1;
    PTR_UIntNative pR2;
    PTR_UIntNative pR3;
    PTR_UIntNative pR4;
    PTR_UIntNative pR5;
    PTR_UIntNative pR6;
    PTR_UIntNative pR7;
    PTR_UIntNative pR8;
    PTR_UIntNative pR9;
    PTR_UIntNative pR10;
    PTR_UIntNative pR11;
    PTR_UIntNative pR12;
    PTR_UIntNative pLR;

    uintptr_t   SP;
    PCODE        IP;

    uint64_t       D[16-8]; // preserved D registers D8..D15 (note that D16-D31 are not preserved according to the ABI spec)
                          // these need to be unwound during a stack walk
                          // for EH, but not adjusted, so we only need
                          // their values, not their addresses

    inline PCODE GetIP() { return IP; }
    inline uintptr_t GetSP() { return SP; }
    inline uintptr_t GetFP() { return *pR11; }
    inline void SetIP(PCODE IP) { this->IP = IP; }
    inline void SetSP(uintptr_t SP) { this->SP = SP; }
};

#elif defined(TARGET_ARM64)

struct REGDISPLAY
{
    PTR_UIntNative pX0;
    PTR_UIntNative pX1;
    PTR_UIntNative pX2;
    PTR_UIntNative pX3;
    PTR_UIntNative pX4;
    PTR_UIntNative pX5;
    PTR_UIntNative pX6;
    PTR_UIntNative pX7;
    PTR_UIntNative pX8;
    PTR_UIntNative pX9;
    PTR_UIntNative pX10;
    PTR_UIntNative pX11;
    PTR_UIntNative pX12;
    PTR_UIntNative pX13;
    PTR_UIntNative pX14;
    PTR_UIntNative pX15;
    PTR_UIntNative pX16;
    PTR_UIntNative pX17;
    PTR_UIntNative pX18;
    PTR_UIntNative pX19;
    PTR_UIntNative pX20;
    PTR_UIntNative pX21;
    PTR_UIntNative pX22;
    PTR_UIntNative pX23;
    PTR_UIntNative pX24;
    PTR_UIntNative pX25;
    PTR_UIntNative pX26;
    PTR_UIntNative pX27;
    PTR_UIntNative pX28;
    PTR_UIntNative pFP; // X29
    PTR_UIntNative pLR; // X30

    uintptr_t   SP;
    PCODE        IP;

    uint64_t       D[16-8]; // Only the bottom 64-bit value of the V registers V8..V15 needs to be preserved
                          // (V0-V7 and V16-V31 are not preserved according to the ABI spec).
                          // These need to be unwound during a stack walk
                          // for EH, but not adjusted, so we only need
                          // their values, not their addresses

    inline PCODE GetIP() { return IP; }
    inline uintptr_t GetSP() { return SP; }
    inline uintptr_t GetFP() { return *pFP; }

    inline void SetIP(PCODE IP) { this->IP = IP; }
    inline void SetSP(uintptr_t SP) { this->SP = SP; }
};
#elif defined(TARGET_WASM)

struct REGDISPLAY
{
    // TODO: WebAssembly doesn't really have registers. What exactly do we need here?

    uintptr_t   SP;
    PCODE        IP;

    inline PCODE GetIP() { return NULL; }
    inline uintptr_t GetSP() { return 0; }
    inline uintptr_t GetFP() { return 0; }

    inline void SetIP(PCODE IP) { }
    inline void SetSP(uintptr_t SP) { }
};
#endif // HOST_X86 || HOST_AMD64 || HOST_ARM || HOST_ARM64 || HOST_WASM

typedef REGDISPLAY * PREGDISPLAY;

#endif //__regdisplay_h__
