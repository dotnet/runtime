// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "daccess.h"
#include "rhassert.h"
#include <minipal/utils.h>

#define UNW_STEP_SUCCESS 1
#define UNW_STEP_END     0

#include <regdisplay.h>
#include "UnwindHelpers.h"

// libunwind headers
#include <libunwind.h>

// HP libunwind ARM headers define only UNW_ARM_Rxx register names.
// NativeAOT unwind code historically used SP/LR/IP aliases.
#if defined(TARGET_ARM)
#ifndef UNW_ARM_SP
#define UNW_ARM_SP UNW_ARM_R13
#endif
#ifndef UNW_ARM_LR
#define UNW_ARM_LR UNW_ARM_R14
#endif
#ifndef UNW_ARM_IP
#define UNW_ARM_IP UNW_ARM_R12
#endif
#endif

// HP libunwind uses AARCH64 register enum names while historical NativeAOT
// code used ARM64-prefixed names from libunwind internals.
#if defined(TARGET_ARM64) && defined(UNW_TARGET_AARCH64) && !defined(UNW_ARM64_X0)
#define UNW_ARM64_X0 UNW_AARCH64_X0
#define UNW_ARM64_X1 UNW_AARCH64_X1
#define UNW_ARM64_X2 UNW_AARCH64_X2
#define UNW_ARM64_X3 UNW_AARCH64_X3
#define UNW_ARM64_X4 UNW_AARCH64_X4
#define UNW_ARM64_X5 UNW_AARCH64_X5
#define UNW_ARM64_X6 UNW_AARCH64_X6
#define UNW_ARM64_X7 UNW_AARCH64_X7
#define UNW_ARM64_X8 UNW_AARCH64_X8
#define UNW_ARM64_X9 UNW_AARCH64_X9
#define UNW_ARM64_X10 UNW_AARCH64_X10
#define UNW_ARM64_X11 UNW_AARCH64_X11
#define UNW_ARM64_X12 UNW_AARCH64_X12
#define UNW_ARM64_X13 UNW_AARCH64_X13
#define UNW_ARM64_X14 UNW_AARCH64_X14
#define UNW_ARM64_X15 UNW_AARCH64_X15
#define UNW_ARM64_X16 UNW_AARCH64_X16
#define UNW_ARM64_X17 UNW_AARCH64_X17
#define UNW_ARM64_X18 UNW_AARCH64_X18
#define UNW_ARM64_X19 UNW_AARCH64_X19
#define UNW_ARM64_X20 UNW_AARCH64_X20
#define UNW_ARM64_X21 UNW_AARCH64_X21
#define UNW_ARM64_X22 UNW_AARCH64_X22
#define UNW_ARM64_X23 UNW_AARCH64_X23
#define UNW_ARM64_X24 UNW_AARCH64_X24
#define UNW_ARM64_X25 UNW_AARCH64_X25
#define UNW_ARM64_X26 UNW_AARCH64_X26
#define UNW_ARM64_X27 UNW_AARCH64_X27
#define UNW_ARM64_X28 UNW_AARCH64_X28
#define UNW_ARM64_FP UNW_AARCH64_X29
#define UNW_ARM64_LR UNW_AARCH64_X30
#define UNW_ARM64_SP UNW_AARCH64_SP
#define UNW_ARM64_D8 UNW_AARCH64_V8
#define UNW_ARM64_D15 UNW_AARCH64_V15
#endif

// HP libunwind uses LOONGARCH64 register enum names while NativeAOT code
// historically used LOONGARCH-prefixed names from libunwind internals.
#if defined(TARGET_LOONGARCH64) && defined(UNW_TARGET_LOONGARCH64) && !defined(UNW_LOONGARCH_R0)
#define UNW_LOONGARCH_R0 UNW_LOONGARCH64_R0
#define UNW_LOONGARCH_R1 UNW_LOONGARCH64_R1
#define UNW_LOONGARCH_R2 UNW_LOONGARCH64_R2
#define UNW_LOONGARCH_R3 UNW_LOONGARCH64_R3
#define UNW_LOONGARCH_R4 UNW_LOONGARCH64_R4
#define UNW_LOONGARCH_R5 UNW_LOONGARCH64_R5
#define UNW_LOONGARCH_R6 UNW_LOONGARCH64_R6
#define UNW_LOONGARCH_R7 UNW_LOONGARCH64_R7
#define UNW_LOONGARCH_R8 UNW_LOONGARCH64_R8
#define UNW_LOONGARCH_R9 UNW_LOONGARCH64_R9
#define UNW_LOONGARCH_R10 UNW_LOONGARCH64_R10
#define UNW_LOONGARCH_R11 UNW_LOONGARCH64_R11
#define UNW_LOONGARCH_R12 UNW_LOONGARCH64_R12
#define UNW_LOONGARCH_R13 UNW_LOONGARCH64_R13
#define UNW_LOONGARCH_R14 UNW_LOONGARCH64_R14
#define UNW_LOONGARCH_R15 UNW_LOONGARCH64_R15
#define UNW_LOONGARCH_R16 UNW_LOONGARCH64_R16
#define UNW_LOONGARCH_R17 UNW_LOONGARCH64_R17
#define UNW_LOONGARCH_R18 UNW_LOONGARCH64_R18
#define UNW_LOONGARCH_R19 UNW_LOONGARCH64_R19
#define UNW_LOONGARCH_R20 UNW_LOONGARCH64_R20
#define UNW_LOONGARCH_R21 UNW_LOONGARCH64_R21
#define UNW_LOONGARCH_R22 UNW_LOONGARCH64_R22
#define UNW_LOONGARCH_R23 UNW_LOONGARCH64_R23
#define UNW_LOONGARCH_R24 UNW_LOONGARCH64_R24
#define UNW_LOONGARCH_R25 UNW_LOONGARCH64_R25
#define UNW_LOONGARCH_R26 UNW_LOONGARCH64_R26
#define UNW_LOONGARCH_R27 UNW_LOONGARCH64_R27
#define UNW_LOONGARCH_R28 UNW_LOONGARCH64_R28
#define UNW_LOONGARCH_R29 UNW_LOONGARCH64_R29
#define UNW_LOONGARCH_R30 UNW_LOONGARCH64_R30
#define UNW_LOONGARCH_R31 UNW_LOONGARCH64_R31
#define UNW_LOONGARCH_PC UNW_LOONGARCH64_PC
#endif

#if defined(TARGET_LOONGARCH64) && !defined(UNW_LOONGARCH_F24) && defined(UNW_LOONGARCH64_F24)
#define UNW_LOONGARCH_F24 UNW_LOONGARCH64_F24
#define UNW_LOONGARCH_F25 UNW_LOONGARCH64_F25
#define UNW_LOONGARCH_F26 UNW_LOONGARCH64_F26
#define UNW_LOONGARCH_F27 UNW_LOONGARCH64_F27
#define UNW_LOONGARCH_F28 UNW_LOONGARCH64_F28
#define UNW_LOONGARCH_F29 UNW_LOONGARCH64_F29
#define UNW_LOONGARCH_F30 UNW_LOONGARCH64_F30
#define UNW_LOONGARCH_F31 UNW_LOONGARCH64_F31
#endif

template <class To, class From>
inline To unwindhelpers_bitcast(From from)
{
    static_assert(sizeof(From)==sizeof(To), "Sizes must match");

    To to;
    memcpy(&to, &from, sizeof(To));
    return to;
}

#ifdef TARGET_AMD64

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    inline uint64_t getRegister(int regNum) const
    {
        if (regNum == UNW_REG_IP || regNum == UNW_X86_64_RIP)
            return IP;

        if (regNum == UNW_REG_SP || regNum == UNW_X86_64_RSP)
            return SP;

        switch (regNum)
        {
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
        if (regNum == UNW_REG_IP || regNum == UNW_X86_64_RIP)
        {
            IP = value;
            return;
        }

        if (regNum == UNW_REG_SP || regNum == UNW_X86_64_RSP)
        {
            SP = value;
            return;
        }

        switch (regNum)
        {
        case UNW_X86_64_RAX:
            pRax = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RDX:
            pRdx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RCX:
            pRcx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RBX:
            pRbx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RSI:
            pRsi = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RDI:
            pRdi = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_RBP:
            pRbp = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R8:
            pR8 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R9:
            pR9 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R10:
            pR10 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R11:
            pR11 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R12:
            pR12 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R13:
            pR13 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R14:
            pR14 = (PTR_uintptr_t)location;
            return;
        case UNW_X86_64_R15:
            pR15 = (PTR_uintptr_t)location;
            return;
        }

        // Unsupported x86_64 register
        abort();
    }

    // N/A for x86_64
    inline bool validFloatRegister(int) { return false; }
    inline bool validVectorRegister(int) { return false; }

    static constexpr int lastDwarfRegNum() { return 16; }

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
    void      setRBP(uint64_t value, uint64_t location) { pRbp = (PTR_uintptr_t)location; }
    uint64_t  getRBX() const { return *pRbx; }
    void      setRBX(uint64_t value, uint64_t location) { pRbx = (PTR_uintptr_t)location; }
    uint64_t  getR12() const { return *pR12; }
    void      setR12(uint64_t value, uint64_t location) { pR12 = (PTR_uintptr_t)location; }
    uint64_t  getR13() const { return *pR13; }
    void      setR13(uint64_t value, uint64_t location) { pR13 = (PTR_uintptr_t)location; }
    uint64_t  getR14() const { return *pR14; }
    void      setR14(uint64_t value, uint64_t location) { pR14 = (PTR_uintptr_t)location; }
    uint64_t  getR15() const { return *pR15; }
    void      setR15(uint64_t value, uint64_t location) { pR15 = (PTR_uintptr_t)location; }
};

#endif // TARGET_AMD64
#if defined(TARGET_X86)
struct Registers_REGDISPLAY : REGDISPLAY
{
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
            pRax = (PTR_uintptr_t)location;
            return;
        case UNW_X86_EDX:
            pRdx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_ECX:
            pRcx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_EBX:
            pRbx = (PTR_uintptr_t)location;
            return;
        case UNW_X86_ESI:
            pRsi = (PTR_uintptr_t)location;
            return;
        case UNW_X86_EDI:
            pRdi = (PTR_uintptr_t)location;
            return;
        case UNW_X86_EBP:
            pRbp = (PTR_uintptr_t)location;
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

    static constexpr int lastDwarfRegNum() { return 16; }

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
    void      setEBP(uint64_t value, uint64_t location) { pRbp = (PTR_uintptr_t)location; }
    uint64_t  getEBX() const { return *pRbx; }
    void      setEBX(uint64_t value, uint64_t location) { pRbx = (PTR_uintptr_t)location; }
};

#endif // TARGET_X86
#if defined(TARGET_ARM)

struct Registers_REGDISPLAY : REGDISPLAY
{
    static constexpr int lastDwarfRegNum() { return UNW_ARM_D15; }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;
    bool        validVectorRegister(int num) const { return false; }

    uint32_t    getRegister(int num) const;
    void        setRegister(int num, uint32_t value, uint32_t location);

    unw_fpreg_t getFloatRegister(int num) const;
    void        setFloatRegister(int num, unw_fpreg_t value);

    uint32_t    getSP() const         { return SP; }
    void        setSP(uint32_t value, uint32_t location) { SP = value; }
    uint32_t    getIP() const         { return IP; }
    void        setIP(uint32_t value, uint32_t location) { IP = value; }
    uint32_t    getFP() const         { return *pR11; }
    void        setFP(uint32_t value, uint32_t location) { pR11 = (PTR_uintptr_t)location; }
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

inline bool Registers_REGDISPLAY::validFloatRegister(int num) const {
    return num >= UNW_ARM_D8 && num <= UNW_ARM_D15;
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
        pLR = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_REG_IP || num == UNW_ARM_IP) {
        IP = value;
        return;
    }

    switch (num)
    {
    case (UNW_ARM_R0):
        pR0 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R1):
        pR1 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R2):
        pR2 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R3):
        pR3 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R4):
        pR4 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R5):
        pR5 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R6):
        pR6 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R7):
        pR7 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R8):
        pR8 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R9):
        pR9 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R10):
        pR10 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R11):
        pR11 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM_R12):
        pR12 = (PTR_uintptr_t)location;
        break;
    default:
        PORTABILITY_ASSERT("unsupported arm register");
    }
}

unw_fpreg_t Registers_REGDISPLAY::getFloatRegister(int num) const
{
    assert(validFloatRegister(num));
    return unwindhelpers_bitcast<unw_fpreg_t>(D[num - UNW_ARM_D8]);
}

void Registers_REGDISPLAY::setFloatRegister(int num, unw_fpreg_t value)
{
    assert(validFloatRegister(num));
    D[num - UNW_ARM_D8] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_ARM

#if defined(TARGET_ARM64)

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    typedef uint64_t reg_t;

    static constexpr int lastDwarfRegNum() { return UNW_ARM64_D15; }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;
    bool        validVectorRegister(int num) const { return false; }

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value, uint64_t location);

    double      getFloatRegister(int num) const;
    void        setFloatRegister(int num, double value);

    uint64_t    getSP() const         { return SP; }
    void        setSP(uint64_t value, uint64_t location) { SP = value; }
    uint64_t    getIP() const         { return IP; }
    void        setIP(uint64_t value, uint64_t location) { IP = value; }
    uint64_t    getFP() const         { return *pFP; }
    void        setFP(uint64_t value, uint64_t location) { pFP = (PTR_uintptr_t)location; }
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

bool Registers_REGDISPLAY::validFloatRegister(int num) const
{
    return num >= UNW_ARM64_D8 && num <= UNW_ARM64_D15;
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
        pFP = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_ARM64_LR) {
        pLR = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_REG_IP) {
        IP = value;
        return;
    }

    switch (num)
    {
    case (UNW_ARM64_X0):
        pX0 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X1):
        pX1 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X2):
        pX2 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X3):
        pX3 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X4):
        pX4 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X5):
        pX5 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X6):
        pX6 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X7):
        pX7 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X8):
        pX8 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X9):
        pX9 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X10):
        pX10 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X11):
        pX11 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X12):
        pX12 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X13):
        pX13 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X14):
        pX14 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X15):
        pX15 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X16):
        pX16 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X17):
        pX17 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X18):
        pX18 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X19):
        pX19 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X20):
        pX20 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X21):
        pX21 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X22):
        pX22 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X23):
        pX23 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X24):
        pX24 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X25):
        pX25 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X26):
        pX26 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X27):
        pX27 = (PTR_uintptr_t)location;
        break;
    case (UNW_ARM64_X28):
        pX28 = (PTR_uintptr_t)location;
        break;
    default:
        PORTABILITY_ASSERT("unsupported arm64 register");
    }
}

double Registers_REGDISPLAY::getFloatRegister(int num) const
{
    assert(validFloatRegister(num));
    return unwindhelpers_bitcast<double>(D[num - UNW_ARM64_D8]);
}

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    D[num - UNW_ARM64_D8] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_ARM64

#if defined(TARGET_LOONGARCH64)

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    static constexpr int lastDwarfRegNum()
    {
#if defined(UNW_LOONGARCH_F31)
        return UNW_LOONGARCH_F31;
#else
        return UNW_LOONGARCH_R31;
#endif
    }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;
    bool        validVectorRegister(int num) const { return false; }

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value, uint64_t location);

    double      getFloatRegister(int num) const;
    void        setFloatRegister(int num, double value);

    uint64_t    getSP() const         { return SP; }
    void        setSP(uint64_t value, uint64_t location) { SP = value; }
    uint64_t    getIP() const         { return IP; }
    void        setIP(uint64_t value, uint64_t location) { IP = value; }
    uint64_t    getFP() const         { return *pFP; }
    void        setFP(uint64_t value, uint64_t location) { pFP = (PTR_uintptr_t)location; }
};

inline bool Registers_REGDISPLAY::validRegister(int num) const {
    if (num == UNW_REG_SP || num == UNW_LOONGARCH_R3)
        return true;

    if (num == UNW_LOONGARCH_R22)
        return true;

    if (num == UNW_REG_IP)
        return true;

    if (num >= UNW_LOONGARCH_R0 && num <= UNW_LOONGARCH_R31)
        return true;

#if defined(UNW_LOONGARCH_F24) && defined(UNW_LOONGARCH_F31)
    if (num >= UNW_LOONGARCH_F24 && num <= UNW_LOONGARCH_F31)
        return true;
#endif

    return false;
}

bool Registers_REGDISPLAY::validFloatRegister(int num) const
{
#if defined(UNW_LOONGARCH_F24) && defined(UNW_LOONGARCH_F31)
    return num >= UNW_LOONGARCH_F24 && num <= UNW_LOONGARCH_F31;
#else
    (void)num;
    return false;
#endif
}

inline uint64_t Registers_REGDISPLAY::getRegister(int regNum) const {
    if (regNum == UNW_REG_SP || regNum == UNW_LOONGARCH_R3)
        return SP;

    if (regNum == UNW_LOONGARCH_R22)
        return *pFP;

    if (regNum == UNW_LOONGARCH_R1)
        return *pRA;

    if (regNum == UNW_REG_IP)
        return IP;

    switch (regNum)
    {
    case (UNW_LOONGARCH_R0):
        return *pR0;
    case (UNW_LOONGARCH_R2):
        return *pR2;
    case (UNW_LOONGARCH_R4):
        return *pR4;
    case (UNW_LOONGARCH_R5):
        return *pR5;
    case (UNW_LOONGARCH_R6):
        return *pR6;
    case (UNW_LOONGARCH_R7):
        return *pR7;
    case (UNW_LOONGARCH_R8):
        return *pR8;
    case (UNW_LOONGARCH_R9):
        return *pR9;
    case (UNW_LOONGARCH_R10):
        return *pR10;
    case (UNW_LOONGARCH_R11):
        return *pR11;
    case (UNW_LOONGARCH_R12):
        return *pR12;
    case (UNW_LOONGARCH_R13):
        return *pR13;
    case (UNW_LOONGARCH_R14):
        return *pR14;
    case (UNW_LOONGARCH_R15):
        return *pR15;
    case (UNW_LOONGARCH_R16):
        return *pR16;
    case (UNW_LOONGARCH_R17):
        return *pR17;
    case (UNW_LOONGARCH_R18):
        return *pR18;
    case (UNW_LOONGARCH_R19):
        return *pR19;
    case (UNW_LOONGARCH_R20):
        return *pR20;
    case (UNW_LOONGARCH_R21):
        return *pR21;
    case (UNW_LOONGARCH_R23):
        return *pR23;
    case (UNW_LOONGARCH_R24):
        return *pR24;
    case (UNW_LOONGARCH_R25):
        return *pR25;
    case (UNW_LOONGARCH_R26):
        return *pR26;
    case (UNW_LOONGARCH_R27):
        return *pR27;
    case (UNW_LOONGARCH_R28):
        return *pR28;
    case (UNW_LOONGARCH_R29):
        return *pR29;
    case (UNW_LOONGARCH_R30):
        return *pR30;
    case (UNW_LOONGARCH_R31):
        return *pR31;
    }

    PORTABILITY_ASSERT("unsupported loongarch64 register");
}

void Registers_REGDISPLAY::setRegister(int num, uint64_t value, uint64_t location)
{
    if (num == UNW_REG_SP || num == UNW_LOONGARCH_R3) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_LOONGARCH_R22) {
        pFP = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_LOONGARCH_R1) {
        pRA = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_REG_IP) {
        IP = value;
        return;
    }

    switch (num)
    {
    case (UNW_LOONGARCH_R0):
        pR0 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R2):
        pR2 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R4):
        pR4 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R5):
        pR5 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R6):
        pR6 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R7):
        pR7 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R8):
        pR8 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R9):
        pR9 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R10):
        pR10 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R11):
        pR11 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R12):
        pR12 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R13):
        pR13 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R14):
        pR14 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R15):
        pR15 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R16):
        pR16 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R17):
        pR17 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R18):
        pR18 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R19):
        pR19 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R20):
        pR20 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R21):
        pR21 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R23):
        pR23 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R24):
        pR24 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R25):
        pR25 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R26):
        pR26 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R27):
        pR27 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R28):
        pR28 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R29):
        pR29 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R30):
        pR30 = (PTR_uintptr_t)location;
        break;
    case (UNW_LOONGARCH_R31):
        pR31 = (PTR_uintptr_t)location;
        break;
    default:
        PORTABILITY_ASSERT("unsupported loongarch64 register");
    }
}

double Registers_REGDISPLAY::getFloatRegister(int num) const
{
#if defined(UNW_LOONGARCH_F24)
    assert(validFloatRegister(num));
    return unwindhelpers_bitcast<double>(F[num - UNW_LOONGARCH_F24]);
#else
    (void)num;
    PORTABILITY_ASSERT("unsupported loongarch64 float register");
    return 0.0;
#endif
}

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
#if defined(UNW_LOONGARCH_F24)
    assert(validFloatRegister(num));
    F[num - UNW_LOONGARCH_F24] = unwindhelpers_bitcast<uint64_t>(value);
#else
    (void)num;
    (void)value;
    PORTABILITY_ASSERT("unsupported loongarch64 float register");
#endif
}

#endif // TARGET_LOONGARCH64

#if defined(TARGET_RISCV64)

// Shim that implements methods required by libunwind over REGDISPLAY
struct Registers_REGDISPLAY : REGDISPLAY
{
    static constexpr int lastDwarfRegNum() { return UNW_RISCV_F27; }

    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;
    bool        validVectorRegister(int num) const { return false; }

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value, uint64_t location);

    double      getFloatRegister(int num) const;
    void        setFloatRegister(int num, double value);

    uint64_t    getSP() const         { return SP; }
    void        setSP(uint64_t value, uint64_t location) { SP = value; }
    uint64_t    getIP() const         { return IP; }
    void        setIP(uint64_t value, uint64_t location) { IP = value; }
    uint64_t    getFP() const         { return *pFP; }
    void        setFP(uint64_t value, uint64_t location) { pFP = (PTR_uintptr_t)location; }
};

inline bool Registers_REGDISPLAY::validRegister(int num) const {
    if (num == UNW_REG_SP || num == UNW_RISCV_X2)
        return true;

    if (num == UNW_REG_IP)
        return true;

    if (num >= UNW_RISCV_X0 && num <= UNW_RISCV_X31)
        return true;

    return false;
}

inline bool Registers_REGDISPLAY::validFloatRegister(int num) const {
    if (num == UNW_RISCV_F8 || num == UNW_RISCV_F9)
        return true;

    if (num >= UNW_RISCV_F18 && num <= UNW_RISCV_F27)
        return true;

    return false;
}

inline uint64_t Registers_REGDISPLAY::getRegister(int regNum) const {
    if (regNum == UNW_REG_IP)
        return IP;

    if (regNum == UNW_REG_SP || regNum == UNW_RISCV_X2)
        return SP;

    switch (regNum) {
    case UNW_RISCV_X1:
        return *pRA;
    case UNW_RISCV_X3:
        return *pGP;
    case UNW_RISCV_X4:
        return *pTP;
    case UNW_RISCV_X5:
        return *pT0;
    case UNW_RISCV_X6:
        return *pT1;
    case UNW_RISCV_X7:
        return *pT2;
    case UNW_RISCV_X28:
        return *pT3;
    case UNW_RISCV_X29:
        return *pT4;
    case UNW_RISCV_X30:
        return *pT5;
    case UNW_RISCV_X31:
        return *pT6;

    case UNW_RISCV_X8:
        return *pFP;
    case UNW_RISCV_X9:
        return *pS1;

    case UNW_RISCV_X18:
        return *pS2;
    case UNW_RISCV_X19:
        return *pS3;
    case UNW_RISCV_X20:
        return *pS4;
    case UNW_RISCV_X21:
        return *pS5;
    case UNW_RISCV_X22:
        return *pS6;
    case UNW_RISCV_X23:
        return *pS7;

    default:
        PORTABILITY_ASSERT("unsupported RISC-V register");
    }
}

void Registers_REGDISPLAY::setRegister(int regNum, uint64_t value, uint64_t location)
{
    if (regNum == UNW_REG_IP)
    {
        IP = (uintptr_t)value;
        return;
    }

    if (regNum == UNW_REG_SP || regNum == UNW_RISCV_X2)
    {
        SP = (uintptr_t)value;
        return;
    }

    switch (regNum) {
    case UNW_RISCV_X1:
        pRA = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X3:
        pGP = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X4:
        pTP = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X5:
        pT0 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X6:
        pT1 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X7:
        pT2 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X28:
        pT3 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X29:
        pT4 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X30:
        pT5 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X31:
        pT6 = (PTR_uintptr_t)location;
        break;

    case UNW_RISCV_X8:
        pFP = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X9:
        pS1 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X18:
        pS2 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X19:
        pS3 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X20:
        pS4 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X21:
        pS5 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X22:
        pS6 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X23:
        pS7 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X24:
        pS8 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X25:
        pS9 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X26:
        pS10 = (PTR_uintptr_t)location;
        break;
    case UNW_RISCV_X27:
        pS11 = (PTR_uintptr_t)location;
        break;


    default:
        PORTABILITY_ASSERT("unsupported RISC-V register");
    }
}

double Registers_REGDISPLAY::getFloatRegister(int num) const
{
    assert(validFloatRegister(num));
    int index = (num < UNW_RISCV_F18) ? (num - UNW_RISCV_F8) : (num - UNW_RISCV_F18 + 2);
    return unwindhelpers_bitcast<double>(F[index]);
}

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    int index = (num < UNW_RISCV_F18) ? (num - UNW_RISCV_F8) : (num - UNW_RISCV_F18 + 2);
    F[index] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_RISCV64

static uintptr_t GetPreviousRegisterLocation(const Registers_REGDISPLAY& previousRegisterSet, int regNum)
{
#if defined(TARGET_AMD64)
    switch (regNum)
    {
    case UNW_X86_64_RAX: return (uintptr_t)previousRegisterSet.pRax;
    case UNW_X86_64_RDX: return (uintptr_t)previousRegisterSet.pRdx;
    case UNW_X86_64_RCX: return (uintptr_t)previousRegisterSet.pRcx;
    case UNW_X86_64_RBX: return (uintptr_t)previousRegisterSet.pRbx;
    case UNW_X86_64_RSI: return (uintptr_t)previousRegisterSet.pRsi;
    case UNW_X86_64_RDI: return (uintptr_t)previousRegisterSet.pRdi;
    case UNW_X86_64_RBP: return (uintptr_t)previousRegisterSet.pRbp;
    case UNW_X86_64_R8:  return (uintptr_t)previousRegisterSet.pR8;
    case UNW_X86_64_R9:  return (uintptr_t)previousRegisterSet.pR9;
    case UNW_X86_64_R10: return (uintptr_t)previousRegisterSet.pR10;
    case UNW_X86_64_R11: return (uintptr_t)previousRegisterSet.pR11;
    case UNW_X86_64_R12: return (uintptr_t)previousRegisterSet.pR12;
    case UNW_X86_64_R13: return (uintptr_t)previousRegisterSet.pR13;
    case UNW_X86_64_R14: return (uintptr_t)previousRegisterSet.pR14;
    case UNW_X86_64_R15: return (uintptr_t)previousRegisterSet.pR15;
    default: return 0;
    }
#elif defined(TARGET_X86)
    switch (regNum)
    {
    case UNW_X86_EAX: return (uintptr_t)previousRegisterSet.pRax;
    case UNW_X86_EDX: return (uintptr_t)previousRegisterSet.pRdx;
    case UNW_X86_ECX: return (uintptr_t)previousRegisterSet.pRcx;
    case UNW_X86_EBX: return (uintptr_t)previousRegisterSet.pRbx;
    case UNW_X86_ESI: return (uintptr_t)previousRegisterSet.pRsi;
    case UNW_X86_EDI: return (uintptr_t)previousRegisterSet.pRdi;
    case UNW_X86_EBP: return (uintptr_t)previousRegisterSet.pRbp;
    default: return 0;
    }
#elif defined(TARGET_ARM64)
    switch (regNum)
    {
    case UNW_ARM64_X0: return (uintptr_t)previousRegisterSet.pX0;
    case UNW_ARM64_X1: return (uintptr_t)previousRegisterSet.pX1;
    case UNW_ARM64_X2: return (uintptr_t)previousRegisterSet.pX2;
    case UNW_ARM64_X3: return (uintptr_t)previousRegisterSet.pX3;
    case UNW_ARM64_X4: return (uintptr_t)previousRegisterSet.pX4;
    case UNW_ARM64_X5: return (uintptr_t)previousRegisterSet.pX5;
    case UNW_ARM64_X6: return (uintptr_t)previousRegisterSet.pX6;
    case UNW_ARM64_X7: return (uintptr_t)previousRegisterSet.pX7;
    case UNW_ARM64_X8: return (uintptr_t)previousRegisterSet.pX8;
    case UNW_ARM64_X9: return (uintptr_t)previousRegisterSet.pX9;
    case UNW_ARM64_X10: return (uintptr_t)previousRegisterSet.pX10;
    case UNW_ARM64_X11: return (uintptr_t)previousRegisterSet.pX11;
    case UNW_ARM64_X12: return (uintptr_t)previousRegisterSet.pX12;
    case UNW_ARM64_X13: return (uintptr_t)previousRegisterSet.pX13;
    case UNW_ARM64_X14: return (uintptr_t)previousRegisterSet.pX14;
    case UNW_ARM64_X15: return (uintptr_t)previousRegisterSet.pX15;
    case UNW_ARM64_X16: return (uintptr_t)previousRegisterSet.pX16;
    case UNW_ARM64_X17: return (uintptr_t)previousRegisterSet.pX17;
    case UNW_ARM64_X18: return (uintptr_t)previousRegisterSet.pX18;
    case UNW_ARM64_X19: return (uintptr_t)previousRegisterSet.pX19;
    case UNW_ARM64_X20: return (uintptr_t)previousRegisterSet.pX20;
    case UNW_ARM64_X21: return (uintptr_t)previousRegisterSet.pX21;
    case UNW_ARM64_X22: return (uintptr_t)previousRegisterSet.pX22;
    case UNW_ARM64_X23: return (uintptr_t)previousRegisterSet.pX23;
    case UNW_ARM64_X24: return (uintptr_t)previousRegisterSet.pX24;
    case UNW_ARM64_X25: return (uintptr_t)previousRegisterSet.pX25;
    case UNW_ARM64_X26: return (uintptr_t)previousRegisterSet.pX26;
    case UNW_ARM64_X27: return (uintptr_t)previousRegisterSet.pX27;
    case UNW_ARM64_X28: return (uintptr_t)previousRegisterSet.pX28;
    case UNW_ARM64_FP: return (uintptr_t)previousRegisterSet.pFP;
    case UNW_ARM64_LR: return (uintptr_t)previousRegisterSet.pLR;
    default: return 0;
    }
#elif defined(TARGET_ARM)
    switch (regNum)
    {
    case UNW_ARM_R0: return (uintptr_t)previousRegisterSet.pR0;
    case UNW_ARM_R1: return (uintptr_t)previousRegisterSet.pR1;
    case UNW_ARM_R2: return (uintptr_t)previousRegisterSet.pR2;
    case UNW_ARM_R3: return (uintptr_t)previousRegisterSet.pR3;
    case UNW_ARM_R4: return (uintptr_t)previousRegisterSet.pR4;
    case UNW_ARM_R5: return (uintptr_t)previousRegisterSet.pR5;
    case UNW_ARM_R6: return (uintptr_t)previousRegisterSet.pR6;
    case UNW_ARM_R7: return (uintptr_t)previousRegisterSet.pR7;
    case UNW_ARM_R8: return (uintptr_t)previousRegisterSet.pR8;
    case UNW_ARM_R9: return (uintptr_t)previousRegisterSet.pR9;
    case UNW_ARM_R10: return (uintptr_t)previousRegisterSet.pR10;
    case UNW_ARM_R11: return (uintptr_t)previousRegisterSet.pR11;
    case UNW_ARM_R12: return (uintptr_t)previousRegisterSet.pR12;
    case UNW_ARM_LR: return (uintptr_t)previousRegisterSet.pLR;
    default: return 0;
    }
#elif defined(TARGET_LOONGARCH64)
    switch (regNum)
    {
    case UNW_LOONGARCH_R1: return (uintptr_t)previousRegisterSet.pRA;
    case UNW_LOONGARCH_R22: return (uintptr_t)previousRegisterSet.pFP;
    default: return 0;
    }
#elif defined(TARGET_RISCV64)
    switch (regNum)
    {
    case UNW_RISCV_X1: return (uintptr_t)previousRegisterSet.pRA;
    case UNW_RISCV_X8: return (uintptr_t)previousRegisterSet.pFP;
    default: return 0;
    }
#else
    (void)previousRegisterSet;
    (void)regNum;
    return 0;
#endif
}

bool UnwindHelpers::StepFrame(REGDISPLAY *regs, unw_word_t start_ip, uint32_t format, unw_word_t unwind_info)
{
    (void)start_ip;
    (void)format;
    (void)unwind_info;

    unw_context_t unwContext;
    unw_cursor_t cursor;

    if (unw_getcontext(&unwContext) < 0)
    {
        return false;
    }

    if (unw_init_local(&cursor, &unwContext) < 0)
    {
        return false;
    }

    Registers_REGDISPLAY* registerSet = reinterpret_cast<Registers_REGDISPLAY*>(regs);
    Registers_REGDISPLAY previousRegisterSet = *registerSet;

    for (int regNum = 0; regNum <= Registers_REGDISPLAY::lastDwarfRegNum(); regNum++)
    {
        if (!registerSet->validRegister(regNum) || registerSet->validFloatRegister(regNum))
        {
            continue;
        }

        unw_word_t value = (unw_word_t)registerSet->getRegister(regNum);
        (void)unw_set_reg(&cursor, regNum, value);
    }

    for (int regNum = 0; regNum <= Registers_REGDISPLAY::lastDwarfRegNum(); regNum++)
    {
        if (!registerSet->validFloatRegister(regNum))
        {
            continue;
        }

        unw_fpreg_t value = (unw_fpreg_t)registerSet->getFloatRegister(regNum);
        (void)unw_set_fpreg(&cursor, regNum, value);
    }

    int stepRet = unw_step(&cursor);
    if (stepRet != UNW_STEP_SUCCESS)
    {
        return false;
    }

    for (int regNum = 0; regNum <= Registers_REGDISPLAY::lastDwarfRegNum(); regNum++)
    {
        if (!registerSet->validRegister(regNum) || registerSet->validFloatRegister(regNum))
        {
            continue;
        }

        unw_word_t value;
        if (unw_get_reg(&cursor, regNum, &value) != UNW_ESUCCESS)
        {
            continue;
        }

        unw_word_t location = (unw_word_t)GetPreviousRegisterLocation(previousRegisterSet, regNum);

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        if (regNum == UNW_ARM64_LR)
        {
            unw_word_t framePointer = 0;
            if (unw_get_reg(&cursor, UNW_ARM64_FP, &framePointer) == UNW_ESUCCESS && framePointer != 0)
            {
                location = framePointer + sizeof(uintptr_t);
            }
        }
#endif

        registerSet->setRegister(regNum, (uint64_t)value, (uint64_t)location);
    }

    for (int regNum = 0; regNum <= Registers_REGDISPLAY::lastDwarfRegNum(); regNum++)
    {
        if (!registerSet->validFloatRegister(regNum))
        {
            continue;
        }

        unw_fpreg_t value;
        if (unw_get_fpreg(&cursor, regNum, &value) != UNW_ESUCCESS)
        {
            continue;
        }

        registerSet->setFloatRegister(regNum, value);
    }

    return true;
}

bool UnwindHelpers::GetUnwindProcInfo(PCODE pc, unw_proc_info_t *procInfo)
{
#if !defined(TARGET_APPLE) && defined(unw_get_proc_info_by_ip)
    return unw_get_proc_info_by_ip(unw_local_addr_space, pc, procInfo, nullptr) == UNW_ESUCCESS;
#else
    unw_context_t unwContext;
    unw_cursor_t cursor;

    if (unw_getcontext(&unwContext) < 0)
    {
        return false;
    }

    if (unw_init_local(&cursor, &unwContext) < 0)
    {
        return false;
    }

    if (unw_set_reg(&cursor, UNW_REG_IP, pc) < 0)
    {
        return false;
    }

    return unw_get_proc_info(&cursor, procInfo) == UNW_ESUCCESS;
#endif
}
