// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once
#include <rhbinder.h>

// TODO: Debugger/DAC support (look for TODO: JIT)

struct REGDISPLAY;

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2

typedef void (*GCEnumCallback)(
    void *              hCallback,      // callback data
    PTR_PTR_VOID        pObject,        // address of object-reference we are reporting
    uint32_t              flags           // is this a pinned and/or interior pointer
);

struct GCEnumContext
{
    GCEnumCallback pCallback;
};

// All values but GCRK_Unknown must correspond to MethodReturnKind enumeration in gcinfo.h
enum GCRefKind : unsigned char
{
    GCRK_Scalar         = 0x00,
    GCRK_Object         = 0x01,
    GCRK_Byref          = 0x02,
#ifdef TARGET_64BIT
    // Composite return kinds for value types returned in two registers (encoded with two bits per register)
    GCRK_Scalar_Obj     = (GCRK_Object << 2) | GCRK_Scalar,
    GCRK_Obj_Obj        = (GCRK_Object << 2) | GCRK_Object,
    GCRK_Byref_Obj      = (GCRK_Object << 2) | GCRK_Byref,
    GCRK_Scalar_Byref   = (GCRK_Byref  << 2) | GCRK_Scalar,
    GCRK_Obj_Byref      = (GCRK_Byref  << 2) | GCRK_Object,
    GCRK_Byref_Byref    = (GCRK_Byref  << 2) | GCRK_Byref,

    GCRK_LastValid      = GCRK_Byref_Byref,
#else // TARGET_ARM64
    GCRK_LastValid      = GCRK_Byref,
#endif // TARGET_ARM64
    GCRK_Unknown        = 0xFF,
};

#ifdef TARGET_ARM64
// Verify that we can use bitwise shifts to convert from GCRefKind to PInvokeTransitionFrameFlags and back
C_ASSERT(PTFF_X0_IS_GCREF == ((uint64_t)GCRK_Object << 32));
C_ASSERT(PTFF_X0_IS_BYREF == ((uint64_t)GCRK_Byref << 32));
C_ASSERT(PTFF_X1_IS_GCREF == ((uint64_t)GCRK_Scalar_Obj << 32));
C_ASSERT(PTFF_X1_IS_BYREF == ((uint64_t)GCRK_Scalar_Byref << 32));

inline uint64_t ReturnKindToTransitionFrameFlags(GCRefKind returnKind)
{
    // just need to report gc ref bits here.
    // appropriate PTFF_SAVE_ bits will be added by the frame building routine.
    return ((uint64_t)returnKind << 32);
}

inline GCRefKind TransitionFrameFlagsToReturnKind(uint64_t transFrameFlags)
{
    GCRefKind returnKind = (GCRefKind)((transFrameFlags & (PTFF_X0_IS_GCREF | PTFF_X0_IS_BYREF | PTFF_X1_IS_GCREF | PTFF_X1_IS_BYREF)) >> 32);
    ASSERT((returnKind == GCRK_Scalar) || ((transFrameFlags & PTFF_SAVE_X0) && (transFrameFlags & PTFF_SAVE_X1)));
    return returnKind;
}

#elif defined(TARGET_AMD64)

// Verify that we can use bitwise shifts to convert from GCRefKind to PInvokeTransitionFrameFlags and back
C_ASSERT(PTFF_RAX_IS_GCREF == ((uint64_t)GCRK_Object << 16));
C_ASSERT(PTFF_RAX_IS_BYREF == ((uint64_t)GCRK_Byref << 16));
C_ASSERT(PTFF_RDX_IS_GCREF == ((uint64_t)GCRK_Scalar_Obj << 16));
C_ASSERT(PTFF_RDX_IS_BYREF == ((uint64_t)GCRK_Scalar_Byref << 16));

inline uint64_t ReturnKindToTransitionFrameFlags(GCRefKind returnKind)
{
    // just need to report gc ref bits here.
    // appropriate PTFF_SAVE_ bits will be added by the frame building routine.
    return ((uint64_t)returnKind << 16);
}

inline GCRefKind TransitionFrameFlagsToReturnKind(uint64_t transFrameFlags)
{
    GCRefKind returnKind = (GCRefKind)((transFrameFlags & (PTFF_RAX_IS_GCREF | PTFF_RAX_IS_BYREF | PTFF_RDX_IS_GCREF | PTFF_RDX_IS_BYREF)) >> 16);
#if defined(TARGET_UNIX)
    ASSERT((returnKind == GCRK_Scalar) || ((transFrameFlags & PTFF_SAVE_RAX) && (transFrameFlags & PTFF_SAVE_RDX)));
#else
    ASSERT((returnKind == GCRK_Scalar) || (transFrameFlags & PTFF_SAVE_RAX));
#endif
    return returnKind;
}

#elif defined(TARGET_X86)

// Verify that we can use bitwise shifts to convert from GCRefKind to PInvokeTransitionFrameFlags and back
C_ASSERT(PTFF_RAX_IS_GCREF == ((uint64_t)GCRK_Object << 16));
C_ASSERT(PTFF_RAX_IS_BYREF == ((uint64_t)GCRK_Byref << 16));

inline uintptr_t ReturnKindToTransitionFrameFlags(GCRefKind returnKind)
{
    // just need to report gc ref bits here.
    // appropriate PTFF_SAVE_ bits will be added by the frame building routine.
    return ((uintptr_t)returnKind << 16);
}

inline GCRefKind TransitionFrameFlagsToReturnKind(uintptr_t transFrameFlags)
{
    GCRefKind returnKind = (GCRefKind)((transFrameFlags & (PTFF_RAX_IS_GCREF | PTFF_RAX_IS_BYREF)) >> 16);
    ASSERT((returnKind == GCRK_Scalar) || (transFrameFlags & PTFF_SAVE_RAX));
    return returnKind;
}

#elif defined(TARGET_ARM)

// Verify that we can use bitwise shifts to convert from GCRefKind to PInvokeTransitionFrameFlags and back
C_ASSERT(PTFF_R0_IS_GCREF == ((uint64_t)GCRK_Object << 14));
C_ASSERT(PTFF_R0_IS_BYREF == ((uint64_t)GCRK_Byref << 14));

inline uint64_t ReturnKindToTransitionFrameFlags(GCRefKind returnKind)
{
    // just need to report gc ref bits here.
    // appropriate PTFF_SAVE_ bits will be added by the frame building routine.
    return ((uint64_t)returnKind << 14);
}

inline GCRefKind TransitionFrameFlagsToReturnKind(uint64_t transFrameFlags)
{
    GCRefKind returnKind = (GCRefKind)((transFrameFlags & (PTFF_R0_IS_GCREF | PTFF_R0_IS_BYREF)) >> 14);
    ASSERT((returnKind == GCRK_Scalar) || (transFrameFlags & PTFF_SAVE_R0));
    return returnKind;
}

#endif

// Extract individual GCRefKind components from a composite return kind
inline GCRefKind ExtractReg0ReturnKind(GCRefKind returnKind)
{
    ASSERT(returnKind <= GCRK_LastValid);
    return (GCRefKind)(returnKind & (GCRK_Object | GCRK_Byref));
}

inline GCRefKind ExtractReg1ReturnKind(GCRefKind returnKind)
{
    ASSERT(returnKind <= GCRK_LastValid);
    return (GCRefKind)(returnKind >> 2);
}

//
// MethodInfo is placeholder type used to allocate space for MethodInfo. Maximum size
// of the actual method should be less or equal to the placeholder size.
// It avoids memory allocation during stackwalk.
//
class MethodInfo
{
    TADDR dummyPtrs[5];
    int32_t dummyInts[8];
};

class EHEnumState
{
    TADDR dummyPtrs[2];
    int32_t dummyInts[2];
};

enum EHClauseKind
{
    EH_CLAUSE_TYPED = 0,
    EH_CLAUSE_FAULT = 1,
    EH_CLAUSE_FILTER = 2,
    EH_CLAUSE_UNUSED = 3,
};

struct EHClause
{
    EHClauseKind m_clauseKind;
    uint32_t m_tryStartOffset;
    uint32_t m_tryEndOffset;
    uint8_t* m_filterAddress;
    uint8_t* m_handlerAddress;
    void* m_pTargetType;
};

// Note: make sure you change the def in System\Runtime\InternalCalls.cs if you change this!
enum class ClasslibFunctionId
{
    GetRuntimeException = 0,
    FailFast = 1,
    UnhandledExceptionHandler = 2,
    AppendExceptionStackFrame = 3,
    // unused = 4,
    GetSystemArrayEEType = 5,
    OnFirstChanceException = 6,
    OnUnhandledException = 7,
    IDynamicCastableIsInterfaceImplemented = 8,
    IDynamicCastableGetInterfaceImplementation = 9,
    ObjectiveCMarshalTryGetTaggedMemory = 10,
    ObjectiveCMarshalGetIsTrackedReferenceCallback = 11,
    ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback = 12,
    ObjectiveCMarshalGetUnhandledExceptionPropagationHandler = 13,
};

enum class AssociatedDataFlags : unsigned char
{
    None = 0,
    HasUnboxingStubTarget = 1,
};

enum UnwindStackFrameFlags
{
    USFF_None = 0,
    // If this is a reverse P/Invoke frame, do not continue the unwind
    // after extracting the saved transition frame.
    USFF_StopUnwindOnTransitionFrame = 1,
    // Registers not containing GC roots can be omitted.
    USFF_GcUnwind = 2,
};

class ICodeManager
{
public:
    virtual bool IsSafePoint(PTR_VOID pvAddress) PURE_VIRTUAL

    virtual bool FindMethodInfo(PTR_VOID        ControlPC,
                                MethodInfo *    pMethodInfoOut) PURE_VIRTUAL

    virtual bool IsFunclet(MethodInfo * pMethodInfo) PURE_VIRTUAL

    virtual PTR_VOID GetFramePointer(MethodInfo *   pMethodInfo,
                                     REGDISPLAY *   pRegisterSet) PURE_VIRTUAL

    virtual void EnumGcRefs(MethodInfo *    pMethodInfo,
                            PTR_VOID        safePointAddress,
                            REGDISPLAY *    pRegisterSet,
                            GCEnumContext * hCallback,
                            bool            isActiveStackFrame) PURE_VIRTUAL

    virtual bool UnwindStackFrame(MethodInfo *    pMethodInfo,
                                  uint32_t        flags,
                                  REGDISPLAY *    pRegisterSet,                     // in/out
                                  PInvokeTransitionFrame**      ppPreviousTransitionFrame) PURE_VIRTUAL   // out

    virtual uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo *   pMethodInfo,
                                                                REGDISPLAY *   pRegisterSet) PURE_VIRTUAL

    virtual bool IsUnwindable(PTR_VOID pvAddress) PURE_VIRTUAL

    virtual bool GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                            REGDISPLAY *    pRegisterSet,           // in
                                            PTR_PTR_VOID *  ppvRetAddrLocation,     // out
                                            GCRefKind *     pRetValueKind) PURE_VIRTUAL     // out

    virtual PTR_VOID RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC) PURE_VIRTUAL

    virtual bool EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumState) PURE_VIRTUAL

    virtual bool EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClause) PURE_VIRTUAL

    virtual PTR_VOID GetMethodStartAddress(MethodInfo * pMethodInfo) PURE_VIRTUAL

    virtual PTR_VOID GetOsModuleHandle() PURE_VIRTUAL

    virtual void * GetClasslibFunction(ClasslibFunctionId functionId) PURE_VIRTUAL

    // Returns any custom data attached to the method. Format:
    //      AssociatedDataFlags        // 1 byte. Flags describing the data stored
    //      Data (stream of bytes)     // Variable size (depending on flags). Custom data associated with method
    virtual PTR_VOID GetAssociatedData(PTR_VOID ControlPC) PURE_VIRTUAL
};
