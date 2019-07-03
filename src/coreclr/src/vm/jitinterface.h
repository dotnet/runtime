// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: JITinterface.H
//

// ===========================================================================


#ifndef JITINTERFACE_H
#define JITINTERFACE_H

#include "corjit.h"
#ifdef FEATURE_PREJIT
#include "corcompile.h"
#endif // FEATURE_PREJIT

#ifndef FEATURE_PAL
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((32*1024)-1)   // when generating JIT code
#else // !FEATURE_PAL
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((GetOsPageSize() / 2) - 1)
#endif // !FEATURE_PAL


enum StompWriteBarrierCompletionAction
{
    SWB_PASS = 0x0,
    SWB_ICACHE_FLUSH = 0x1,
    SWB_EE_RESTART = 0x2
};

enum SignatureKind
{
    SK_NOT_CALLSITE,
    SK_CALLSITE,
    SK_VIRTUAL_CALLSITE,
};

class Stub;
class MethodDesc;
class NativeCodeVersion;
class FieldDesc;
enum RuntimeExceptionKind;
class AwareLock;
class PtrArray;
#if defined(FEATURE_GDBJIT)
class CalledMethod;
#endif

#include "genericdict.h"

inline FieldDesc* GetField(CORINFO_FIELD_HANDLE fieldHandle)
{
    LIMITED_METHOD_CONTRACT;
    return (FieldDesc*) fieldHandle;
}

inline
bool SigInfoFlagsAreValid (CORINFO_SIG_INFO *sig)
{
    LIMITED_METHOD_CONTRACT;
    return !(sig->flags & ~(  CORINFO_SIGFLAG_IS_LOCAL_SIG
                            | CORINFO_SIGFLAG_IL_STUB
                            ));
}


void InitJITHelpers1();
void InitJITHelpers2();

PCODE UnsafeJitFunction(NativeCodeVersion nativeCodeVersion, COR_ILMETHOD_DECODER* header,
                        CORJIT_FLAGS flags, ULONG* sizeOfCode = NULL);

void getMethodInfoHelper(MethodDesc * ftn,
                         CORINFO_METHOD_HANDLE ftnHnd,
                         COR_ILMETHOD_DECODER * header,
                         CORINFO_METHOD_INFO *  methInfo);

void getMethodInfoILMethodHeaderHelper(
    COR_ILMETHOD_DECODER* header,
    CORINFO_METHOD_INFO* methInfo
    );


BOOL LoadDynamicInfoEntry(Module *currentModule,
                          RVA fixupRva,
                          SIZE_T *entry);

//
// The legacy x86 monitor helpers do not need a state argument
//
#if !defined(_TARGET_X86_)

#define FCDECL_MONHELPER(funcname, arg) FCDECL2(void, funcname, arg, BYTE* pbLockTaken)
#define HCIMPL_MONHELPER(funcname, arg) HCIMPL2(void, funcname, arg, BYTE* pbLockTaken)
#define MONHELPER_STATE(x) x
#define MONHELPER_ARG pbLockTaken

#else

#define FCDECL_MONHELPER(funcname, arg) FCDECL1(void, funcname, arg)
#define HCIMPL_MONHELPER(funcname, arg) HCIMPL1(void, funcname, arg)
#define MONHELPER_STATE(x)
#define MONHELPER_ARG NULL

#endif // _TARGET_X86_


//
// JIT HELPER ALIASING FOR PORTABILITY.
//
// The portable helper is used if the platform does not provide optimized implementation.
//

#ifndef JIT_MonEnter
#define JIT_MonEnter JIT_MonEnter_Portable
#endif
EXTERN_C FCDECL1(void, JIT_MonEnter, Object *obj);
EXTERN_C FCDECL1(void, JIT_MonEnter_Portable, Object *obj);

#ifndef JIT_MonEnterWorker
#define JIT_MonEnterWorker JIT_MonEnterWorker_Portable
#endif
EXTERN_C FCDECL_MONHELPER(JIT_MonEnterWorker, Object *obj);
EXTERN_C FCDECL_MONHELPER(JIT_MonEnterWorker_Portable, Object *obj);

#ifndef JIT_MonReliableEnter
#define JIT_MonReliableEnter JIT_MonReliableEnter_Portable
#endif
EXTERN_C FCDECL2(void, JIT_MonReliableEnter, Object* obj, BYTE *tookLock);
EXTERN_C FCDECL2(void, JIT_MonReliableEnter_Portable, Object* obj, BYTE *tookLock);

#ifndef JIT_MonTryEnter
#define JIT_MonTryEnter JIT_MonTryEnter_Portable
#endif
EXTERN_C FCDECL3(void, JIT_MonTryEnter, Object *obj, INT32 timeout, BYTE* pbLockTaken);
EXTERN_C FCDECL3(void, JIT_MonTryEnter_Portable, Object *obj, INT32 timeout, BYTE* pbLockTaken);

#ifndef JIT_MonExit
#define JIT_MonExit JIT_MonExit_Portable
#endif
EXTERN_C FCDECL1(void, JIT_MonExit, Object *obj);
EXTERN_C FCDECL1(void, JIT_MonExit_Portable, Object *obj);

#ifndef JIT_MonExitWorker
#define JIT_MonExitWorker JIT_MonExitWorker_Portable
#endif
EXTERN_C FCDECL_MONHELPER(JIT_MonExitWorker, Object *obj);
EXTERN_C FCDECL_MONHELPER(JIT_MonExitWorker_Portable, Object *obj);

#ifndef JIT_MonEnterStatic
#define JIT_MonEnterStatic JIT_MonEnterStatic_Portable  
#endif
EXTERN_C FCDECL_MONHELPER(JIT_MonEnterStatic, AwareLock *lock);
EXTERN_C FCDECL_MONHELPER(JIT_MonEnterStatic_Portable, AwareLock *lock);

#ifndef JIT_MonExitStatic
#define JIT_MonExitStatic JIT_MonExitStatic_Portable
#endif
EXTERN_C FCDECL_MONHELPER(JIT_MonExitStatic, AwareLock *lock);
EXTERN_C FCDECL_MONHELPER(JIT_MonExitStatic_Portable, AwareLock *lock);


#ifndef JIT_GetSharedGCStaticBase
#define JIT_GetSharedGCStaticBase JIT_GetSharedGCStaticBase_Portable
#endif
EXTERN_C FCDECL2(void*, JIT_GetSharedGCStaticBase, DomainLocalModule *pLocalModule, DWORD dwModuleClassID);
EXTERN_C FCDECL2(void*, JIT_GetSharedGCStaticBase_Portable, DomainLocalModule *pLocalModule, DWORD dwModuleClassID);

#ifndef JIT_GetSharedNonGCStaticBase
#define JIT_GetSharedNonGCStaticBase JIT_GetSharedNonGCStaticBase_Portable
#endif
EXTERN_C FCDECL2(void*, JIT_GetSharedNonGCStaticBase, DomainLocalModule *pLocalModule, DWORD dwModuleClassID);
EXTERN_C FCDECL2(void*, JIT_GetSharedNonGCStaticBase_Portable, DomainLocalModule *pLocalModule, DWORD dwModuleClassID);

#ifndef JIT_GetSharedGCStaticBaseNoCtor
#define JIT_GetSharedGCStaticBaseNoCtor JIT_GetSharedGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetSharedGCStaticBaseNoCtor, DomainLocalModule *pLocalModule);
EXTERN_C FCDECL1(void*, JIT_GetSharedGCStaticBaseNoCtor_Portable, DomainLocalModule *pLocalModule);

#ifndef JIT_GetSharedNonGCStaticBaseNoCtor
#define JIT_GetSharedNonGCStaticBaseNoCtor JIT_GetSharedNonGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetSharedNonGCStaticBaseNoCtor, DomainLocalModule *pLocalModule);
EXTERN_C FCDECL1(void*, JIT_GetSharedNonGCStaticBaseNoCtor_Portable, DomainLocalModule *pLocalModule);

#ifndef JIT_ChkCastClass
#define JIT_ChkCastClass JIT_ChkCastClass_Portable
#endif
EXTERN_C FCDECL2(Object*, JIT_ChkCastClass, MethodTable* pMT, Object* pObject);
EXTERN_C FCDECL2(Object*, JIT_ChkCastClass_Portable, MethodTable* pMT, Object* pObject);

#ifndef JIT_ChkCastClassSpecial
#define JIT_ChkCastClassSpecial JIT_ChkCastClassSpecial_Portable
#endif
EXTERN_C FCDECL2(Object*, JIT_ChkCastClassSpecial, MethodTable* pMT, Object* pObject);
EXTERN_C FCDECL2(Object*, JIT_ChkCastClassSpecial_Portable, MethodTable* pMT, Object* pObject);

#ifndef JIT_IsInstanceOfClass
#define JIT_IsInstanceOfClass JIT_IsInstanceOfClass_Portable
#endif
EXTERN_C FCDECL2(Object*, JIT_IsInstanceOfClass, MethodTable* pMT, Object* pObject);
EXTERN_C FCDECL2(Object*, JIT_IsInstanceOfClass_Portable, MethodTable* pMT, Object* pObject);

#ifndef JIT_ChkCastInterface
#define JIT_ChkCastInterface JIT_ChkCastInterface_Portable
#endif
EXTERN_C FCDECL2(Object*, JIT_ChkCastInterface, MethodTable* pMT, Object* pObject);
EXTERN_C FCDECL2(Object*, JIT_ChkCastInterface_Portable, MethodTable* pMT, Object* pObject);

#ifndef JIT_IsInstanceOfInterface
#define JIT_IsInstanceOfInterface JIT_IsInstanceOfInterface_Portable
#endif
EXTERN_C FCDECL2(Object*, JIT_IsInstanceOfInterface, MethodTable* pMT, Object* pObject);
EXTERN_C FCDECL2(Object*, JIT_IsInstanceOfInterface_Portable, MethodTable* pMT, Object* pObject);

extern FCDECL1(Object*, JIT_NewS_MP_FastPortable, CORINFO_CLASS_HANDLE typeHnd_);
extern FCDECL1(Object*, JIT_New, CORINFO_CLASS_HANDLE typeHnd_);

#ifndef JIT_NewCrossContext
#define JIT_NewCrossContext JIT_NewCrossContext_Portable
#endif
EXTERN_C FCDECL1(Object*, JIT_NewCrossContext, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL1(Object*, JIT_NewCrossContext_Portable, CORINFO_CLASS_HANDLE typeHnd_);

extern FCDECL1(StringObject*, AllocateString_MP_FastPortable, DWORD stringLength);
extern FCDECL1(StringObject*, UnframedAllocateString, DWORD stringLength);
extern FCDECL1(StringObject*, FramedAllocateString, DWORD stringLength);

#ifdef FEATURE_UTF8STRING
extern FCDECL1(Utf8StringObject*, AllocateUtf8String_MP_FastPortable, DWORD stringLength);
extern FCDECL1(Utf8StringObject*, FramedAllocateUtf8String, DWORD stringLength);
#endif // FEATURE_UTF8STRING

extern FCDECL2(Object*, JIT_NewArr1VC_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
extern FCDECL2(Object*, JIT_NewArr1OBJ_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
extern FCDECL2(Object*, JIT_NewArr1_R2R, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);
extern FCDECL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);

#ifndef JIT_Stelem_Ref
#define JIT_Stelem_Ref JIT_Stelem_Ref_Portable
#endif
EXTERN_C FCDECL3(void, JIT_Stelem_Ref, PtrArray* array, unsigned idx, Object* val);
EXTERN_C FCDECL3(void, JIT_Stelem_Ref_Portable, PtrArray* array, unsigned idx, Object* val);

EXTERN_C FCDECL_MONHELPER(JITutil_MonEnterWorker, Object* obj);
EXTERN_C FCDECL2(void, JITutil_MonReliableEnter, Object* obj, BYTE* pbLockTaken);
EXTERN_C FCDECL3(void, JITutil_MonTryEnter, Object* obj, INT32 timeOut, BYTE* pbLockTaken);
EXTERN_C FCDECL_MONHELPER(JITutil_MonExitWorker, Object* obj);
EXTERN_C FCDECL_MONHELPER(JITutil_MonSignal, AwareLock* lock);
EXTERN_C FCDECL_MONHELPER(JITutil_MonContention, AwareLock* awarelock);
EXTERN_C FCDECL2(void, JITutil_MonReliableContention, AwareLock* awarelock, BYTE* pbLockTaken);

// Slow versions to tail call if the fast version fails
EXTERN_C FCDECL2(void*, JIT_GetSharedNonGCStaticBase_Helper, DomainLocalModule *pLocalModule, DWORD dwClassDomainID);
EXTERN_C FCDECL2(void*, JIT_GetSharedGCStaticBase_Helper, DomainLocalModule *pLocalModule, DWORD dwClassDomainID);

EXTERN_C void DoJITFailFast ();
EXTERN_C FCDECL0(void, JIT_FailFast);
extern FCDECL3(void, JIT_ThrowAccessException, RuntimeExceptionKind, CORINFO_METHOD_HANDLE caller, void * callee);

FCDECL1(void*, JIT_SafeReturnableByref, void* byref);

#if !defined(FEATURE_USE_ASM_GC_WRITE_BARRIERS) && defined(FEATURE_COUNT_GC_WRITE_BARRIERS)
// Extra argument for the classification of the checked barriers.
extern "C" FCDECL3(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref, CheckedWriteBarrierKinds kind);
#else
// Regular checked write barrier.
extern "C" FCDECL2(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref);
#endif

extern "C" FCDECL2(VOID, JIT_WriteBarrier, Object **dst, Object *ref);

extern "C" FCDECL2(VOID, JIT_WriteBarrierEnsureNonHeapTarget, Object **dst, Object *ref);

extern "C" FCDECL2(Object*, JIT_ChkCastAny, CORINFO_CLASS_HANDLE type, Object *pObject);   // JITInterfaceX86.cpp, etc.
extern "C" FCDECL2(Object*, JIT_IsInstanceOfAny, CORINFO_CLASS_HANDLE type, Object *pObject);

extern "C" FCDECL2(Object*, JITutil_ChkCastInterface, MethodTable *pInterfaceMT, Object *obj);
extern "C" FCDECL2(Object*, JITutil_IsInstanceOfInterface, MethodTable *pInterfaceMT, Object *obj);
extern "C" FCDECL2(Object*, JITutil_ChkCastAny, CORINFO_CLASS_HANDLE type, Object *obj);
extern "C" FCDECL2(Object*, JITutil_IsInstanceOfAny, CORINFO_CLASS_HANDLE type, Object *obj);

extern "C" FCDECL1(void, JIT_InternalThrow, unsigned exceptNum);
extern "C" FCDECL1(void*, JIT_InternalThrowFromHelper, unsigned exceptNum);

#ifdef _TARGET_AMD64_


class WriteBarrierManager
{
public:
    enum WriteBarrierType
    {
        WRITE_BARRIER_UNINITIALIZED,
        WRITE_BARRIER_PREGROW64,
        WRITE_BARRIER_POSTGROW64,
#ifdef FEATURE_SVR_GC
        WRITE_BARRIER_SVR64,
#endif // FEATURE_SVR_GC
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        WRITE_BARRIER_WRITE_WATCH_PREGROW64,
        WRITE_BARRIER_WRITE_WATCH_POSTGROW64,
#ifdef FEATURE_SVR_GC
        WRITE_BARRIER_WRITE_WATCH_SVR64,
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        WRITE_BARRIER_BUFFER
    };

    WriteBarrierManager();
    void Initialize();
    
    int UpdateEphemeralBounds(bool isRuntimeSuspended);
    int UpdateWriteWatchAndCardTableLocations(bool isRuntimeSuspended, bool bReqUpperBoundsCheck);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(bool isRuntimeSuspended);
    int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    size_t GetCurrentWriteBarrierSize();

protected:
    size_t GetSpecificWriteBarrierSize(WriteBarrierType writeBarrier);
    PBYTE  CalculatePatchLocation(LPVOID base, LPVOID label, int offset);
    PCODE  GetCurrentWriteBarrierCode();
    int ChangeWriteBarrierTo(WriteBarrierType newWriteBarrier, bool isRuntimeSuspended);
    bool   NeedDifferentWriteBarrier(bool bReqUpperBoundsCheck, WriteBarrierType* pNewWriteBarrierType);

private:    
    void Validate();
    
    WriteBarrierType    m_currentWriteBarrier;

    PBYTE   m_pWriteWatchTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH |
    PBYTE   m_pLowerBoundImmediate;         // PREGROW | POSTGROW |     | WRITE_WATCH |
    PBYTE   m_pCardTableImmediate;          // PREGROW | POSTGROW | SVR | WRITE_WATCH |
    PBYTE   m_pCardBundleTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH |
    PBYTE   m_pUpperBoundImmediate;         //         | POSTGROW |     | WRITE_WATCH |
};

#endif // _TARGET_AMD64_

#ifdef _WIN64
EXTERN_C FCDECL1(Object*, JIT_TrialAllocSFastMP_InlineGetThread, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, JIT_BoxFastMP_InlineGetThread, CORINFO_CLASS_HANDLE type, void* data);
EXTERN_C FCDECL2(Object*, JIT_NewArr1VC_MP_InlineGetThread, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
EXTERN_C FCDECL2(Object*, JIT_NewArr1OBJ_MP_InlineGetThread, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);

#endif // _WIN64

EXTERN_C FCDECL2_VV(INT64, JIT_LMul, INT64 val1, INT64 val2);

EXTERN_C FCDECL1_V(INT64, JIT_Dbl2Lng, double val);
EXTERN_C FCDECL1_V(INT64, JIT_Dbl2IntSSE2, double val);
EXTERN_C FCDECL1_V(INT64, JIT_Dbl2LngP4x87, double val);
EXTERN_C FCDECL1_V(INT64, JIT_Dbl2LngSSE3, double val);
EXTERN_C FCDECL1_V(INT64, JIT_Dbl2LngOvf, double val);

EXTERN_C FCDECL1_V(INT32, JIT_Dbl2IntOvf, double val);

EXTERN_C FCDECL2_VV(float, JIT_FltRem, float dividend, float divisor);
EXTERN_C FCDECL2_VV(double, JIT_DblRem, double dividend, double divisor);

#ifndef BIT64
#ifdef _TARGET_X86_
// JIThelp.asm
EXTERN_C void STDCALL JIT_LLsh();
EXTERN_C void STDCALL JIT_LRsh();
EXTERN_C void STDCALL JIT_LRsz();
#else // _TARGET_X86_
EXTERN_C FCDECL2_VV(UINT64, JIT_LLsh, UINT64 num, int shift);
EXTERN_C FCDECL2_VV(INT64, JIT_LRsh, INT64 num, int shift);
EXTERN_C FCDECL2_VV(UINT64, JIT_LRsz, UINT64 num, int shift);
#endif // !_TARGET_X86_
#endif // !BIT64

#ifdef _TARGET_X86_

extern "C"
{
    void STDCALL JIT_CheckedWriteBarrierEAX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_CheckedWriteBarrierEBX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_CheckedWriteBarrierECX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_CheckedWriteBarrierESI(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_CheckedWriteBarrierEDI(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_CheckedWriteBarrierEBP(); // JIThelp.asm/JIThelp.s

    void STDCALL JIT_DebugWriteBarrierEAX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_DebugWriteBarrierEBX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_DebugWriteBarrierECX(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_DebugWriteBarrierESI(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_DebugWriteBarrierEDI(); // JIThelp.asm/JIThelp.s
    void STDCALL JIT_DebugWriteBarrierEBP(); // JIThelp.asm/JIThelp.s

    void STDCALL JIT_WriteBarrierEAX();        // JIThelp.asm/JIThelp.s
    void STDCALL JIT_WriteBarrierEBX();        // JIThelp.asm/JIThelp.s
    void STDCALL JIT_WriteBarrierECX();        // JIThelp.asm/JIThelp.s
    void STDCALL JIT_WriteBarrierESI();        // JIThelp.asm/JIThelp.s
    void STDCALL JIT_WriteBarrierEDI();        // JIThelp.asm/JIThelp.s
    void STDCALL JIT_WriteBarrierEBP();        // JIThelp.asm/JIThelp.s

    void STDCALL JIT_WriteBarrierGroup();
    void STDCALL JIT_WriteBarrierGroup_End();

    void STDCALL JIT_PatchedWriteBarrierGroup();
    void STDCALL JIT_PatchedWriteBarrierGroup_End();
}

void ValidateWriteBarrierHelpers();

#endif //_TARGET_X86_

extern "C"
{
#ifndef WIN64EXCEPTIONS
    void STDCALL JIT_EndCatch();               // JIThelp.asm/JIThelp.s
#endif // _TARGET_X86_

    void STDCALL JIT_ByRefWriteBarrier();      // JIThelp.asm/JIThelp.s

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

    FCDECL2VA(void, JIT_TailCall, PCODE copyArgs, PCODE target);

#else // _TARGET_AMD64_ || _TARGET_ARM_

    void STDCALL JIT_TailCall();                    // JIThelp.asm

#endif // _TARGET_AMD64_ || _TARGET_ARM_

    void STDCALL JIT_MemSet(void *dest, int c, SIZE_T count);
    void STDCALL JIT_MemCpy(void *dest, const void *src, SIZE_T count);

    void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle);
};



/*********************************************************************/
/*********************************************************************/
class CEEInfo : public ICorJitInfo
{
    friend class CEEDynamicCodeInfo;
    
    const char * __stdcall ICorMethodInfo_Hack_getMethodName(CORINFO_METHOD_HANDLE ftnHnd, const char** scopeName)
    {
        WRAPPER_NO_CONTRACT;
        return getMethodName(ftnHnd, scopeName);
    }

    mdMethodDef __stdcall ICorClassInfo_Hack_getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
    {
        WRAPPER_NO_CONTRACT;
        return getMethodDefFromMethod(hMethod);
    }
    
public:
    // ICorClassInfo stuff
    CorInfoType asCorInfoType (CORINFO_CLASS_HANDLE cls);
    // This normalizes EE type information into the form expected by the JIT.
    //
    // If typeHnd contains exact type information, then *clsRet will contain
    // the normalized CORINFO_CLASS_HANDLE information on return.
    static CorInfoType asCorInfoType (CorElementType cet, 
                                      TypeHandle typeHnd = TypeHandle() /* optional in */,
                                      CORINFO_CLASS_HANDLE *clsRet = NULL /* optional out */ );

    CORINFO_MODULE_HANDLE getClassModule(CORINFO_CLASS_HANDLE clsHnd);
    CORINFO_ASSEMBLY_HANDLE getModuleAssembly(CORINFO_MODULE_HANDLE mod);
    const char* getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem);
    void* LongLifetimeMalloc(size_t sz);
    void LongLifetimeFree(void* obj);
    size_t getClassModuleIdForStatics(CORINFO_CLASS_HANDLE clsHnd, CORINFO_MODULE_HANDLE *pModuleHandle, void **ppIndirection);
    const char* getClassName (CORINFO_CLASS_HANDLE cls);
    const char* getClassNameFromMetadata (CORINFO_CLASS_HANDLE cls, const char** namespaceName);
    CORINFO_CLASS_HANDLE getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index);
    const char* getHelperName(CorInfoHelpFunc ftnNum);
    int appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf,
                                  int* pnBufLen,
                                  CORINFO_CLASS_HANDLE    cls,
                                  BOOL fNamespace,
                                  BOOL fFullInst,
                                  BOOL fAssembly);
    BOOL isValueClass (CORINFO_CLASS_HANDLE cls);
    CorInfoInlineTypeCheck canInlineTypeCheck (CORINFO_CLASS_HANDLE cls, CorInfoInlineTypeCheckSource source);
    BOOL canInlineTypeCheckWithObjectVTable (CORINFO_CLASS_HANDLE cls);

    DWORD getClassAttribs (CORINFO_CLASS_HANDLE cls);

    // Internal version without JIT-EE transition
    DWORD getClassAttribsInternal (CORINFO_CLASS_HANDLE cls);

    BOOL isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls);

    unsigned getClassSize (CORINFO_CLASS_HANDLE cls);
    unsigned getHeapClassSize(CORINFO_CLASS_HANDLE cls);
    BOOL canAllocateOnStack(CORINFO_CLASS_HANDLE cls);
    unsigned getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint);
    static unsigned getClassAlignmentRequirementStatic(TypeHandle clsHnd);

    // Used for HFA's on IA64...and later for type based disambiguation
    CORINFO_FIELD_HANDLE getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num);

    mdMethodDef getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod);
    BOOL checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional);

    unsigned getClassGClayout (CORINFO_CLASS_HANDLE cls, BYTE* gcPtrs); /* really GCType* gcPtrs */
    unsigned getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls);

    // returns the enregister info for a struct based on type of fields, alignment, etc.
    bool getSystemVAmd64PassStructInRegisterDescriptor(
        /*IN*/  CORINFO_CLASS_HANDLE _structHnd,
        /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr);

    // Check Visibility rules.
    // For Protected (family access) members, type of the instance is also
    // considered when checking visibility rules.
    

    CorInfoHelpFunc getNewHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_METHOD_HANDLE callerHandle, bool * pHasSideEffects = NULL);
    static CorInfoHelpFunc getNewHelperStatic(MethodTable * pMT, bool * pHasSideEffects = NULL);

    CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls);
    static CorInfoHelpFunc getNewArrHelperStatic(TypeHandle clsHnd);

    CorInfoHelpFunc getCastingHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, bool fThrowing);
    static CorInfoHelpFunc getCastingHelperStatic(TypeHandle clsHnd, bool fThrowing, bool * pfClassMustBeRestored);

    CorInfoHelpFunc getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd);
    CorInfoHelpFunc getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn);
    CORINFO_CLASS_HANDLE getTypeForBox(CORINFO_CLASS_HANDLE  cls); 
    CorInfoHelpFunc getBoxHelper(CORINFO_CLASS_HANDLE cls);
    CorInfoHelpFunc getUnBoxHelper(CORINFO_CLASS_HANDLE cls);

    bool getReadyToRunHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_LOOKUP_KIND *    pGenericLookupKind,
            CorInfoHelpFunc          id,
            CORINFO_CONST_LOOKUP *   pLookup
            );

    void getReadyToRunDelegateCtorHelper(
            CORINFO_RESOLVED_TOKEN * pTargetMethod,
            CORINFO_CLASS_HANDLE     delegateType,
            CORINFO_LOOKUP *   pLookup
            );

    CorInfoInitClassResult initClass(
            CORINFO_FIELD_HANDLE    field,
            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONTEXT_HANDLE  context,
            BOOL                    speculative = FALSE);

    void classMustBeLoadedBeforeCodeIsRun (CORINFO_CLASS_HANDLE cls);
    void methodMustBeLoadedBeforeCodeIsRun (CORINFO_METHOD_HANDLE meth);
    CORINFO_METHOD_HANDLE mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE methHnd);
    CORINFO_CLASS_HANDLE getBuiltinClass(CorInfoClassId classId);
    void getGSCookie(GSCookie * pCookieVal, GSCookie ** ppCookieVal);

    // "System.Int32" ==> CORINFO_TYPE_INT..
    CorInfoType getTypeForPrimitiveValueClass(
            CORINFO_CLASS_HANDLE        cls
            );

    // "System.Int32" ==> CORINFO_TYPE_INT..
    // "System.UInt32" ==> CORINFO_TYPE_UINT..
    CorInfoType getTypeForPrimitiveNumericClass(
            CORINFO_CLASS_HANDLE        cls
            );

    // TRUE if child is a subtype of parent
    // if parent is an interface, then does child implement / extend parent
    BOOL canCast(
            CORINFO_CLASS_HANDLE        child,
            CORINFO_CLASS_HANDLE        parent
            );

    // TRUE if cls1 and cls2 are considered equivalent types.
    BOOL areTypesEquivalent(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            );

    // See if a cast from fromClass to toClass will succeed, fail, or needs
    // to be resolved at runtime.
    TypeCompareState compareTypesForCast(
            CORINFO_CLASS_HANDLE        fromClass,
            CORINFO_CLASS_HANDLE        toClass
            );

    // See if types represented by cls1 and cls2 compare equal, not
    // equal, or the comparison needs to be resolved at runtime.
    TypeCompareState compareTypesForEquality(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            );

    // returns is the intersection of cls1 and cls2.
    CORINFO_CLASS_HANDLE mergeClasses(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            );

    // Returns true if cls2 is known to be a more specific type
    // than cls1 (a subtype or more restrictive shared type).
    BOOL isMoreSpecificType(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            );

    // Given a class handle, returns the Parent type.
    // For COMObjectType, it returns Class Handle of System.Object.
    // Returns 0 if System.Object is passed in.
    CORINFO_CLASS_HANDLE getParentType (
            CORINFO_CLASS_HANDLE        cls
            );

    // Returns the CorInfoType of the "child type". If the child type is
    // not a primitive type, *clsRet will be set.
    // Given an Array of Type Foo, returns Foo.
    // Given BYREF Foo, returns Foo
    CorInfoType getChildType (
            CORINFO_CLASS_HANDLE       clsHnd,
            CORINFO_CLASS_HANDLE       *clsRet
            );

    // Check constraints on type arguments of this class and parent classes
    BOOL satisfiesClassConstraints(
            CORINFO_CLASS_HANDLE cls
            );

    // Check if this is a single dimensional array type
    BOOL isSDArray(
            CORINFO_CLASS_HANDLE        cls
            );

    // Get the number of dimensions in an array 
    unsigned getArrayRank(
            CORINFO_CLASS_HANDLE        cls
            );

    // Get static field data for an array
    void * getArrayInitializationData(
            CORINFO_FIELD_HANDLE        field,
            DWORD                       size
            );

    // Check Visibility rules.
    CorInfoIsAccessAllowedResult canAccessClass(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_METHOD_HANDLE   callerHandle,
            CORINFO_HELPER_DESC    *pAccessHelper /* If canAccessClass returns something other
                                                     than ALLOWED, then this is filled in. */
            );

    // Returns that compilation flags that are shared between JIT and NGen
    static CORJIT_FLAGS GetBaseCompileFlags(MethodDesc * ftn);

    // Resolve metadata token into runtime method handles.
    void resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken);

    // Attempt to resolve a metadata token into a runtime method handle. Returns true
    // if resolution succeeded and false otherwise.
    bool tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken);

    void getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                       CORINFO_METHOD_HANDLE  callerHandle,
                       CORINFO_ACCESS_FLAGS   flags,
                       CORINFO_FIELD_INFO    *pResult
                      );
    static CorInfoHelpFunc getSharedStaticsHelper(FieldDesc * pField, MethodTable * pFieldMT);

    bool isFieldStatic(CORINFO_FIELD_HANDLE fldHnd);

    // Given a signature token sigTOK, use class/method instantiation in context to instantiate any type variables in the signature and return a new signature
    void findSig(CORINFO_MODULE_HANDLE scopeHnd, unsigned sigTOK, CORINFO_CONTEXT_HANDLE context, CORINFO_SIG_INFO* sig);
    void findCallSiteSig(CORINFO_MODULE_HANDLE scopeHnd, unsigned methTOK, CORINFO_CONTEXT_HANDLE context, CORINFO_SIG_INFO* sig);
    CORINFO_CLASS_HANDLE getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken);

    size_t findNameOfToken (CORINFO_MODULE_HANDLE module, mdToken metaTOK,
                                      __out_ecount (FQNameCapacity) char * szFQName, size_t FQNameCapacity);

    CorInfoCanSkipVerificationResult canSkipVerification(CORINFO_MODULE_HANDLE moduleHnd);

    // Checks if the given metadata token is valid
    BOOL isValidToken (
            CORINFO_MODULE_HANDLE       module,
            mdToken                    metaTOK);

    // Checks if the given metadata token is valid StringRef
    BOOL isValidStringRef (
            CORINFO_MODULE_HANDLE       module,
            mdToken                    metaTOK);

    static size_t findNameOfToken (Module* module, mdToken metaTOK, 
                            __out_ecount (FQNameCapacity) char * szFQName, size_t FQNameCapacity);

    // ICorMethodInfo stuff
    const char* getMethodName (CORINFO_METHOD_HANDLE ftnHnd, const char** scopeName);
    const char* getMethodNameFromMetadata (CORINFO_METHOD_HANDLE ftnHnd, const char** className, const char** namespaceName, const char **enclosingClassName);
    unsigned getMethodHash (CORINFO_METHOD_HANDLE ftnHnd);

    DWORD getMethodAttribs (CORINFO_METHOD_HANDLE ftnHnd);
    // Internal version without JIT-EE transition
    DWORD getMethodAttribsInternal (CORINFO_METHOD_HANDLE ftnHnd);

    void setMethodAttribs (CORINFO_METHOD_HANDLE ftnHnd, CorInfoMethodRuntimeFlags attribs);

    bool getMethodInfo (
            CORINFO_METHOD_HANDLE ftnHnd,
            CORINFO_METHOD_INFO*  methInfo);

    CorInfoInline canInline (
            CORINFO_METHOD_HANDLE  callerHnd,
            CORINFO_METHOD_HANDLE  calleeHnd,
            DWORD*                 pRestrictions);

    void reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                 CORINFO_METHOD_HANDLE inlineeHnd,
                                 CorInfoInline inlineResult,
                                 const char * reason);

    // Used by ngen
    CORINFO_METHOD_HANDLE instantiateMethodAtObject(CORINFO_METHOD_HANDLE method);

    // Loads the constraints on a typical method definition, detecting cycles;
    // used by verifiers.
    void initConstraintsForVerification(
            CORINFO_METHOD_HANDLE   method,
            BOOL *pfHasCircularClassConstraints,
            BOOL *pfHasCircularMethodConstraints
            );

    CorInfoInstantiationVerification isInstantiationOfVerifiedGeneric (
            CORINFO_METHOD_HANDLE  methodHnd);


    bool canTailCall (
            CORINFO_METHOD_HANDLE  callerHnd,
            CORINFO_METHOD_HANDLE  declaredCalleeHnd,
            CORINFO_METHOD_HANDLE  exactCalleeHnd,
            bool fIsTailPrefix);

    void reportTailCallDecision (CORINFO_METHOD_HANDLE callerHnd,
                                 CORINFO_METHOD_HANDLE calleeHnd,
                                 bool fIsTailPrefix,
                                 CorInfoTailCall tailCallResult,
                                 const char * reason);

    CorInfoCanSkipVerificationResult canSkipMethodVerification(
        CORINFO_METHOD_HANDLE ftnHnd);
    
    // Given a method descriptor ftnHnd, extract signature information into sigInfo
    // Obtain (representative) instantiation information from ftnHnd's owner class
    //@GENERICSVER: added explicit owner parameter
    void getMethodSig (
            CORINFO_METHOD_HANDLE ftnHnd,
            CORINFO_SIG_INFO* sigInfo,
            CORINFO_CLASS_HANDLE owner = NULL
            );
    // Internal version without JIT-EE transition
    void getMethodSigInternal (
            CORINFO_METHOD_HANDLE ftnHnd,
            CORINFO_SIG_INFO* sigInfo,
            CORINFO_CLASS_HANDLE owner = NULL,
            SignatureKind signatureKind = SK_NOT_CALLSITE
            );

    void getEHinfo(
            CORINFO_METHOD_HANDLE ftn,
            unsigned      EHnumber,
            CORINFO_EH_CLAUSE* clause);

    CORINFO_CLASS_HANDLE getMethodClass (CORINFO_METHOD_HANDLE methodHnd);
    CORINFO_MODULE_HANDLE getMethodModule (CORINFO_METHOD_HANDLE methodHnd);

    void getMethodVTableOffset (
            CORINFO_METHOD_HANDLE methodHnd,
            unsigned * pOffsetOfIndirection,
            unsigned * pOffsetAfterIndirection,
            bool * isRelative);

    CORINFO_METHOD_HANDLE resolveVirtualMethod(
        CORINFO_METHOD_HANDLE virtualMethod,
        CORINFO_CLASS_HANDLE implementingClass,
        CORINFO_CONTEXT_HANDLE ownerType
        );

    CORINFO_METHOD_HANDLE resolveVirtualMethodHelper(
        CORINFO_METHOD_HANDLE virtualMethod,
        CORINFO_CLASS_HANDLE implementingClass,
        CORINFO_CONTEXT_HANDLE ownerType
        );

    CORINFO_METHOD_HANDLE getUnboxedEntry(
        CORINFO_METHOD_HANDLE ftn,
        bool* requiresInstMethodTableArg
    );

    CORINFO_CLASS_HANDLE getDefaultEqualityComparerClass(
        CORINFO_CLASS_HANDLE elemType
        );

    CORINFO_CLASS_HANDLE getDefaultEqualityComparerClassHelper(
        CORINFO_CLASS_HANDLE elemType
        );

    void expandRawHandleIntrinsic(
        CORINFO_RESOLVED_TOKEN *        pResolvedToken,
        CORINFO_GENERICHANDLE_RESULT *  pResult);

    CorInfoIntrinsics getIntrinsicID(CORINFO_METHOD_HANDLE method,
                                     bool * pMustExpand = NULL);

    bool isInSIMDModule(CORINFO_CLASS_HANDLE classHnd);

    CorInfoUnmanagedCallConv getUnmanagedCallConv(CORINFO_METHOD_HANDLE method);
    BOOL pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig);

    // Generate a cookie based on the signature that would needs to be passed
    //  to the above generic stub
    LPVOID GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig, void ** ppIndirection);
    bool canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig);
    
    // Check Visibility rules.

    // should we enforce the new (for whidbey) restrictions on calling virtual methods?
    BOOL shouldEnforceCallvirtRestriction(
            CORINFO_MODULE_HANDLE   scope);

    // Check constraints on method type arguments (only).
    // The parent class should be checked separately using satisfiesClassConstraints(parent).
    BOOL satisfiesMethodConstraints(
            CORINFO_CLASS_HANDLE        parent, // the exact parent of the method
            CORINFO_METHOD_HANDLE       method
            );

    // Given a Delegate type and a method, check if the method signature
    // is Compatible with the Invoke method of the delegate.
    //@GENERICSVER: new (suitable for generics)
    BOOL isCompatibleDelegate(
            CORINFO_CLASS_HANDLE        objCls,
            CORINFO_CLASS_HANDLE        methodParentCls,
            CORINFO_METHOD_HANDLE       method,
            CORINFO_CLASS_HANDLE        delegateCls,
            BOOL*                       pfIsOpenDelegate);

    // ICorFieldInfo stuff
    const char* getFieldName (CORINFO_FIELD_HANDLE field,
                              const char** scopeName);

    CORINFO_CLASS_HANDLE getFieldClass (CORINFO_FIELD_HANDLE field);

    //@GENERICSVER: added owner parameter
    CorInfoType getFieldType (CORINFO_FIELD_HANDLE field, CORINFO_CLASS_HANDLE* structType = NULL,CORINFO_CLASS_HANDLE owner = NULL);
    // Internal version without JIT-EE transition
    CorInfoType getFieldTypeInternal (CORINFO_FIELD_HANDLE field, CORINFO_CLASS_HANDLE* structType = NULL,CORINFO_CLASS_HANDLE owner = NULL);

    unsigned getFieldOffset (CORINFO_FIELD_HANDLE field);

    bool isWriteBarrierHelperRequired(CORINFO_FIELD_HANDLE field);

    void* getFieldAddress(CORINFO_FIELD_HANDLE field, void **ppIndirection);

    CORINFO_CLASS_HANDLE getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative);

    // ICorDebugInfo stuff
    void * allocateArray(ULONG cBytes);
    void freeArray(void *array);
    void getBoundaries(CORINFO_METHOD_HANDLE ftn,
                       unsigned int *cILOffsets, DWORD **pILOffsets,
                       ICorDebugInfo::BoundaryTypes *implictBoundaries);
    void setBoundaries(CORINFO_METHOD_HANDLE ftn,
                       ULONG32 cMap, ICorDebugInfo::OffsetMapping *pMap);
    void getVars(CORINFO_METHOD_HANDLE ftn, ULONG32 *cVars,
                 ICorDebugInfo::ILVarInfo **vars, bool *extendOthers);
    void setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars,
                 ICorDebugInfo::NativeVarInfo *vars);

    // ICorArgInfo stuff

    CorInfoTypeWithMod getArgType (
            CORINFO_SIG_INFO*       sig,
            CORINFO_ARG_LIST_HANDLE    args,
            CORINFO_CLASS_HANDLE       *vcTypeRet
            );

    CORINFO_CLASS_HANDLE getArgClass (
            CORINFO_SIG_INFO*       sig,
            CORINFO_ARG_LIST_HANDLE    args
            );

    CorInfoType getHFAType (
            CORINFO_CLASS_HANDLE hClass
            );

    CORINFO_ARG_LIST_HANDLE getArgNext (
            CORINFO_ARG_LIST_HANDLE args
            );

    // ICorErrorInfo stuff

    HRESULT GetErrorHRESULT(struct _EXCEPTION_POINTERS *pExceptionPointers);
    ULONG GetErrorMessage(__out_ecount(bufferLength) LPWSTR buffer,
                          ULONG bufferLength);
    int FilterException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    void HandleException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    void ThrowExceptionForJitResult(HRESULT result);
    void ThrowExceptionForHelper(const CORINFO_HELPER_DESC * throwHelper);

    // ICorStaticInfo stuff
    void getEEInfo(CORINFO_EE_INFO *pEEInfoOut);

    LPCWSTR getJitTimeLogFilename();

    //ICorDynamicInfo stuff
    DWORD getFieldThreadLocalStoreID (CORINFO_FIELD_HANDLE field, void **ppIndirection);

    // Stub dispatch stuff
    void getCallInfo(
                        CORINFO_RESOLVED_TOKEN * pResolvedToken,
                        CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                        CORINFO_METHOD_HANDLE   callerHandle,
                        CORINFO_CALLINFO_FLAGS  flags,
                        CORINFO_CALL_INFO      *pResult /*out */);
    BOOL canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                         CORINFO_CLASS_HANDLE hInstanceType);

protected:

    static void getEHinfoHelper(
        CORINFO_METHOD_HANDLE   ftnHnd,
        unsigned                EHnumber,
        CORINFO_EH_CLAUSE*      clause,
        COR_ILMETHOD_DECODER*   pILHeader);

    bool isVerifyOnly()
    {
        return m_fVerifyOnly;
    }

public:

    BOOL isRIDClassDomainID(CORINFO_CLASS_HANDLE cls);
    unsigned getClassDomainID (CORINFO_CLASS_HANDLE   cls, void **ppIndirection);
    CORINFO_VARARGS_HANDLE getVarArgsHandle(CORINFO_SIG_INFO *sig, void **ppIndirection);
    bool canGetVarArgsHandle(CORINFO_SIG_INFO *sig);
    void* getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method, void **ppIndirection);
    void* getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method, void **ppIndirection);
    void getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP *pLookup);
    CORINFO_JUST_MY_CODE_HANDLE getJustMyCodeHandle(CORINFO_METHOD_HANDLE method, CORINFO_JUST_MY_CODE_HANDLE **ppIndirection);

    void GetProfilingHandle(
                    BOOL                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    BOOL                      *pbIndirectedHandles
                    );

    InfoAccessType constructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok, void **ppValue);
    InfoAccessType emptyStringLiteral(void ** ppValue);
    void* getMethodSync(CORINFO_METHOD_HANDLE ftnHnd, void **ppIndirection);

    DWORD getThreadTLSIndex(void **ppIndirection);
    const void * getInlinedCallFrameVptr(void **ppIndirection);

    LONG * getAddrOfCaptureThreadGlobal(void **ppIndirection);
    void* getHelperFtn(CorInfoHelpFunc    ftnNum,                 /* IN  */
                       void **            ppIndirection);         /* OUT */

    void* getTailCallCopyArgsThunk(CORINFO_SIG_INFO       *pSig,
                                   CorInfoHelperTailCallSpecialHandling flags);

    bool convertPInvokeCalliToCall(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                   bool fMustConvert);

    void getFunctionEntryPoint(CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
                               CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
                               CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY);

    void getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE   ftn,
                                    CORINFO_CONST_LOOKUP *  pResult);

    // get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*). 
    // Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
    CorInfoHelpFunc getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle);

    CORINFO_MODULE_HANDLE embedModuleHandle(CORINFO_MODULE_HANDLE handle,
                                            void **ppIndirection);
    CORINFO_CLASS_HANDLE embedClassHandle(CORINFO_CLASS_HANDLE handle,
                                          void **ppIndirection);
    CORINFO_FIELD_HANDLE embedFieldHandle(CORINFO_FIELD_HANDLE handle,
                                          void **ppIndirection);
    CORINFO_METHOD_HANDLE embedMethodHandle(CORINFO_METHOD_HANDLE handle,
                                            void **ppIndirection);

	void embedGenericHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken,
		BOOL                     fEmbedParent,
		CORINFO_GENERICHANDLE_RESULT *pResult);

    CORINFO_LOOKUP_KIND getLocationOfThisType(CORINFO_METHOD_HANDLE context);


    void setOverride(ICorDynamicInfo *pOverride, CORINFO_METHOD_HANDLE currentMethod)
    {
        LIMITED_METHOD_CONTRACT;
        m_pOverride = pOverride;
        m_pMethodBeingCompiled = (MethodDesc *)currentMethod;     // method being compiled

        m_hMethodForSecurity_Key = NULL;
        m_pMethodForSecurity_Value = NULL;
    }

    // Returns whether we are generating code for NGen image.
    BOOL IsCompilingForNGen()
    {
        LIMITED_METHOD_CONTRACT;
        // NGen is the only place where we set the override
        return this != m_pOverride;
    }

    void addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo);
    CORINFO_METHOD_HANDLE GetDelegateCtor(
                        CORINFO_METHOD_HANDLE       methHnd,
                        CORINFO_CLASS_HANDLE        clsHnd,
                        CORINFO_METHOD_HANDLE       targetMethodHnd,
                        DelegateCtorArgs *          pCtorData);

    void MethodCompileComplete(
                CORINFO_METHOD_HANDLE methHnd);

    //
    // ICorJitInfo stuff - none of this should be called on this class
    //

    IEEMemoryManager* getMemoryManager();

    void allocMem (
            ULONG               hotCodeSize,    /* IN */
            ULONG               coldCodeSize,   /* IN */
            ULONG               roDataSize,     /* IN */
            ULONG               xcptnsCount,    /* IN */
            CorJitAllocMemFlag  flag,           /* IN */
            void **             hotCodeBlock,   /* OUT */
            void **             coldCodeBlock,  /* OUT */
            void **             roDataBlock     /* OUT */
            );

    void reserveUnwindInfo (
            BOOL                isFunclet,             /* IN */
            BOOL                isColdCode,            /* IN */
            ULONG               unwindSize             /* IN */
            );

    void allocUnwindInfo (
            BYTE *              pHotCode,              /* IN */
            BYTE *              pColdCode,             /* IN */
            ULONG               startOffset,           /* IN */
            ULONG               endOffset,             /* IN */
            ULONG               unwindSize,            /* IN */
            BYTE *              pUnwindBlock,          /* IN */
            CorJitFuncKind      funcKind               /* IN */
            );

    void * allocGCInfo (
            size_t                  size        /* IN */
            );

    void yieldExecution();

    void setEHcount (
            unsigned		     cEH    /* IN */
            );

    void setEHinfo (
            unsigned		     EHnumber,   /* IN  */
            const CORINFO_EH_CLAUSE *clause      /* IN */
            );

    BOOL logMsg(unsigned level, const char* fmt, va_list args);

    int doAssert(const char* szFile, int iLine, const char* szExpr);
    
    void reportFatalError(CorJitResult result);

    void logSQMLongJitEvent(unsigned mcycles, unsigned msec, unsigned ilSize, unsigned numBasicBlocks, bool minOpts, 
                            CORINFO_METHOD_HANDLE methodHnd);

    HRESULT allocMethodBlockCounts (
            UINT32                count,           // the count of <ILOffset, ExecutionCount> tuples
            BlockCounts **        pBlockCounts     // pointer to array of <ILOffset, ExecutionCount> tuples
            );

    HRESULT getMethodBlockCounts(
            CORINFO_METHOD_HANDLE ftnHnd,
            UINT32 *              pCount,          // pointer to the count of <ILOffset, ExecutionCount> tuples
            BlockCounts **        pBlockCounts,    // pointer to array of <ILOffset, ExecutionCount> tuples
            UINT32 *              pNumRuns
            );

    void recordCallSite(
            ULONG                 instrOffset,  /* IN */
            CORINFO_SIG_INFO *    callSig,      /* IN */
            CORINFO_METHOD_HANDLE methodHandle  /* IN */
            );

    void recordRelocation(
            void *                 location,   /* IN  */
            void *                 target,     /* IN  */
            WORD                   fRelocType, /* IN  */
            WORD                   slotNum = 0,  /* IN  */
            INT32                  addlDelta = 0 /* IN  */
            );

    WORD getRelocTypeHint(void * target);

    void getModuleNativeEntryPointRange(
            void ** pStart, /* OUT */
            void ** pEnd    /* OUT */
            );

    DWORD getExpectedTargetArchitecture();

    CEEInfo(MethodDesc * fd = NULL, bool fVerifyOnly = false, bool fAllowInlining = true) :
        m_pOverride(NULL),
        m_pMethodBeingCompiled(fd),
        m_fVerifyOnly(fVerifyOnly),
        m_pThread(GetThread()),
        m_hMethodForSecurity_Key(NULL),
        m_pMethodForSecurity_Value(NULL),
#if defined(FEATURE_GDBJIT)
        m_pCalledMethods(NULL),
#endif
        m_allowInlining(fAllowInlining)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual ~CEEInfo()
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Performs any work JIT-related work that should be performed at process shutdown.
    void JitProcessShutdownWork();

    void setJitFlags(const CORJIT_FLAGS& jitFlags);

    DWORD getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes);

    bool runWithErrorTrap(void (*function)(void*), void* param);

private:
    // Shrinking these buffers drastically reduces the amount of stack space
    // required for each instance of the interpreter, and thereby reduces SOs.
#ifdef FEATURE_INTERPRETER
#define CLS_STRING_SIZE 8  // force heap allocation
#define CLS_BUFFER_SIZE SBUFFER_PADDED_SIZE(8)
#else
#define CLS_STRING_SIZE MAX_CLASSNAME_LENGTH
#define CLS_BUFFER_SIZE MAX_CLASSNAME_LENGTH
#endif

#ifdef _DEBUG
    InlineSString<MAX_CLASSNAME_LENGTH> ssClsNameBuff;
    ScratchBuffer<MAX_CLASSNAME_LENGTH> ssClsNameBuffScratch;
#endif

public:

    //@GENERICS:
    // The method handle is used to instantiate method and class type parameters
    // It's also used to determine whether an extra dictionary parameter is required
    static 
    void 
    ConvToJitSig(
        PCCOR_SIGNATURE       pSig, 
        DWORD                 cbSig, 
        CORINFO_MODULE_HANDLE scopeHnd, 
        mdToken               token, 
        CORINFO_SIG_INFO *    sigRet, 
        MethodDesc *          context, 
        bool                  localSig, 
        TypeHandle            owner = TypeHandle());

    MethodDesc * GetMethodForSecurity(CORINFO_METHOD_HANDLE callerHandle);

    // Prepare the information about how to do a runtime lookup of the handle with shared
    // generic variables.
    void ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind entryKind,
                                                   CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                   CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken /* for ConstrainedMethodEntrySlot */,
                                                   MethodDesc * pTemplateMD /* for method-based slots */,
                                                   CORINFO_LOOKUP *pResultLookup);

#if defined(FEATURE_GDBJIT)
    CalledMethod * GetCalledMethods() { return m_pCalledMethods; }
#endif

protected:
    // NGen provides its own modifications to EE-JIT interface. From technical reason it cannot simply inherit 
    // from code:CEEInfo class (because it has dependencies on VM that NGen does not want).
    // Therefore the "normal" EE-JIT interface has code:m_pOverride hook that is set either to 
    //   * 'this' (code:CEEInfo) at runtime, or to 
    //   *  code:ZapInfo - the NGen specific implementation of the interface.
    ICorDynamicInfo * m_pOverride;
    
    MethodDesc*             m_pMethodBeingCompiled;             // Top-level method being compiled
    bool                    m_fVerifyOnly;
    Thread *                m_pThread;                          // Cached current thread for faster JIT-EE transitions
    CORJIT_FLAGS            m_jitFlags;

    CORINFO_METHOD_HANDLE getMethodBeingCompiled()
    {
        LIMITED_METHOD_CONTRACT;
        return (CORINFO_METHOD_HANDLE)m_pMethodBeingCompiled;
    }

    // Cache of last GetMethodForSecurity() lookup
    CORINFO_METHOD_HANDLE   m_hMethodForSecurity_Key;
    MethodDesc *            m_pMethodForSecurity_Value;

#if defined(FEATURE_GDBJIT)
    CalledMethod *          m_pCalledMethods;
#endif

    bool                    m_allowInlining;

    // Tracking of module activation dependencies. We have two flavors: 
    // - Fast one that gathers generic arguments from EE handles, but does not work inside generic context.
    // - Slow one that operates on typespec and methodspecs from metadata.
    void ScanForModuleDependencies(Module* pModule, SigPointer psig);
    void ScanMethodSpec(Module * pModule, PCCOR_SIGNATURE pMethodSpec, ULONG cbMethodSpec);
    // Returns true if it is ok to proceed with scan of parent chain
    BOOL ScanTypeSpec(Module * pModule, PCCOR_SIGNATURE pTypeSpec, ULONG cbTypeSpec);
    void ScanInstantiation(Module * pModule, Instantiation inst);

    // The main entrypoints for module activation tracking
    void ScanToken(Module * pModule, CORINFO_RESOLVED_TOKEN * pResolvedToken, TypeHandle th, MethodDesc * pMD = NULL);
    void ScanTokenForDynamicScope(CORINFO_RESOLVED_TOKEN * pResolvedToken, TypeHandle th, MethodDesc * pMD = NULL);
};


/*********************************************************************/

class  EEJitManager;
struct _hpCodeHdr;
typedef struct _hpCodeHdr CodeHeader;

#ifndef CROSSGEN_COMPILE
// CEEJitInfo is the concrete implementation of callbacks that the EE must provide for the JIT to do its
// work.   See code:ICorJitInfo#JitToEEInterface for more on this interface. 
class CEEJitInfo : public CEEInfo
{
public:
    // ICorJitInfo stuff

    void allocMem (
            ULONG               hotCodeSize,    /* IN */
            ULONG               coldCodeSize,   /* IN */
            ULONG               roDataSize,     /* IN */
            ULONG               xcptnsCount,    /* IN */
            CorJitAllocMemFlag  flag,           /* IN */
            void **             hotCodeBlock,   /* OUT */
            void **             coldCodeBlock,  /* OUT */
            void **             roDataBlock     /* OUT */
            );

    void reserveUnwindInfo(BOOL isFunclet, BOOL isColdCode, ULONG unwindSize);

    void allocUnwindInfo (
            BYTE * pHotCode,              /* IN */
            BYTE * pColdCode,             /* IN */
            ULONG  startOffset,           /* IN */
            ULONG  endOffset,             /* IN */
            ULONG  unwindSize,            /* IN */
            BYTE * pUnwindBlock,          /* IN */
            CorJitFuncKind funcKind       /* IN */
            );

    void * allocGCInfo (size_t  size);

    void setEHcount (unsigned cEH);

    void setEHinfo (
            unsigned      EHnumber,
            const CORINFO_EH_CLAUSE* clause);

    void getEHinfo(
            CORINFO_METHOD_HANDLE ftn,              /* IN  */
            unsigned      EHnumber,                 /* IN */
            CORINFO_EH_CLAUSE* clause               /* OUT */
            );


    HRESULT allocMethodBlockCounts (
        UINT32                        count,         // the count of <ILOffset, ExecutionCount> tuples
        ICorJitInfo::BlockCounts **   pBlockCounts   // pointer to array of <ILOffset, ExecutionCount> tuples
    );

    HRESULT getMethodBlockCounts(
        CORINFO_METHOD_HANDLE         ftnHnd,
        UINT32 *                      pCount,        // pointer to the count of <ILOffset, ExecutionCount> tuples
        BlockCounts **                pBlockCounts,  // pointer to array of <ILOffset, ExecutionCount> tuples
        UINT32 *                      pNumRuns
    );

    void recordCallSite(
            ULONG                     instrOffset,  /* IN */
            CORINFO_SIG_INFO *        callSig,      /* IN */
            CORINFO_METHOD_HANDLE     methodHandle  /* IN */
            );

    void recordRelocation(
            void                    *location,
            void                    *target,
            WORD                     fRelocType,
            WORD                     slot,
            INT32                    addlDelta);

    WORD getRelocTypeHint(void * target);

    void getModuleNativeEntryPointRange(
            void**                   pStart,
            void**                   pEnd);

    DWORD getExpectedTargetArchitecture();

    CodeHeader* GetCodeHeader()
    {
        LIMITED_METHOD_CONTRACT;
        return m_CodeHeader;
    }

    void SetCodeHeader(CodeHeader* pValue)
    {
        LIMITED_METHOD_CONTRACT;
        m_CodeHeader = pValue;
    }

    void ResetForJitRetry()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        m_CodeHeader = NULL;

        if (m_pOffsetMapping != NULL)
            delete [] ((BYTE*) m_pOffsetMapping);

        if (m_pNativeVarInfo != NULL)
            delete [] ((BYTE*) m_pNativeVarInfo);

        m_iOffsetMapping = 0;
        m_pOffsetMapping = NULL;
        m_iNativeVarInfo = 0;
        m_pNativeVarInfo = NULL;

#ifdef WIN64EXCEPTIONS
        m_moduleBase = NULL;
        m_totalUnwindSize = 0;
        m_usedUnwindSize = 0;
        m_theUnwindBlock = NULL;
        m_totalUnwindInfos = 0;
        m_usedUnwindInfos = 0;
#endif // WIN64EXCEPTIONS
    }

#ifdef _TARGET_AMD64_
    void SetAllowRel32(BOOL fAllowRel32)
    {
        LIMITED_METHOD_CONTRACT;
        m_fAllowRel32 = fAllowRel32;
    }
#endif

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    void SetJumpStubOverflow(BOOL fJumpStubOverflow)
    {
        LIMITED_METHOD_CONTRACT;
        m_fJumpStubOverflow = fJumpStubOverflow;
    }

    BOOL IsJumpStubOverflow()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fJumpStubOverflow;
    }

    BOOL JitAgain()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fJumpStubOverflow;
    }

    size_t GetReserveForJumpStubs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_reserveForJumpStubs;
    }

    void SetReserveForJumpStubs(size_t value)
    {
        LIMITED_METHOD_CONTRACT;
        m_reserveForJumpStubs = value;
    }
#else
    BOOL JitAgain()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    size_t GetReserveForJumpStubs()
    {
        LIMITED_METHOD_CONTRACT;
        return 0;
    }
#endif

    CEEJitInfo(MethodDesc* fd,  COR_ILMETHOD_DECODER* header, 
               EEJitManager* jm, bool fVerifyOnly, bool allowInlining = true)
        : CEEInfo(fd, fVerifyOnly, allowInlining),
          m_jitManager(jm),
          m_CodeHeader(NULL),
          m_ILHeader(header),
#ifdef WIN64EXCEPTIONS
          m_moduleBase(NULL),
          m_totalUnwindSize(0),
          m_usedUnwindSize(0),
          m_theUnwindBlock(NULL),
          m_totalUnwindInfos(0),
          m_usedUnwindInfos(0),
#endif
#ifdef _TARGET_AMD64_
          m_fAllowRel32(FALSE),
#endif
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
          m_fJumpStubOverflow(FALSE),
          m_reserveForJumpStubs(0),
#endif
          m_GCinfo_len(0),
          m_EHinfo_len(0),
          m_iOffsetMapping(0),
          m_pOffsetMapping(NULL),
          m_iNativeVarInfo(0),
          m_pNativeVarInfo(NULL),
          m_gphCache()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        m_pOverride = this;
    }

    ~CEEJitInfo()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        if (m_pOffsetMapping != NULL)
            delete [] ((BYTE*) m_pOffsetMapping);

        if (m_pNativeVarInfo != NULL)
            delete [] ((BYTE*) m_pNativeVarInfo);
    }

    // ICorDebugInfo stuff.
    void setBoundaries(CORINFO_METHOD_HANDLE ftn,
                       ULONG32 cMap, ICorDebugInfo::OffsetMapping *pMap);
    void setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars,
                 ICorDebugInfo::NativeVarInfo *vars);
    void CompressDebugInfo();

    void* getHelperFtn(CorInfoHelpFunc    ftnNum,                 /* IN  */
                       void **            ppIndirection);         /* OUT */
    static PCODE getHelperFtnStatic(CorInfoHelpFunc ftnNum);

    // Override active dependency to talk to loader
    void addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo);

    // Override of CEEInfo::GetProfilingHandle.  The first time this is called for a
    // method desc, it calls through to CEEInfo::GetProfilingHandle and caches the
    // result in CEEJitInfo::GetProfilingHandleCache.  Thereafter, this wrapper regurgitates the cached values
    // rather than calling into CEEInfo::GetProfilingHandle each time.  This avoids
    // making duplicate calls into the profiler's FunctionIDMapper callback.
    void GetProfilingHandle(
                    BOOL                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    BOOL                      *pbIndirectedHandles
                    );

    InfoAccessType constructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok, void **ppValue);
    InfoAccessType emptyStringLiteral(void ** ppValue);
    void* getFieldAddress(CORINFO_FIELD_HANDLE field, void **ppIndirection);
    CORINFO_CLASS_HANDLE getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative);
    void* getMethodSync(CORINFO_METHOD_HANDLE ftnHnd, void **ppIndirection);

    void BackoutJitData(EEJitManager * jitMgr);

protected :
    EEJitManager*           m_jitManager;   // responsible for allocating memory
    CodeHeader*             m_CodeHeader;   // descriptor for JITTED code
    COR_ILMETHOD_DECODER *  m_ILHeader;     // the code header as exist in the file
#ifdef WIN64EXCEPTIONS
    TADDR                   m_moduleBase;       // Base for unwind Infos
    ULONG                   m_totalUnwindSize;  // Total reserved unwind space
    ULONG                   m_usedUnwindSize;   // used space in m_theUnwindBlock
    BYTE *                  m_theUnwindBlock;   // start of the unwind memory block
    ULONG                   m_totalUnwindInfos; // Number of RUNTIME_FUNCTION needed
    ULONG                   m_usedUnwindInfos;
#endif

#ifdef _TARGET_AMD64_
    BOOL                    m_fAllowRel32;      // Use 32-bit PC relative address modes
#endif
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    BOOL                    m_fJumpStubOverflow;   // Overflow while trying to alocate jump stub slot within PC relative branch region
                                                   // The code will need to be regenerated (with m_fRel32Allowed == FALSE for AMD64).
    size_t                  m_reserveForJumpStubs; // Space to reserve for jump stubs when allocating code
#endif

#if defined(_DEBUG)
    ULONG                   m_codeSize;     // Code size requested via allocMem
#endif

    size_t                  m_GCinfo_len;   // Cached copy of GCinfo_len so we can backout in BackoutJitData()
    size_t                  m_EHinfo_len;   // Cached copy of EHinfo_len so we can backout in BackoutJitData()

    ULONG32                 m_iOffsetMapping;
    ICorDebugInfo::OffsetMapping * m_pOffsetMapping;

    ULONG32                 m_iNativeVarInfo;
    ICorDebugInfo::NativeVarInfo * m_pNativeVarInfo;

    // The first time a call is made to CEEJitInfo::GetProfilingHandle() from this thread
    // for this method, these values are filled in.   Thereafter, these values are used
    // in lieu of calling into the base CEEInfo::GetProfilingHandle() again.  This protects the
    // profiler from duplicate calls to its FunctionIDMapper() callback.
    struct GetProfilingHandleCache
    {
        GetProfilingHandleCache() :
            m_bGphIsCacheValid(false),
            m_bGphHookFunction(false),
            m_pvGphProfilerHandle(NULL)
        {
            LIMITED_METHOD_CONTRACT;
        }
          
        bool                    m_bGphIsCacheValid : 1;        // Tells us whether below values are valid
        bool                    m_bGphHookFunction : 1;
        void*                   m_pvGphProfilerHandle;
    } m_gphCache;

};
#endif // CROSSGEN_COMPILE

/*********************************************************************/
/*********************************************************************/

typedef struct {
    void * pfnHelper;
#ifdef _DEBUG
    const char* name;
#endif
} VMHELPDEF;

#if defined(DACCESS_COMPILE)

GARY_DECL(VMHELPDEF, hlpFuncTable, CORINFO_HELP_COUNT);

#else

extern "C" const VMHELPDEF hlpFuncTable[CORINFO_HELP_COUNT];

#endif

#if defined(_DEBUG) && (defined(_TARGET_AMD64_) || defined(_TARGET_X86_)) && !defined(FEATURE_PAL)
typedef struct {
    void*       pfnRealHelper;
    const char* helperName;
    LONG        count;
    LONG        helperSize;
} VMHELPCOUNTDEF;

extern "C" VMHELPCOUNTDEF hlpFuncCountTable[CORINFO_HELP_COUNT+1];

void InitJitHelperLogging();
void WriteJitHelperCountToSTRESSLOG();
#else
inline void InitJitHelperLogging() { }
inline void WriteJitHelperCountToSTRESSLOG() { }
#endif

// enum for dynamically assigned helper calls
enum DynamicCorInfoHelpFunc {
#define JITHELPER(code, pfnHelper, sig)
#define DYNAMICJITHELPER(code, pfnHelper, sig) DYNAMIC_##code,
#include "jithelpers.h"
    DYNAMIC_CORINFO_HELP_COUNT
};

#ifdef _MSC_VER
// GCC complains about duplicate "extern". And it is not needed for the GCC build
extern "C"
#endif
GARY_DECL(VMHELPDEF, hlpDynamicFuncTable, DYNAMIC_CORINFO_HELP_COUNT);

#define SetJitHelperFunction(ftnNum, pFunc) _SetJitHelperFunction(DYNAMIC_##ftnNum, (void*)(pFunc))
void    _SetJitHelperFunction(DynamicCorInfoHelpFunc ftnNum, void * pFunc);
#ifdef ENABLE_FAST_GCPOLL_HELPER
//These should only be called from ThreadStore::TrapReturningThreads!

//Called when the VM wants to suspend one or more threads.
void    EnableJitGCPoll();
//Called when there are no threads to suspend.
void    DisableJitGCPoll();
#endif

// Helper for RtlVirtualUnwind-based tail calls
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

// The Stub-linker generated assembly routine to copy arguments from the va_list
// into the CONTEXT and the stack.
//
typedef size_t (*pfnCopyArgs)(va_list, _CONTEXT *, DWORD_PTR *, size_t);

// Forward declaration from Frames.h
class TailCallFrame;

// The shared stub return location
EXTERN_C void JIT_TailCallHelperStub_ReturnAddress();

#endif // _TARGET_AMD64_ || _TARGET_ARM_

void *GenFastGetSharedStaticBase(bool bCheckCCtor);

#ifdef HAVE_GCCOVER
void SetupGcCoverage(NativeCodeVersion nativeCodeVersion, BYTE* nativeCode);
void SetupGcCoverageForNativeImage(Module* module);
BOOL OnGcCoverageInterrupt(PT_CONTEXT regs);
void DoGcStress (PT_CONTEXT regs, NativeCodeVersion nativeCodeVersion);
#endif //HAVE_GCCOVER

EXTERN_C FCDECL2(LPVOID, ArrayStoreCheck, Object** pElement, PtrArray** pArray);

OBJECTHANDLE ConstructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok);

FCDECL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* data);
FCDECL0(VOID, JIT_PollGC);
#ifdef ENABLE_FAST_GCPOLL_HELPER
EXTERN_C FCDECL0(VOID, JIT_PollGC_Nop);
#endif

BOOL ObjIsInstanceOf(Object *pObject, TypeHandle toTypeHnd, BOOL throwCastException = FALSE);
EXTERN_C TypeHandle::CastResult STDCALL ObjIsInstanceOfNoGC(Object *pObject, TypeHandle toTypeHnd);

#ifdef _WIN64
class InlinedCallFrame;
Thread * __stdcall JIT_InitPInvokeFrame(InlinedCallFrame *pFrame, PTR_VOID StubSecretArg);
#endif

#ifdef _DEBUG
extern LONG g_JitCount;
#endif

struct VirtualFunctionPointerArgs
{
    CORINFO_CLASS_HANDLE classHnd;
    CORINFO_METHOD_HANDLE methodHnd;
};

FCDECL2(CORINFO_MethodPtr, JIT_VirtualFunctionPointer_Dynamic, Object * objectUNSAFE, VirtualFunctionPointerArgs * pArgs);

typedef HCCALL2_PTR(TADDR, FnStaticBaseHelper, TADDR arg0, TADDR arg1);

struct StaticFieldAddressArgs
{
    FnStaticBaseHelper staticBaseHelper;
    TADDR arg0;
    TADDR arg1;
    SIZE_T offset;
};

FCDECL1(TADDR, JIT_StaticFieldAddress_Dynamic, StaticFieldAddressArgs * pArgs);
FCDECL1(TADDR, JIT_StaticFieldAddressUnbox_Dynamic, StaticFieldAddressArgs * pArgs);

struct GenericHandleArgs
{
    LPVOID signature;
    CORINFO_MODULE_HANDLE module;
    DWORD dictionaryIndexAndSlot;
};

FCDECL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleMethodWithSlotAndModule, CORINFO_METHOD_HANDLE  methodHnd, GenericHandleArgs * pArgs);
FCDECL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleClassWithSlotAndModule, CORINFO_CLASS_HANDLE classHnd, GenericHandleArgs * pArgs);

CORINFO_GENERIC_HANDLE JIT_GenericHandleWorker(MethodDesc   *pMD,
                                               MethodTable  *pMT,
                                               LPVOID        signature,
                                               DWORD         dictionaryIndexAndSlot = -1,
                                               Module *      pModule = NULL);

void ClearJitGenericHandleCache(AppDomain *pDomain);

CORJIT_FLAGS GetDebuggerCompileFlags(Module* pModule, CORJIT_FLAGS flags);

bool __stdcall TrackAllocationsEnabled();

#endif // JITINTERFACE_H

