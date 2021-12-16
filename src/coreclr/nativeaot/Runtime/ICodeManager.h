// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#define ICODEMANAGER_INCLUDED

// TODO: Debugger/DAC support (look for TODO: JIT)

struct REGDISPLAY;

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2
#define GC_CALL_CHECK_APP_DOMAIN    0x4
#define GC_CALL_STATIC              0x8

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
#ifdef TARGET_ARM64
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
#endif // TARGET_ARM64

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
};

enum class AssociatedDataFlags : unsigned char
{
    None = 0,
    HasUnboxingStubTarget = 1,
};

class ICodeManager
{
public:
    virtual bool FindMethodInfo(PTR_VOID        ControlPC,
                                MethodInfo *    pMethodInfoOut) = 0;

    virtual bool IsFunclet(MethodInfo * pMethodInfo) = 0;

    virtual PTR_VOID GetFramePointer(MethodInfo *   pMethodInfo,
                                     REGDISPLAY *   pRegisterSet) = 0;

    virtual void EnumGcRefs(MethodInfo *    pMethodInfo,
                            PTR_VOID        safePointAddress,
                            REGDISPLAY *    pRegisterSet,
                            GCEnumContext * hCallback) = 0;

    virtual bool UnwindStackFrame(MethodInfo *    pMethodInfo,
                                  REGDISPLAY *    pRegisterSet,                     // in/out
                                  PTR_VOID *      ppPreviousTransitionFrame) = 0;   // out

    virtual uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo *   pMethodInfo,
                                                                REGDISPLAY *   pRegisterSet) = 0;

    virtual bool GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                            REGDISPLAY *    pRegisterSet,           // in
                                            PTR_PTR_VOID *  ppvRetAddrLocation,     // out
                                            GCRefKind *     pRetValueKind) = 0;     // out

    virtual void UnsynchronizedHijackMethodLoops(MethodInfo * pMethodInfo) = 0;

    virtual PTR_VOID RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC) = 0;

    virtual bool EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumState) = 0;

    virtual bool EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClause) = 0;

    virtual PTR_VOID GetMethodStartAddress(MethodInfo * pMethodInfo) = 0;

    virtual PTR_VOID GetOsModuleHandle() = 0;

    virtual void * GetClasslibFunction(ClasslibFunctionId functionId) = 0;

    // Returns any custom data attached to the method. Format:
    //      AssociatedDataFlags        // 1 byte. Flags describing the data stored
    //      Data (stream of bytes)     // Variable size (depending on flags). Custom data associated with method
    virtual PTR_VOID GetAssociatedData(PTR_VOID ControlPC) = 0;
};
