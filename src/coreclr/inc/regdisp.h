// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __REGDISP_H
#define __REGDISP_H


#ifdef DEBUG_REGDISPLAY
class Thread;
struct REGDISPLAY;
void CheckRegDisplaySP (REGDISPLAY *pRD);
#endif // DEBUG_REGDISPLAY

struct REGDISPLAY_BASE {
    PT_CONTEXT pContext;    // This is the context of the active call frame;
                            // either returned by GetContext or provided at
                            // exception time.
                            //
                            // This will be used to resume execution, so
                            // do NOT trash it! But DO update any static
                            // registers here.

#ifdef FEATURE_EH_FUNCLETS
    PT_CONTEXT pCurrentContext;   // [trashed] points to current Context of stackwalk
    PT_CONTEXT pCallerContext;    // [trashed] points to the Context of the caller during stackwalk -- used for GC crawls

    // [trashed] points to current context pointers of stackwalk
    T_KNONVOLATILE_CONTEXT_POINTERS *pCurrentContextPointers;
    // [trashed] points to the context pointers of the caller during stackwalk -- used for GC crawls
    T_KNONVOLATILE_CONTEXT_POINTERS *pCallerContextPointers;

    BOOL IsCallerContextValid;  // TRUE if pCallerContext really contains the caller's context
    BOOL IsCallerSPValid;       // Don't add usage of this field.  This is only temporary.

    T_CONTEXT  ctxOne;    // used by stackwalk
    T_CONTEXT  ctxTwo;    // used by stackwalk

    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsOne;  // used by stackwalk
    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsTwo;  // used by stackwalk
#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG_REGDISPLAY
    Thread *_pThread;
#endif // DEBUG_REGDISPLAY

    TADDR SP;
    TADDR ControlPC; // LOONGARCH: use RA for PC
};

inline PCODE GetControlPC(const REGDISPLAY_BASE *pRD) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (PCODE)(pRD->ControlPC);
}

inline TADDR GetRegdisplaySP(REGDISPLAY_BASE *pRD) {
    LIMITED_METHOD_DAC_CONTRACT;

    return pRD->SP;
}

inline void SetRegdisplaySP(REGDISPLAY_BASE *pRD, LPVOID sp) {
    LIMITED_METHOD_DAC_CONTRACT;

    pRD->SP = (TADDR)sp;
}

#if defined(TARGET_X86)

struct REGDISPLAY : public REGDISPLAY_BASE {

#ifndef FEATURE_EH_FUNCLETS
    // TODO: Unify with pCurrentContext / pCallerContext used on 64-bit
    PCONTEXT pContextForUnwind; // scratch context for unwinding
                                // used to preserve context saved in the frame that
                                // could be otherwise wiped by the unwinding

    DWORD * pEdi;
    DWORD * pEsi;
    DWORD * pEbx;
    DWORD * pEdx;
    DWORD * pEcx;
    DWORD * pEax;

    DWORD * pEbp;
#endif // !FEATURE_EH_FUNCLETS

#ifndef FEATURE_EH_FUNCLETS

#define REG_METHODS(reg) \
    inline PDWORD Get##reg##Location(void) { return p##reg;  } \
    inline void   Set##reg##Location(PDWORD p##reg) { this->p##reg = p##reg; }

#else // !FEATURE_EH_FUNCLETS

#define REG_METHODS(reg) \
    inline PDWORD Get##reg##Location(void) { return pCurrentContextPointers->reg; } \
    inline void   Set##reg##Location(PDWORD p##reg) \
    { \
        pCurrentContextPointers->reg = p##reg; \
        pCurrentContext->reg = *p##reg; \
    }

#endif // FEATURE_EH_FUNCLETS

    REG_METHODS(Eax)
    REG_METHODS(Ecx)
    REG_METHODS(Edx)

    REG_METHODS(Ebx)
    REG_METHODS(Esi)
    REG_METHODS(Edi)
    REG_METHODS(Ebp)

#undef REG_METHODS

    TADDR   PCTAddr;
};

inline TADDR GetRegdisplayFP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_EH_FUNCLETS
    return (TADDR)display->pCurrentContext->Ebp;
#else
    return (TADDR)*display->GetEbpLocation();
#endif
}

inline LPVOID GetRegdisplayFPAddress(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)display->GetEbpLocation();
}


// This function tells us if the given stack pointer is in one of the frames of the functions called by the given frame
inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer) {
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    return stackPointer < ((LPVOID)(display->SP));
#else
    return (TADDR)stackPointer < display->PCTAddr;
#endif
}
inline TADDR GetRegdisplayStackMark(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    _ASSERTE(GetRegdisplaySP(display) == GetSP(display->pCurrentContext));
    return GetRegdisplaySP(display);
#else
    return display->PCTAddr;
#endif
}

#elif defined(TARGET_64BIT)

#if defined(TARGET_ARM64)
typedef struct _Arm64VolatileContextPointer
{
    union {
        struct {
            PDWORD64 X0;
            PDWORD64 X1;
            PDWORD64 X2;
            PDWORD64 X3;
            PDWORD64 X4;
            PDWORD64 X5;
            PDWORD64 X6;
            PDWORD64 X7;
            PDWORD64 X8;
            PDWORD64 X9;
            PDWORD64 X10;
            PDWORD64 X11;
            PDWORD64 X12;
            PDWORD64 X13;
            PDWORD64 X14;
            PDWORD64 X15;
            PDWORD64 X16;
            PDWORD64 X17;
            //X18 is reserved by OS, in userspace it represents TEB
        };
        PDWORD64 X[18];
    };
} Arm64VolatileContextPointer;
#endif //TARGET_ARM64

#if defined(TARGET_LOONGARCH64)
typedef struct _Loongarch64VolatileContextPointer
{
    PDWORD64 R0;
    PDWORD64 A0;
    PDWORD64 A1;
    PDWORD64 A2;
    PDWORD64 A3;
    PDWORD64 A4;
    PDWORD64 A5;
    PDWORD64 A6;
    PDWORD64 A7;
    PDWORD64 T0;
    PDWORD64 T1;
    PDWORD64 T2;
    PDWORD64 T3;
    PDWORD64 T4;
    PDWORD64 T5;
    PDWORD64 T6;
    PDWORD64 T7;
    PDWORD64 T8;
    PDWORD64 X0;
} Loongarch64VolatileContextPointer;
#endif

#if defined(TARGET_RISCV64)
typedef struct _Riscv64VolatileContextPointer
{
    PDWORD64 R0;
    PDWORD64 A0;
    PDWORD64 A1;
    PDWORD64 A2;
    PDWORD64 A3;
    PDWORD64 A4;
    PDWORD64 A5;
    PDWORD64 A6;
    PDWORD64 A7;
    PDWORD64 T0;
    PDWORD64 T1;
    PDWORD64 T2;
    PDWORD64 T3;
    PDWORD64 T4;
    PDWORD64 T5;
    PDWORD64 T6;
} Riscv64VolatileContextPointer;
#endif

struct REGDISPLAY : public REGDISPLAY_BASE {
#ifdef TARGET_ARM64
    Arm64VolatileContextPointer     volatileCurrContextPointers;
#endif

#ifdef TARGET_LOONGARCH64
    Loongarch64VolatileContextPointer    volatileCurrContextPointers;
#endif

#ifdef TARGET_RISCV64
    Riscv64VolatileContextPointer    volatileCurrContextPointers;
#endif

    REGDISPLAY()
    {
        // Initialize
        memset(this, 0, sizeof(REGDISPLAY));
    }
};


inline TADDR GetRegdisplayFP(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return NULL;
}

inline TADDR GetRegdisplayFPAddress(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return NULL;
}

// This function tells us if the given stack pointer is in one of the frames of the functions called by the given frame
inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer)
{
    LIMITED_METHOD_CONTRACT;
    return stackPointer < ((LPVOID)(display->SP));
}

inline TADDR GetRegdisplayStackMark(REGDISPLAY *display)
{
#if defined(TARGET_AMD64)
    // On AMD64, the MemoryStackFp value is the current sp (i.e. the sp value when calling another method).
    _ASSERTE(GetRegdisplaySP(display) == GetSP(display->pCurrentContext));
    return GetRegdisplaySP(display);

#elif defined(TARGET_ARM64)

    _ASSERTE(display->IsCallerContextValid);
    return GetSP(display->pCallerContext);

#else  // TARGET_AMD64
    PORTABILITY_ASSERT("GetRegdisplayStackMark NYI for this platform (Regdisp.h)");
    return NULL;
#endif // TARGET_AMD64
}

#elif defined(TARGET_ARM)

// ResumableFrame is pushed on the stack before
// starting the GC. registers r0-r3 in ResumableFrame can
// contain roots which might need to be updated if they are
// relocated. On Stack walking the addresses of the registers in the
// resumable Frame are passed to GC using pCurrentContextPointers
// member in _REGDISPLAY. However On ARM KNONVOLATILE_CONTEXT_POINTERS
// does not contain pointers for volatile registers. Therefore creating
// this structure to store pointers to volatile registers and adding an object
// as member in _REGDISPLAY
typedef struct _ArmVolatileContextPointer
{
    PDWORD R0;
    PDWORD R1;
    PDWORD R2;
    PDWORD R3;
    PDWORD R12;
} ArmVolatileContextPointer;

struct REGDISPLAY : public REGDISPLAY_BASE {
    ArmVolatileContextPointer     volatileCurrContextPointers;

    DWORD *  pPC;                // processor neutral name
    REGDISPLAY()
    {
        // Initialize regdisplay
        memset(this, 0, sizeof(REGDISPLAY));

        // Setup the pointer to ControlPC field
        pPC = &ControlPC;
    }
};

// This function tells us if the given stack pointer is in one of the frames of the functions called by the given frame
inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer) {
    LIMITED_METHOD_CONTRACT;
    return stackPointer < ((LPVOID)(TADDR)(display->SP));
}

inline TADDR GetRegdisplayStackMark(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    // ARM uses the establisher frame as the marker
    _ASSERTE(display->IsCallerContextValid);
    return GetSP(display->pCallerContext);
}

#else // none of the above processors
#error "RegDisplay functions are not implemented on this platform."
#endif

#if defined(TARGET_64BIT) || defined(TARGET_ARM) || (defined(TARGET_X86) && defined(FEATURE_EH_FUNCLETS))
// This needs to be implemented for platforms that have funclets.
inline LPVOID GetRegdisplayReturnValue(REGDISPLAY *display)
{
    LIMITED_METHOD_CONTRACT;

#if defined(TARGET_AMD64)
    return (LPVOID)display->pCurrentContext->Rax;
#elif defined(TARGET_ARM64)
    return (LPVOID)display->pCurrentContext->X0;
#elif defined(TARGET_ARM)
    return (LPVOID)((TADDR)display->pCurrentContext->R0);
#elif defined(TARGET_X86)
    return (LPVOID)display->pCurrentContext->Eax;
#elif defined(TARGET_LOONGARCH64)
    return (LPVOID)display->pCurrentContext->A0;
#elif defined(TARGET_RISCV64)
    return (LPVOID)display->pCurrentContext->A0;
#else
    PORTABILITY_ASSERT("GetRegdisplayReturnValue NYI for this platform (Regdisp.h)");
    return NULL;
#endif
}

inline void SyncRegDisplayToCurrentContext(REGDISPLAY* pRD)
{
    LIMITED_METHOD_CONTRACT;

#if defined(TARGET_64BIT)
    pRD->SP         = (INT_PTR)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = INT_PTR(GetIP(pRD->pCurrentContext));
#elif defined(TARGET_ARM)
    pRD->SP         = (DWORD)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = (DWORD)GetIP(pRD->pCurrentContext);
#elif defined(TARGET_X86)
    pRD->SP         = (DWORD)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = (DWORD)GetIP(pRD->pCurrentContext);
#else // TARGET_X86
    PORTABILITY_ASSERT("SyncRegDisplayToCurrentContext");
#endif

#ifdef DEBUG_REGDISPLAY
    CheckRegDisplaySP(pRD);
#endif // DEBUG_REGDISPLAY
}
#endif // TARGET_64BIT || TARGET_ARM || (TARGET_X86 && FEATURE_EH_FUNCLETS)

typedef REGDISPLAY *PREGDISPLAY;

#ifdef FEATURE_EH_FUNCLETS
inline void FillContextPointers(PT_KNONVOLATILE_CONTEXT_POINTERS pCtxPtrs, PT_CONTEXT pCtx)
{
#ifdef TARGET_AMD64
    for (int i = 0; i < 16; i++)
    {
        *(&pCtxPtrs->Rax + i) = (&pCtx->Rax + i);
    }
#elif defined(TARGET_ARM64) // TARGET_AMD64
    for (int i = 0; i < 12; i++)
    {
        *(&pCtxPtrs->X19 + i) = (&pCtx->X19 + i);
    }
#elif defined(TARGET_LOONGARCH64)  // TARGET_ARM64
    *(&pCtxPtrs->S0) = &pCtx->S0;
    *(&pCtxPtrs->S1) = &pCtx->S1;
    *(&pCtxPtrs->S2) = &pCtx->S2;
    *(&pCtxPtrs->S3) = &pCtx->S3;
    *(&pCtxPtrs->S4) = &pCtx->S4;
    *(&pCtxPtrs->S5) = &pCtx->S5;
    *(&pCtxPtrs->S6) = &pCtx->S6;
    *(&pCtxPtrs->S7) = &pCtx->S7;
    *(&pCtxPtrs->S8) = &pCtx->S8;
    *(&pCtxPtrs->Tp) = &pCtx->Tp;
    *(&pCtxPtrs->Fp) = &pCtx->Fp;
    *(&pCtxPtrs->Ra) = &pCtx->Ra;
#elif defined(TARGET_ARM) // TARGET_LOONGARCH64
    // Copy over the nonvolatile integer registers (R4-R11)
    for (int i = 0; i < 8; i++)
    {
        *(&pCtxPtrs->R4 + i) = (&pCtx->R4 + i);
    }
#elif defined(TARGET_X86) // TARGET_ARM
    for (int i = 0; i < 7; i++)
    {
        *(&pCtxPtrs->Edi + i) = (&pCtx->Edi + i);
    }
#elif defined(TARGET_RISCV64) // TARGET_X86
    *(&pCtxPtrs->S1) = &pCtx->S1;
    *(&pCtxPtrs->S2) = &pCtx->S2;
    *(&pCtxPtrs->S3) = &pCtx->S3;
    *(&pCtxPtrs->S4) = &pCtx->S4;
    *(&pCtxPtrs->S5) = &pCtx->S5;
    *(&pCtxPtrs->S6) = &pCtx->S6;
    *(&pCtxPtrs->S7) = &pCtx->S7;
    *(&pCtxPtrs->S8) = &pCtx->S8;
    *(&pCtxPtrs->S9) = &pCtx->S9;
    *(&pCtxPtrs->S10) = &pCtx->S10;
    *(&pCtxPtrs->S11) = &pCtx->S11;
    *(&pCtxPtrs->Gp) = &pCtx->Gp;
    *(&pCtxPtrs->Tp) = &pCtx->Tp;
    *(&pCtxPtrs->Fp) = &pCtx->Fp;
    *(&pCtxPtrs->Ra) = &pCtx->Ra;
#else // TARGET_RISCV64
    PORTABILITY_ASSERT("FillContextPointers");
#endif // _TARGET_???_ (ELSE)
}
#endif // FEATURE_EH_FUNCLETS

inline void FillRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx, PT_CONTEXT pCallerCtx = NULL)
{
    WRAPPER_NO_CONTRACT;

    SUPPORTS_DAC;

#ifndef FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
    pRD->pContext = pctx;
    pRD->pContextForUnwind = NULL;
    pRD->pEdi = &(pctx->Edi);
    pRD->pEsi = &(pctx->Esi);
    pRD->pEbx = &(pctx->Ebx);
    pRD->pEbp = &(pctx->Ebp);
    pRD->pEax = &(pctx->Eax);
    pRD->pEcx = &(pctx->Ecx);
    pRD->pEdx = &(pctx->Edx);
    pRD->SP   = pctx->Esp;
    pRD->ControlPC = (PCODE)(pctx->Eip);
    pRD->PCTAddr = (UINT_PTR)&(pctx->Eip);
#else // TARGET_X86
    PORTABILITY_ASSERT("FillRegDisplay");
#endif // _TARGET_???_ (ELSE)

#else // !FEATURE_EH_FUNCLETS
    pRD->pContext   = pctx;

    // Setup the references
    pRD->pCurrentContextPointers = &pRD->ctxPtrsOne;
    pRD->pCallerContextPointers = &pRD->ctxPtrsTwo;

    pRD->pCurrentContext = &(pRD->ctxOne);
    pRD->pCallerContext  = &(pRD->ctxTwo);

    // copy the active context to initialize our stackwalk
    *(pRD->pCurrentContext)     = *(pctx);

    // copy the caller context as well if it's specified
    if (pCallerCtx == NULL)
    {
        pRD->IsCallerContextValid = FALSE;
        pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
    }
    else
    {
        *(pRD->pCallerContext)    = *(pCallerCtx);
        pRD->IsCallerContextValid = TRUE;
        pRD->IsCallerSPValid      = TRUE;        // Don't add usage of this field.  This is only temporary.
    }

    FillContextPointers(&pRD->ctxPtrsOne, pctx);

#if defined(TARGET_ARM)
    // Fill volatile context pointers. They can be used by GC in the case of the leaf frame
    pRD->volatileCurrContextPointers.R0 = &pctx->R0;
    pRD->volatileCurrContextPointers.R1 = &pctx->R1;
    pRD->volatileCurrContextPointers.R2 = &pctx->R2;
    pRD->volatileCurrContextPointers.R3 = &pctx->R3;
    pRD->volatileCurrContextPointers.R12 = &pctx->R12;

    pRD->ctxPtrsOne.Lr = &pctx->Lr;
    pRD->pPC = &pRD->pCurrentContext->Pc;
#elif defined(TARGET_ARM64) // TARGET_ARM
    // Fill volatile context pointers. They can be used by GC in the case of the leaf frame
    for (int i=0; i < 18; i++)
        pRD->volatileCurrContextPointers.X[i] = &pctx->X[i];
#elif defined(TARGET_LOONGARCH64) // TARGET_ARM64
    pRD->volatileCurrContextPointers.A0 = &pctx->A0;
    pRD->volatileCurrContextPointers.A1 = &pctx->A1;
    pRD->volatileCurrContextPointers.A2 = &pctx->A2;
    pRD->volatileCurrContextPointers.A3 = &pctx->A3;
    pRD->volatileCurrContextPointers.A4 = &pctx->A4;
    pRD->volatileCurrContextPointers.A5 = &pctx->A5;
    pRD->volatileCurrContextPointers.A6 = &pctx->A6;
    pRD->volatileCurrContextPointers.A7 = &pctx->A7;
    pRD->volatileCurrContextPointers.T0 = &pctx->T0;
    pRD->volatileCurrContextPointers.T1 = &pctx->T1;
    pRD->volatileCurrContextPointers.T2 = &pctx->T2;
    pRD->volatileCurrContextPointers.T3 = &pctx->T3;
    pRD->volatileCurrContextPointers.T4 = &pctx->T4;
    pRD->volatileCurrContextPointers.T5 = &pctx->T5;
    pRD->volatileCurrContextPointers.T6 = &pctx->T6;
    pRD->volatileCurrContextPointers.T7 = &pctx->T7;
    pRD->volatileCurrContextPointers.T8 = &pctx->T8;
    pRD->volatileCurrContextPointers.X0 = &pctx->X0;
#elif defined(TARGET_RISCV64) // TARGET_LOONGARCH64
    pRD->volatileCurrContextPointers.A0 = &pctx->A0;
    pRD->volatileCurrContextPointers.A1 = &pctx->A1;
    pRD->volatileCurrContextPointers.A2 = &pctx->A2;
    pRD->volatileCurrContextPointers.A3 = &pctx->A3;
    pRD->volatileCurrContextPointers.A4 = &pctx->A4;
    pRD->volatileCurrContextPointers.A5 = &pctx->A5;
    pRD->volatileCurrContextPointers.A6 = &pctx->A6;
    pRD->volatileCurrContextPointers.A7 = &pctx->A7;
    pRD->volatileCurrContextPointers.T0 = &pctx->T0;
    pRD->volatileCurrContextPointers.T1 = &pctx->T1;
    pRD->volatileCurrContextPointers.T2 = &pctx->T2;
    pRD->volatileCurrContextPointers.T3 = &pctx->T3;
    pRD->volatileCurrContextPointers.T4 = &pctx->T4;
    pRD->volatileCurrContextPointers.T5 = &pctx->T5;
    pRD->volatileCurrContextPointers.T6 = &pctx->T6;
#endif // TARGET_RISCV64

#ifdef DEBUG_REGDISPLAY
    pRD->_pThread = NULL;
#endif // DEBUG_REGDISPLAY

    // This will setup the PC and SP
    SyncRegDisplayToCurrentContext(pRD);
#endif // !FEATURE_EH_FUNCLETS
}

// Initialize a new REGDISPLAY/CONTEXT pair from an existing valid REGDISPLAY.
inline void CopyRegDisplay(const PREGDISPLAY pInRD, PREGDISPLAY pOutRD, T_CONTEXT *pOutCtx)
{
    WRAPPER_NO_CONTRACT;

    // The general strategy is to extract the register state from the input REGDISPLAY
    // into the new CONTEXT then simply call FillRegDisplay.

    T_CONTEXT* pOutCallerCtx = NULL;

#ifndef FEATURE_EH_FUNCLETS

#if defined(TARGET_X86)
    if (pInRD->pEdi != NULL) {pOutCtx->Edi = *pInRD->pEdi;} else {pInRD->pEdi = NULL;}
    if (pInRD->pEsi != NULL) {pOutCtx->Esi = *pInRD->pEsi;} else {pInRD->pEsi = NULL;}
    if (pInRD->pEbx != NULL) {pOutCtx->Ebx = *pInRD->pEbx;} else {pInRD->pEbx = NULL;}
    if (pInRD->pEbp != NULL) {pOutCtx->Ebp = *pInRD->pEbp;} else {pInRD->pEbp = NULL;}
    if (pInRD->pEax != NULL) {pOutCtx->Eax = *pInRD->pEax;} else {pInRD->pEax = NULL;}
    if (pInRD->pEcx != NULL) {pOutCtx->Ecx = *pInRD->pEcx;} else {pInRD->pEcx = NULL;}
    if (pInRD->pEdx != NULL) {pOutCtx->Edx = *pInRD->pEdx;} else {pInRD->pEdx = NULL;}
    pOutCtx->Esp = pInRD->SP;
    pOutCtx->Eip = pInRD->ControlPC;
#else // TARGET_X86
    PORTABILITY_ASSERT("CopyRegDisplay");
#endif // _TARGET_???_

#else // FEATURE_EH_FUNCLETS

    *pOutCtx = *(pInRD->pCurrentContext);
    if (pInRD->IsCallerContextValid)
    {
        pOutCallerCtx = pInRD->pCallerContext;
    }

#endif // FEATURE_EH_FUNCLETS

    if (pOutRD)
        FillRegDisplay(pOutRD, pOutCtx, pOutCallerCtx);
}

// Get address of a register in a CONTEXT given the reg number. For X86,
// the reg number is the R/M number from ModR/M byte or base in SIB byte
inline size_t * getRegAddr (unsigned regNum, PTR_CONTEXT regs)
{
#ifdef TARGET_X86
    _ASSERTE(regNum < 8);

    static const SIZE_T OFFSET_OF_REGISTERS[] =
    {
        offsetof(CONTEXT, Eax),
        offsetof(CONTEXT, Ecx),
        offsetof(CONTEXT, Edx),
        offsetof(CONTEXT, Ebx),
        offsetof(CONTEXT, Esp),
        offsetof(CONTEXT, Ebp),
        offsetof(CONTEXT, Esi),
        offsetof(CONTEXT, Edi),
    };

    return (PTR_size_t)(PTR_BYTE(regs) + OFFSET_OF_REGISTERS[regNum]);
#elif defined(TARGET_AMD64)
    _ASSERTE(regNum < 16);
    return (size_t *)&regs->Rax + regNum;
#elif defined(TARGET_ARM)
        _ASSERTE(regNum < 16);
        return (size_t *)&regs->R0 + regNum;
#elif defined(TARGET_ARM64)
    _ASSERTE(regNum < 31);
    return (size_t *)&regs->X0 + regNum;
#elif defined(TARGET_LOONGARCH64)
    _ASSERTE(regNum < 32);
    return (size_t *)&regs->R0 + regNum;
#elif defined(TARGET_RISCV64)
    _ASSERTE(regNum < 32);
    return (size_t *)&regs->R0 + regNum;
#else
    _ASSERTE(!"@TODO Port - getRegAddr (Regdisp.h)");
#endif
    return(0);
}

//---------------------------------------------------------------------------------------
//
// This is just a simpler helper function to convert a REGDISPLAY to a CONTEXT.
//
// Arguments:
//    pRegDisp - the REGDISPLAY to be converted
//    pContext - the buffer for storing the converted CONTEXT
//
inline void UpdateContextFromRegDisp(PREGDISPLAY pRegDisp, PT_CONTEXT pContext)
{
    _ASSERTE((pRegDisp != NULL) && (pContext != NULL));

#ifndef FEATURE_EH_FUNCLETS

#if defined(TARGET_X86)
    pContext->ContextFlags = (CONTEXT_INTEGER | CONTEXT_CONTROL);
    pContext->Edi = *pRegDisp->pEdi;
    pContext->Esi = *pRegDisp->pEsi;
    pContext->Ebx = *pRegDisp->pEbx;
    pContext->Ebp = *pRegDisp->pEbp;
    pContext->Eax = *pRegDisp->pEax;
    pContext->Ecx = *pRegDisp->pEcx;
    pContext->Edx = *pRegDisp->pEdx;
    pContext->Esp = pRegDisp->SP;
    pContext->Eip = pRegDisp->ControlPC;
#else // TARGET_X86
    PORTABILITY_ASSERT("UpdateContextFromRegDisp");
#endif // _TARGET_???_

#else // FEATURE_EH_FUNCLETS

    *pContext = *pRegDisp->pCurrentContext;

#endif // FEATURE_EH_FUNCLETS
}


#endif  // __REGDISP_H


