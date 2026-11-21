// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "daccess.h"
#include "rhassert.h"
#include <minipal/utils.h>

#include <regdisplay.h>
#include "UnwindHelpers.h"

template <class To, class From>
inline To unwindhelpers_bitcast(From from)
{
    static_assert(sizeof(From)==sizeof(To), "Sizes must match");

    To to;
    memcpy(&to, &from, sizeof(To));
    return to;
}

#if !defined(TARGET_AMD64) && !defined(TARGET_ARM) && !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
#error "Unwinding is not implemented for this architecture yet."
#endif

// Reads the saved value at location, which may be unaligned
template <class T>
static T ReadSavedValue(uintptr_t location)
{
    T value;
    memcpy(&value, (const void*)location, sizeof(T));
    return value;
}

#ifdef TARGET_AMD64

// Accessors of the registers of a REGDISPLAY by DWARF register number
struct Registers_REGDISPLAY : REGDISPLAY
{
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

    // Callee-saved registers are tracked by the location they were saved at, see setRegisterLocation
    inline void setRegister(int regNum, uint64_t value)
    {
        switch (regNum)
        {
        case UNW_REG_IP:
        case UNW_X86_64_RIP:
            IP = value;
            return;
        case UNW_REG_SP:
        case UNW_X86_64_RSP:
            SP = value;
            return;
        }
    }

    inline void setRegisterLocation(int regNum, uint64_t location)
    {
        switch (regNum)
        {
        case UNW_REG_IP:
        case UNW_X86_64_RIP:
        case UNW_REG_SP:
        case UNW_X86_64_RSP:
            return;
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
    inline   void setFloatRegister(int, double) { abort(); }

    void      setSP(uint64_t value) { SP = value; }
    void      setIP(uint64_t value) { IP = value; }
};

#endif // TARGET_AMD64
#if defined(TARGET_ARM)

// Accessors of the registers of a REGDISPLAY by DWARF register number
struct Registers_REGDISPLAY : REGDISPLAY
{
    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;

    uint32_t    getRegister(int num) const;
    void        setRegister(int num, uint32_t value);
    void        setRegisterLocation(int num, uint32_t location);

    void        setFloatRegister(int num, double value);

    void        setSP(uint32_t value) { SP = value; }
    void        setIP(uint32_t value) { IP = value; }
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

// Callee-saved registers are tracked by the location they were saved at, see setRegisterLocation
void Registers_REGDISPLAY::setRegister(int num, uint32_t value)
{
    if (num == UNW_REG_SP || num == UNW_ARM_SP) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_REG_IP || num == UNW_ARM_IP) {
        IP = value;
        return;
    }
}

void Registers_REGDISPLAY::setRegisterLocation(int num, uint32_t location)
{
    if (num == UNW_REG_SP || num == UNW_ARM_SP || num == UNW_REG_IP || num == UNW_ARM_IP)
        return;

    if (num == UNW_ARM_LR) {
        pLR = (PTR_uintptr_t)location;
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

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    D[num - UNW_ARM_D8] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_ARM

#if defined(TARGET_ARM64)

// Accessors of the registers of a REGDISPLAY by DWARF register number
struct Registers_REGDISPLAY : REGDISPLAY
{
    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value);
    void        setRegisterLocation(int num, uint64_t location);

    void        setFloatRegister(int num, double value);

    void        setSP(uint64_t value) { SP = value; }
    void        setIP(uint64_t value) { IP = value; }
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

// The locations of X0-X28, FP and LR are consecutive in REGDISPLAY, in DWARF register order
static_assert(offsetof(REGDISPLAY, pX28) - offsetof(REGDISPLAY, pX0) == UNW_ARM64_X28 * sizeof(PTR_uintptr_t), "REGDISPLAY layout");
static_assert(offsetof(REGDISPLAY, pFP) - offsetof(REGDISPLAY, pX0) == UNW_ARM64_FP * sizeof(PTR_uintptr_t), "REGDISPLAY layout");
static_assert(offsetof(REGDISPLAY, pLR) - offsetof(REGDISPLAY, pX0) == UNW_ARM64_LR * sizeof(PTR_uintptr_t), "REGDISPLAY layout");

inline uint64_t Registers_REGDISPLAY::getRegister(int regNum) const {
    if (regNum >= UNW_ARM64_X0 && regNum <= UNW_ARM64_LR)
        return *(&pX0)[regNum];

    if (regNum == UNW_REG_SP || regNum == UNW_ARM64_SP)
        return SP;

    if (regNum == UNW_REG_IP)
        return IP;

    PORTABILITY_ASSERT("unsupported arm64 register");
}

// Callee-saved registers are tracked by the location they were saved at, see setRegisterLocation
void Registers_REGDISPLAY::setRegister(int num, uint64_t value)
{
    if (num == UNW_REG_SP || num == UNW_ARM64_SP) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_REG_IP) {
        IP = value;
        return;
    }
}

inline void Registers_REGDISPLAY::setRegisterLocation(int num, uint64_t location)
{
    if (num >= UNW_ARM64_X0 && num <= UNW_ARM64_LR) {
        (&pX0)[num] = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_REG_SP || num == UNW_ARM64_SP || num == UNW_REG_IP)
        return;

    PORTABILITY_ASSERT("unsupported arm64 register");
}

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    D[num - UNW_ARM64_D8] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_ARM64

#if defined(TARGET_LOONGARCH64)

// Accessors of the registers of a REGDISPLAY by DWARF register number
struct Registers_REGDISPLAY : REGDISPLAY
{
    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value);
    void        setRegisterLocation(int num, uint64_t location);

    void        setFloatRegister(int num, double value);

    void        setSP(uint64_t value) { SP = value; }
    void        setIP(uint64_t value) { IP = value; }
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

    if (num >= UNW_LOONGARCH_F24 && num <= UNW_LOONGARCH_F31)
        return true;

    return false;
}

bool Registers_REGDISPLAY::validFloatRegister(int num) const
{
    return num >= UNW_LOONGARCH_F24 && num <= UNW_LOONGARCH_F31;
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

// Callee-saved registers are tracked by the location they were saved at, see setRegisterLocation
void Registers_REGDISPLAY::setRegister(int num, uint64_t value)
{
    if (num == UNW_REG_SP || num == UNW_LOONGARCH_R3) {
        SP = (uintptr_t )value;
        return;
    }

    if (num == UNW_REG_IP) {
        IP = value;
        return;
    }
}

void Registers_REGDISPLAY::setRegisterLocation(int num, uint64_t location)
{
    if (num == UNW_REG_SP || num == UNW_LOONGARCH_R3 || num == UNW_REG_IP)
        return;

    if (num == UNW_LOONGARCH_R22) {
        pFP = (PTR_uintptr_t)location;
        return;
    }

    if (num == UNW_LOONGARCH_R1) {
        pRA = (PTR_uintptr_t)location;
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

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    F[num - UNW_LOONGARCH_F24] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_LOONGARCH64

#if defined(TARGET_RISCV64)

// Accessors of the registers of a REGDISPLAY by DWARF register number
struct Registers_REGDISPLAY : REGDISPLAY
{
    bool        validRegister(int num) const;
    bool        validFloatRegister(int num) const;

    uint64_t    getRegister(int num) const;
    void        setRegister(int num, uint64_t value);
    void        setRegisterLocation(int num, uint64_t location);

    void        setFloatRegister(int num, double value);

    void        setSP(uint64_t value) { SP = value; }
    void        setIP(uint64_t value) { IP = value; }
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
    switch (regNum) {
    case UNW_REG_IP:
        return IP;
    case UNW_RISCV_X1:
        return *pRA;
    case UNW_REG_SP:
    case UNW_RISCV_X2:
        return SP;
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

// Callee-saved registers are tracked by the location they were saved at, see setRegisterLocation
void Registers_REGDISPLAY::setRegister(int regNum, uint64_t value)
{
    switch (regNum) {
    case UNW_REG_IP:
        IP = (uintptr_t)value;
        break;
    case UNW_REG_SP:
    case UNW_RISCV_X2:
        SP = (uintptr_t)value;
        break;
    }
}

void Registers_REGDISPLAY::setRegisterLocation(int regNum, uint64_t location)
{
    switch (regNum) {
    case UNW_REG_IP:
    case UNW_REG_SP:
    case UNW_RISCV_X2:
        break;
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

void Registers_REGDISPLAY::setFloatRegister(int num, double value)
{
    assert(validFloatRegister(num));
    int index = (num < UNW_RISCV_F18) ? (num - UNW_RISCV_F8) : (num - UNW_RISCV_F18 + 2);
    F[index] = unwindhelpers_bitcast<uint64_t>(value);
}

#endif // TARGET_RISCV64

// Applies the rules of a register run. The register set is updated in place, so every rule is checked first.
static bool ApplyRegisterRun(Registers_REGDISPLAY *registers, const CompactUnwindDescriptor::RegisterRun *pRun, bool descending,
    int returnAddressRegister, uintptr_t cfa, uintptr_t *pReturnAddress)
{
    uint32_t mask = pRun->RegisterMask;
    for (uint32_t remaining = mask; remaining != 0; remaining &= remaining - 1)
    {
        if (!registers->validRegister(__builtin_ctz(remaining)))
            return false;
    }

    int extraRegister = pRun->ExtraRegister;
    if (extraRegister != CompactUnwindDescriptor::NoRegister && !registers->validRegister(extraRegister) && !registers->validFloatRegister(extraRegister))
        return false;

    // The location of each register only depends on the mask, not on a record read from the descriptor
    uintptr_t location = cfa - (uintptr_t)pRun->FirstSlot * sizeof(uintptr_t);
    if (descending)
    {
        for (uint32_t remaining = mask; remaining != 0; location -= sizeof(uintptr_t))
        {
            int regNum = 31 - __builtin_clz(remaining);
            remaining &= ~(1u << regNum);
            registers->setRegisterLocation(regNum, location);
        }
    }
    else
    {
        for (uint32_t remaining = mask; remaining != 0; remaining &= remaining - 1, location -= sizeof(uintptr_t))
            registers->setRegisterLocation(__builtin_ctz(remaining), location);
    }

    if (extraRegister != CompactUnwindDescriptor::NoRegister)
    {
        uintptr_t extraLocation = cfa - (uintptr_t)pRun->ExtraSlot * sizeof(uintptr_t);
        if (registers->validFloatRegister(extraRegister))
            registers->setFloatRegister(extraRegister, ReadSavedValue<double>(extraLocation));
        else
            registers->setRegisterLocation(extraRegister, extraLocation);
    }

    if (pRun->ReturnAddressSlot != 0)
    {
        uintptr_t returnAddressLocation = cfa - (uintptr_t)pRun->ReturnAddressSlot * sizeof(uintptr_t);
        *pReturnAddress = ReadSavedValue<uintptr_t>(returnAddressLocation);
        registers->setRegisterLocation(returnAddressRegister, returnAddressLocation);
    }
    else
    {
        // Leaf frames keep the return address in a register
        *pReturnAddress = registers->getRegister(returnAddressRegister);
    }

    return true;
}

// Applies the rules of the saved register records. The register set is updated in place, so every rule is checked first.
static bool ApplySavedRegisters(Registers_REGDISPLAY *registers, const CompactUnwindDescriptor *pDescriptor,
    int returnAddressRegister, uintptr_t cfa, uintptr_t *pReturnAddress)
{
    for (uint32_t i = 0; i < pDescriptor->RegisterCount; i++)
    {
        int regNum;
        uint32_t slot;
        pDescriptor->GetSavedRegister(i, &regNum, &slot);
        if (!registers->validFloatRegister(regNum) && regNum != returnAddressRegister && !registers->validRegister(regNum))
            return false;
    }

    uintptr_t returnAddress = 0;
    bool returnAddressSaved = false;

    for (uint32_t i = 0; i < pDescriptor->RegisterCount; i++)
    {
        int regNum;
        uint32_t slot;
        pDescriptor->GetSavedRegister(i, &regNum, &slot);
        uintptr_t location = cfa - (uintptr_t)slot * sizeof(uintptr_t);

        if (registers->validFloatRegister(regNum))
        {
            registers->setFloatRegister(regNum, ReadSavedValue<double>(location));
        }
        else if (regNum == returnAddressRegister)
        {
            returnAddress = ReadSavedValue<uintptr_t>(location);
            returnAddressSaved = true;
            registers->setRegisterLocation(regNum, location);
        }
        else
        {
            registers->setRegister(regNum, ReadSavedValue<uintptr_t>(location));
            registers->setRegisterLocation(regNum, location);
        }
    }

    // Leaf frames keep the return address in a register, which no rule changed
    if (!returnAddressSaved)
        returnAddress = registers->getRegister(returnAddressRegister);

    *pReturnAddress = returnAddress;
    return true;
}

// Mirrors DwarfInstructions::stepWithDwarf of libunwind for a frame whose rules are given by a compact descriptor
bool UnwindHelpers::StepFrame(REGDISPLAY *regs, const CompactUnwindDescriptor *pDescriptor)
{
#if defined(TARGET_AMD64)
    const int returnAddressRegister = UNW_X86_64_RIP;
#elif defined(TARGET_ARM64)
    const int returnAddressRegister = UNW_ARM64_LR;
#elif defined(TARGET_LOONGARCH64)
    const int returnAddressRegister = UNW_LOONGARCH_R1;
#elif defined(TARGET_RISCV64)
    const int returnAddressRegister = UNW_RISCV_X1;
#elif defined(TARGET_ARM)
    const int returnAddressRegister = UNW_ARM_LR;
#endif

    Registers_REGDISPLAY *registers = (Registers_REGDISPLAY *)regs;
    if (!registers->validRegister(pDescriptor->CfaRegister))
        return false;

#if !defined(TARGET_ARM64)
    if ((pDescriptor->Flags & CompactUnwindDescriptor::FlagReturnAddressSigned) != 0)
        return false;
#endif

    uintptr_t cfa = (uintptr_t)registers->getRegister(pDescriptor->CfaRegister) + pDescriptor->CfaOffset;
    uintptr_t returnAddress = 0;

    if ((pDescriptor->Flags & CompactUnwindDescriptor::FlagRegisterRun) != 0)
    {
        if (!ApplyRegisterRun(registers, pDescriptor->GetRegisterRun(), (pDescriptor->Flags & CompactUnwindDescriptor::FlagRunDescending) != 0,
                returnAddressRegister, cfa, &returnAddress))
            return false;
    }
    else if (!ApplySavedRegisters(registers, pDescriptor, returnAddressRegister, cfa, &returnAddress))
    {
        return false;
    }

#if defined(TARGET_ARM64)
    if ((pDescriptor->Flags & CompactUnwindDescriptor::FlagReturnAddressSigned) != 0 && returnAddress != 0)
    {
        // Authenticate the return address like the pac-ret case of DwarfInstructions::stepWithDwarf.
        // autia1716 is in the hint space, so it is a NOP on hardware without pointer authentication.
        register uint64_t x17 __asm("x17") = returnAddress;
        register uint64_t x16 __asm("x16") = cfa;
        __asm__("hint 0xc" : "+r"(x17) : "r"(x16)); // autia1716
        returnAddress = x17;
    }
#endif

    registers->setSP(cfa);
    registers->setIP(returnAddress);
    return true;
}
