// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __regdisplay_h__
#define __regdisplay_h__

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#include "PalRedhawkCommon.h" // Fp128

struct REGDISPLAY
{
    PTR_uintptr_t pRax;
    PTR_uintptr_t pRcx;
    PTR_uintptr_t pRdx;
    PTR_uintptr_t pRbx;
    //           pEsp;
    PTR_uintptr_t pRbp;
    PTR_uintptr_t pRsi;
    PTR_uintptr_t pRdi;
#ifdef TARGET_AMD64
    PTR_uintptr_t pR8;
    PTR_uintptr_t pR9;
    PTR_uintptr_t pR10;
    PTR_uintptr_t pR11;
    PTR_uintptr_t pR12;
    PTR_uintptr_t pR13;
    PTR_uintptr_t pR14;
    PTR_uintptr_t pR15;
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

#ifdef TARGET_X86
    TADDR PCTAddr;

    inline unsigned long *GetEbpLocation() { return (unsigned long *)pRbp; }
    inline unsigned long *GetEbxLocation() { return (unsigned long *)pRbx; }
    inline unsigned long *GetEsiLocation() { return (unsigned long *)pRsi; }
    inline unsigned long *GetEdiLocation() { return (unsigned long *)pRdi; }

    inline void SetEaxLocation(unsigned long *loc) { pRax = (PTR_uintptr_t)loc; }
    inline void SetEcxLocation(unsigned long *loc) { pRcx = (PTR_uintptr_t)loc; }
    inline void SetEdxLocation(unsigned long *loc) { pRdx = (PTR_uintptr_t)loc; }
    inline void SetEbxLocation(unsigned long *loc) { pRbx = (PTR_uintptr_t)loc; }
    inline void SetEsiLocation(unsigned long *loc) { pRsi = (PTR_uintptr_t)loc; }
    inline void SetEdiLocation(unsigned long *loc) { pRdi = (PTR_uintptr_t)loc; }
    inline void SetEbpLocation(unsigned long *loc) { pRbp = (PTR_uintptr_t)loc; }
#endif
};

inline TADDR GetRegdisplayFP(REGDISPLAY *display)
{
    return (TADDR)*display->GetEbpLocation();
}

inline LPVOID GetRegdisplayFPAddress(REGDISPLAY *display)
{
    return (LPVOID)display->GetEbpLocation();
}

inline void SetRegdisplayPCTAddr(REGDISPLAY *display, TADDR addr)
{
    display->PCTAddr = addr;
    display->SetIP(*PTR_PCODE(addr));
}

#elif defined(TARGET_ARM)

struct REGDISPLAY
{
    PTR_uintptr_t pR0;
    PTR_uintptr_t pR1;
    PTR_uintptr_t pR2;
    PTR_uintptr_t pR3;
    PTR_uintptr_t pR4;
    PTR_uintptr_t pR5;
    PTR_uintptr_t pR6;
    PTR_uintptr_t pR7;
    PTR_uintptr_t pR8;
    PTR_uintptr_t pR9;
    PTR_uintptr_t pR10;
    PTR_uintptr_t pR11;
    PTR_uintptr_t pR12;
    PTR_uintptr_t pLR;

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
    PTR_uintptr_t pX0;
    PTR_uintptr_t pX1;
    PTR_uintptr_t pX2;
    PTR_uintptr_t pX3;
    PTR_uintptr_t pX4;
    PTR_uintptr_t pX5;
    PTR_uintptr_t pX6;
    PTR_uintptr_t pX7;
    PTR_uintptr_t pX8;
    PTR_uintptr_t pX9;
    PTR_uintptr_t pX10;
    PTR_uintptr_t pX11;
    PTR_uintptr_t pX12;
    PTR_uintptr_t pX13;
    PTR_uintptr_t pX14;
    PTR_uintptr_t pX15;
    PTR_uintptr_t pX16;
    PTR_uintptr_t pX17;
    PTR_uintptr_t pX18;
    PTR_uintptr_t pX19;
    PTR_uintptr_t pX20;
    PTR_uintptr_t pX21;
    PTR_uintptr_t pX22;
    PTR_uintptr_t pX23;
    PTR_uintptr_t pX24;
    PTR_uintptr_t pX25;
    PTR_uintptr_t pX26;
    PTR_uintptr_t pX27;
    PTR_uintptr_t pX28;
    PTR_uintptr_t pFP; // X29
    PTR_uintptr_t pLR; // X30

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
