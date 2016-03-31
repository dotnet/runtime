// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __REGDISP_H
#define __REGDISP_H


#ifdef DEBUG_REGDISPLAY
class Thread;
#endif // DEBUG_REGDISPLAY


#if defined(_TARGET_X86_)

struct REGDISPLAY {
    PCONTEXT pContext;    // points to current Context; either
                          // returned by GetContext or provided
                          // at exception time.

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
    DWORD   Esp;                // (Esp) Stack Pointer
    PCODE   ControlPC;
    TADDR   PCTAddr;

};

inline TADDR GetRegdisplaySP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

    return (TADDR)display->Esp;
}

inline void SetRegdisplaySP(REGDISPLAY *display, LPVOID sp ) {
    LIMITED_METHOD_DAC_CONTRACT;

    (display->Esp) = (DWORD)(size_t)sp;
}

inline TADDR GetRegdisplayFP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

    return (TADDR)*(display->pEbp);
}

inline LPVOID GetRegdisplayFPAddress(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    
    return (LPVOID)display->pEbp;
}

inline PCODE GetControlPC(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

    return display->ControlPC;
}

// This function tells us if the given stack pointer is in one of the frames of the functions called by the given frame
inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer) {
    LIMITED_METHOD_CONTRACT;

    return (TADDR)stackPointer < display->PCTAddr;
}
inline TADDR GetRegdisplayStackMark(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;

    return display->PCTAddr;
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
struct REGDISPLAY {
    PT_CONTEXT pContext;          // This is the context of the active call frame.  This
                                // will be used to resume execution, so do not use trash it!
                                // But DO update any static registers here.

    PT_CONTEXT pCurrentContext;   // [trashed] points to current Context of stackwalk
    PT_CONTEXT pCallerContext;    // [trashed] points to the Context of the caller during stackwalk -- used for GC crawls

    size_t  ControlPC;

    size_t  SP;

    T_KNONVOLATILE_CONTEXT_POINTERS *pCurrentContextPointers;  // [trashed] points to current context pointers of stackwalk
    T_KNONVOLATILE_CONTEXT_POINTERS *pCallerContextPointers;   // [trashed] points to the context pointers of the caller during stackwalk -- used for GC crawls
#ifdef _TARGET_ARM64_
    Arm64VolatileContextPointer     volatileCurrContextPointers;
#endif

    BOOL IsCallerContextValid;  // TRUE if pCallerContext really contains the caller's context
    BOOL IsCallerSPValid;       // Don't add usage of this field.  This is only temporary.

    T_CONTEXT  ctxOne;    // used by stackwalk
    T_CONTEXT  ctxTwo;    // used by stackwalk

    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsOne;  // used by stackwalk
    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsTwo;  // used by stackwalk

#ifdef DEBUG_REGDISPLAY
    Thread *_pThread;
#endif // DEBUG_REGDISPLAY
};

inline TADDR GetRegdisplaySP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (TADDR)display->SP;
}

inline TADDR GetRegdisplayFP(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return NULL; 
}

inline TADDR GetRegdisplayFPAddress(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return NULL; 
}

inline PCODE GetControlPC(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (PCODE)(display->ControlPC);
}

#ifdef DEBUG_REGDISPLAY
void CheckRegDisplaySP (REGDISPLAY *pRD);
#endif // DEBUG_REGDISPLAY

inline void SyncRegDisplayToCurrentContext(REGDISPLAY* pRD)
{
    LIMITED_METHOD_CONTRACT;

    pRD->SP         = (INT_PTR)GetSP(pRD->pCurrentContext);

#ifdef DEBUG_REGDISPLAY
    CheckRegDisplaySP(pRD);
#endif // DEBUG_REGDISPLAY

    pRD->ControlPC  = INT_PTR(GetIP(pRD->pCurrentContext));
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

// This needs to be implemented for platforms that have funclets.
inline LPVOID GetRegdisplayReturnValue(REGDISPLAY *display)
{
    LIMITED_METHOD_CONTRACT;

#if defined(_TARGET_AMD64_)
    return (LPVOID)display->pCurrentContext->Rax;
#elif defined(_TARGET_ARM64_)
    return (LPVOID)display->pCurrentContext->X0;
#else
    PORTABILITY_ASSERT("GetRegdisplayReturnValue NYI for this platform (Regdisp.h)");
    return NULL;
#endif
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

struct REGDISPLAY {
    PT_CONTEXT pContext;          // points to current Context; either
                                // returned by GetContext or provided
                                // at exception time.

    PT_CONTEXT pCurrentContext;   // [trashed] points to current Context of stackwalk
    PT_CONTEXT pCallerContext;    // [trashed] points to the Context of the caller during stackwalk -- used for GC crawls

    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsOne;  // used by stackwalk
    T_KNONVOLATILE_CONTEXT_POINTERS ctxPtrsTwo;  // used by stackwalk

    PT_KNONVOLATILE_CONTEXT_POINTERS pCurrentContextPointers;
    PT_KNONVOLATILE_CONTEXT_POINTERS pCallerContextPointers;
    ArmVolatileContextPointer     volatileCurrContextPointers;

    BOOL IsCallerContextValid;  // TRUE if pCallerContext really contains the caller's context
    BOOL IsCallerSPValid;       // Don't add usage of this field.  This is only temporary.

    DWORD     SP;
    DWORD     ControlPC; 
    DWORD *  pPC;                // processor neutral name

    T_CONTEXT  ctxOne;    // used by stackwalk
    T_CONTEXT  ctxTwo;    // used in ExceptionTracker::InitializeCrawlFrame

    REGDISPLAY()
    {
        // Initialize regdisplay
        memset(this, 0, sizeof(REGDISPLAY));

        // Setup the pointer to ControlPC field
        pPC = &ControlPC;
    }

#ifdef DEBUG_REGDISPLAY
    Thread *_pThread;
#endif // DEBUG_REGDISPLAY

};

#ifdef DEBUG_REGDISPLAY
void CheckRegDisplaySP (REGDISPLAY *pRD);
#endif // DEBUG_REGDISPLAY

inline TADDR GetRegdisplaySP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (TADDR)(size_t)display->SP;
}

inline PCODE GetControlPC(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (PCODE)(display->ControlPC);
}


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

inline void SyncRegDisplayToCurrentContext(REGDISPLAY* pRD)
{
    LIMITED_METHOD_CONTRACT;
    pRD->SP         = (DWORD)GetSP(pRD->pCurrentContext);
    pRD->ControlPC  = (DWORD)GetIP(pRD->pCurrentContext);
}

// This needs to be implemented for platforms that have funclets.
inline LPVOID GetRegdisplayReturnValue(REGDISPLAY *display)
{
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)display->pCurrentContext->R0;
}

#else // none of the above processors

PORTABILITY_WARNING("RegDisplay functions are not implemented on this platform.")

struct REGDISPLAY {
    PCONTEXT pContext;          // points to current Context
    size_t   SP;
    size_t * FramePtr;
    SLOT   * pPC;
};

inline PCODE GetControlPC(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return (PCODE) NULL;
}

inline LPVOID GetRegdisplaySP(REGDISPLAY *display) {
    LIMITED_METHOD_DAC_CONTRACT;
    return (LPVOID)display->SP;
}

inline TADDR GetRegdisplayFP(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return (TADDR)*(display->FramePtr);
}

inline BOOL IsInCalleesFrames(REGDISPLAY *display, LPVOID stackPointer) {
    LIMITED_METHOD_CONTRACT;
    return FALSE;
}
inline LPVOID GetRegdisplayStackMark(REGDISPLAY *display) {
    LIMITED_METHOD_CONTRACT;
    return (LPVOID)display->SP;
}

#endif

typedef REGDISPLAY *PREGDISPLAY;


inline void FillRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx, PT_CONTEXT pCallerCtx = NULL)
{
    WRAPPER_NO_CONTRACT;

    SUPPORTS_DAC;

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
    pRD->Esp  = pctx->Esp;
    pRD->ControlPC = (PCODE)(pctx->Eip);
    pRD->PCTAddr = (UINT_PTR)&(pctx->Eip);
#elif defined(_WIN64)
    pRD->pContext   = pctx;
#ifdef _TARGET_AMD64_
    for (int i = 0; i < 16; i++)
    {
        *(&pRD->ctxPtrsOne.Rax + i) = (&pctx->Rax + i);
    }
#elif defined(_TARGET_ARM64_)
    for (int i = 0; i < 12; i++)
    {
        *(&pRD->ctxPtrsOne.X19 + i) = (&pctx->X19 + i);
    }
#endif // _TARGET_AMD64_

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

#ifdef DEBUG_REGDISPLAY
    pRD->_pThread = NULL;
#endif // DEBUG_REGDISPLAY

    SyncRegDisplayToCurrentContext(pRD);
#elif defined(_TARGET_ARM_)
    pRD->pContext = pctx;

    // Copy over the nonvolatile integer registers (R4-R11)
    for (int i = 0; i < 8; i++)
    {
        *(&pRD->ctxPtrsOne.R4 + i) = (&pctx->R4 + i);
    }

    pRD->ctxPtrsOne.Lr = &pctx->Lr; 

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

    pRD->pPC = &pRD->pCurrentContext->Pc;

#ifdef DEBUG_REGDISPLAY
    pRD->_pThread = NULL;
#endif // DEBUG_REGDISPLAY

    // This will setup the PC and SP
    SyncRegDisplayToCurrentContext(pRD);
#else
    PORTABILITY_ASSERT("@NYI Platform - InitRegDisplay (Threads.cpp)");
#endif
}

// Initialize a new REGDISPLAY/CONTEXT pair from an existing valid REGDISPLAY.
inline void CopyRegDisplay(const PREGDISPLAY pInRD, PREGDISPLAY pOutRD, T_CONTEXT *pOutCtx)
{
    WRAPPER_NO_CONTRACT;

    // The general strategy is to extract the register state from the input REGDISPLAY 
    // into the new CONTEXT then simply call FillRegDisplay.

    T_CONTEXT* pOutCallerCtx = NULL;

#ifdef _TARGET_X86_
    if (pInRD->pEdi != NULL) {pOutCtx->Edi = *pInRD->pEdi;} else {pInRD->pEdi = NULL;}
    if (pInRD->pEsi != NULL) {pOutCtx->Esi = *pInRD->pEsi;} else {pInRD->pEsi = NULL;}
    if (pInRD->pEbx != NULL) {pOutCtx->Ebx = *pInRD->pEbx;} else {pInRD->pEbx = NULL;}
    if (pInRD->pEbp != NULL) {pOutCtx->Ebp = *pInRD->pEbp;} else {pInRD->pEbp = NULL;}
    if (pInRD->pEax != NULL) {pOutCtx->Eax = *pInRD->pEax;} else {pInRD->pEax = NULL;}
    if (pInRD->pEcx != NULL) {pOutCtx->Ecx = *pInRD->pEcx;} else {pInRD->pEcx = NULL;}
    if (pInRD->pEdx != NULL) {pOutCtx->Edx = *pInRD->pEdx;} else {pInRD->pEdx = NULL;}
    pOutCtx->Esp = pInRD->Esp;
    pOutCtx->Eip = pInRD->ControlPC;
#else
    *pOutCtx = *(pInRD->pCurrentContext);
    if (pInRD->IsCallerContextValid)
    {
        pOutCallerCtx = pInRD->pCallerContext;
    }
#endif

    if (pOutRD)
        FillRegDisplay(pOutRD, pOutCtx, pOutCallerCtx);
}

// Get address of a register in a CONTEXT given the reg number. For X86, 
// the reg number is the R/M number from ModR/M byte or base in SIB byte
inline size_t * getRegAddr (unsigned regNum, PTR_CONTEXT regs)
{
#ifdef _TARGET_X86_
    switch (regNum)
    {
    case 0:
        return (size_t *)&regs->Eax;
        break;
    case 1:
        return (size_t *)&regs->Ecx;
        break;
    case 2:
        return (size_t *)&regs->Edx;
        break;
    case 3:
        return (size_t *)&regs->Ebx;
        break;
    case 4:
        return (size_t *)&regs->Esp;
        break;
    case 5:
        return (size_t *)&regs->Ebp;
        break;
    case 6:
        return (size_t *)&regs->Esi;
        break;
    case 7:
        return (size_t *)&regs->Edi;
        break;
    default:
        _ASSERTE (!"unknown regNum");
    }
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

#if defined(_TARGET_X86_)
    pContext->ContextFlags = (CONTEXT_INTEGER | CONTEXT_CONTROL);
    pContext->Edi = *pRegDisp->pEdi;
    pContext->Esi = *pRegDisp->pEsi;
    pContext->Ebx = *pRegDisp->pEbx;
    pContext->Ebp = *pRegDisp->pEbp;
    pContext->Eax = *pRegDisp->pEax;
    pContext->Ecx = *pRegDisp->pEcx;
    pContext->Edx = *pRegDisp->pEdx;
    pContext->Esp = pRegDisp->Esp;
    pContext->Eip = pRegDisp->ControlPC;
#else
    *pContext = *pRegDisp->pCurrentContext;
#endif
}


#endif  // __REGDISP_H


