// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef WIN64EXCEPTIONS
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
#endif // WIN64EXCEPTIONS

#ifdef DEBUG_REGDISPLAY
    Thread *_pThread;
#endif // DEBUG_REGDISPLAY

    TADDR SP;
    TADDR ControlPC;
};

inline PCODE GetControlPC(REGDISPLAY_BASE *pRD) {
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

#if defined(_TARGET_X86_)

struct REGDISPLAY : public REGDISPLAY_BASE {

#ifndef WIN64EXCEPTIONS
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
#endif // !WIN64EXCEPTIONS

#ifndef WIN64EXCEPTIONS

#define REG_METHODS(reg) \
    inline PDWORD Get##reg##Location(void) { return p##reg;  } \
    inline void   Set##reg##Location(PDWORD p##reg) { this->p##reg = p##reg; }

#else // !WIN64EXCEPTIONS

#define REG_METHODS(reg) \
    inline PDWORD Get##reg##Location(void) { return pCurrentContextPointers->reg; } \
    inline void   Set##reg##Location(PDWORD p##reg) { pCurrentContextPointers->reg = p##reg; }

#endif // WIN64EXCEPTIONS

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

    return (TADDR)*display->GetEbpLocation();
}

inline LPVOID GetRegdisplayFPAddress(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    
    return (LPVOID)display->GetEbpLocation();
}


// This function tells us if the given stack pointer is in one of the frames of the functions called by the given frame
inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer) {
    LIMITED_METHOD_CONTRACT;

#ifdef WIN64EXCEPTIONS
    return stackPointer < ((LPVOID)(display->SP));
#else
    return (TADDR)stackPointer < display->PCTAddr;
#endif
}
inline TADDR GetRegdisplayStackMark(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef WIN64EXCEPTIONS
    _ASSERTE(GetRegdisplaySP(display) == GetSP(display->pCurrentContext));
    return GetRegdisplaySP(display);
#else
    return display->PCTAddr;
#endif
}

#elif defined(_WIN64)

#if defined(_TARGET_ARM64_)
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
#endif //_TARGET_ARM64_
struct REGDISPLAY : public REGDISPLAY_BASE {
#ifdef _TARGET_ARM64_
    Arm64VolatileContextPointer     volatileCurrContextPointers;
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
#if defined(_TARGET_AMD64_)
    // On AMD64, the MemoryStackFp value is the current sp (i.e. the sp value when calling another method).
    _ASSERTE(GetRegdisplaySP(display) == GetSP(display->pCurrentContext));
    return GetRegdisplaySP(display);

#elif defined(_TARGET_ARM64_)

    _ASSERTE(display->IsCallerContextValid);
    return GetSP(display->pCallerContext);

#else  // _TARGET_AMD64_
    PORTABILITY_ASSERT("GetRegdisplayStackMark NYI for this platform (Regdisp.h)");
    return NULL;
#endif // _TARGET_AMD64_
}

#elif defined(_TARGET_ARM_)

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

#if defined(_WIN64) || defined(_TARGET_ARM_) || (defined(_TARGET_X86_) && defined(WIN64EXCEPTIONS))
// This needs to be implemented for platforms that have funclets.
inline LPVOID GetRegdisplayReturnValue(REGDISPLAY *display)
{
    LIMITED_METHOD_CONTRACT;

#if defined(_TARGET_AMD64_)
    return (LPVOID)display->pCurrentContext->Rax;
#elif defined(_TARGET_ARM64_)
    return (LPVOID)display->pCurrentContext->X0;
#elif defined(_TARGET_ARM_)
    return (LPVOID)display->pCurrentContext->R0;
#elif defined(_TARGET_X86_)
    return (LPVOID)display->pCurrentContext->Eax;
#else
    PORTABILITY_ASSERT("GetRegdisplayReturnValue NYI for this platform (Regdisp.h)");
    return NULL;
#endif
}

inline void SyncRegDisplayToCurrentContext(REGDISPLAY* pRD)
{
    LIMITED_METHOD_CONTRACT;

#if defined(_WIN64)
    pRD->SP         = (INT_PTR)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = INT_PTR(GetIP(pRD->pCurrentContext));
#elif defined(_TARGET_ARM_) // _WIN64
    pRD->SP         = (DWORD)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = (DWORD)GetIP(pRD->pCurrentContext);
#elif defined(_TARGET_X86_) // _TARGET_ARM_
    pRD->SP         = (DWORD)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = (DWORD)GetIP(pRD->pCurrentContext);
#else // _TARGET_X86_
    PORTABILITY_ASSERT("SyncRegDisplayToCurrentContext");
#endif // _TARGET_ARM_ || _TARGET_X86_

#ifdef DEBUG_REGDISPLAY
    CheckRegDisplaySP(pRD);
#endif // DEBUG_REGDISPLAY
}
#endif // _WIN64 || _TARGET_ARM_ || (_TARGET_X86_ && WIN64EXCEPTIONS)

typedef REGDISPLAY *PREGDISPLAY;

#ifdef WIN64EXCEPTIONS
inline void FillContextPointers(PT_KNONVOLATILE_CONTEXT_POINTERS pCtxPtrs, PT_CONTEXT pCtx)
{
#ifdef _TARGET_AMD64_
    for (int i = 0; i < 16; i++)
    {
        *(&pCtxPtrs->Rax + i) = (&pCtx->Rax + i);
    }
#elif defined(_TARGET_ARM64_) // _TARGET_AMD64_
    for (int i = 0; i < 12; i++)
    {
        *(&pCtxPtrs->X19 + i) = (&pCtx->X19 + i);
    }
#elif defined(_TARGET_ARM_) // _TARGET_ARM64_
    // Copy over the nonvolatile integer registers (R4-R11)
    for (int i = 0; i < 8; i++)
    {
        *(&pCtxPtrs->R4 + i) = (&pCtx->R4 + i);
    }
#elif defined(_TARGET_X86_) // _TARGET_ARM_
    for (int i = 0; i < 7; i++)
    {
        *(&pCtxPtrs->Edi + i) = (&pCtx->Edi + i);
    }
#else // _TARGET_X86_
    PORTABILITY_ASSERT("FillContextPointers");
#endif // _TARGET_???_ (ELSE)
}
#endif // WIN64EXCEPTIONS

inline void FillRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx, PT_CONTEXT pCallerCtx = NULL)
{
    WRAPPER_NO_CONTRACT;

    SUPPORTS_DAC;

#ifndef WIN64EXCEPTIONS
#ifdef _TARGET_X86_
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
#else // _TARGET_X86_
    PORTABILITY_ASSERT("FillRegDisplay");
#endif // _TARGET_???_ (ELSE)

#else // !WIN64EXCEPTIONS
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

#if defined(_TARGET_ARM_)
    pRD->ctxPtrsOne.Lr = &pctx->Lr;
    pRD->pPC = &pRD->pCurrentContext->Pc;
#endif // _TARGET_ARM_

#ifdef DEBUG_REGDISPLAY
    pRD->_pThread = NULL;
#endif // DEBUG_REGDISPLAY

    // This will setup the PC and SP
    SyncRegDisplayToCurrentContext(pRD);
#endif // !WIN64EXCEPTIONS
}

// Initialize a new REGDISPLAY/CONTEXT pair from an existing valid REGDISPLAY.
inline void CopyRegDisplay(const PREGDISPLAY pInRD, PREGDISPLAY pOutRD, T_CONTEXT *pOutCtx)
{
    WRAPPER_NO_CONTRACT;

    // The general strategy is to extract the register state from the input REGDISPLAY 
    // into the new CONTEXT then simply call FillRegDisplay.

    T_CONTEXT* pOutCallerCtx = NULL;

#ifndef WIN64EXCEPTIONS

#if defined(_TARGET_X86_)
    if (pInRD->pEdi != NULL) {pOutCtx->Edi = *pInRD->pEdi;} else {pInRD->pEdi = NULL;}
    if (pInRD->pEsi != NULL) {pOutCtx->Esi = *pInRD->pEsi;} else {pInRD->pEsi = NULL;}
    if (pInRD->pEbx != NULL) {pOutCtx->Ebx = *pInRD->pEbx;} else {pInRD->pEbx = NULL;}
    if (pInRD->pEbp != NULL) {pOutCtx->Ebp = *pInRD->pEbp;} else {pInRD->pEbp = NULL;}
    if (pInRD->pEax != NULL) {pOutCtx->Eax = *pInRD->pEax;} else {pInRD->pEax = NULL;}
    if (pInRD->pEcx != NULL) {pOutCtx->Ecx = *pInRD->pEcx;} else {pInRD->pEcx = NULL;}
    if (pInRD->pEdx != NULL) {pOutCtx->Edx = *pInRD->pEdx;} else {pInRD->pEdx = NULL;}
    pOutCtx->Esp = pInRD->SP;
    pOutCtx->Eip = pInRD->ControlPC;
#else // _TARGET_X86_
    PORTABILITY_ASSERT("CopyRegDisplay");
#endif // _TARGET_???_

#else // WIN64EXCEPTIONS

    *pOutCtx = *(pInRD->pCurrentContext);
    if (pInRD->IsCallerContextValid)
    {
        pOutCallerCtx = pInRD->pCallerContext;
    }

#endif // WIN64EXCEPTIONS

    if (pOutRD)
        FillRegDisplay(pOutRD, pOutCtx, pOutCallerCtx);
}

// Get address of a register in a CONTEXT given the reg number. For X86, 
// the reg number is the R/M number from ModR/M byte or base in SIB byte
inline size_t * getRegAddr (unsigned regNum, PTR_CONTEXT regs)
{
#ifdef _TARGET_X86_
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
#elif defined(_TARGET_AMD64_)
    _ASSERTE(regNum < 16);
    return &regs->Rax + regNum;
#elif defined(_TARGET_ARM_)
        _ASSERTE(regNum < 16);
        return (size_t *)&regs->R0 + regNum;
#elif defined(_TARGET_ARM64_)
    _ASSERTE(regNum < 31);
    return (size_t *)&regs->X0 + regNum;
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

#ifndef WIN64EXCEPTIONS

#if defined(_TARGET_X86_)
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
#else // _TARGET_X86_
    PORTABILITY_ASSERT("UpdateContextFromRegDisp");
#endif // _TARGET_???_

#else // WIN64EXCEPTIONS

    *pContext = *pRegDisp->pCurrentContext;

#endif // WIN64EXCEPTIONS
}


#endif  // __REGDISP_H


