// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: COMDelegate.h
//
// This module contains the native methods for the Delegate class.
//


#ifndef _COMDELEGATE_H_
#define _COMDELEGATE_H_

class Stub;
class ShuffleThunkCache;

#include "cgensys.h"
#include "dllimportcallback.h"
#include "stubcache.h"

#ifndef FEATURE_MULTICASTSTUB_AS_IL
typedef ArgBasedStubCache MulticastStubCache;
#endif

VOID GenerateShuffleArray(MethodDesc* pInvoke, MethodDesc *pTargetMeth, struct ShuffleEntry * pShuffleEntryArray, size_t nEntries);

enum class ShuffleComputationType
{
    InstantiatingStub,
    DelegateShuffleThunk
};
BOOL GenerateShuffleArrayPortable(MethodDesc* pMethodSrc, MethodDesc *pMethodDst, SArray<ShuffleEntry> * pShuffleEntryArray, ShuffleComputationType shuffleType);

// This class represents the native methods for the Delegate class
class COMDelegate
{
private:
    // friend VOID CPUSTUBLINKER::EmitMulticastInvoke(...);
    // friend VOID CPUSTUBLINKER::EmitShuffleThunk(...);
    friend class CPUSTUBLINKER;
    friend BOOL MulticastFrame::TraceFrame(Thread *thread, BOOL fromPatch,
                                TraceDestination *trace, REGDISPLAY *regs);

#ifndef FEATURE_MULTICASTSTUB_AS_IL
    static MulticastStubCache* m_pMulticastStubCache;
#endif

    static CrstStatic   s_DelegateToFPtrHashCrst;   // Lock for the following hash.
    static PtrHashMap*  s_pDelegateToFPtrHash;      // Hash table containing the Delegate->FPtr pairs
                                                    // passed out to unmanaged code.
public:
    static ShuffleThunkCache *m_pShuffleThunkCache;

    // One time init.
    static void Init();

    static FCDECL3(void, DelegateConstruct, Object* refThis, Object* target, PCODE method);

    // Get the invoke method for the delegate. Used to transition delegates to multicast delegates.
    static FCDECL1(PCODE, GetMulticastInvoke, Object* refThis);
    static FCDECL1(MethodDesc*, GetInvokeMethod, Object* refThis);
    static PCODE GetWrapperInvoke(MethodDesc* pMD);
    // determines where the delegate needs to be wrapped for non-security reason
    static BOOL NeedsWrapperDelegate(MethodDesc* pTargetMD);
    // on entry delegate points to the delegate to wrap
    static DELEGATEREF CreateWrapperDelegate(DELEGATEREF delegate, MethodDesc* pTargetMD);

    // Marshals a delegate to a unmanaged callback.
    static LPVOID ConvertToCallback(OBJECTREF pDelegate);

    // Marshals an unmanaged callback to Delegate
    static OBJECTREF ConvertToDelegate(LPVOID pCallback, MethodTable* pMT);

#ifdef FEATURE_COMINTEROP
    static ComPlusCallInfo * PopulateComPlusCallInfo(MethodTable * pDelMT);
#endif // FEATURE_COMINTEROP

    static PCODE GetStubForILStub(EEImplMethodDesc* pDelegateMD, MethodDesc** ppStubMD, DWORD dwStubFlags);
    static MethodDesc* GetILStubMethodDesc(EEImplMethodDesc* pDelegateMD, DWORD dwStubFlags);

    static void ValidateDelegatePInvoke(MethodDesc* pMD);

    static void RemoveEntryFromFPtrHash(UPTR key);

    // Decides if pcls derives from Delegate.
    static BOOL IsDelegate(MethodTable *pMT);

    // Decides if this is a wrapper delegate
    static BOOL IsWrapperDelegate(DELEGATEREF dRef);

    // Get the cpu stub for a delegate invoke.
    static PCODE GetInvokeMethodStub(EEImplMethodDesc* pMD);

    // get the one single delegate invoke stub
    static PCODE TheDelegateInvokeStub();

    static MethodDesc * __fastcall GetMethodDesc(OBJECTREF obj);
    static OBJECTREF GetTargetObject(OBJECTREF obj);

    static BOOL IsTrueMulticastDelegate(OBJECTREF delegate);

    // Throw if the method violates any usage restrictions
    // for UnmanagedCallersOnlyAttribute.
    static void ThrowIfInvalidUnmanagedCallersOnlyUsage(MethodDesc* pMD);

private:
    static Stub* SetupShuffleThunk(MethodTable * pDelMT, MethodDesc *pTargetMeth);

public:
    static MethodDesc* FindDelegateInvokeMethod(MethodTable *pMT);
    static BOOL IsDelegateInvokeMethod(MethodDesc *pMD);

    static bool IsMethodDescCompatible(TypeHandle   thFirstArg,
                                       TypeHandle   thExactMethodType,
                                       MethodDesc  *pTargetMethod,
                                       TypeHandle   thDelegate,
                                       MethodDesc  *pInvokeMethod,
                                       int          flags,
                                       bool        *pfIsOpenDelegate);
    static MethodDesc* GetDelegateCtor(TypeHandle delegateType, MethodDesc *pTargetMethod, DelegateCtorArgs *pCtorData);

    static void BindToMethod(DELEGATEREF   *pRefThis,
                             OBJECTREF     *pRefFirstArg,
                             MethodDesc    *pTargetMethod,
                             MethodTable   *pExactMethodType,
                             BOOL           fIsOpenDelegate);
};

extern "C" PCODE QCALLTYPE Delegate_AdjustTarget(QCall::ObjectHandleOnStack target, PCODE method);

extern "C" void QCALLTYPE Delegate_InitializeVirtualCallStub(QCall::ObjectHandleOnStack d, PCODE method);

// These flags effect the way BindToMethodInfo and BindToMethodName are allowed to bind a delegate to a target method. Their
// values must be kept in sync with the definition in bcl\system\delegate.cs.
enum DelegateBindingFlags
{
    DBF_StaticMethodOnly    =   0x00000001, // Can only bind to static target methods
    DBF_InstanceMethodOnly  =   0x00000002, // Can only bind to instance (including virtual) methods
    DBF_OpenDelegateOnly    =   0x00000004, // Only allow the creation of delegates open over the 1st argument
    DBF_ClosedDelegateOnly  =   0x00000008, // Only allow the creation of delegates closed over the 1st argument
    DBF_NeverCloseOverNull  =   0x00000010, // A null target will never been considered as a possible null 1st argument
    DBF_CaselessMatching    =   0x00000020, // Use case insensitive lookup for methods matched by name
    DBF_RelaxedSignature    =   0x00000040, // Allow relaxed signature matching (co/contra variance)
};

extern "C" BOOL QCALLTYPE Delegate_BindToMethodName(QCall::ObjectHandleOnStack d, QCall::ObjectHandleOnStack target,
    QCall::TypeHandle pMethodType, LPCUTF8 pszMethodName, DelegateBindingFlags flags);

extern "C" BOOL QCALLTYPE Delegate_BindToMethodInfo(QCall::ObjectHandleOnStack d, QCall::ObjectHandleOnStack target,
    MethodDesc * method, QCall::TypeHandle pMethodType, DelegateBindingFlags flags);

extern "C" void QCALLTYPE Delegate_InternalAlloc(QCall::TypeHandle pType, QCall::ObjectHandleOnStack d);

extern "C" void QCALLTYPE Delegate_InternalAllocLike(QCall::ObjectHandleOnStack d);

extern "C" void QCALLTYPE Delegate_FindMethodHandle(QCall::ObjectHandleOnStack d, QCall::ObjectHandleOnStack retMethodInfo);

extern "C" BOOL QCALLTYPE Delegate_InternalEqualMethodHandles(QCall::ObjectHandleOnStack left, QCall::ObjectHandleOnStack right);


void DistributeEvent(OBJECTREF *pDelegate,
                     OBJECTREF *pDomain);

void DistributeUnhandledExceptionReliably(OBJECTREF *pDelegate,
                                          OBJECTREF *pDomain,
                                          OBJECTREF *pThrowable,
                                          BOOL       isTerminating);

// Want no unused bits in ShuffleEntry since unused bits can make
// equivalent ShuffleEntry arrays look unequivalent and deoptimize our
// hashing.
#include <pshpack1.h>

// To handle a call to a static delegate, we create an array of ShuffleEntry
// structures. Each entry instructs the shuffler to move a chunk of bytes.
// The size of the chunk is StackElemSize (typically a DWORD): long arguments
// have to be expressed as multiple ShuffleEntry's.
//
// The ShuffleEntry array serves two purposes:
//
//  1. A platform-independent blueprint for creating the platform-specific
//     shuffle thunk.
//  2. A hash key for finding the shared shuffle thunk for a particular
//     signature.
struct ShuffleEntry
{
    // Offset masks and special value
    enum {
        REGMASK      = 0x8000, // Register offset bit
        FPREGMASK    = 0x4000, // Floating point register bit
        FPSINGLEMASK = 0x2000, // Single precising floating point register
        OFSMASK      = 0x7fff, // Mask to get stack offset
        OFSREGMASK   = 0x1fff, // Mask to get register index
        SENTINEL     = 0xffff, // Indicates end of shuffle array
        HELPERREG    = 0xcfff, // Use a helper register as source or destination (used to handle cycles in the shuffling)
    };

    UINT16    srcofs;

    union {
        UINT16    dstofs;           //if srcofs != SENTINEL
        UINT16    stacksizedelta;   //if srcofs == SENTINEL, difference in stack size between virtual and static sigs
    };
};


#include <poppack.h>

class ShuffleThunkCache : public StubCacheBase
{
public:
    ShuffleThunkCache(LoaderHeap* heap) : StubCacheBase(heap)
    {
    }
private:
    //---------------------------------------------------------
    // Compile a static delegate shufflethunk. Always returns
    // STANDALONE since we don't interpret these things.
    //---------------------------------------------------------
    virtual DWORD CompileStub(const BYTE *pRawStub,
                             StubLinker *pstublinker)
    {
        STANDARD_VM_CONTRACT;

        ((CPUSTUBLINKER*)pstublinker)->EmitShuffleThunk((ShuffleEntry*)pRawStub);
        return NEWSTUB_FL_THUNK;
    }

    //---------------------------------------------------------
    // Tells the StubCacheBase the length of a ShuffleEntryArray.
    //---------------------------------------------------------
    virtual UINT Length(const BYTE *pRawStub)
    {
        LIMITED_METHOD_CONTRACT;
        ShuffleEntry *pse = (ShuffleEntry*)pRawStub;
        while (pse->srcofs != ShuffleEntry::SENTINEL)
        {
            pse++;
        }
        return sizeof(ShuffleEntry) * (UINT)(1 + (pse - (ShuffleEntry*)pRawStub));
    }
};

#endif  // _COMDELEGATE_H_
