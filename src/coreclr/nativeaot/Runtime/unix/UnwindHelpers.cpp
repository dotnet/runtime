// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "UnwinderPal.h"
#include <regdisplay.h>
#include "UnwindHelpers.h"
#include "unwinder.h"
#include <alloca.h>

// The unwinder looks up the function entry of the target of a jump out of the frame to tell a tail call from a
// jump to another part of the same function. Managed code isn't split, so such a jump is always a tail call.
HRESULT OOPStackUnwinder::GetModuleBase(DWORD64 address, _Out_ PDWORD64 pdwBase)
{
    return E_FAIL;
}

HRESULT OOPStackUnwinder::GetFunctionEntry(DWORD64 address, _Out_writes_(cbBuffer) PVOID pBuffer, DWORD cbBuffer)
{
    return E_FAIL;
}

uint32_t UnwindHelpers::GetUnwindInfoSize(PTR_uint8_t pUnwindInfo)
{
#if defined(TARGET_AMD64)
    PTR_UNWIND_INFO pInfo = dac_cast<PTR_UNWIND_INFO>(pUnwindInfo);

    // Managed code has no chained unwind information and no handlers
    ASSERT((pInfo->Flags & (UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER | UNW_FLAG_CHAININFO)) == 0);
    return offsetof(UNWIND_INFO, UnwindCode) + sizeof(UNWIND_CODE) * pInfo->CountOfUnwindCodes;
#else
    // The header word has the number of words of unwind codes and the number of epilog scopes, or an extension
    // word follows with them, see https://learn.microsoft.com/cpp/build/arm64-exception-handling
    uint32_t header = *dac_cast<PTR_uint32_t>(pUnwindInfo);
#if defined(TARGET_ARM)
    uint32_t codeWords = header >> 28;
    uint32_t epilogScopes = (header >> 23) & 0x1F;
#else
    uint32_t codeWords = header >> 27;
    uint32_t epilogScopes = (header >> 22) & 0x1F;
#endif
    uint32_t size = 4;
    if (codeWords == 0 && epilogScopes == 0)
    {
        uint32_t extension = *dac_cast<PTR_uint32_t>(pUnwindInfo + 4);
        codeWords = (extension >> 16) & 0xFF;
        epilogScopes = extension & 0xFFFF;
        size += 4;
    }

    // Without the E bit the epilog scopes follow the header
    if ((header & (1 << 21)) == 0)
        size += 4 * epilogScopes;

    // Managed code has no exception handler data (X bit)
    ASSERT((header & (1 << 20)) == 0);
    return size + 4 * codeWords;
#endif
}

// The registers the unwinder can restore, as the CONTEXT and REGDISPLAY fields
#if defined(TARGET_AMD64)
#define FOR_EACH_REGISTER(F) \
    F(Rax, pRax) F(Rcx, pRcx) F(Rdx, pRdx) F(Rbx, pRbx) F(Rbp, pRbp) F(Rsi, pRsi) F(Rdi, pRdi) \
    F(R8, pR8) F(R9, pR9) F(R10, pR10) F(R11, pR11) F(R12, pR12) F(R13, pR13) F(R14, pR14) F(R15, pR15)
#define CONTEXT_PC Rip
#define CONTEXT_SP Rsp
#elif defined(TARGET_ARM)
#define FOR_EACH_REGISTER(F) \
    F(R4, pR4) F(R5, pR5) F(R6, pR6) F(R7, pR7) F(R8, pR8) F(R9, pR9) F(R10, pR10) F(R11, pR11) F(Lr, pLR)
#define FOR_EACH_FLOAT_REGISTER(OP) for (int i = 0; i < 8; i++) { OP(D[8 + i], D[i]) }
#define CONTEXT_PC Pc
#define CONTEXT_SP Sp
#elif defined(TARGET_ARM64)
#define FOR_EACH_REGISTER(F) \
    F(X19, pX19) F(X20, pX20) F(X21, pX21) F(X22, pX22) F(X23, pX23) F(X24, pX24) F(X25, pX25) \
    F(X26, pX26) F(X27, pX27) F(X28, pX28) F(Fp, pFP) F(Lr, pLR)
#define FOR_EACH_FLOAT_REGISTER(OP) for (int i = 0; i < 8; i++) { OP(V[8 + i].Low, D[i]) }
#define CONTEXT_PC Pc
#define CONTEXT_SP Sp
#elif defined(TARGET_LOONGARCH64)
#define FOR_EACH_REGISTER(F) \
    F(S0, pR23) F(S1, pR24) F(S2, pR25) F(S3, pR26) F(S4, pR27) F(S5, pR28) F(S6, pR29) F(S7, pR30) \
    F(S8, pR31) F(Fp, pFP) F(Ra, pRA)
#define FOR_EACH_FLOAT_REGISTER(OP) for (int i = 0; i < 8; i++) { OP(F[24 + i], F[i]) }
#define CONTEXT_PC Pc
#define CONTEXT_SP Sp
#elif defined(TARGET_RISCV64)
#define FOR_EACH_REGISTER(F) \
    F(S1, pS1) F(S2, pS2) F(S3, pS3) F(S4, pS4) F(S5, pS5) F(S6, pS6) F(S7, pS7) F(S8, pS8) F(S9, pS9) \
    F(S10, pS10) F(S11, pS11) F(Fp, pFP) F(Ra, pRA)
#define FOR_EACH_FLOAT_REGISTER(OP) for (int i = 0; i < 32; i++) { OP(F[i], F[i]) }
#define CONTEXT_PC Pc
#define CONTEXT_SP Sp
#endif

#if !defined(TARGET_AMD64)
// The epilog scopes of the unwind information that the compact unwind table shares have no start offsets when the
// first one is 0. The 16-bit start offsets of the frame are then right before its LSDA, which pEpilogStarts points to.
static uint32_t ExternalEpilogStart(PTR_uint8_t pEpilogStarts, uint32_t epilogScopes, uint32_t index)
{
    PTR_uint8_t pStart = pEpilogStarts - 2 * (epilogScopes - index);
    return pStart[0] | ((uint32_t)pStart[1] << 8);
}
#endif

#if defined(TARGET_ARM64) || defined(TARGET_AMD64)

// The integer registers that frame bodies restore are numbered from kFirstBodyRegister in BodyUnwindState and
// BodyUnwindSummary
#if defined(TARGET_ARM64)
const int kFirstBodyRegister = 19;
const int kBodyRegisterCount = 12;

// Returns the location of the integer register with the given number (19-30) in the REGDISPLAY
static PTR_uintptr_t* RegisterLocation(REGDISPLAY *regs, int reg)
{
    static_assert(offsetof(REGDISPLAY, pFP) == offsetof(REGDISPLAY, pX0) + 29 * sizeof(PTR_uintptr_t), "pX0-pX28, pFP and pLR must be contiguous");
    static_assert(offsetof(REGDISPLAY, pLR) == offsetof(REGDISPLAY, pX0) + 30 * sizeof(PTR_uintptr_t), "pX0-pX28, pFP and pLR must be contiguous");
    return &regs->pX0 + reg;
}

// The caller's PC, once the REGDISPLAY has the caller's registers
static PCODE CallerIP(REGDISPLAY *regs)
{
    return *regs->pLR;
}
#else
const int kFirstBodyRegister = 0;
const int kBodyRegisterCount = 16;

// Returns the location of the integer register with the given number (0-15, except rsp) in the REGDISPLAY
static PTR_uintptr_t* RegisterLocation(REGDISPLAY *regs, int reg)
{
    static_assert(offsetof(REGDISPLAY, pRbp) == offsetof(REGDISPLAY, pRax) + 4 * sizeof(PTR_uintptr_t), "pRax-pR15 must be contiguous, without rsp");
    static_assert(offsetof(REGDISPLAY, pR15) == offsetof(REGDISPLAY, pRax) + 14 * sizeof(PTR_uintptr_t), "pRax-pR15 must be contiguous, without rsp");
    ASSERT(reg != 4);
    return &regs->pRax + (reg < 4 ? reg : reg - 1);
}

// The caller's PC, once the REGDISPLAY has the caller's stack pointer, which is right above the return address
static PCODE CallerIP(REGDISPLAY *regs)
{
    return *dac_cast<PTR_PCODE>(regs->SP - sizeof(PCODE));
}
#endif

// The locations of the integer registers and of d8-d15 that unwinding a frame body restores, which only replace
// the ones of the REGDISPLAY once the whole frame is unwound
struct BodyUnwindState
{
    uintptr_t sp;
    uint32_t restoredRegisters;
    uint32_t restoredFloats;
    PTR_uintptr_t locations[kBodyRegisterCount];
    PTR_uint64_t floatLocations[8];
    // The codes set the stack pointer from baseRegister, after restoring registers relative to the stack pointer
    // when mixedBase
    bool fpBased;
    bool mixedBase;
    int baseRegister;
};

static_assert(kBodyRegisterCount <= BodyUnwindSummary::FirstFloat, "the integer registers come before the floating point ones");

// Returns the address that unwinding the frame body makes relative to, its frame register or stack pointer
static uintptr_t BodyUnwindBase(REGDISPLAY *regs, bool fpBased, int baseRegister)
{
    return fpBased ? **RegisterLocation(regs, baseRegister) : regs->SP;
}

// Records the effect of the codes of a frame body relative to its frame register or stack pointer, for the other
// frames with the same unwind information
static void StoreBodyUnwindSummary(BodyUnwindSummary* pSummary, const BodyUnwindState& state, uintptr_t base, uint32_t prologSize)
{
    // The prolog checks use the prolog size of a computed summary, so a summary is only computed with it
    if (prologSize > BodyUnwindSummary::PrologSizeMask)
        return;

    uint32_t summaryState = BodyUnwindSummary::Computed | (prologSize << BodyUnwindSummary::PrologSizeShift);
    intptr_t cfaOffset = (intptr_t)(state.sp - base);
    bool representable = !state.mixedBase && cfaOffset == (int32_t)cfaOffset;
    for (uint32_t i = 0; i < BodyUnwindSummary::FirstFloat + 8 && representable; i++)
    {
        bool isFloat = i >= BodyUnwindSummary::FirstFloat;
        uint32_t bit = isFloat ? i - BodyUnwindSummary::FirstFloat : i;
        if (((isFloat ? state.restoredFloats : state.restoredRegisters) & (1u << bit)) == 0)
            continue;
        intptr_t delta = (intptr_t)(isFloat ? (TADDR)state.floatLocations[bit] : (TADDR)state.locations[bit]) - (intptr_t)base;
        representable = delta % 8 == 0 && delta / 8 == (int8_t)(delta / 8);
        pSummary->Offsets[i] = (int8_t)(delta / 8);
    }
    if (representable)
    {
        pSummary->SavedMask = state.restoredRegisters | (state.restoredFloats << BodyUnwindSummary::FirstFloat);
        pSummary->CfaOffset = (int32_t)cfaOffset;
        summaryState |= BodyUnwindSummary::Usable;
        if (state.fpBased)
            summaryState |= BodyUnwindSummary::FpBased | ((uint32_t)state.baseRegister << BodyUnwindSummary::BaseRegisterShift);
    }
    __atomic_store_n(&pSummary->State, summaryState, __ATOMIC_RELEASE);
}

static bool ApplyBodyCodes(BodyUnwindState& state, REGDISPLAY *regs, PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd);
static uint32_t BodyPrologSize(PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd);

// Unwinds a frame whose PC is in the body of the function, outside the prolog and the epilogs, with the cached
// effect of its unwind codes, or by applying them to the REGDISPLAY and caching their effect. The unwinders do
// the same through a CONTEXT. Returns false for the codes it doesn't handle, without changing the REGDISPLAY.
static bool StepFrameBody(REGDISPLAY *regs, BodyUnwindSummary* pSummary, uint32_t summaryState, PTR_uint8_t pUnwindInfo,
    PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd)
{
    if ((summaryState & BodyUnwindSummary::Usable) != 0)
    {
        uintptr_t base = BodyUnwindBase(regs, (summaryState & BodyUnwindSummary::FpBased) != 0,
            (int)(summaryState >> BodyUnwindSummary::BaseRegisterShift));
        for (uint32_t mask = pSummary->SavedMask & ((1u << BodyUnwindSummary::FirstFloat) - 1); mask != 0; mask &= mask - 1)
        {
            uint32_t i = __builtin_ctz(mask);
            *RegisterLocation(regs, kFirstBodyRegister + i) = (PTR_uintptr_t)(base + 8 * (intptr_t)pSummary->Offsets[i]);
        }
#if defined(TARGET_ARM64)
        for (uint32_t mask = pSummary->SavedMask >> BodyUnwindSummary::FirstFloat; mask != 0; mask &= mask - 1)
        {
            uint32_t i = __builtin_ctz(mask);
            regs->D[i] = *(PTR_uint64_t)(base + 8 * (intptr_t)pSummary->Offsets[BodyUnwindSummary::FirstFloat + i]);
        }
#endif
        regs->SP = base + pSummary->CfaOffset;
        regs->IP = CallerIP(regs);
        return true;
    }
    if ((summaryState & BodyUnwindSummary::Unsupported) != 0)
        return false;

    BodyUnwindState state;
    state.sp = regs->SP;
    state.restoredRegisters = 0;
    state.restoredFloats = 0;
    state.fpBased = false;
    state.mixedBase = false;
    state.baseRegister = 0;
    if (!ApplyBodyCodes(state, regs, pUnwindInfo, pCodes, pCodesEnd))
    {
        if (pSummary != NULL)
            __atomic_store_n(&pSummary->State, BodyUnwindSummary::Computed | BodyUnwindSummary::Unsupported, __ATOMIC_RELEASE);
        return false;
    }

    if (pSummary != NULL)
        StoreBodyUnwindSummary(pSummary, state, BodyUnwindBase(regs, state.fpBased, state.baseRegister),
            BodyPrologSize(pUnwindInfo, pCodes, pCodesEnd));

    for (uint32_t mask = state.restoredRegisters; mask != 0; mask &= mask - 1)
    {
        uint32_t i = __builtin_ctz(mask);
        *RegisterLocation(regs, kFirstBodyRegister + i) = state.locations[i];
    }
#if defined(TARGET_ARM64)
    for (uint32_t mask = state.restoredFloats; mask != 0; mask &= mask - 1)
    {
        uint32_t i = __builtin_ctz(mask);
        regs->D[i] = *state.floatLocations[i];
    }
#endif
    regs->SP = state.sp;
    regs->IP = CallerIP(regs);
    return true;
}

#endif // TARGET_ARM64 || TARGET_AMD64

#if defined(TARGET_ARM64)

// Number of instructions of the codes from pCode up to the end code
static uint32_t ScopeSize(PTR_uint8_t pCode, PTR_uint8_t pEnd)
{
    static const uint8_t s_sizeOfE0toFF[32] = { 4,1,2,1,1,1,1,3, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 2,3,4,5,1,1,1,1 };
    static const uint8_t s_instructionsOfE0toFF[32] = { 1,1,1,1,1,1,1,1, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 1,1,1,1,1,1,1,1 };
    uint32_t size = 0;
    while (pCode < pEnd && (*pCode & 0xFE) != 0xE4)
    {
        uint8_t code = *pCode;
        if (code < 0xE0)
        {
            size++;
            pCode += code < 0xC0 ? 1 : 2;
        }
        else
        {
            size += s_instructionsOfE0toFF[code - 0xE0];
            pCode += s_sizeOfE0toFF[code - 0xE0];
        }
    }
    return size;
}

// Restores count registers from [sp + spOffset], or from [sp] and then adds -spOffset to sp when it's negative. The
// integer registers are numbered from 32, to tell them from the floating point ones.
static inline __attribute__((always_inline)) void RestoreRange(BodyUnwindState &state, int32_t spOffset, int firstRegister, uint32_t count)
{
    uintptr_t address = spOffset >= 0 ? state.sp + spOffset : state.sp;
    for (uint32_t i = 0; i < count; i++, address += 8)
    {
        int reg = firstRegister + (int)i;
        if (reg >= 32 + 19)
        {
            state.locations[reg - 32 - 19] = (PTR_uintptr_t)address;
            state.restoredRegisters |= 1u << (reg - 32 - 19);
        }
        else if (reg >= 8 && reg < 16)
        {
            state.floatLocations[reg - 8] = (PTR_uint64_t)address;
            state.restoredFloats |= 1u << (reg - 8);
        }
    }
    if (spOffset < 0)
        state.sp -= spOffset;
}

static uint32_t BodyPrologSize(PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd)
{
    return ScopeSize(pCodes, pCodesEnd);
}

// Unwinds a frame whose PC is in the body of the function, outside the prolog and the epilogs, by applying its unwind
// codes to the REGDISPLAY, which the arm64 unwinder does through a CONTEXT. Returns false for the frames it doesn't
// handle, without changing the REGDISPLAY. The function length of the unwind information comes from the frame.
static bool TryStepFrameBody(REGDISPLAY *regs, TADDR frameStart, TADDR frameEnd, PTR_uint8_t pUnwindInfo, bool packed, PTR_uint8_t pEpilogStarts,
    BodyUnwindSummary* pSummary)
{
    uint32_t offset = (uint32_t)(regs->IP - frameStart) / 4;
    uint32_t functionLength = (uint32_t)(frameEnd - frameStart) / 4;

    if (packed)
    {
        // The chained frame of the packed form without homed parameters: the integer registers from x19 and the
        // floating point registers from d8 at the top of the frame, then the locals with the frame record at the
        // bottom, which the frame pointer points to
        uint32_t data = *dac_cast<PTR_uint32_t>(pUnwindInfo);
        uint32_t regF = (data >> 13) & 0x7;
        uint32_t regI = (data >> 16) & 0xF;
        uint32_t h = (data >> 20) & 0x1;
        uint32_t cr = (data >> 21) & 0x3;
        uint32_t frameSize = (data >> 23) * 16;
        uint32_t fpCount = regF != 0 ? regF + 1 : 0;
        uint32_t saveSize = ((regI + fpCount + 1) & ~1u) * 8;
        if ((data & 3) != 1 || cr != 3 || h != 0 || frameSize <= saveSize)
            return false;

        uint32_t localSize = frameSize - saveSize;
        uint32_t prologSize = regI / 2 + regI % 2 + fpCount / 2 + fpCount % 2 + (localSize <= 512 ? 2 : 3);

        // The epilog mirrors the prolog without the set_fp, plus the ret
        if (offset < prologSize || offset >= functionLength - prologSize)
            return false;

        uintptr_t fp = *regs->pFP;
        uintptr_t cfa = fp + frameSize;
        uintptr_t saveArea = cfa - saveSize;
        for (uint32_t i = 0; i < regI; i++)
            *RegisterLocation(regs, 19 + i) = (PTR_uintptr_t)(saveArea + 8 * i);
        for (uint32_t i = 0; i < fpCount; i++)
            regs->D[i] = *(uint64_t*)(saveArea + 8 * (regI + i));
        regs->pFP = (PTR_uintptr_t)fp;
        regs->pLR = (PTR_uintptr_t)(fp + 8);
        regs->SP = cfa;
        regs->IP = *regs->pLR;
        return true;
    }

    uint32_t header = *dac_cast<PTR_uint32_t>(pUnwindInfo);
    uint32_t codeWords = header >> 27;
    uint32_t epilogScopes = (header >> 22) & 0x1F;
    PTR_uint8_t p = pUnwindInfo + 4;
    if (codeWords == 0 && epilogScopes == 0)
    {
        uint32_t extension = *dac_cast<PTR_uint32_t>(p);
        codeWords = (extension >> 16) & 0xFF;
        epilogScopes = extension & 0xFFFF;
        p += 4;
    }

    bool singleEpilog = (header & (1 << 21)) != 0;
    PTR_uint8_t pScopes = p;
    PTR_uint8_t pCodes = singleEpilog ? p : p + 4 * epilogScopes;
    PTR_uint8_t pCodesEnd = pCodes + 4 * codeWords;

    uint32_t summaryState = pSummary != NULL ? __atomic_load_n(&pSummary->State, __ATOMIC_ACQUIRE) : 0;
    if ((summaryState & BodyUnwindSummary::Computed) != 0
        ? offset < ((summaryState >> BodyUnwindSummary::PrologSizeShift) & BodyUnwindSummary::PrologSizeMask)
        : offset < 4 * codeWords && offset < ScopeSize(pCodes, pCodesEnd))
        return false;

    if (singleEpilog)
    {
        if (offset + 4 * codeWords - epilogScopes >= functionLength &&
            offset >= functionLength - (ScopeSize(pCodes + epilogScopes, pCodesEnd) + 1))
            return false;
    }
    else
    {
        bool externalStarts = pEpilogStarts != NULL && (*dac_cast<PTR_uint32_t>(pScopes) & 0x3FFFF) == 0;
        for (uint32_t i = 0; i < epilogScopes; i++)
        {
            uint32_t scope = *dac_cast<PTR_uint32_t>(pScopes + 4 * i);
            uint32_t scopeStart = externalStarts ? ExternalEpilogStart(pEpilogStarts, epilogScopes, i) : scope & 0x3FFFF;
            if (offset < scopeStart)
                break;
            uint32_t index = scope >> 22;
            if (offset < scopeStart + 4 * codeWords - index && offset < scopeStart + ScopeSize(pCodes + index, pCodesEnd) + 1)
                return false;
        }
    }

    return StepFrameBody(regs, pSummary, summaryState, pUnwindInfo, pCodes, pCodesEnd);
}

// Applies the codes from the last prolog instruction to the first, like RtlpUnwindFunctionFull does. Returns false
// for the codes it doesn't handle.
static bool ApplyBodyCodes(BodyUnwindState& state, REGDISPLAY *regs, PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd)
{
    uint32_t saveNexts = 0;
    const int X = 32;

    // The codes that save_next doesn't extend fail when it precedes them
    for (PTR_uint8_t pCode = pCodes; pCode < pCodesEnd;)
    {
        uint8_t code = *pCode++;
        uint8_t next;
        switch (code)
        {
        case 0x00 ... 0x1F:
            if (saveNexts != 0) return false;
            state.sp += 16 * code;
            break;
        case 0x20 ... 0x3F:
            RestoreRange(state, -8 * (code & 0x1F), X + 19, 2 + 2 * saveNexts);
            saveNexts = 0;
            break;
        case 0x40 ... 0x7F:
            if (saveNexts != 0) return false;
            RestoreRange(state, 8 * (code & 0x3F), X + 29, 2);
            break;
        case 0x80 ... 0xBF:
            if (saveNexts != 0) return false;
            RestoreRange(state, -8 * ((code & 0x3F) + 1), X + 29, 2);
            break;
        case 0xC0 ... 0xC7:
            if (saveNexts != 0) return false;
            state.sp += 16 * (((code & 7) << 8) | *pCode++);
            break;
        case 0xC8 ... 0xCB:
            next = *pCode++;
            RestoreRange(state, 8 * (next & 0x3F), X + 19 + ((code & 3) << 2) + (next >> 6), 2 + 2 * saveNexts);
            saveNexts = 0;
            break;
        case 0xCC ... 0xCF:
            next = *pCode++;
            RestoreRange(state, -8 * ((next & 0x3F) + 1), X + 19 + ((code & 3) << 2) + (next >> 6), 2 + 2 * saveNexts);
            saveNexts = 0;
            break;
        case 0xD0 ... 0xD3:
            if (saveNexts != 0) return false;
            next = *pCode++;
            RestoreRange(state, 8 * (next & 0x3F), X + 19 + ((code & 3) << 2) + (next >> 6), 1);
            break;
        case 0xD4 ... 0xD5:
            if (saveNexts != 0) return false;
            next = *pCode++;
            RestoreRange(state, -8 * ((next & 0x1F) + 1), X + 19 + ((code & 1) << 3) + (next >> 5), 1);
            break;
        case 0xD6 ... 0xD7:
        {
            if (saveNexts != 0) return false;
            next = *pCode++;
            int reg = 19 + 2 * (((code & 1) << 2) + (next >> 6));
            RestoreRange(state, 8 * (next & 0x3F), X + reg, 1);
            RestoreRange(state, 8 * (next & 0x3F) + 8, X + 30, 1);
            break;
        }
        case 0xD8 ... 0xD9:
            next = *pCode++;
            RestoreRange(state, 8 * (next & 0x3F), 8 + ((code & 1) << 2) + (next >> 6), 2 + 2 * saveNexts);
            saveNexts = 0;
            break;
        case 0xDA ... 0xDB:
            next = *pCode++;
            RestoreRange(state, -8 * ((next & 0x3F) + 1), 8 + ((code & 1) << 2) + (next >> 6), 2 + 2 * saveNexts);
            saveNexts = 0;
            break;
        case 0xDC ... 0xDD:
            if (saveNexts != 0) return false;
            next = *pCode++;
            RestoreRange(state, 8 * (next & 0x3F), 8 + ((code & 1) << 2) + (next >> 6), 1);
            break;
        case 0xDE:
            if (saveNexts != 0) return false;
            next = *pCode++;
            RestoreRange(state, -8 * ((next & 0x1F) + 1), 8 + (next >> 5), 1);
            break;
        case 0xE0:
            if (saveNexts != 0) return false;
            state.sp += 16 * (((uint32_t)pCode[0] << 16) | ((uint32_t)pCode[1] << 8) | pCode[2]);
            pCode += 3;
            break;
        case 0xE1:
        case 0xE2:
            // set_fp and add_fp come first, before the frame pointer is restored
            if (saveNexts != 0 || (state.restoredRegisters & (1u << (29 - 19))) != 0)
                return false;
            state.sp = *regs->pFP - (code == 0xE2 ? 8 * (uintptr_t)*pCode++ : 0);
            state.mixedBase |= (state.restoredRegisters | state.restoredFloats) != 0;
            state.fpBased = true;
            state.baseRegister = 29;
            break;
        case 0xE3:
            if (saveNexts != 0) return false;
            break;
        case 0xE4:
        case 0xE5:
            pCode = pCodesEnd;
            break;
        case 0xE6:
            saveNexts++;
            break;
        case 0xF9:
        {
            // save_regp_rev, the NativeAOT extension: <reg + 1, reg> at [sp+#Z*8], or with p at [sp-(#Z+1)*8]!
            if (saveNexts != 0) return false;
            next = pCode[0];
            bool preIndexed = (next & 0x80) != 0;
            int first = ((next & 0x40) != 0 ? 8 : X + 19) + (next & 0x1F);
            int32_t z = pCode[1] & 0x3F;
            int32_t low = preIndexed ? 0 : 8 * z;
            RestoreRange(state, low + 8, first, 1);
            RestoreRange(state, preIndexed ? -8 * (z + 1) : low, first + 1, 1);
            pCode += 2;
            break;
        }
        default:
            // pac_sign_lr and the codes managed code doesn't use
            return false;
        }
    }

    return saveNexts == 0;
}

#endif // TARGET_ARM64

#if defined(TARGET_AMD64)

static uint32_t BodyPrologSize(PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd)
{
    return dac_cast<PTR_UNWIND_INFO>(pUnwindInfo)->SizeOfProlog;
}

// Applies the codes from the last prolog instruction to the first, like the unwinder does for a PC in the body.
// Returns false for the codes it doesn't handle.
static bool ApplyBodyCodes(BodyUnwindState& state, REGDISPLAY *regs, PTR_uint8_t pUnwindInfo, PTR_uint8_t pCodes, PTR_uint8_t pCodesEnd)
{
    PTR_UNWIND_INFO pInfo = dac_cast<PTR_UNWIND_INFO>(pUnwindInfo);
    DPTR(UNWIND_CODE) pCode = dac_cast<DPTR(UNWIND_CODE)>(pCodes);
    uint32_t count = pInfo->CountOfUnwindCodes;

    // The saved registers are relative to the stack pointer of the body, or to the stack pointer that the frame
    // register was set from
    state.fpBased = pInfo->FrameRegister != 0;
    state.baseRegister = pInfo->FrameRegister;
    uintptr_t frameBase = state.fpBased ? **RegisterLocation(regs, state.baseRegister) - 16 * pInfo->FrameOffset : regs->SP;
    bool frameRegisterSet = false;

    for (uint32_t i = 0; i < count; i++)
    {
        uint32_t opInfo = pCode[i].OpInfo;
        switch (pCode[i].UnwindOp)
        {
        case UWOP_PUSH_NONVOL:
            if (opInfo == 4)
                return false;
            state.mixedBase |= state.fpBased && !frameRegisterSet;
            state.locations[opInfo] = (PTR_uintptr_t)state.sp;
            state.restoredRegisters |= 1u << opInfo;
            state.sp += 8;
            break;
        case UWOP_ALLOC_LARGE:
            if (opInfo == 0)
            {
                state.sp += 8 * (uintptr_t)pCode[i + 1].FrameOffset;
                i += 1;
            }
            else
            {
                state.sp += pCode[i + 1].FrameOffset + ((uintptr_t)pCode[i + 2].FrameOffset << 16);
                i += 2;
            }
            break;
        case UWOP_ALLOC_SMALL:
            state.sp += 8 * opInfo + 8;
            break;
        case UWOP_SET_FPREG:
            state.sp = frameBase;
            frameRegisterSet = true;
            break;
        case UWOP_SAVE_NONVOL:
        case UWOP_SAVE_NONVOL_FAR:
        {
            if (opInfo == 4)
                return false;
            uintptr_t offset = pCode[i].UnwindOp == UWOP_SAVE_NONVOL
                ? 8 * (uintptr_t)pCode[i + 1].FrameOffset
                : pCode[i + 1].FrameOffset + ((uintptr_t)pCode[i + 2].FrameOffset << 16);
            i += pCode[i].UnwindOp == UWOP_SAVE_NONVOL ? 1 : 2;
            state.locations[opInfo] = (PTR_uintptr_t)(frameBase + offset);
            state.restoredRegisters |= 1u << opInfo;
            break;
        }
        default:
            // The large frame register offset, the xmm registers, which aren't callee saved on Unix, and the machine
            // frames
            return false;
        }
    }

    // Pop the return address
    state.sp += 8;
    return true;
}

// Unwinds a frame whose PC is in the body of the function, outside the prolog and the epilogs. Returns false for the
// frames it doesn't handle, without changing the REGDISPLAY.
static bool TryStepFrameBody(REGDISPLAY *regs, TADDR frameStart, TADDR frameEnd, PTR_uint8_t pUnwindInfo, BodyUnwindSummary* pSummary)
{
    PTR_UNWIND_INFO pInfo = dac_cast<PTR_UNWIND_INFO>(pUnwindInfo);
    if (regs->IP - frameStart < pInfo->SizeOfProlog)
        return false;

    // The unwinder tells an epilog by its instructions: an optional add or lea to rsp, the pops, then a ret or a
    // jmp out of the function. The JIT puts a nop between a call and an epilog, so return addresses are never in an
    // epilog. A PC at a pop, a ret or a jmp out of the function can be in an epilog, which the unwinder handles. A
    // PC at the add or lea is at the start of an epilog, which unwinds like the body.
    PTR_uint8_t pInstr = dac_cast<PTR_uint8_t>(regs->IP);
    bool rex = (pInstr[0] & 0xF0) == 0x40;
    uint8_t opcode = pInstr[rex ? 1 : 0];
    uint8_t next = pInstr[rex ? 2 : 1];
    if ((opcode >= 0x58 && opcode <= 0x5F) || opcode == 0xC3 || opcode == 0xC2 || (opcode == 0xF3 && next == 0xC3) ||
        (opcode == 0xFF && ((next >> 3) & 7) >= 4 && ((next >> 3) & 7) <= 5))
        return false;
    if (!rex && (opcode == 0xE9 || opcode == 0xEB))
    {
        TADDR target = opcode == 0xEB
            ? regs->IP + 2 + (int8_t)next
            : regs->IP + 5 + (int32_t)(pInstr[1] | ((uint32_t)pInstr[2] << 8) | ((uint32_t)pInstr[3] << 16) | ((uint32_t)pInstr[4] << 24));
        if (target < frameStart || target >= frameEnd)
            return false;
    }

    uint32_t summaryState = pSummary != NULL ? __atomic_load_n(&pSummary->State, __ATOMIC_ACQUIRE) : 0;
    return StepFrameBody(regs, pSummary, summaryState, pUnwindInfo, dac_cast<PTR_uint8_t>(&pInfo->UnwindCode[0]),
        dac_cast<PTR_uint8_t>(&pInfo->UnwindCode[pInfo->CountOfUnwindCodes]));
}

#endif // TARGET_AMD64

bool UnwindHelpers::StepFrame(REGDISPLAY *regs, TADDR frameStart, TADDR frameEnd, PTR_uint8_t pUnwindInfo, bool packed, PTR_uint8_t pEpilogStarts,
    BodyUnwindSummary *pSummary, uintptr_t *pSpForPacSign)
{
#if defined(TARGET_ARM64)
    if (TryStepFrameBody(regs, frameStart, frameEnd, pUnwindInfo, packed, pEpilogStarts, pSummary))
        return true;
#elif defined(TARGET_AMD64)
    if (TryStepFrameBody(regs, frameStart, frameEnd, pUnwindInfo, pSummary))
        return true;
#endif

    CONTEXT context;
    KNONVOLATILE_CONTEXT_POINTERS contextPointers;

#ifdef _DEBUG
    memset(&context, 0xDD, sizeof(context));
    memset(&contextPointers, 0xDD, sizeof(contextPointers));
#endif

#define REGDISPLAY_TO_CONTEXT(contextField, regDisplayField) \
    contextPointers.contextField = (decltype(contextPointers.contextField))regs->regDisplayField; \
    if (regs->regDisplayField != NULL) context.contextField = *regs->regDisplayField;
#define FLOAT_REGDISPLAY_TO_CONTEXT(contextField, regDisplayField) context.contextField = regs->regDisplayField;

    FOR_EACH_REGISTER(REGDISPLAY_TO_CONTEXT);
#ifdef FOR_EACH_FLOAT_REGISTER
    FOR_EACH_FLOAT_REGISTER(FLOAT_REGDISPLAY_TO_CONTEXT);
#endif
    context.CONTEXT_SP = regs->SP;
    context.CONTEXT_PC = regs->IP;

    PVOID handlerData;
    bool success;

#if defined(TARGET_AMD64)
    // The function entry refers to the code and the unwind information relative to an image base below both
    TADDR imageBase = frameStart < (TADDR)pUnwindInfo ? frameStart : (TADDR)pUnwindInfo;
    ASSERT(frameEnd - imageBase <= UINT32_MAX && (TADDR)pUnwindInfo - imageBase <= UINT32_MAX);
    _IMAGE_RUNTIME_FUNCTION_ENTRY functionEntry;
    functionEntry.BeginAddress = (DWORD)(frameStart - imageBase);
    functionEntry.EndAddress = (DWORD)(frameEnd - imageBase);
    functionEntry.UnwindInfoAddress = (DWORD)((TADDR)pUnwindInfo - imageBase);

    DWORD64 establisherFrame;
    success = SUCCEEDED(OOPStackUnwinderAMD64::VirtualUnwind(0, imageBase, regs->IP, &functionEntry, &context,
        &handlerData, &establisherFrame, &contextPointers, NULL));
#else
    // The unwind information of the table doesn't have the function length, so the unwinder gets a copy with it.
    // The unwinders only use the image base to compute offsets, so the function and the copy of the unwind
    // information are given relative to an image base below the copy, with a control PC at the same offset in the
    // function.
#if defined(TARGET_ARM) || defined(TARGET_RISCV64)
    const uint32_t lengthUnit = 2;
#else
    const uint32_t lengthUnit = 4;
#endif
    uint32_t functionLength = (uint32_t)(frameEnd - frameStart) / lengthUnit;
    uint32_t unwindInfoSize = packed ? 0 : GetUnwindInfoSize(pUnwindInfo);
    uint32_t* pCopy = (uint32_t*)alloca(unwindInfoSize + 4);
    memcpy(pCopy, pUnwindInfo, unwindInfoSize);
    pCopy[0] = (pCopy[0] & ~0x3FFFFu) | functionLength;
#if !defined(TARGET_ARM)
    if (!packed && (pCopy[0] & (1 << 21)) == 0 && pEpilogStarts != NULL)
    {
        uint32_t epilogScopes = (pCopy[0] >> 22) & 0x1F;
        uint32_t* pScopes = pCopy + 1;
        if ((pCopy[0] >> 27) == 0 && epilogScopes == 0)
        {
            epilogScopes = pCopy[1] & 0xFFFF;
            pScopes++;
        }
        if (epilogScopes != 0 && (pScopes[0] & 0x3FFFF) == 0)
        {
            for (uint32_t i = 0; i < epilogScopes; i++)
                pScopes[i] |= ExternalEpilogStart(pEpilogStarts, epilogScopes, i);
        }
    }
#endif

    TADDR fakeImageBase = (TADDR)pCopy - 0x1000;
    TADDR controlPc = fakeImageBase + (regs->IP - frameStart);
    RUNTIME_FUNCTION functionEntry;
    functionEntry.BeginAddress = 0;
    functionEntry.UnwindData = packed ? (*dac_cast<PTR_uint32_t>(pUnwindInfo) | (functionLength << 2)) : 0x1000;

#if defined(TARGET_ARM)
    // The control PC of a caller frame is its return address, which has the Thumb bit
    ULONG establisherFrame;
    RtlVirtualUnwind(0, (ULONG)fakeImageBase, (ULONG)(controlPc & ~(TADDR)1), &functionEntry, &context,
        &handlerData, &establisherFrame, &contextPointers);
#elif defined(TARGET_ARM64)
    ULONG64 establisherFrame;
    ULONG64 spForPacSign = 0;
    RtlVirtualUnwindWithSpForPacSign(0, fakeImageBase, controlPc, &functionEntry, &context, &handlerData,
        &establisherFrame, &contextPointers, &spForPacSign);
    if (pSpForPacSign != NULL)
        *pSpForPacSign = (uintptr_t)spForPacSign;
#else
    ULONG64 establisherFrame;
    RtlVirtualUnwind(0, fakeImageBase, controlPc, &functionEntry, &context, &handlerData, &establisherFrame,
        &contextPointers);
#endif

    // The unwinders clear the PC when they fail
    success = context.CONTEXT_PC != 0;
#endif

    if (!success)
        return false;

#define CONTEXT_TO_REGDISPLAY(contextField, regDisplayField) \
    regs->regDisplayField = (PTR_uintptr_t)contextPointers.contextField;
#define FLOAT_CONTEXT_TO_REGDISPLAY(contextField, regDisplayField) regs->regDisplayField = context.contextField;

    FOR_EACH_REGISTER(CONTEXT_TO_REGDISPLAY);
#ifdef FOR_EACH_FLOAT_REGISTER
    FOR_EACH_FLOAT_REGISTER(FLOAT_CONTEXT_TO_REGDISPLAY);
#endif
    regs->SP = context.CONTEXT_SP;
    regs->IP = context.CONTEXT_PC;
    return true;
}
