// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "daccess.h"
#include "rhassert.h"

#define UNW_STEP_SUCCESS 1
#define UNW_STEP_END     0

#if defined(TARGET_APPLE)
#include <mach-o/getsect.h>
#endif

#include <regdisplay.h>
#include "UnwindHelpers.h"

// libunwind headers
#include <libunwind.h>
#include <external/llvm-libunwind/src/config.h>
#include <external/llvm-libunwind/src/Registers.hpp>
#include <external/llvm-libunwind/src/AddressSpace.hpp>
#if defined(TARGET_ARM)
#include <external/llvm-libunwind/src/libunwind_ext.h>
#endif
#include <external/llvm-libunwind/src/UnwindCursor.hpp>


#if defined(TARGET_AMD64)
using libunwind::Registers_x86_64;
using libunwind::CompactUnwinder_x86_64;
#elif defined(TARGET_ARM)
using libunwind::Registers_arm;
#elif defined(TARGET_ARM64)
using libunwind::Registers_arm64;
using libunwind::CompactUnwinder_arm64;
#elif defined(TARGET_X86)
using libunwind::Registers_x86;
#else
#error "Unwinding is not implemented for this architecture yet."
#endif
using libunwind::LocalAddressSpace;
using libunwind::EHHeaderParser;
#if _LIBUNWIND_SUPPORT_DWARF_UNWIND
using libunwind::DwarfInstructions;
#endif
using libunwind::UnwindInfoSections;

LocalAddressSpace _addressSpace;

#ifdef TARGET_AMD64

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    static int  getArch() { return libunwind::REGISTERS_X86_64; }

    inline uint64_t getRegister(int regNum) const
    {
        switch (regNum)
        {
        case UNW_REG_IP:
        case UNW_X86_64_RIP:
            return IP;
        case UNW_REG_SP:
            return SP;
        case UNW_X86_64_RAX:
            return *pRax;
        case UNW_X86_64_RDX:
            return *pRdx;
        case UNW_X86_64_RCX:
            return *pRcx;
        case UNW_X86_64_RBX:
            return *pRbx;
        case UNW_X86_64_RSI:
            return *pRsi;
        case UNW_X86_64_RDI:
            return *pRdi;
        case UNW_X86_64_RBP:
            return *pRbp;
        case UNW_X86_64_RSP:
            return SP;
        case UNW_X86_64_R8:
            return *pR8;
        case UNW_X86_64_R9:
            return *pR9;
        case UNW_X86_64_R10:
            return *pR10;
        case UNW_X86_64_R11:
            return *pR11;
        case UNW_X86_64_R12:
            return *pR12;
        case UNW_X86_64_R13:
            return *pR13;
        case UNW_X86_64_R14:
            return *pR14;
        case UNW_X86_64_R15:
            return *pR15;
        }

        // Unsupported register requested
        abort();
    }

    inline void setRegister(int regNum, uint64_t value, uint64_t location)
    {
        switch (regNum)
        {
        case UNW_REG_IP:
        case UNW_X86_64_RIP:
            IP = value;
            return;
        case UNW_REG_SP:
            SP = value;
            return;
        case UNW_X86_64_RAX:
            pRax = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RDX:
            pRdx = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RCX:
            pRcx = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RBX:
            pRbx = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RSI:
            pRsi = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RDI:
            pRdi = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RBP:
            pRbp = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_RSP:
            SP = value;
            return;
        case UNW_X86_64_R8:
            pR8 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R9:
            pR9 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R10:
            pR10 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R11:
            pR11 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R12:
            pR12 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R13:
            pR13 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R14:
            pR14 = (PTR_UIntNative)location;
            return;
        case UNW_X86_64_R15:
            pR15 = (PTR_UIntNative)location;
            return;
        }

        // Unsupported x86_64 register
        abort();
    }

    // N/A for x86_64
    inline bool validFloatRegister(int) { return false; }
    inline bool validVectorRegister(int) { return false; }

    inline static int  lastDwarfRegNum() { return 16; }

    inline bool validRegister(int regNum) const
    {
        if (regNum == UNW_REG_IP)
            return true;
        if (regNum == UNW_REG_SP)
            return true;
        if (regNum < 0)
            return false;
        if (regNum > 16)
            return false;
        return true;
    }

    // N/A for x86_64
    inline double getFloatRegister(int) const { abort(); }
    inline   void setFloatRegister(int, double) { abort(); }
    inline double getVectorRegister(int) const { abort(); }
    inline   void setVectorRegister(int, ...) { abort(); }

    uint64_t  getSP() const { return SP; }
    void      setSP(uint64_t value, uint64_t location) { SP = value; }

    uint64_t  getIP() const { return IP; }

    void      setIP(uint64_t value, uint64_t location) { IP = value; }

    uint64_t  getRBP() const { return *pRbp; }
    void      setRBP(uint64_t value, uint64_t location) { pRbp = (PTR_UIntNative)location; }
    uint64_t  getRBX() const { return *pRbx; }
    void      setRBX(uint64_t value, uint64_t location) { pRbx = (PTR_UIntNative)location; }
    uint64_t  getR12() const { return *pR12; }
    void      setR12(uint64_t value, uint64_t location) { pR12 = (PTR_UIntNative)location; }
    uint64_t  getR13() const { return *pR13; }
    void      setR13(uint64_t value, uint64_t location) { pR13 = (PTR_UIntNative)location; }
    uint64_t  getR14() const { return *pR14; }
    void      setR14(uint64_t value, uint64_t location) { pR14 = (PTR_UIntNative)location; }
    uint64_t  getR15() const { return *pR15; }
    void      setR15(uint64_t value, uint64_t location) { pR15 = (PTR_UIntNative)location; }
};

#endif // TARGET_AMD64
#if defined(TARGET_X86)
struct Registers_REGDISPLAY : REGDISPLAY
{
    static int  getArch() { return libunwind::REGISTERS_X86; }

    inline uint64_t getRegister(int regNum) const
    {
        switch (regNum)
        {
        case UNW_REG_IP:
            return IP;
        case UNW_REG_SP:
            return SP;
        case UNW_X86_EAX:
            return *pRax;
        case UNW_X86_EDX:
            return *pRdx;
        case UNW_X86_ECX:
            return *pRcx;
        case UNW_X86_EBX:
            return *pRbx;
        case UNW_X86_ESI:
            return *pRsi;
        case UNW_X86_EDI:
            return *pRdi;
        case UNW_X86_EBP:
            return *pRbp;
        case UNW_X86_ESP:
            return SP;
        }

        // Unsupported register requested
        abort();
    }

    inline void setRegister(int regNum, uint64_t value, uint64_t location)
    {
        switch (regNum)
        {
        case UNW_REG_IP:
            IP = value;
            return;
        case UNW_REG_SP:
            SP = value;
            return;
        case UNW_X86_EAX:
            pRax = (PTR_UIntNative)location;
            return;
        case UNW_X86_EDX:
            pRdx = (PTR_UIntNative)location;
            return;
        case UNW_X86_ECX:
            pRcx = (PTR_UIntNative)location;
            return;
        case UNW_X86_EBX:
            pRbx = (PTR_UIntNative)location;
            return;
        case UNW_X86_ESI:
            pRsi = (PTR_UIntNative)location;
            return;
        case UNW_X86_EDI:
            pRdi = (PTR_UIntNative)location;
            return;
        case UNW_X86_EBP:
            pRbp = (PTR_UIntNative)location;
            return;
        case UNW_X86_ESP:
            SP = value;
            return;
        }

        // Unsupported x86_64 register
        abort();
    }

    // N/A for x86
    inline bool validFloatRegister(int) { return false; }
    inline bool validVectorRegister(int) { return false; }

    inline static int  lastDwarfRegNum() { return 16; }

    inline bool validRegister(int regNum) const
    {
        if (regNum == UNW_REG_IP)
            return true;
        if (regNum == UNW_REG_SP)
            return true;
        if (regNum < 0)
            return false;
        if (regNum > 15)
            return false;
        return true;
    }

    // N/A for x86
    inline double getFloatRegister(int) const { abort(); }
    inline   void setFloatRegister(int, double) { abort(); }
    inline double getVectorRegister(int) const { abort(); }
    inline   void setVectorRegister(int, ...) { abort(); }

    void      setSP(uint64_t value, uint64_t location) { SP = value; }

    uint64_t  getIP() const { return IP; }

    void      setIP(uint64_t value, uint64_t location) { IP = value; }

    uint64_t  getEBP() const { return *pRbp; }
    void      setEBP(uint64_t value, uint64_t location) { pRbp = (PTR_UIntNative)location; }
    uint64_t  getEBX() const { return *pRbx; }
    void      setEBX(uint64_t value, uint64_t location) { pRbx = (PTR_UIntNative)location; }
};

#endif // TARGET_X86
#if defined(TARGET_ARM)

struct Registers_REGDISPLAY : REGDISPLAY
{
    inline static int  getArch() { return libunwind::REGISTERS_ARM; }
    inline static int  lastDwarfRegNum() { return _LIBUNWIND_HIGHEST_DWARF_REGISTER_ARM; }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) { return false; };
    bool        validVectorRegister(int num) const { return false; };

    uint32_t    getRegister(int num) const;
    void        setRegister(int num, uint32_t value, uint32_t location);

    double      getFloatRegister(int num) const {abort();}
    void        setFloatRegister(int num, double value) {abort();}

    libunwind::v128    getVectorRegister(int num) const {abort();}
    void        setVectorRegister(int num, libunwind::v128 value) {abort();}

    uint32_t    getSP() const         { return SP;}
    void        setSP(uint32_t value, uint32_t location) { SP = value;}
    uint32_t    getIP() const         { return IP;}
    void        setIP(uint32_t value, uint32_t location) { IP = value; }
    uint32_t    getFP() const         { return *pR11;}
    void        setFP(uint32_t value, uint32_t location) { pR11 = (PTR_UIntNative)location;}
};

inline bool Registers_REGDISPLAY::validRegister(int num) const {
    if (num == UNW_REG_SP || num == UNW_ARM_SP)
        return true;

    if (num == UNW_ARM_LR)
        return true;

    if (num == UNW_REG_IP || num == UNW_ARM_IP)
        return true;

    if (num >= UNW_ARM_R0 && num <= UNW_ARM_R12)
        return true;

    return false;
}

inline uint32_t Registers_REGDISPLAY::getRegister(int regNum) const {
    if (regNum == UNW_REG_SP || regNum == UNW_ARM_SP)
        return SP;

    if (regNum == UNW_ARM_LR)
        return *pLR;

    if (regNum == UNW_REG_IP || regNum == UNW_ARM_IP)
        return IP;

    switch (regNum)
    {
    case (UNW_ARM_R0):
        return *pR0;
    case (UNW_ARM_R1):
        return *pR1;
    case (UNW_ARM_R2):
        return *pR2;
    case (UNW_ARM_R3):
        return *pR3;
    case (UNW_ARM_R4):
        return *pR4;
    case (UNW_ARM_R5):
        return *pR5;
    case (UNW_ARM_R6):
        return *pR6;
    case (UNW_ARM_R7):
        return *pR7;
    case (UNW_ARM_R8):
        return *pR8;
    case (UNW_ARM_R9):
        return *pR9;
    case (UNW_ARM_R10):
        return *pR10;
    case (UNW_ARM_R11):
        return *pR11;
    case (UNW_ARM_R12):
        return *pR12;
    }

    PORTABILITY_ASSERT("unsupported arm register");
}

void Registers_REGDISPLAY::setRegister(int num, uint32_t value, uint32_t location)
{
    if (num == UNW_REG_SP || num == UNW_ARM_SP) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_ARM_LR) {
        pLR = (PTR_UIntNative)location;
        return;
    }

    if (num == UNW_REG_IP || num == UNW_ARM_IP) {
        IP = value;
        return;
    }

    switch (num)
    {
    case (UNW_ARM_R0):
        pR0 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R1):
        pR1 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R2):
        pR2 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R3):
        pR3 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R4):
        pR4 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R5):
        pR5 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R6):
        pR6 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R7):
        pR7 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R8):
        pR8 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R9):
        pR9 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R10):
        pR10 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R11):
        pR11 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM_R12):
        pR12 = (PTR_UIntNative)location;
        break;
    default:
        PORTABILITY_ASSERT("unsupported arm register");
    }
}

#endif // TARGET_ARM

#if defined(TARGET_ARM64)

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    inline static int  getArch() { return libunwind::REGISTERS_ARM64; }
    inline static int  lastDwarfRegNum() { return _LIBUNWIND_HIGHEST_DWARF_REGISTER_ARM64; }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) { return false; };
    bool        validVectorRegister(int num) const;

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value, uint64_t location);

    double      getFloatRegister(int num) const {abort();}
    void        setFloatRegister(int num, double value) {abort();}

    libunwind::v128    getVectorRegister(int num) const;
    void        setVectorRegister(int num, libunwind::v128 value);

    uint64_t    getSP() const         { return SP;}
    void        setSP(uint64_t value, uint64_t location) { SP = value;}
    uint64_t    getIP() const         { return IP;}
    void        setIP(uint64_t value, uint64_t location) { IP = value; }
    uint64_t    getFP() const         { return *pFP;}
    void        setFP(uint64_t value, uint64_t location) { pFP = (PTR_UIntNative)location;}
};

inline bool Registers_REGDISPLAY::validRegister(int num) const {
    if (num == UNW_REG_SP || num == UNW_ARM64_SP)
        return true;

    if (num == UNW_ARM64_FP)
        return true;

    if (num == UNW_ARM64_LR)
        return true;

    if (num == UNW_REG_IP)
        return true;

    if (num >= UNW_ARM64_X0 && num <= UNW_ARM64_X28)
        return true;

    return false;
}

bool Registers_REGDISPLAY::validVectorRegister(int num) const
{
    if (num >= UNW_ARM64_D8 && num <= UNW_ARM64_D15)
        return true;

    return false;
}

inline uint64_t Registers_REGDISPLAY::getRegister(int regNum) const {
    if (regNum == UNW_REG_SP || regNum == UNW_ARM64_SP)
        return SP;

    if (regNum == UNW_ARM64_FP)
        return *pFP;

    if (regNum == UNW_ARM64_LR)
        return *pLR;

    if (regNum == UNW_REG_IP)
        return IP;

    switch (regNum)
    {
    case (UNW_ARM64_X0):
        return *pX0;
    case (UNW_ARM64_X1):
        return *pX1;
    case (UNW_ARM64_X2):
        return *pX2;
    case (UNW_ARM64_X3):
        return *pX3;
    case (UNW_ARM64_X4):
        return *pX4;
    case (UNW_ARM64_X5):
        return *pX5;
    case (UNW_ARM64_X6):
        return *pX6;
    case (UNW_ARM64_X7):
        return *pX7;
    case (UNW_ARM64_X8):
        return *pX8;
    case (UNW_ARM64_X9):
        return *pX9;
    case (UNW_ARM64_X10):
        return *pX10;
    case (UNW_ARM64_X11):
        return *pX11;
    case (UNW_ARM64_X12):
        return *pX12;
    case (UNW_ARM64_X13):
        return *pX13;
    case (UNW_ARM64_X14):
        return *pX14;
    case (UNW_ARM64_X15):
        return *pX15;
    case (UNW_ARM64_X16):
        return *pX16;
    case (UNW_ARM64_X17):
        return *pX17;
    case (UNW_ARM64_X18):
        return *pX18;
    case (UNW_ARM64_X19):
        return *pX19;
    case (UNW_ARM64_X20):
        return *pX20;
    case (UNW_ARM64_X21):
        return *pX21;
    case (UNW_ARM64_X22):
        return *pX22;
    case (UNW_ARM64_X23):
        return *pX23;
    case (UNW_ARM64_X24):
        return *pX24;
    case (UNW_ARM64_X25):
        return *pX25;
    case (UNW_ARM64_X26):
        return *pX26;
    case (UNW_ARM64_X27):
        return *pX27;
    case (UNW_ARM64_X28):
        return *pX28;
    }

    PORTABILITY_ASSERT("unsupported arm64 register");
}

void Registers_REGDISPLAY::setRegister(int num, uint64_t value, uint64_t location)
{
    if (num == UNW_REG_SP || num == UNW_ARM64_SP) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_ARM64_FP) {
        pFP = (PTR_UIntNative)location;
        return;
    }

    if (num == UNW_ARM64_LR) {
        pLR = (PTR_UIntNative)location;
        return;
    }

    if (num == UNW_REG_IP) {
        IP = value;
        return;
    }

    switch (num)
    {
    case (UNW_ARM64_X0):
        pX0 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X1):
        pX1 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X2):
        pX2 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X3):
        pX3 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X4):
        pX4 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X5):
        pX5 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X6):
        pX6 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X7):
        pX7 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X8):
        pX8 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X9):
        pX9 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X10):
        pX10 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X11):
        pX11 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X12):
        pX12 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X13):
        pX13 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X14):
        pX14 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X15):
        pX15 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X16):
        pX16 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X17):
        pX17 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X18):
        pX18 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X19):
        pX19 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X20):
        pX20 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X21):
        pX21 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X22):
        pX22 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X23):
        pX23 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X24):
        pX24 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X25):
        pX25 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X26):
        pX26 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X27):
        pX27 = (PTR_UIntNative)location;
        break;
    case (UNW_ARM64_X28):
        pX28 = (PTR_UIntNative)location;
        break;
    default:
        PORTABILITY_ASSERT("unsupported arm64 register");
    }
}

libunwind::v128 Registers_REGDISPLAY::getVectorRegister(int num) const
{
    num -= UNW_ARM64_D8;

    if (num < 0 || num >= sizeof(D) / sizeof(uint64_t))
    {
        PORTABILITY_ASSERT("unsupported arm64 vector register");
    }

    libunwind::v128 result;

    result.vec[0] = 0;
    result.vec[1] = 0;
    result.vec[2] = D[num] >> 32;
    result.vec[3] = D[num] & 0xFFFFFFFF;

    return result;
}

void Registers_REGDISPLAY::setVectorRegister(int num, libunwind::v128 value)
{
    num -= UNW_ARM64_D8;

    if (num < 0 || num >= sizeof(D) / sizeof(uint64_t))
    {
        PORTABILITY_ASSERT("unsupported arm64 vector register");
    }

    D[num] = (uint64_t)value.vec[2] << 32 | (uint64_t)value.vec[3];
}

#endif // TARGET_ARM64

bool UnwindHelpers::StepFrame(REGDISPLAY *regs, unw_word_t start_ip, uint32_t format, unw_word_t unwind_info)
{
#if _LIBUNWIND_SUPPORT_DWARF_UNWIND

#if _LIBUNWIND_SUPPORT_COMPACT_UNWIND

#if defined(TARGET_ARM64)
    if ((format & UNWIND_ARM64_MODE_MASK) != UNWIND_ARM64_MODE_DWARF) {
        CompactUnwinder_arm64<LocalAddressSpace, Registers_REGDISPLAY> compactInst;
        int stepRet = compactInst.stepWithCompactEncoding(format, start_ip, _addressSpace, *(Registers_REGDISPLAY*)regs);
        return stepRet == UNW_STEP_SUCCESS;
    }
#elif defined(TARGET_AMD64)
    if ((format & UNWIND_X86_64_MODE_MASK) != UNWIND_X86_64_MODE_DWARF) {
        CompactUnwinder_x86_64<LocalAddressSpace, Registers_REGDISPLAY> compactInst;
        int stepRet = compactInst.stepWithCompactEncoding(format, start_ip, _addressSpace, *(Registers_REGDISPLAY*)regs);
        return stepRet == UNW_STEP_SUCCESS;
    }
#else
    PORTABILITY_ASSERT("DoTheStep");
#endif

#endif

    uintptr_t pc = regs->GetIP();
    bool isSignalFrame = false;

    DwarfInstructions<LocalAddressSpace, Registers_REGDISPLAY> dwarfInst;
    int stepRet = dwarfInst.stepWithDwarf(_addressSpace, pc, unwind_info, *(Registers_REGDISPLAY*)regs, isSignalFrame, /* stage2 */ false);

    if (stepRet != UNW_STEP_SUCCESS)
    {
        return false;
    }

#elif defined(_LIBUNWIND_ARM_EHABI)
    PORTABILITY_ASSERT("StepFrame");
#else
    PORTABILITY_ASSERT("StepFrame");
#endif

    return true;
}

bool UnwindHelpers::GetUnwindProcInfo(PCODE pc, UnwindInfoSections &uwInfoSections, unw_proc_info_t *procInfo)
{
#if defined(TARGET_AMD64)
    libunwind::UnwindCursor<LocalAddressSpace, Registers_x86_64> uc(_addressSpace);
#elif defined(TARGET_ARM)
    libunwind::UnwindCursor<LocalAddressSpace, Registers_arm> uc(_addressSpace);
#elif defined(TARGET_ARM64)
    libunwind::UnwindCursor<LocalAddressSpace, Registers_arm64> uc(_addressSpace);
#elif defined(HOST_X86)
    libunwind::UnwindCursor<LocalAddressSpace, Registers_x86> uc(_addressSpace);
#else
    #error "Unwinding is not implemented for this architecture yet."
#endif

#if _LIBUNWIND_SUPPORT_DWARF_UNWIND
    uint32_t dwarfOffsetHint = 0;

#if _LIBUNWIND_SUPPORT_COMPACT_UNWIND
    // If there is a compact unwind encoding table, look there first.
    if (uwInfoSections.compact_unwind_section != 0 && uc.getInfoFromCompactEncodingSection(pc, uwInfoSections)) {
        uc.getInfo(procInfo);

#if defined(TARGET_ARM64)
        if ((procInfo->format & UNWIND_ARM64_MODE_MASK) != UNWIND_ARM64_MODE_DWARF) {
            return true;
        } else {
            dwarfOffsetHint = procInfo->format & UNWIND_ARM64_DWARF_SECTION_OFFSET;
        }
#elif defined(TARGET_AMD64)
        if ((procInfo->format & UNWIND_X86_64_MODE_MASK) != UNWIND_X86_64_MODE_DWARF) {
            return true;
        } else {
            dwarfOffsetHint = procInfo->format & UNWIND_X86_64_DWARF_SECTION_OFFSET;
        }
#else
        PORTABILITY_ASSERT("GetUnwindProcInfo");
#endif
    }
#endif

    bool retVal = uc.getInfoFromDwarfSection(pc, uwInfoSections, dwarfOffsetHint);
    if (!retVal)
    {
        return false;
    }

#elif defined(_LIBUNWIND_ARM_EHABI)
    PORTABILITY_ASSERT("GetUnwindProcInfo");
#else
    PORTABILITY_ASSERT("GetUnwindProcInfo");
#endif

    uc.getInfo(procInfo);
    return true;
}

#if defined(TARGET_APPLE)
// Apple considers _dyld_find_unwind_sections to be private API that cannot be used
// by apps submitted to App Store and TestFlight, both for iOS-like and macOS platforms.
// We reimplement it using public API surface.
//
// Ref: https://github.com/llvm/llvm-project/blob/c37145cab12168798a603e22af6b6bf6f606b705/libunwind/src/AddressSpace.hpp#L67-L93
bool _dyld_find_unwind_sections(void* addr, dyld_unwind_sections* info)
{
    // Find mach-o image containing address.
    Dl_info dlinfo;
    if (!dladdr(addr, &dlinfo))
      return false;

    const struct mach_header_64 *mh = (const struct mach_header_64 *)dlinfo.dli_fbase;

    // Initialize the return struct
    info->mh = (const struct mach_header *)mh;
    info->dwarf_section = getsectiondata(mh, "__TEXT", "__eh_frame", &info->dwarf_section_length);
    info->compact_unwind_section = getsectiondata(mh, "__TEXT", "__unwind_info", &info->compact_unwind_section_length);

    if (!info->dwarf_section) {
        info->dwarf_section_length = 0;
    }
    if (!info->compact_unwind_section) {
        info->compact_unwind_section_length = 0;
    }

    return true;
}
#endif
